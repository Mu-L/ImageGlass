/*
ImageGlass - A lightweight, versatile image viewer
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.Photoing;

public partial class PhotoManager
{
    private CancellationTokenSource? _cacheCts;
    private readonly Lock _cacheLock = new();

    /// <summary>
    /// Tracks indexes of photos that were loaded by the caching logic.
    /// The current photo index is NOT included here.
    /// </summary>
    private readonly HashSet<int> _cachedIndexes = [];


    // Debug properties
    #region Debug properties

    /// <summary>
    /// Gets the number of photos currently held in cache.
    /// </summary>
    public int CachedCount
    {
        get
        {
            lock (_cacheLock) return _cachedIndexes.Count;
        }
    }

    /// <summary>
    /// Gets the estimated total cached memory in MB (includes the current photo).
    /// </summary>
    public double CachedMemoryMb
    {
        get
        {
            var bytes = EstimateCachedMemory(CurrentIndex);
            return Math.Round(bytes / (1024.0 * 1024.0), 1);
        }
    }

    /// <summary>
    /// Gets a snapshot of the currently cached indexes for debug display.
    /// </summary>
    public int[] CachedIndexSnapshot
    {
        get
        {
            lock (_cacheLock) return [.. _cachedIndexes];
        }
    }

    #endregion // Debug properties


    /// <summary>
    /// Requests background caching around the given center index.
    /// Cancels any previously running cache pass before starting a new one.
    /// </summary>
    public void RequestCacheAround(int centerIndex)
    {
        // skip caching during quick browsing (user is holding arrow keys)
        if (Core.API.IsQuickBrowsing) return;

        CancellationToken token;
        lock (_cacheLock)
        {
            _cacheCts?.Cancel();
            _cacheCts?.Dispose();
            _cacheCts = new CancellationTokenSource();
            token = _cacheCts.Token;
        }

        if (Core.Config.CacheMaxMemoryInMb == 0
            || Core.Config.CacheMaxFileSizeInMb == 0
            || Core.Config.CacheMaxDimension == 0) return;

        // run on a dedicated thread to avoid thread pool starvation
        _ = Task.Factory.StartNew(
            () => RunCacheAroundAsync(centerIndex, token),
            token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }


    /// <summary>
    /// Cancels any in-progress caching operation.
    /// </summary>
    public void CancelCaching()
    {
        lock (_cacheLock)
        {
            _cacheCts?.Cancel();
            _cacheCts?.Dispose();
            _cacheCts = null;
        }
    }


    /// <summary>
    /// Unloads all cached photos (not the current photo) and resets tracking.
    /// </summary>
    public void ClearCache()
    {
        CancelCaching();

        int[] snapshot;
        lock (_cacheLock)
        {
            snapshot = [.. _cachedIndexes];
            _cachedIndexes.Clear();
        }

        foreach (var idx in snapshot)
        {
            Get(idx)?.Unload();
        }
    }


    /// <summary>
    /// Unloads the cached photo at the specified index if it was loaded by caching.
    /// </summary>
    public void InvalidateCacheAt(int index)
    {
        lock (_cacheLock)
        {
            if (!_cachedIndexes.Remove(index)) return;
        }

        Get(index)?.Unload();
    }


    /// <summary>
    /// Unloads the cached photo by file path if it was loaded by caching.
    /// </summary>
    public void InvalidateCacheAt(string filePath)
    {
        var index = IndexOf(filePath);
        if (index >= 0) InvalidateCacheAt(index);
    }


    /// <summary>
    /// Core caching loop. Loads photos in center-right-left expanding pattern
    /// until the memory budget is exhausted or all reachable photos are cached.
    /// </summary>
    private async Task RunCacheAroundAsync(int centerIndex, CancellationToken token)
    {
        try
        {
            var maxMemoryBytes = (long)Core.Config.CacheMaxMemoryInMb * 1024L * 1024L;
            var maxFileSizeBytes = (long)(Core.Config.CacheMaxFileSizeInMb * 1024.0 * 1024.0);
            var maxDimension = Core.Config.CacheMaxDimension;
            var totalCount = (int)Count;

            if (totalCount == 0 || centerIndex < 0) return;

            // determine how far we can reach (at most half the list on each side)
            var maxRange = Math.Min(totalCount / 2 + 1, totalCount);

            // generate ordered indexes in spiral pattern:
            // right-1, left-1, right-2, left-2, ...
            var indexes = GenerateSpiralIndexes(centerIndex, maxRange, totalCount);

            // estimate current memory usage from already-cached photos
            var usedMemory = EstimateCachedMemory(centerIndex);

            // collect the set of indexes that should remain cached after this pass
            var newCachedSet = new HashSet<int>();

            foreach (var idx in indexes)
            {
                if (token.IsCancellationRequested) return;

                // re-check quick browsing each iteration
                if (Core.API.IsQuickBrowsing) return;

                var photo = Get(idx);
                if (photo is null) continue;

                // already loaded (either by a previous cache pass or by the viewer)
                if (photo.State == PhotoState.Loaded)
                {
                    var photoMem = EstimatePhotoMemory(photo);
                    if (usedMemory + photoMem > maxMemoryBytes) break;

                    usedMemory += photoMem;
                    newCachedSet.Add(idx);
                    continue;
                }

                // skip the photo currently being loaded by the viewer
                // to avoid cancelling its ongoing load via CancelLoading()
                if (idx == CurrentIndex)
                {
                    newCachedSet.Add(idx);
                    continue;
                }

                // check file size constraint
                if (maxFileSizeBytes > 0 && !SatisfiesFileSizeLimit(photo.FilePath, maxFileSizeBytes))
                {
                    continue;
                }

                // check dimension constraint (requires metadata)
                if (maxDimension > 0)
                {
                    await photo.LoadMetadataAsync(useCache: true);
                    if (token.IsCancellationRequested) return;

                    if (photo.Metadata.Width > maxDimension || photo.Metadata.Height > maxDimension)
                    {
                        continue;
                    }
                }

                // estimate memory before loading
                var estimatedMem = EstimatePhotoMemoryFromMetadata(photo);
                if (usedMemory + estimatedMem > maxMemoryBytes) break;

                // load the photo
                await photo.LoadAsync(useCache: true, skipLoadingEvent: true);
                if (token.IsCancellationRequested) return;

                usedMemory += estimatedMem;
                newCachedSet.Add(idx);
            }

            if (token.IsCancellationRequested) return;

            // unload photos that were cached in a previous pass but are no longer needed
            int[] toUnload;
            lock (_cacheLock)
            {
                toUnload = [.. _cachedIndexes];
                _cachedIndexes.Clear();
                foreach (var idx in newCachedSet)
                {
                    _cachedIndexes.Add(idx);
                }
            }

            foreach (var idx in toUnload)
            {
                if (token.IsCancellationRequested) return;
                if (newCachedSet.Contains(idx)) continue;
                if (idx == centerIndex) continue;

                Get(idx)?.Unload();
            }
        }
        catch (OperationCanceledException) { /* expected on navigation */ }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌❌❌ RunCacheAroundAsync: {ex.Message}");
        }
    }


    /// <summary>
    /// Generates an ordered list of indexes in the spiral pattern:
    /// right-1, left-1, right-2, left-2, ...
    /// This preserves insertion order, unlike <see cref="BHelper.GenerateWrappedIndexes"/>
    /// which uses an unordered HashSet.
    /// </summary>
    private static List<int> GenerateSpiralIndexes(int centerIndex, int maxRange, int totalCount)
    {
        var result = new List<int>(maxRange * 2);
        var seen = new HashSet<int>();

        for (var i = 1; i <= maxRange; i++)
        {
            var rightIndex = BHelper.ComputeIndexInRange(centerIndex + i, (uint)totalCount, true);
            var leftIndex = BHelper.ComputeIndexInRange(centerIndex - i, (uint)totalCount, true);

            // right first, then left — order is preserved by List
            if (rightIndex != centerIndex && seen.Add(rightIndex))
            {
                result.Add(rightIndex);
            }
            if (leftIndex != centerIndex && seen.Add(leftIndex))
            {
                result.Add(leftIndex);
            }
        }

        return result;
    }


    /// <summary>
    /// Checks if the file size is within the allowed caching limit.
    /// </summary>
    private static bool SatisfiesFileSizeLimit(string filePath, long maxFileSizeBytes)
    {
        try
        {
            var fi = new FileInfo(filePath);
            return fi.Length <= maxFileSizeBytes;
        }
        catch
        {
            return false;
        }
    }


    /// <summary>
    /// Estimates the memory footprint of a loaded photo (4 bytes per pixel for BGRA32).
    /// Returns 0 if the photo is not loaded.
    /// </summary>
    private static long EstimatePhotoMemory(Photo photo)
    {
        if (photo.State != PhotoState.Loaded) return 0;

        return (long)photo.Width * photo.Height * 4;
    }


    /// <summary>
    /// Estimates memory from metadata before the photo is fully loaded.
    /// </summary>
    private static long EstimatePhotoMemoryFromMetadata(Photo photo)
    {
        var w = photo.Metadata.Width;
        var h = photo.Metadata.Height;
        if (w == 0 || h == 0) return 8L * 1024 * 1024; // fallback estimate: 8 MB

        return (long)w * h * 4;
    }


    /// <summary>
    /// Sums up the estimated memory of the current photo and all cached photos.
    /// </summary>
    private long EstimateCachedMemory(int centerIndex)
    {
        long total = 0;

        // include the current photo
        var current = Get(centerIndex);
        if (current is not null)
        {
            total += EstimatePhotoMemory(current);
        }

        lock (_cacheLock)
        {
            foreach (var idx in _cachedIndexes)
            {
                var photo = Get(idx);
                if (photo is not null)
                {
                    total += EstimatePhotoMemory(photo);
                }
            }
        }

        return total;
    }
}
