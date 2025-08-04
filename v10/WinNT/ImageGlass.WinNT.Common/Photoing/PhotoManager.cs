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
using ImageGlass.Common.Photoing;
using ImageGlass.WinNT.Common.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageGlass.WinNT.Common.Photoing;


/// <summary>
/// <inheritdoc/>
/// </summary>
public partial class PhotoManager : PhotoManagerImpl<Photo, FileSearcher, FileShellSearchOptions>
{

    public PhotoManager(IEnumerable<string>? list = null) : base(list)
    {
        //
    }



    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override FileSearcher CreateFileSearcher()
    {
        return new FileSearcher();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override Photo CreatePhotoItem(string filePath)
    {
        return new Photo(filePath);
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override Photo? StartLoadingFiles(string[] paths, FileShellSearchOptions searchOptions, IProgress<FileSearchingEventArgs> progress)
    {
        // 1. stop any ongoing search
        _fileSearcher.CancelSearching();

        // reset the photo list
        Clear();
        InitPhoto = null;


        // 2. get distinct dir paths for searching
        var inputPaths = WHelper.GetDistinctDirsFromPaths(paths);


        // 3. create init photo
        var initFilePath = inputPaths.FilePaths.FirstOrDefault() ?? "";
        if (!string.IsNullOrWhiteSpace(initFilePath))
        {
            InitPhoto = CreatePhotoItem(initFilePath);
        }


        // 4. start searching files in a new thread
        _ = _fileSearcher.SearchAsync(inputPaths.DirPaths, searchOptions, progress);

        return InitPhoto;
    }

}

