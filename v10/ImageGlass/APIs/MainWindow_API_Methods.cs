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

namespace ImageGlass;

public partial class MainWindow
{

    public static void IG_Exit()
    {
        Application.Current.Exit();
    }


    public async void IG_OpenFile()
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


    public async void IG_OpenFolder()
    {
        var picker = new FolderPicker(AppWindow.Id);

        var dir = await picker.PickSingleFolderAsync();
        IG_OpenPath(dir?.Path);
    }


    public void IG_OpenPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        PrepareLoadPhoto([path], false);
    }

}
