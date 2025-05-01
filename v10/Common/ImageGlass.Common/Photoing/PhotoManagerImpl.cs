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
    /// Gets or sets the range of items to preload in advance (in LRU queue).
    /// </summary>
    public uint PreloadRange { get; set; } = 1;

    /// <summary>
    /// Gets, sets the maximum image dimension to cache.
    /// If this value is <c>≤ 0</c>, the option will be ignored.
    /// </summary>
    public int MaxImageDimensionToCache { get; set; } = 0;

    /// <summary>
    /// Gets, sets the maximum image file size (in MB) to cache.
    /// If this value is <c>≤ 0</c>, the option will be ignored.
    /// </summary>
    public float MaxFileSizeInMbToCache { get; set; } = 0f;

    #endregion // Public Properties



    /// <summary>
    /// Initializes a new instance of <see cref="PhotoManagerImpl{T}"/>.
    /// </summary>
    public PhotoManagerImpl(IEnumerable<string>? list = null)
    {
        if (list != null)
        {
            Add(list);
        }
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
    protected new virtual void OnDisposing()
    {
        base.OnDisposing();

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




    /// <summary>
    /// Loads the photo at the specified index if it has not already been loaded.
    /// </summary>
    public async Task LoadAsync(int index, bool useCache = true,
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
    /// Gets a photo at the specified index and initiates caching for surrounding photos.
    /// </summary>
    public T? GetAndCache(int index, bool cacheCurrentIndex)
    {
        var photo = Get(index);

        if (photo is not null)
        {
            _ = StartCachingAsync(index, cacheCurrentIndex);
        }

        return photo;
    }


    /// <summary>
    /// Start caching photos.
    /// </summary>
    /// <param name="index">The center index to cache the surrounding photos.</param>
    /// <param name="cacheCurrentIndex">Should cache the <paramref name="index"/>?</param>
    public async Task StartCachingAsync(int index, bool cacheCurrentIndex)
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
                () => CacheAsync__(index, cacheCurrentIndex, _tokenStartCaching.Token),
                _tokenStartCaching.Token);
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
    private async Task CacheAsync__(int index, bool cacheCurrentIndex, CancellationToken token)
    {
        var cachingIndex = index;

        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();


            // 1. get the range to cache
            var rangeToCache = BHelper.GenerateWrappedIndexes(
                index, (int)PreloadRange, _photos.Count, cacheCurrentIndex);
            Log.Info($"{nameof(CacheAsync__)}: Range to cache {nameof(rangeToCache)}=[{string.Join(",", rangeToCache)}]");


            // 2. unload the out-of-range items
            for (int i = 0; i < _photos.Count; i++)
            {
                if (rangeToCache.IndexOf(i) == -1)
                {
                    // unload image data but keep metadata
                    Get(i)?.Unload(false);
                    Log.Info($"{nameof(CacheAsync__)}: \t⤷ Unloaded index={i}, {GetFilePath(i)}");
                }
            }


            // 3. start caching items in the range
            var indexes = await GetIndexesForCaching__(index, cacheCurrentIndex);
            for (var i = 0; i < indexes.Count; i++)
            {
                cachingIndex = i;
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
        var rangeToCache = BHelper.GenerateWrappedIndexes(
            index, (int)PreloadRange, _photos.Count, cacheCurrentIndex);
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


