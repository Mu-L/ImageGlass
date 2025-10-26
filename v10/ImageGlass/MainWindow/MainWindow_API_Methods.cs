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
using ImageGlass.Common.Photoing;
using ImageGlass.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

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
            PrepareLoadPhotoList([path],
                currentFilePath: null, disposeForegroundShell: false, loadInitPhoto: true);
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
        _ = ViewPhotoAsync(photo);
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
        var oldZoom = Math.Round(Viewer.ZoomFactor * 100f, 3);

        var result = await ModalWindow.ShowInputAsync(this,
            AP.Config.Lang[LangId.FrmMain_MnuCustomZoom],
            AP.Config.Lang[LangId.FrmMain_MnuCustomZoom_Description],
            inputValue: oldZoom.ToString(),
            thumbnailIcon: StockIconId.Find,
            acceptValue: TextBoxAcceptValue.UnsignedFloatValueOnly);

        if (result.ExitCode != DialogExitCode.OK) return;

        if (float.TryParse(result.InputValue.Trim(), out var newZoom))
        {
            Viewer.ZoomFactor = newZoom / 100f;
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
        _ = Viewer.ZoomToPoint(factor);
    }


    /// <summary>
    /// Sets the zoom mode value.
    /// </summary>
    public void IG_SetZoomMode(string? modeStr)
    {
        if (!Enum.TryParse<ZoomMode>(modeStr, out var mode))
        {
            throw new ArgumentException($"""
                '{modeStr}' is not a valid zoom mode.
                
                ----------
                👉🏼 Method: {nameof(IG_SetZoomMode)}
                """,
                nameof(modeStr));
        }

        IG_SetZoomMode(mode);
    }

    /// <summary>
    /// Sets the zoom mode value.
    /// </summary>
    public void IG_SetZoomMode(ZoomMode mode)
    {
        Viewer.ZoomMode = AP.Config.ZoomMode = mode;
    }


    /// <summary>
    /// Start drawing animation.
    /// </summary>
    public void IG_Animate(AnimationSources source, int durationMs = 100, Action? callbackFn = null)
    {
        Viewer.StartDrawingAnimation(source);

        BHelper.Debounce(durationMs, () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Viewer.StopDrawingAnimation(source);
                callbackFn?.Invoke();
            });
        });
    }


    /// <summary>
    /// Zooms into the image.
    /// </summary>
    public void IG_ZoomIn()
    {
        if (Viewer.ZoomLevels.Length > 0)
        {
            Viewer.ZoomIn();
            return;
        }

        // smooth zooming
        IG_Animate(AnimationSources.ZoomIn);
    }


    /// <summary>
    /// Zooms out of the image.
    /// </summary>
    public void IG_ZoomOut()
    {
        if (Viewer.ZoomLevels.Length > 0)
        {
            Viewer.ZoomOut();
            return;
        }

        // smooth zooming
        IG_Animate(AnimationSources.ZoomOut);
    }


    #region Panning APIs

    /// <summary>
    /// Pans the viewing image to left.
    /// </summary>
    public void IG_PanLeft()
    {
        // smooth zooming
        IG_Animate(AnimationSources.PanLeft);
    }


    /// <summary>
    /// Pans the viewing image to right.
    /// </summary>
    public void IG_PanRight()
    {
        // smooth zooming
        IG_Animate(AnimationSources.PanRight);
    }


    /// <summary>
    /// Pans the viewing image to top.
    /// </summary>
    public void IG_PanUp()
    {
        // smooth zooming
        IG_Animate(AnimationSources.PanUp);
    }


    /// <summary>
    /// Pans the viewing image to bottom.
    /// </summary>
    public void IG_PanDown()
    {
        // smooth zooming
        IG_Animate(AnimationSources.PanDown);
    }


    /// <summary>
    /// Pans the viewing image to left side.
    /// </summary>
    public void IG_PanToLeft()
    {
        var distanceX = Viewer.DrawingSrcRect.X;
        Viewer.PanSpeed = distanceX * Viewer.ZoomFactor / 10;

        IG_Animate(AnimationSources.PanLeft, 200, () =>
        {
            Viewer.PanSpeed = AP.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to right side.
    /// </summary>
    public void IG_PanToRight()
    {
        var x = Viewer.BitmapSize.Width - Viewer.DrawingSrcRect.Width;
        var distanceX = x + Viewer.DrawingSrcRect.X;
        Viewer.PanSpeed = distanceX * Viewer.ZoomFactor / 10;

        IG_Animate(AnimationSources.PanRight, 200, () =>
        {
            Viewer.PanSpeed = AP.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to top.
    /// </summary>
    public void IG_PanToTop()
    {
        var distanceY = Viewer.DrawingSrcRect.Y;
        Viewer.PanSpeed = distanceY * Viewer.ZoomFactor / 10;

        IG_Animate(AnimationSources.PanUp, 200, () =>
        {
            Viewer.PanSpeed = AP.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to bottom.
    /// </summary>
    public void IG_PanToBottom()
    {
        var y = Viewer.BitmapSize.Height - Viewer.DrawingSrcRect.Height;
        var distanceY = y + Viewer.DrawingSrcRect.Y;
        Viewer.PanSpeed = distanceY * Viewer.ZoomFactor / 10;

        IG_Animate(AnimationSources.PanDown, 200, () =>
        {
            Viewer.PanSpeed = AP.Config.PanSpeed;
        });
    }

    #endregion // Panning APIs


    /// <summary>
    /// Refreshes image viewport.
    /// </summary>
    public void IG_Refresh()
    {
        Viewer.Refresh(true, false, AP.Config.EnableWindowFit);
    }


    /// <summary>
    /// Reloads image file.
    /// </summary>
    public void IG_Reload()
    {
        var photo = AP.Photos.Get(AP.Photos.CurrentIndex);
        _ = ViewPhotoAsync(photo, useCache: false);
    }


    /// <summary>
    /// Reloads images list.
    /// </summary>
    public void IG_ReloadList()
    {
        PrepareLoadPhotoList(AP.Photos.DistinctDirs,
            AP.Photos.CurrentFilePath, disposeForegroundShell: false, loadInitPhoto: false);
    }


    #region Clipboard APIs

    /// <summary>
    /// Opens image from clipboard.
    /// </summary>
    private async Task IG_PasteImageAsync()
    {
        var data = Clipboard.GetContent();

        // 1. if clipboard contains a file
        if (data.Contains(StandardDataFormats.StorageItems))
        {
            var fileItems = await data.GetStorageItemsAsync();
            var filePath = fileItems[0].Path;

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                PrepareLoadPhotoList([filePath],
                    currentFilePath: filePath, disposeForegroundShell: true, loadInitPhoto: true);
            }
            return;
        }


        // 2. if clipboard contains image pixels
        if (data.Contains(StandardDataFormats.Bitmap))
        {
            var wicBmp = await BHelper.GetClipboardImageAsync();
            if (wicBmp is not null)
            {
                //var meta = await MagickDecoder.LoadMetadataAsync();
                var photo = new Photo(wicBmp, new PhotoMetadata()
                {
                    Width = (uint)wicBmp.Size.Width,
                    Height = (uint)wicBmp.Size.Height,
                    FrameCount = 1,
                });

                await LoadClipboardPhotoAsync(photo);
            }
            return;
        }


        // 3. if clipboard contains file path
        if (data.Contains(StandardDataFormats.Text))
        {
            var text = await data.GetTextAsync();
            var path = BHelper.ResolvePath(text);

            // 3.1 try to get absolute path
            if (File.Exists(path) || Directory.Exists(path))
            {
                PrepareLoadPhotoList([path],
                    currentFilePath: null, disposeForegroundShell: true, loadInitPhoto: true);
                return;
            }


            // 3.2 get photo from base64 string 
            var photo = await MagickDecoder.DecodeBase64Async(text);
            if (photo is not null)
            {
                await LoadClipboardPhotoAsync(photo);
            }
        }

    }


    private async Task LoadClipboardPhotoAsync(Photo? photo)
    {
        // cancel the current loading image
        AP.Photos.Current?.CancelLoading();

        await ViewPhotoAsync(photo, true, false);

        AP.ClipboardImage = photo;
    }


    /// <summary>
    /// Copies image pixels.
    /// </summary>
    public async Task IG_CopyImagePixelsAsync()
    {
        if (Viewer.SourceKind == PhotoSource.None) return;

        // 1. get rendered bitmap
        var wicBmp = Viewer.GetRenderedBitmap();
        if (wicBmp.IsDisposed()) return;

        // 2. show message
        await _contentEl.ShowMessageAsync(null);
        _ = _contentEl.ShowMessageAsync(AP.Config.Lang[LangId.FrmMain_MnuCopyImagePixels_Copying], delayMs: 1000);

        // 3. copy the selected area
        if (!Viewer.SourceSelection.IsEmpty())
        {
            // TODO:
        }

        // 4. copy to clipboard
        var success = await Task.Run(async () => await BHelper.SetClipboardImageAsync(wicBmp));
        if (success)
        {
            _ = _contentEl.ShowMessageAsync(AP.Config.Lang[LangId.FrmMain_MnuCopyImagePixels_Success]);
        }
    }


    /// <summary>
    /// Copy the current image path.
    /// </summary>
    public void IG_CopyImagePath()
    {
        if (string.IsNullOrWhiteSpace(AP.Photos.CurrentFilePath)) return;

        try
        {
            var data = new DataPackage();
            data.SetText(AP.Photos.CurrentFilePath);

            Clipboard.SetContent(data);

            // show message
            _ = _contentEl.ShowMessageAsync(AP.Config.Lang[LangId.FrmMain_MnuCopyPath_Success]);
        }
        catch { }
    }


    /// <summary>
    /// Copies the current photo file.
    /// </summary>
    public async Task IG_CopyFilesAsync()
    {
        await SetFileToClipboardAsync(AP.Photos.CurrentFilePath, false);
    }


    /// <summary>
    /// Cuts the current photo file.
    /// </summary>
    public async Task IG_CutFilesAsync()
    {
        await SetFileToClipboardAsync(AP.Photos.CurrentFilePath, true);
    }


    /// <summary>
    /// Sets file to clipboard
    /// </summary>
    private async Task SetFileToClipboardAsync(string? filePath, bool forCutting)
    {
        if (!File.Exists(filePath)) return;

        // 1. cut/copy single file
        if (forCutting)
        {
            if (!AP.Config.EnableCutMultipleFiles)
            {
                AP.StringClipboard.Clear();
            }
        }
        else
        {
            if (!AP.Config.EnableCopyMultipleFiles)
            {
                AP.StringClipboard.Clear();
            }
        }


        // 2. try adding current photo path to clipboard paths
        _ = AP.StringClipboard.Add(filePath);


        // 3. set files to clipboard
        var success = await BHelper.SetClipboardFilesAsync(AP.StringClipboard.ToArray(), forCutting);


        // 4. show message
        if (success)
        {
            _ = _contentEl.ShowMessageAsync(
                AP.Config.Lang[forCutting
                    ? LangId.FrmMain_MnuCutFile_Success
                    : LangId.FrmMain_MnuCopyFile_Success,
                AP.StringClipboard.Count]);
        }

    }


    /// <summary>
    /// Clears clipboard.
    /// </summary>
    public async Task IG_ClearClipboardAsync()
    {
        // clear clipboard
        AP.StringClipboard.Clear();
        await BHelper.ClearClipboardAsync();

        // show message
        _ = _contentEl.ShowMessageAsync(AP.Config.Lang[LangId.FrmMain_MnuClearClipboard_Success]);
    }

    #endregion // Clipboard APIs



}
