

using ImageMagick;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace ImageGlass.Common.Photoing;

public class PhotoManager : IDisposable
{

    #region IDisposable Disposing

    public bool IsDisposed { get; protected set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            OnDisposing();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PhotoManager()
    {
        Dispose(false);
    }

    #endregion


    // photo list
    private readonly List<PhotoImpl> _photos = new();
    // store file_path and its index in the list
    private readonly ConcurrentDictionary<string, int> _pathDict = new();

    // concurrent loads control for photo loading
    private readonly SemaphoreSlim _lockGetAndCache = new(1, 1);

    // photo preloading
    private readonly int _preloadRange = 2;
    private readonly SemaphoreSlim _lockPreloadPhotos = new(4, 4);
    private CancellationTokenSource? _tokenStartCaching;

    // thumbnail
    private readonly SemaphoreSlim _lockGetThumbnails = new(4, 4);
    private readonly SemaphoreSlim _lockManageThumbnailCache = new(1, 1);
    private CancellationTokenSource? _tokenThumbnail;
    private readonly long _maxThumbnailCacheSizeInMb = 100; // 100MB



    /// <summary>
    /// Gets the number of photos currently in the collection.
    /// </summary>
    public int Count => _photos.Count;

    /// <summary>
    /// Gets a list of file paths associated with the photos.
    /// </summary>
    public IEnumerable<string> FilePaths => _photos.Select(i => i.FilePath);

    /// <summary>
    /// Gets, sets options for reading photo.
    /// </summary>
    public PhotoReadOptions ReadOptions { get; set; } = new();

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



    /// <summary>
    /// Initializes a new instance of <see cref="PhotoManager"/>.
    /// </summary>
    public PhotoManager(IEnumerable<string>? list = null)
    {
        if (list != null)
        {
            Add(list);
        }
    }


    /// <summary>
    /// Adds a file path to the collection.
    /// </summary>
    public void Add(string filePath, int index = -1)
    {
        var addedIndex = index;

        if (index < 0)
        {
            addedIndex = _photos.Count;
            _photos.Add(new PhotoImpl(filePath));
        }
        else
        {
            _photos.Insert(index, new PhotoImpl(filePath));
        }


        // update path dictionary
        for (int i = addedIndex; i < _photos.Count; i++)
        {
            var key = _photos[i].FilePath.ToLowerInvariant();
            _pathDict.AddOrUpdate(key, i, (fIndex, oldValue) => i);
        }
    }


    /// <summary>
    /// Adds a list of file paths to the collection.
    /// </summary>
    public void Add(IEnumerable<string> filePaths, int index = -1)
    {
        var addedIndex = index;
        var items = filePaths.Select(i => new PhotoImpl(i));

        if (index < 0)
        {
            addedIndex = _photos.Count;
            _photos.AddRange(items);
        }
        else
        {
            _photos.InsertRange(index, items);
        }


        // update path dictionary
        for (int i = addedIndex; i < _photos.Count; i++)
        {
            var key = _photos[i].FilePath.ToLowerInvariant();
            _pathDict.AddOrUpdate(key, i, (fIndex, oldValue) => i);
        }
    }


    /// <summary>
    /// Gets a photo from the list.
    /// </summary>
    public PhotoImpl? Get(int index)
    {
        if (index < 0 || index >= _photos.Count) return null;

        return _photos[index];
    }


    /// <summary>
    /// Get file path of the photo at the specified index.
    /// </summary>
    public string GetFilePath(int index)
    {
        return Get(index)?.FilePath ?? string.Empty;
    }


    /// <summary>
    /// Set file path of the photo at the specified index.
    /// </summary>
    public void SetFilePath(int index, string filePath)
    {
        var photo = Get(index);
        if (photo is null) return;

        photo.FilePath = filePath;
        photo.Metadata.SetFilePath(filePath);

        var key = filePath.ToLowerInvariant();
        _pathDict.Remove(key, out _);
        _pathDict.AddOrUpdate(key, index, (fIndex, oldValue) => index);
    }


    /// <summary>
    /// Find index of the photo with the given file path.
    /// </summary>
    public int IndexOf(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return -1;

        var key = filePath.ToLowerInvariant();
        if (_pathDict.TryGetValue(key, out var index))
        {
            return index;
        }

        return -1;
    }


    /// <summary>
    /// Checks if the photo is cached.
    /// </summary>
    public bool IsCached(int index)
    {
        var photo = Get(index);
        if (photo == null) return false;

        return photo.IsDone;
    }


    /// <summary>
    /// Start caching photos.
    /// </summary>
    /// <param name="index">The center index to cache the surrounding photos.</param>
    /// <param name="includeCurrentIndex">Should cache the <paramref name="index"/>?</param>
    public async Task StartCachingAsync(int index, bool includeCurrentIndex)
    {
        // limit only 1 concurrent access
        await _lockGetAndCache.WaitAsync();

        try
        {
            _tokenStartCaching?.Cancel();
            _tokenStartCaching?.Dispose();
            _tokenStartCaching = new CancellationTokenSource();


            // preload the surrounding items
            _ = Task.Run(
                () => CacheAsync__(index, includeCurrentIndex, _tokenStartCaching.Token),
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
    private async Task CacheAsync__(int index, bool includeCurrentIndex, CancellationToken token)
    {
        var cachingIndex = index;

        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();


            // 1. unload the out-of-range items
            for (int i = 0; i < _photos.Count; i++)
            {
                if (i < index - _preloadRange || i > index + _preloadRange)
                {
                    // unload image data but keep metadata
                    Get(i)?.Unload(false);
                }
            }


            // 2. start caching items in the range
            var indexes = await GetIndexesForCaching__(index, includeCurrentIndex);
            for (var i = 0; i < indexes.Count; i++)
            {
                cachingIndex = i;

                // cancel if requested
                token.ThrowIfCancellationRequested();

                await LoadAsync(cachingIndex, true, token);
                Log.Info($"Cached photo index={cachingIndex}");
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Error($"Cancelled {nameof(StartCachingAsync)} for index={cachingIndex}");
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
    /// <param name="includeCurrentIndex">Should cache the <paramref name="index"/>?</param>
    private async Task<List<int>> GetIndexesForCaching__(int index, bool includeCurrentIndex)
    {
        // 1. check valid index
        if (index < 0 || index >= Count) return [];


        // 2. get the list of index for caching
        var set = new HashSet<int>();
        if (includeCurrentIndex) set.Add(index);

        for (int offset = 1; offset <= _preloadRange; offset++)
        {
            set.Add(index + offset);
            set.Add(index - offset);
        }


        // 3. check if the photo can be cached
        var newQueueList = new List<int>();

        foreach (var itemIndex in set)
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
    public async Task LoadAsync(int index, bool useCache = true, CancellationToken token = default)
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
                await photo.LoadAsync(useCache, ReadOptions);
            }
        }
        finally
        {
            _lockPreloadPhotos.Release();
        }
    }


    /// <summary>
    /// Unloads and releases resources of the photo at the specified index from the collection.
    /// </summary>
    public void Unload(int index, bool disposeMetadata = false)
    {
        Get(index)?.Unload(disposeMetadata);
    }


    /// <summary>
    /// Removes the photo at the specified index from the collection.
    /// </summary>
    public void Remove(int index)
    {
        Unload(index, true);

        var photo = Get(index);
        if (photo is null) return;

        _photos.RemoveAt(index);

        var key = photo.FilePath.ToLowerInvariant();
        _pathDict.Remove(key, out _);
    }


    /// <summary>
    /// Clears all photos from the collection and releases any associated resources.
    /// </summary>
    public void Clear()
    {
        foreach (var item in _photos)
        {
            item.Dispose();
        }

        _photos.Clear();
    }


    /// <summary>
    /// Clears and disposes the resources of <see cref="PhotoManager"/> instance.
    /// </summary>
    private void OnDisposing()
    {
        Clear();

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
                    Log.Info($"Cancelled {nameof(ManageThumbnailsDiskCacheAsync)} totalCacheSizeInMb={totalCacheSizeInMb}");
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


