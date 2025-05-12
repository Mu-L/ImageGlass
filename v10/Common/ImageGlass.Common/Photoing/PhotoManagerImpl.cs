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
using ImageGlass.Common.FileSystem;
using ImageMagick;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Class for managing a collection of photos.
/// </summary>
public abstract partial class PhotoManagerImpl<T, Fs, FsOptions> : DisposableImpl
    where T : PhotoImpl
    where Fs : FileSearcherImpl<FsOptions>
    where FsOptions : FileSearchOptions
{
    // photo list
    protected readonly List<T> _photos = new();

    // store file paths and the index for quick access photo in the list
    protected readonly ConcurrentDictionary<string, int> _pathDict = new(StringComparer.OrdinalIgnoreCase);


    // thumbnail
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
        _fileSearcher = CreateFileSearcher();

        if (list is not null) Add(list);
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

        Clear();
        DisposeFileSearcher();

        _tokenThumbnail?.Cancel();
        _tokenThumbnail?.Dispose();
        _tokenThumbnail = null;
    }

    #endregion // Abstract / Virtual functions




    /// <summary>
    /// Gets a photo by step.
    /// </summary>
    public T? GetByStep(int step, bool loopBackNavigation)
    {
        // calculate new index
        var newIndex = CurrentIndex + step;
        var safeIndex = BHelper.ComputeIndexInRange(newIndex, Count, loopBackNavigation);

        var photo = Get(safeIndex);

        CurrentIndex = safeIndex;
        return photo;
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


        // get thumbnail from the photo
        thumbnail = await Task.Run(() => photo.Metadata.GetEmbeddedPreview(token), token);


        if (token.IsCancellationRequested)
        {
            thumbnail?.Dispose();
            thumbnail = null;
        }
        else
        {
            // check the thumbnail disk cache
            _ = Task.Run(() => ManageThumbnailsDiskCache(token), token);
        }


        return thumbnail;
    }


    /// <summary>
    /// Manages the disk cache for thumbnails,
    /// ensures the total cache size does not exceed the configured maximum size.
    /// </summary>
    private void ManageThumbnailsDiskCache(CancellationToken token)
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
                Log.Info($"Cancelled {nameof(totalCacheSizeInMb)}={totalCacheSizeInMb}",
                    nameof(ManageThumbnailsDiskCache), nameof(PhotoManagerImpl<T, Fs, FsOptions>));
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


}


