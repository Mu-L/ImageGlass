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
using ImageGlass.Common;
using ImageGlass.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ImageGlass;

public partial class MainWindow
{
    /// <summary>
    /// Exit the app.
    /// </summary>
    public static void IG_Exit()
    {
        Application.Current.Exit();
    }


    /// <summary>
    /// Shows main menu.
    /// </summary>
    public void IG_OpenMainMenu()
    {
        _contentEl.ToolbarMain.OpenMainMenu();
    }


    /// <summary>
    /// Shows file picker to open a photo.
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
    /// Shows folder picker to open a photo folder. 
    /// </summary>
    public async Task IG_OpenFolderAsync()
    {
        var picker = new FolderPicker(AppWindow.Id);

        var dir = await picker.PickSingleFolderAsync();
        IG_OpenPath(dir?.Path);
    }


    /// <summary>
    /// Opens photo by the given path.
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
            IG_ViewByIndex(imageIndex);
        }
    }


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

        var step = photoIndex - AP.Photos.CurrentIndex;
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
        var photo = AP.Photos.GetByStep(step, true);
        ViewPhoto(photo);
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
        if (AP.Photos.Count == 0) return;

        var oldIndex = AP.Photos.CurrentIndex + 1;
        var result = await ModalWindow.ShowInputAsync(this,
            AP.Config.Lang[LangId.FrmMain_MnuGoTo],
            AP.Config.Lang[LangId.FrmMain_MnuGoTo_Description],
            inputValue: oldIndex.ToString(),
            acceptValue: TextBoxAcceptValue.UnsignedIntValueOnly);

        if (result.ExitCode != DialogExitCode.OK) return;


        if (int.TryParse(result.InputValue, out var newIndex))
        {
            newIndex--;

            if (newIndex != AP.Photos.CurrentIndex
                && 0 <= newIndex && newIndex < AP.Photos.Count)
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
        IG_ViewByIndex((int)AP.Photos.Count - 1);
    }


    /// <summary>
    /// Toggles the viewer's checkerboard mode.
    /// </summary>
    public void IG_ToggleCheckerboard(string? mode = null)
    {
        var isValid = Enum.TryParse<CheckerboardMode>(mode, out var wantedMode);

        IG_ToggleCheckerboard(isValid ? wantedMode : null);
    }


    /// <summary>
    /// Toggles the viewer's checkerboard mode.
    /// </summary>
    public void IG_ToggleCheckerboard(CheckerboardMode? mode = null)
    {
        var wantedMode = AP.Config.CheckerboardMode;

        if (mode is not null)
        {
            wantedMode = mode.Value;
        }
        else
        {
            switch (wantedMode)
            {
                case CheckerboardMode.None:
                    wantedMode = CheckerboardMode.Client;
                    break;
                case CheckerboardMode.Client:
                    wantedMode = CheckerboardMode.Image;
                    break;
                case CheckerboardMode.Image:
                    wantedMode = CheckerboardMode.None;
                    break;
                default:
                    break;
            }
        }

        Viewer.CheckerboardMode = AP.Config.CheckerboardMode = wantedMode;
    }



    /// <summary>
    /// Shows input dialog for custom zoom.
    /// </summary>
    public void IG_CustomZoom()
    {
        _ = IG_CustomZoomAsync();
    }

    /// <summary>
    /// Shows input dialog for custom zoom.
    /// </summary>
    public async Task IG_CustomZoomAsync()
    {
        var oldZoom = Math.Round(_contentEl.Viewer.ZoomFactor * 100f, 3);

        var result = await ModalWindow.ShowInputAsync(this,
            AP.Config.Lang[LangId.FrmMain_MnuCustomZoom],
            AP.Config.Lang[LangId.FrmMain_MnuCustomZoom_Description],
            inputValue: oldZoom.ToString(),
            thumbnailIcon: StockIconId.Find,
            acceptValue: TextBoxAcceptValue.UnsignedFloatValueOnly);

        if (result.ExitCode != DialogExitCode.OK) return;

        if (float.TryParse(result.InputValue.Trim(), out var newZoom))
        {
            _contentEl.Viewer.ZoomFactor = newZoom / 100f;
        }
    }


    /// <summary>
    /// Zoom to the current cursor location by the given factor.
    /// </summary>
    public void IG_SetZoom(string? factorStr)
    {
        if (!float.TryParse(factorStr, out var factor))
        {
            throw new ArgumentException($"""
                Zoom factor '{factorStr}' is not a valid float.
                
                ----------
                👉🏼 Method: {nameof(IG_SetZoom)}
                """,
                nameof(factorStr));
        }

        IG_SetZoom(factor);
    }

    /// <summary>
    /// Zoom to the current cursor location by the given factor.
    /// </summary>
    public void IG_SetZoom(float factor)
    {
        _ = _contentEl.Viewer.ZoomToPoint(factor);
    }

}
