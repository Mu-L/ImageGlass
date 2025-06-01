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

public partial class PhotoManagerImpl<T, Fs, FsOptions>
{

    /// <summary>
    /// Adds a file path to the collection.
    /// </summary>
    public void Add(string filePath, int index = -1)
    {
        Add([filePath], index);
    }


    /// <summary>
    /// Adds a list of file paths to the collection.
    /// </summary>
    public void Add(IEnumerable<string> filePaths, int index = -1)
    {
        var addedIndex = index;

        if (index < 0)
        {
            addedIndex = (int)Count;
            _paths.AddItems(filePaths);
        }
        else
        {
            _paths.InsertItems(filePaths, index);
        }


        // update path dictionary
        for (int i = addedIndex; i < Count; i++)
        {
            var path = _paths[i];
            var photoItem = CreatePhotoItem(path);
            photoItem.Index = i;

            _photosDict.AddOrUpdate(path, photoItem, (fIndex, oldValue) => photoItem);
        }
    }


    /// <summary>
    /// Gets a photo from the list.
    /// </summary>
    public T? Get(int index)
    {
        if (index < 0 || index >= Count) return null;
        var path = _paths[index];

        return _photosDict.GetValueOrDefault(path);
    }


    /// <summary>
    /// Gets a photo from the list.
    /// </summary>
    public T? Get(string filePath)
    {
        return _photosDict.GetValueOrDefault(filePath);
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

        _photosDict.Remove(filePath, out _);
        _photosDict.AddOrUpdate(filePath, photo, (fIndex, oldValue) => photo);
    }


    /// <summary>
    /// Find index of the photo with the given file path.
    /// </summary>
    public int IndexOf(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return -1;

        if (_photosDict.TryGetValue(filePath, out var photo))
        {
            return photo.Index;
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
    public void Unload(int index)
    {
        Get(index)?.Unload();
    }


    /// <summary>
    /// Removes the photo at the specified index from the collection.
    /// </summary>
    public void Remove(int index)
    {
        var photo = Get(index);
        if (photo is null) return;

        Remove(photo.FilePath);
    }


    /// <summary>
    /// Removes the photo by the given file path from the collection.
    /// </summary>
    public void Remove(string filePath)
    {
        // remove from the lists
        _photosDict.Remove(filePath, out var removedItem);
        if (removedItem is null) return;

        _paths.RemoveAt(removedItem.Index);


        // update index of affected items
        for (int i = removedItem.Index; i < _paths.Count; i++)
        {
            var photoItem = Get(_paths[i]);

            if (photoItem is not null)
            {
                _photosDict[photoItem.FilePath].Index = i;
            }
        }

        // dispose removed item
        removedItem.Dispose();
    }


    /// <summary>
    /// Clears all photos from the collection and releases any associated resources.
    /// </summary>
    public void Clear()
    {
        // clear init photo
        InitPhoto?.Dispose();
        InitPhoto = null;
        CurrentIndex = -1;

        // dispose photos in the list
        Parallel.ForEach(_photosDict, item =>
        {
            item.Value?.Dispose();
        });
        _photosDict.Clear();
        _paths.Clear();
        DistinctDirs.Clear();

        Log.Info($"Cleared photo list!", nameof(Clear), nameof(PhotoManagerImpl<T, Fs, FsOptions>));
    }

}
