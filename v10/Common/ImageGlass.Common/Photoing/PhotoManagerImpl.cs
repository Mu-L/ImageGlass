/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
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
using ImageMagick;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Class for managing a collection of photos.
/// </summary>
public abstract partial class PhotoManagerImpl<T> : DisposableImpl where T : PhotoImpl
{
    // photo list
    protected readonly List<T> _photos = new();

    // store file paths and the index for quick access photo in the list
    protected readonly ConcurrentDictionary<string, int> _pathDict = new();

    // concurrent loads control for photo loading
    protected readonly SemaphoreSlim _lockGetAndCache = new(1, 1);

    // photo preloading
    protected readonly SemaphoreSlim _lockPreloadPhotos = new(4, 4);
    protected CancellationTokenSource? _tokenStartCaching;

    // thumbnail
    protected readonly SemaphoreSlim _lockGetThumbnails = new(4, 4);
    protected readonly SemaphoreSlim _lockManageThumbnailCache = new(1, 1);
    protected CancellationTokenSource? _tokenThumbnail;
    protected readonly long _maxThumbnailCacheSizeInMb = 100; // 100MB


    protected CancellationTokenSource? _cancelWorker;
    protected List<int> _queueList = [];
    protected HashSet<int> _freeList = [];


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets the number of photos currently in the collection.
    /// </summary>
    public uint Count => (uint)_photos.Count;

    /// <summary>
    /// Gets a list of file paths associated with the photos.
    /// </summary>
    public IEnumerable<string> FilePaths => _photos.Select(i => i.FilePath);

    /// <summary>
    /// Gets, sets the distinct directories list.
    /// </summary>
    public List<string> DistinctDirs { get; set; } = [];

    /// <summary>
    /// Gets, sets options for reading photo.
    /// </summary>
    public PhotoReadOptions ReadOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the range of items to preload in advance (in the Center-Right-Left order).
    /// </summary>
    public uint PreloadRange { get; set; } = 1;

    /// <summary>
    /// Gets, sets the maximum image dimension to cache.
    /// If this value is <c>≤ 0</c>, the option will be ignored.
    /// </summary>
    public int MaxImageDimensionToCache { get; set; } = 8_000;

    /// <summary>
    /// Gets, sets the maximum image file size (in MB) to cache.
    /// If this value is <c>≤ 0</c>, the option will be ignored.
    /// </summary>
    public float MaxFileSizeInMbToCache { get; set; } = 100f;

    #endregion // Public Properties



    /// <summary>
    /// Initializes a new instance of <see cref="PhotoManagerImpl{T}"/>.
    /// </summary>
    public PhotoManagerImpl(IEnumerable<string>? list = null)
    {
        if (list != null) Add(list);

        // run background worker
        _cancelWorker = new();
        _ = Task.Run(async () => await RunBackgroundWorker(_cancelWorker.Token), _cancelWorker.Token);
    }


    /// <summary>
    /// Preload photos in a background thread.
    /// </summary>
    private async Task RunBackgroundWorker(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_queueList.Count > 0)
            {
                // pop out the first item
                var index = _queueList[0];
                var photo = _photos[index];
                _queueList.RemoveAt(0);


                // if photo is cached
                if (photo.IsDone)
                {
                    Log.Info(
                        $"\t-> Skipped caching photo index={index}",
                        nameof(RunBackgroundWorker), nameof(PhotoManagerImpl<T>));
                }
                // if photo is not cached, load from file
                else
                {
                    await photo.LoadAsync(true, ReadOptions);

                    Log.Info(
                        $"\t-> Done caching photo index={index}",
                        nameof(RunBackgroundWorker), nameof(PhotoManagerImpl<T>));
                }
            }

