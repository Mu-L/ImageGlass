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
using ImageGlass.Common.FileSystem;
using System;
using System.Collections.Generic;

namespace ImageGlass.Common.Photoing;

public partial class PhotoManagerImpl<Fs, FsOptions>
{
    protected Fs _fileSearcher;
    protected int _currentIndex = -1;


    // Public properties
    #region Public properties

    /// <summary>
    /// Gets index of the viewed photo.
    /// </summary>
    public int CurrentIndex => _currentIndex;


    /// <summary>
    /// Gets the current file path of the viewed photo.
    /// </summary>
    public string CurrentFilePath => GetFilePath(CurrentIndex);


    /// <summary>
    /// Gets the metadata of the viewed photo.
    /// </summary>
    public PhotoMetadata? CurrentMetadata => Get(CurrentIndex)?.Metadata;


    /// <summary>
    /// Gets current photo.
    /// </summary>
    public Photo? Current => Get(CurrentIndex);


    /// <summary>
    /// The initial photo,
    /// can be the photo from the initial file path or the first photo of the directory.
    /// </summary>
    public Photo? InitPhoto { get; set; } = null;

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
        _fileSearcher.Dispose();
    }


    /// <summary>
    /// Loads files from the input path, returns the initial photo.
    /// </summary>
    /// <param name="path">Full path of file or directory</param>
    public virtual Photo? StartLoadingFiles(ICollection<string> paths, string? currentFilePath,
        FsOptions searchOptions, IProgress<FileSearchingEventArgs> progress)
    {
        throw new NotImplementedException();
    }

}
