/*
ImageGlass Project - Image viewer for Windows
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
using Avalonia.Collections;
using ImageGlass.Common.Types;
using ImageMagick;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Class for managing a collection of photos.
/// </summary>
public abstract partial class PhotoManagerImpl : DisposableImpl
{
    // photo list
    protected AvaloniaList<Photo> _items = [];
    protected readonly ConcurrentDictionary<string, int> _dict = new(StringComparer.OrdinalIgnoreCase);

    // thumbnail
    protected CancellationTokenSource? _tokenThumbnail;
    protected readonly long _maxThumbnailCacheSizeInMb = 100; // 100MB



    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets the number of photos currently in the collection.
    /// </summary>
    public uint Count => (uint)Items.Count;

    /// <summary>
    /// Gets a list of photos.
    /// </summary>
    public AvaloniaList<Photo> Items
    {
        get => _items;
        set
        {
            if (_items != value)
            {
                _items = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(Count));
            }
        }
    }

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
    /// Selects and gets a photo by step.
    /// </summary>
    public Photo? GetByStep(int step, bool loopBackNavigation)
    {
        // calculate new index
        var newIndex = CurrentIndex + step;
        var safeIndex = BHelper.ComputeIndexInRange(newIndex, Count, loopBackNavigation);

        var photo = Select(safeIndex);

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
        thumbnail = await Task.Run(() => photo.Metadata.GetEmbeddedPreview(), token);


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
            if (token.IsCancellationRequested) break;

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


