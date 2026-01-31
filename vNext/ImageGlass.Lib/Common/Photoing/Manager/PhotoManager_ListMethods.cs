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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Common.Photoing;

public partial class PhotoManager
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
        var newItems = filePaths.Select(path => new Photo(path));

        if (index < 0)
        {
            addedIndex = (int)Count;

            Items.AddRange(newItems);
        }
        else
        {
            Items.InsertRange(index, newItems);
        }

        // update path dictionary
        for (int i = addedIndex; i < Count; i++)
        {
            var item = Items[i];
            _dict.AddOrUpdate(item.FilePath, i, (fIndex, oldValue) => i);
        }

        _ = OnPropertyChanged(nameof(Count));
    }


    /// <summary>
    /// Gets a photo from the list.
    /// </summary>
    public Photo? Get(int index)
    {
        if (index < 0 || index >= Count) return null;
        var item = Items[index];

        return item;
    }


    /// <summary>
    /// Gets a photo from the list.
    /// </summary>
    public Photo? Get(string filePath)
    {
        var index = _dict.GetValueOrDefault(filePath, -1);
        return Get(index);
    }


    /// <summary>
    /// Selects the specified file by its path, updating the current selection.
    /// </summary>
    public Photo? Select(string filePath)
    {
        var newSelectionIndex = IndexOf(filePath);

        return Select(newSelectionIndex);
    }


    /// <summary>
    /// Selects an item at the specified index, updating the current selection.
    /// </summary>
    public Photo? Select(int index)
    {
        // deselect old index
        if (0 <= CurrentIndex && CurrentIndex < Count)
        {
            Items[CurrentIndex].IsCurrent = false;
        }

        // validate new index
        if (index < 0 || index >= Count) return null;

        // select new index
        Items[index].IsCurrent = true;
        _currentIndex = index;

        _ = OnPropertyChanged(nameof(Current));
        _ = OnPropertyChanged(nameof(CurrentIndex));
        _ = OnPropertyChanged(nameof(CurrentFilePath));
        _ = OnPropertyChanged(nameof(CurrentMetadata));

        return Get(index);
    }


    /// <summary>
    /// Checks whether the given file path is currently selected.
    /// </summary>
    public bool IsSelected(string filePath)
    {
        var index = IndexOf(filePath);

        return IsSelected(index);
    }


    /// <summary>
    /// Checks whether the specified index is currently selected.
    /// </summary>
    public bool IsSelected(int index)
    {
        return Get(index)?.IsCurrent ?? false;
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

        _dict.Remove(filePath, out _);
        _dict.AddOrUpdate(filePath, index, (fIndex, oldValue) => index);

        if (index == CurrentIndex)
        {
            _ = OnPropertyChanged(nameof(Current));
            _ = OnPropertyChanged(nameof(CurrentFilePath));
            _ = OnPropertyChanged(nameof(CurrentMetadata));
        }
    }


    /// <summary>
    /// Find index of the photo with the given file path.
    /// </summary>
    public int IndexOf(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return -1;

        if (_dict.TryGetValue(filePath, out var photoIndex))
        {
            return photoIndex;
        }

        return -1;
    }


    /// <summary>
    /// Checks if the photo is cached.
    /// </summary>
    public bool IsCached(int index)
    {
        var photo = Get(index);
        return photo?.State == PhotoLoadingState.Loaded;
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
        var index = IndexOf(filePath);
        if (index < 0) return;

        var isCurrentPhoto = CurrentIndex == index;

        // update index of affected items
        for (int i = index + 1; i < Count; i++)
        {
            var newIndex = i - 1;
            var itemPath = GetFilePath(i);
            _dict[itemPath] = newIndex;
        }

        // dispose removed item
        Items[index].Dispose();

        // remove from the lists
        _dict.Remove(filePath, out var _);
        Items.RemoveAt(index);

        _ = OnPropertyChanged(nameof(Count));

        if (isCurrentPhoto)
        {
            _ = OnPropertyChanged(nameof(Current));
            _ = OnPropertyChanged(nameof(CurrentFilePath));
            _ = OnPropertyChanged(nameof(CurrentMetadata));
        }
    }


    /// <summary>
    /// Clears all photos from the collection and releases any associated resources.
    /// </summary>
    public void Clear()
    {
        // clear init photo
        InitPhoto?.Dispose();
        InitPhoto = null;
        _currentIndex = -1;

        // dispose photos in the list
        Parallel.ForEach(Items, item =>
        {
            item.WithNoReactive(() => item.Dispose());
        });

        Items.Clear();
        _dict.Clear();
        DistinctDirs.Clear();
    }

}
