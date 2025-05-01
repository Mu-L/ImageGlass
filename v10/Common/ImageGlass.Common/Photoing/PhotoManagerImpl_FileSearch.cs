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
    /// Loads files from the input path, returns the initial photo.
    /// </summary>
    /// <param name="path">Full path of file or directory</param>
    public async Task<T?> LoadFolderAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var pathType = BHelper.CheckPath(path);
        if (pathType == PathType.Unknown) return null;


        // 1. stop any ongoing search
        await _fileSearcher.CancelAsync();
        _fileSearcher.FileSearching -= FileSearchProvider_FileSearching;
        _fileSearcher.FileSearching += FileSearchProvider_FileSearching;

        Clear();


        string? dirPath = null;
        string? initFilePath = null;
        T? initPhoto = null;

        // 2. check the input path
        // path is a directory
        if (pathType == PathType.Dir)
        {
            dirPath = path;
        }
        // path is a file
        else
        {
            initFilePath = path;
            dirPath = Path.GetDirectoryName(path);
        }


        // 3. create initiate photo
        if (!string.IsNullOrWhiteSpace(initFilePath))
        {
            initPhoto = CreatePhotoItem(initFilePath);
        }


        // 4. start new file search
        if (!string.IsNullOrWhiteSpace(dirPath))
        {
            await _fileSearcher.StartAsync([dirPath]);

            if (initPhoto is null)
            {
                // wait for some results
                while (Count == 0 && !_fileSearcher.IsSearchEnded)
                {
                    await Task.Delay(10);
                }

                // get the first item as the init photo
                initPhoto = GetAndCache(0, true);
            }
        }


        return initPhoto;
    }


    private void FileSearchProvider_FileSearching(object? sender, FileSearchingEventArgs e)
    {
        Add(e.Results);

        Log.Info($"Added {e.Results.Count()} files to the list");
    }


    protected void DisposeFileSearcher()
    {
        _fileSearcher.FileSearching -= FileSearchProvider_FileSearching;
        _fileSearcher.Dispose();
    }

}
