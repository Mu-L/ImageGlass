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

public partial class PhotoManagerImpl<T, Fs, FsOptions>
{
    protected Fs _fileSearcher;


    // Public properties
    #region Public properties

    /// <summary>
    /// Gets, sets index of the viewing photo.
    /// </summary>
    public int CurrentIndex { get; set; } = -1;


    /// <summary>
    /// The initial photo,
    /// can be the photo from the initial file path or the first photo of the directory.
    /// </summary>
    public T? InitPhoto { get; set; } = null;

    #endregion // Public properties



    /// <summary>
    /// Creates file searcher service.
    /// </summary>
    protected abstract Fs CreateFileSearcher();



    /// <summary>
    /// Frees resources of file searcher.
    /// </summary>
    protected void DisposeFileSearcher()
    {
        _fileSearcher.FileSearching -= FileSearchProvider_FileSearching;
        _fileSearcher.Dispose();
    }


    /// <summary>
    /// Loads files from the input path, returns the initial photo.
    /// </summary>
    /// <param name="path">Full path of file or directory</param>
    public async Task<T?> LoadFolderAsync(string path, FsOptions searchOptions)
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
            dirPath = Path.GetDirectoryName(path);
            InitPhoto = CreatePhotoItem(path);
        }


        // 3. start new file search
        if (!string.IsNullOrWhiteSpace(dirPath))
        {
            _fileSearcher.StartAsync([dirPath], searchOptions);


            // if user selects a folder
            if (InitPhoto is null)
            {
                // wait for some results
                while (Count == 0 && !_fileSearcher.IsSearchEnded)
                {
                    await Task.Delay(10);
                }

                // get the first item as the init photo
                CurrentIndex = 0;
                InitPhoto = Get(CurrentIndex);
            }
        }


        return InitPhoto;
    }


    /// <summary>
    /// Handles search results.
    /// </summary>
    private void FileSearchProvider_FileSearching(object? sender, FileSearchingEventArgs e)
    {
        Add(e.Results);

        // if we haven't found current index for the init photo yet
        if (InitPhoto is not null && CurrentIndex == -1)
        {
            CurrentIndex = IndexOf(InitPhoto.FilePath);

            // save the init photo to the list
            if (CurrentIndex >= 0 && InitPhoto is not null)
            {
                _photos[CurrentIndex]?.Dispose();
                _photos[CurrentIndex] = InitPhoto;
            }
        }


        Log.Info(
            $"Added files to the list, " +
            $"{nameof(CurrentIndex)}={CurrentIndex}/{Count - 1}.",
            nameof(FileSearchProvider_FileSearching), nameof(PhotoManagerImpl<T, Fs, FsOptions>));
    }




}
