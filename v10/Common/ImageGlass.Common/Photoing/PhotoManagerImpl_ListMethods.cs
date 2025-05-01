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
namespace ImageGlass.Common.Photoing;

public partial class PhotoManagerImpl<T>
{

    /// <summary>
    /// Adds a file path to the collection.
    /// </summary>
    public void Add(string filePath, int index = -1)
    {
        var addedIndex = index;

        if (index < 0)
        {
            addedIndex = _photos.Count;
            _photos.Add(CreatePhotoItem(filePath));
        }
        else
        {
            _photos.Insert(index, CreatePhotoItem(filePath));
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
        var items = filePaths.Select(i => CreatePhotoItem(i));

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
    public T? Get(int index)
    {
        if (index < 0 || index >= _photos.Count) return null;

        return _photos[index];
    }


    /// <summary>
    /// Gets a photo from the list.
    /// </summary>
    public T? Get(string filePath)
    {
        var index = IndexOf(filePath);
        return Get(index);
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
        InitPhoto?.Dispose();
        InitPhoto = null;

        foreach (var item in _photos)
        {
            item?.Dispose();
        }

        _photos.Clear();
        DistinctDirs.Clear();

        CurrentIndex = -1;
        InitInputPath = string.Empty;

        Log.Info($"{nameof(Clear)}: Cleared photo list!");
    }

}
