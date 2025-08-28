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
using ImageGlass.Win64.Common;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;

namespace ImageGlass;

public partial class MainWindow
{
    /// <summary>
    /// API name: <see cref="API.IG_Exit"/>.
    /// </summary>
    public static void IG_Exit()
    {
        Application.Current.Exit();
    }


    /// <summary>
    /// API name: <see cref="API.IG_OpenFile"/>.
    /// </summary>
    public async Task IG_OpenFileAsync()
    {
        var picker = new FileOpenPicker(AppWindow.Id)
        {
            ViewMode = PickerViewMode.Thumbnail,
        };

        // set file extensions
        if (AP.Config.FileFormats.Count == 0)
        {
            picker.FileTypeFilter.Add("*");
        }
        else
        {
            foreach (var ext in AP.Config.FileFormats)
            {
                picker.FileTypeFilter.Add(ext);
            }
        }


        var file = await picker.PickSingleFileAsync();
        IG_OpenPath(file?.Path);
    }


    /// <summary>
    /// API name: <see cref="API.IG_OpenFolder"/>.
    /// </summary>
    public async Task IG_OpenFolderAsync()
    {
        var picker = new FolderPicker(AppWindow.Id);

        var dir = await picker.PickSingleFolderAsync();
        IG_OpenPath(dir?.Path);
    }


    /// <summary>
    /// API name: <see cref="API.IG_OpenPath"/>.
    /// </summary>
    public void IG_OpenPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        // 1. check if the path is being opened
        var imageIndex = AP.Photos.IndexOf(path);

        // 2.1 The file is located another folder, load the entire folder
        if (imageIndex == -1 || AP.CanUseForegroundShell())
        {
            PrepareLoadPhoto([path], false);
        }
        // 2.2 The file is in current folder AND it is the viewing image
        else if (AP.Photos.CurrentIndex == imageIndex)
        {
            //do nothing
        }
        // 2.3 The file is in current folder AND it is NOT the viewing image
        else
        {
            ViewByIndex(imageIndex);
        }
    }

}