            await Task.Delay(10, token).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// Gets photo indexes for queuing.
    /// </summary>
    /// <param name="index">Current index of photo list</param>
    /// <param name="includeCurrentIndex">Include current index in the queue list</param>
    private async Task<List<int>> GetQueueListAsync(int index, bool includeCurrentIndex)
    {
        // check valid index
        if (index < 0 || index >= Count) return [];


        // 1. get the indexes to cache
        var newQueueList = BHelper.GenerateWrappedIndexes(index, PreloadRange, Count, includeCurrentIndex);


        // 2. release the out-of-range resources
        var oldFreeList = new List<int>(_freeList);
        var unloadedList = new HashSet<int>();
        foreach (var photoIndex in oldFreeList)
        {
            if (photoIndex == index) continue;
            if (!newQueueList.Contains(photoIndex))
            {
                var photo = Get(photoIndex);
                photo?.CancelLoading();
                photo?.Unload();

                _freeList.Remove(photoIndex);

                unloadedList.Add(photoIndex);
            }
        }

        Log.Info(
            $"New index={index}, " +
            $"Unloaded photos=[{string.Join(",", unloadedList)}]",
            nameof(GetQueueListAsync), nameof(PhotoManagerImpl<T>));

        // save the indexes to free for next time
        _freeList.UnionWith(newQueueList);


        // 3. create final photo indexes to cache
        var finalQueueList = new HashSet<int>();

        foreach (var itemIndex in newQueueList)
        {
            try
            {
                var item = Get(itemIndex);
                if (item is null) continue;

                // load metadata
                await item.LoadMetadataAsync(ReadOptions);


                // check image dimension
                var notExceedDimension = MaxImageDimensionToCache <= 0
                    || (item.Metadata.Width <= MaxImageDimensionToCache
                        && item.Metadata.Height <= MaxImageDimensionToCache);

                // check file size
                var notExceedFileSize = MaxFileSizeInMbToCache <= 0
                    || (item.Metadata.FileSizeInBytes / 1024f / 1024f <= MaxFileSizeInMbToCache);

                // check if this photo can be cached
                var canCache = !item.IsDone && notExceedDimension && notExceedFileSize;

                if (canCache) finalQueueList.Add(itemIndex);
            }
            catch { }
        }


        Log.Info(
            $"\t-> " +
            $"Old _freeList=[{string.Join(",", oldFreeList)}], " +
            $"New _freeList=[{string.Join(",", _freeList)}]",
            nameof(GetQueueListAsync), nameof(PhotoManagerImpl<T>));

        return finalQueueList.ToList();
    }


    /// <summary>
    /// Gets a photo at the specified index and initiates caching for surrounding photos.
    /// </summary>
    private async Task<T?> GetAndCacheAsync(int index, CancellationToken token)
    {
        var photo = Get(index);
        if (photo is null) return null;


        // get queue list according to index, don't include current index
        var newQueueList = await GetQueueListAsync(index, false);

        // cancel if requested
        token.ThrowIfCancellationRequested();

        Log.Error($"newQueueList=[{string.Join(",", newQueueList)}]");

        _queueList.Clear();
        _queueList.AddRange(newQueueList);


        return photo;
    }


    /// <summary>
    /// Gets a photo by given step,
    /// optionally loop back the index if its new value is out of range.
    /// </summary>
    public async Task<T?> GetByStepAsync(int step, bool loopBackNavigation, CancellationToken token)
    {
        // calculate new index
        var newIndex = CurrentIndex + step;
        var safeIndex = BHelper.ComputeIndexInRange(newIndex, Count, loopBackNavigation);

        var photo = await GetAndCacheAsync(safeIndex, token);

        CurrentIndex = safeIndex;
        return photo;
    }


    public T? GetByStep(int step, bool loopBackNavigation)
    {
        // calculate new index
        var newIndex = CurrentIndex + step;
        var safeIndex = BHelper.ComputeIndexInRange(newIndex, Count, loopBackNavigation);

        var photo = Get(safeIndex);

        CurrentIndex = safeIndex;
        return photo;
    }



    // Abstract / Virtual functions
    #region Abstract / Virtual functions

    /// <summary>
    /// Creates a photo item from the specified file path.
    /// </summary>
    protected abstract T CreatePhotoItem(string filePath);


    /// <summary>
    /// Clears and disposes the resources of <see cref="PhotoManagerImpl{T}"/> instance.
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        // stop the worker
        _cancelWorker?.Cancel();
        _cancelWorker?.Dispose();
        _cancelWorker = null;

        Clear();
        DisposeFileSearcher();

        _tokenStartCaching?.Cancel();
        _tokenStartCaching?.Dispose();
        _tokenStartCaching = null;

        _tokenThumbnail?.Cancel();
        _tokenThumbnail?.Dispose();
        _tokenThumbnail = null;

