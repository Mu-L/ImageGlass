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

namespace ImageGlass.Common.Photoing;

public partial class PhotoManagerImpl<T>
{

    protected FilesSearcher _fileSearcher = new();


    /// <summary>
    /// Gets, sets index of the viewing photo.
    /// </summary>
    public int CurrentIndex { get; set; } = -1;

    /// <summary>
    /// The current "initial" path (file or dir) we're viewing for rebuilding the photo list.
    /// </summary>
    public string InitInputPath { get; set; } = string.Empty;

    /// <summary>
    /// The "initial" photo.
    /// </summary>
    public T? InitPhoto { get; set; } = null;




    /// <summary>
    /// Loads files from the input path, returns the initial photo.
    /// </summary>
    /// <param name="path">Full path of file or directory</param>
    public async Task<T?> LoadFolderAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var pathType = BHelper.CheckPath(path);
        if (pathType == PathType.Unknown) return null;


        // 1. stop any ongoing search
        _fileSearcher.CancelSearching();
        _fileSearcher.FileSearching -= FileSearchProvider_FileSearching;
        _fileSearcher.FileSearching += FileSearchProvider_FileSearching;

        // reset the photo list
        Clear();


        string? dirPath = null;

        // 2. check the input path
        // path is a directory
        if (pathType == PathType.Dir)
        {
            dirPath = path;
        }
        // path is a file
        else
        {
            InitInputPath = path;
            dirPath = Path.GetDirectoryName(path);
        }


        // 3. create initiate photo
        if (!string.IsNullOrWhiteSpace(InitInputPath))
        {
            InitPhoto = CreatePhotoItem(InitInputPath);
        }


        // 4. start new file search
        if (!string.IsNullOrWhiteSpace(dirPath))
        {
            _ = _fileSearcher.StartAsync([dirPath]);

            if (InitPhoto is null)
            {
                // wait for some results
                while (Count == 0 && !_fileSearcher.IsSearchEnded)
                {
                    await Task.Delay(10);
                }

                // get the first item as the init photo
                CurrentIndex = 0;

                InitPhoto = GetAndCache(CurrentIndex, true);
                InitInputPath = InitPhoto?.FilePath ?? InitInputPath;
            }
        }


        return InitPhoto;
    }


    private void FileSearchProvider_FileSearching(object? sender, FileSearchingEventArgs e)
    {
        Add(e.Results);

        // find the current index
        if (CurrentIndex == -1)
        {
            CurrentIndex = IndexOf(InitInputPath);

            // save the init photo to the list
            if (CurrentIndex >= 0 && InitPhoto is not null)
            {
                _photos[CurrentIndex]?.Dispose();
                _photos[CurrentIndex] = InitPhoto;
            }
        }


        Log.Info($"{nameof(FileSearchProvider_FileSearching)}: Added {e.Results.Count()} files to the list, " +
            $"{nameof(CurrentIndex)}={CurrentIndex}/{Count}.");
    }


    protected void DisposeFileSearcher()
    {
        _fileSearcher.FileSearching -= FileSearchProvider_FileSearching;
        _fileSearcher.Dispose();
    }

}
