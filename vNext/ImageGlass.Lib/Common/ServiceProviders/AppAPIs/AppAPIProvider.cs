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
using ImageGlass.Common.Localization;
using ImageGlass.Common.Windows;
using ImageGlass.UI;
using ImageGlass.UI.Windowing;
using System;
using System.Threading.Tasks;

namespace ImageGlass.Common.ServiceProviders;

public class AppAPIProvider
{
    private MainWindow _mainWindow;


    public AppAPIProvider(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }



    /// <summary>
    /// Opens photo by the given path, shortcut for method <see cref="PrepareLoadPhotoList"/> with params:
    /// <list type="bullet">
    ///   <item><c>currentFilePath</c> = <c>null</c></item>
    ///   <item><c>disposeForegroundShell</c> = <c>false</c></item>
    ///   <item><c>loadInitPhoto</c> = <c>true</c></item>
    /// </list>
    /// </summary>
    public void IG_OpenPath(string? path)
    {
        var fullPath = BHelper.ResolvePath(path);
        if (string.IsNullOrWhiteSpace(fullPath)) return;

        // 1. check if the path is being opened
        var imageIndex = Core.Photos.IndexOf(fullPath);

        // 2.1 The file is located another folder, load the entire folder
        if (imageIndex == -1 || (Core.ShellProvider?.CanUseForegroundShell() ?? false))
        {
            _mainWindow.PART_MainView.PrepareLoadPhotoList([fullPath],
                currentFilePath: null, disposeForegroundShell: false, loadInitPhoto: true);
        }
        // 2.2 The file is in current folder AND it is the viewing image
        else if (Core.Photos.CurrentIndex == imageIndex)
        {
            //do nothing
        }
        // 2.3 The file is in current folder AND it is NOT the viewing image
        else
        {
            IG_ViewByIndex(imageIndex);
        }
    }






    #region Navigation APIs

    /// <summary>
    /// View a photo in the list by the given index.
    /// </summary>
    public void IG_ViewByIndex(string? photoIndexStr)
    {
        if (!int.TryParse(photoIndexStr, out var photoIndex)) return;

        IG_ViewByIndex(photoIndex);
    }


    /// <summary>
    /// View a photo in the list by the given index.
    /// </summary>
    public void IG_ViewByIndex(int photoIndex)
    {
        if (photoIndex < 0) return;

        var step = photoIndex - Core.Photos.CurrentIndex;
        IG_ViewByStep(step);
    }


    /// <summary>
    /// View a photo in the list by the given step.
    /// </summary>
    public void IG_ViewByStep(string? stepStr)
    {
        if (!int.TryParse(stepStr, out var step))
        {
            throw new ArgumentException($"""
                Step '{stepStr}' is not a valid integer.
                
                ----------
                👉🏼 Method: {nameof(IG_ViewByStep)}
                """,
                nameof(stepStr));
        }

        IG_ViewByStep(step);
    }


    /// <summary>
    /// View a photo in the list by the given step.
    /// </summary>
    public void IG_ViewByStep(int step)
    {
        var photo = Core.Photos.GetByStep(step, true);
        _ = _mainWindow.PART_MainView.ViewPhotoAsync(photo);
    }


    /// <summary>
    /// View the next photo.
    /// </summary>
    public void IG_ViewNext()
    {
        IG_ViewByStep(1);
    }


    /// <summary>
    /// View the previous photo.
    /// </summary>
    public void IG_ViewPrevious()
    {
        IG_ViewByStep(-1);
    }


    /// <summary>
    /// Shows an input dialog, and opens the user-input photo.
    /// </summary>
    public void IG_GoTo()
    {
        _ = IG_GoToAsync();
    }

    /// <summary>
    /// Shows an input dialog, and opens the user-input photo.
    /// </summary>
    public async Task IG_GoToAsync()
    {
        if (Core.Photos.Count == 0) return;

        var oldIndex = Core.Photos.CurrentIndex + 1;
        var result = await ModalWindow.ShowInputAsync(_mainWindow, new ModalWindowOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuGoTo],
            Description = Core.Lang[LangId.FrmMain_MnuGoTo_Description],
            InputValue = oldIndex.ToString(),
            AcceptValue = TextBoxAcceptValue.UnsignedIntValueOnly,
        });

        if (result.ExitCode != DialogExitCode.OK) return;


        if (int.TryParse(result.InputValue, out var newIndex))
        {
            newIndex--;

            if (newIndex != Core.Photos.CurrentIndex
                && 0 <= newIndex && newIndex < Core.Photos.Count)
            {
                IG_ViewByIndex(newIndex);
            }
        }
    }


    /// <summary>
    /// Views the first photo in the list.
    /// </summary>
    public void IG_GoToFirst()
    {
        IG_ViewByIndex(0);
    }


    /// <summary>
    /// Views the last photo in the list.
    /// </summary>
    public void IG_GoToLast()
    {
        IG_ViewByIndex((int)Core.Photos.Count - 1);
    }

    #endregion // Navigation APIs



}