        _lockGetAndCache?.Dispose();
        _lockPreloadPhotos?.Dispose();
        _lockGetThumbnails?.Dispose();
        _lockManageThumbnailCache?.Dispose();
    }

    #endregion // Abstract / Virtual functions







    ///// <summary>
    ///// Gets a photo at the specified index and initiates caching for surrounding photos.
    ///// </summary>
    //public T? GetAndCache(int index, bool cacheCurrentIndex)
    //{
    //    var photo = Get(index);

    //    if (photo is not null)
    //    {
    //        _ = StartCachingAsync(index, CurrentIndex, cacheCurrentIndex);
    //    }

    //    return photo;
    //}


    /// <summary>
    /// Start caching photos.
    /// </summary>
    /// <param name="newIndex">The center index to cache the surrounding photos.</param>
    /// <param name="cacheCurrentIndex">Should cache the <paramref name="newIndex"/>?</param>
    public async Task StartCachingAsync(int newIndex, int oldIndex, bool cacheCurrentIndex)
    {
        // limit only 1 concurrent access
        await _lockGetAndCache.WaitAsync();

        try
        {
            _tokenStartCaching?.Cancel();
            _tokenStartCaching?.Dispose();
            _tokenStartCaching = new();


            // preload the surrounding items
            _ = Task.Run(
                () => CacheAsync__(newIndex, oldIndex, cacheCurrentIndex, _tokenStartCaching.Token),
                _tokenStartCaching.Token);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            _lockGetAndCache.Release();
        }
    }


    /// <summary>
    /// Caches the specified photo and its surrounding,
    /// Unloads the photos those are not in the range.
    /// </summary>
    private async Task CacheAsync__(int newIndex, int oldIndex, bool cacheCurrentIndex,
        CancellationToken token)
    {
        var cachingIndex = newIndex;

        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();


            // 1. get the range
            var oldRange = BHelper.GenerateWrappedIndexes(oldIndex, PreloadRange, Count, true);
            var newRange = BHelper.GenerateWrappedIndexes(newIndex, PreloadRange, Count, true);
            var cacheRange = await GetIndexesForCaching__(newIndex, cacheCurrentIndex);

            Log.Info($"{nameof(CacheAsync__)}: {nameof(oldIndex)}={oldIndex}, {nameof(newIndex)}={newIndex}");
            Log.Info($"{nameof(CacheAsync__)}: " +
                $"{nameof(oldRange)}=[{string.Join(",", oldRange)}], " +
                $"{nameof(newRange)}=[{string.Join(",", newRange)}], " +
                $"{nameof(cacheRange)}=[{string.Join(",", cacheRange)}]");


            // 2. unload the old range
            for (int i = 0; i < oldRange.Count; i++)
            {
                var oIndex = oldRange[i];

                if (newRange.IndexOf(oIndex) == -1)
                {
                    // unload image data but keep metadata
                    Get(oIndex)?.Unload();
                    Log.Info($"{nameof(CacheAsync__)}: \t⤷ Unloaded index={oIndex}, {GetFilePath(oIndex)}");
                }
            }


            // 3. start caching items in the range
            for (var i = 0; i < cacheRange.Count; i++)
            {
                cachingIndex = cacheRange[i];
                Log.Info($"{nameof(CacheAsync__)}: \t⤷ Caching index={cachingIndex}, {GetFilePath(cachingIndex)}");

                // cancel if requested
                token.ThrowIfCancellationRequested();

                await LoadAsync(cachingIndex, true, null, token);
                Log.Info($"{nameof(CacheAsync__)}: \t\t⤷ Cached index={cachingIndex}, {GetFilePath(cachingIndex)}");
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Info($"{nameof(CacheAsync__)}: Cancelled caching index={cachingIndex}, {GetFilePath(cachingIndex)}");
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }


    /// <summary>
    /// Gets the list of indexes for caching.
    /// </summary>
    /// <param name="index">The center index to cache the surrounding photos.</param>
    /// <param name="cacheCurrentIndex">Should cache the <paramref name="index"/>?</param>
    private async Task<List<int>> GetIndexesForCaching__(int index, bool cacheCurrentIndex)
    {
        // 1. get the list of index for caching
        var rangeToCache = BHelper.GenerateWrappedIndexes(index, PreloadRange, Count, cacheCurrentIndex);
        if (rangeToCache.Count == 0) return [];


        // 2. check if the photo can be cached
        var newQueueList = new List<int>();

        foreach (var itemIndex in rangeToCache)
        {
            try
            {
                var item = Get(itemIndex);
                if (item is null) continue;

                // load metadata
                await item.LoadMetadataAsync(ReadOptions);


                // check image dimension
                var notExceedDimension = MaxImageDimensionToCache <= 0
                    || (item.Metadata.Width <= MaxImageDimensionToCache
                        && item.Metadata.Height <= MaxImageDimensionToCache);

                // check file size
                var notExceedFileSize = MaxFileSizeInMbToCache <= 0
                    || (item.Metadata.FileSizeInBytes / 1024f / 1024f <= MaxFileSizeInMbToCache);

                // only put the index to the queue if it does not exceed the size limit
                var canCache = !item.IsDone && notExceedDimension && notExceedFileSize;

                if (canCache)
                {
                    newQueueList.Add(itemIndex);
                }
            }
            catch { }
        }

        return newQueueList;
    }



    /// <summary>
    /// Loads the photo at the specified index if it has not already been loaded.
    /// </summary>
    private async Task LoadAsync(int index, bool useCache = true,
        IProgress<PhotoLoadingEventArgs>? progress = null,
        CancellationToken token = default)
    {
        var photo = Get(index);
        if (photo is null) return;

        // use cached data
        if (useCache && photo.IsDone) return;

        // limit the number of concurrent loads
        await _lockPreloadPhotos.WaitAsync(token);

        try
        {
            // start loading photo
            if (!photo.IsDone)
            {
                await photo.LoadAsync(useCache, ReadOptions, progress);
            }
        }
        finally
        {
            _lockPreloadPhotos.Release();
        }
    }
















    /// <summary>
    /// Gets the thumbnail for the photo at the specified index.
    /// </summary>
    public async Task<IDisposable?> GetThumbnailAsync(int index, CancellationToken token = default)
    {
        // item not found
        var photo = Get(index);
        if (photo is null) return null;

        MagickImage? thumbnail = null;


        // limit the number of concurrent loads
        await _lockPreloadPhotos.WaitAsync(token);

        try
        {
            // get thumbnail from the photo
            thumbnail = await Task.Run(() => photo.Metadata.GetPreview(token), token);
        }
        finally
        {
            _lockPreloadPhotos.Release();
        }


        if (token.IsCancellationRequested)
        {
            thumbnail?.Dispose();
            thumbnail = null;
        }
        else
        {
            // check the thumbnail disk cache
            _ = Task.Run(() => ManageThumbnailsDiskCacheAsync(token), token);
        }


        return thumbnail;
    }


    /// <summary>
    /// Manages the disk cache for thumbnails,
    /// ensures the total cache size does not exceed the configured maximum size.
    /// </summary>
    private async Task ManageThumbnailsDiskCacheAsync(CancellationToken token)
    {
        // limit the number of concurrent loads to 1
        await _lockManageThumbnailCache.WaitAsync(token);

        try
        {
            // get the thumbnail files
            var filesDict = Directory
                .EnumerateFiles("Thumbnails", "*.thumb.jpg")
                .Select(fp =>
                {
                    var fi = new FileInfo(fp);
                    var fileSizeInMb = fi.Length / 1024 / 1024;
                    var lastAccessed = fi.LastAccessTimeUtc;
                    var tuple = (fileSizeInMb, lastAccessed);

                    return new KeyValuePair<string, (long SizeInMb, DateTime LastAccessed)>(fp, tuple);
                })
                .OrderByDescending(i => i.Value.LastAccessed)
                .ToImmutableDictionary();


            // calculate total cache size
            var totalCacheSizeInMb = filesDict.Values.Sum(i => i.SizeInMb);


            // clear old cached files
            while (totalCacheSizeInMb > _maxThumbnailCacheSizeInMb)
            {
                // cancel if requested
                if (token.IsCancellationRequested)
                {
                    Log.Info($"{nameof(ManageThumbnailsDiskCacheAsync)}: Cancelled {nameof(totalCacheSizeInMb)}={totalCacheSizeInMb}");
                    break;
                }

                var oldestFilePath = filesDict.FirstOrDefault().Key;

                // delete the cached file
                if (!string.IsNullOrWhiteSpace(oldestFilePath))
                {
                    try
                    {
                        File.Delete(oldestFilePath);
                    }
                    catch { }

                    filesDict.Remove(oldestFilePath);
                }

                // recalculate total cache size
                totalCacheSizeInMb = filesDict.Values.Sum(i => i.SizeInMb);
            }
        }
        finally
        {
            _lockManageThumbnailCache.Release();
        }

    }


}


