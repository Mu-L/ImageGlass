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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.Common.Windows;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using ImageGlass.UI.Windowing;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Common.ServiceProviders;

public partial class AppAPIProvider
{
    private MainWindow _mainWindow;

    // variable to back up / restore window layout when changing window mode
    private bool _isFramelessBeforeFullscreen;
    private bool _isWindowFitBeforeFullscreen;
    private bool _showToolbar = true;
    private bool _showGallery = true;
    private Rect _windowBound;
    private bool _windowMaximized = false;


    private ViewerControl Viewer => _mainWindow.PART_MainView.PART_Viewer;
    private GalleryControl Gallery => _mainWindow.PART_MainView.PART_Gallery;
    private MessageControl Message => _mainWindow.PART_MainView.PART_Message;


    public AppAPIProvider(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }




    #region Main Menu APIs

    /// <summary>
    /// Shows main menu.
    /// </summary>
    public void IG_OpenMainMenu()
    {
        _mainWindow.PART_MainView.PART_Toolbar.PART_BtnMainMenu.OpenDropdownMenu();
    }


    /// <summary>
    /// Exit the app.
    /// </summary>
    public static void IG_Exit()
    {
        BHelper.ExitApp(false);
    }

    #endregion // Main Menu APIs



    #region File APIs

    /// <summary>
    /// Shows file picker to open a photo.
    /// </summary>
    public async Task IG_OpenFileAsync()
    {
        var supportFileExtPatterns = Core.Config.FileFormats.Select(ext => $"*{ext}")
            .ToImmutableList();

        var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuOpenFile],
            FileTypeFilter = [
                new FilePickerFileType(Core.Lang[LangId.FrmMain_OpenFileDialog]) {
                    Patterns = supportFileExtPatterns,
                },
            ],
        });

        var file = files?.ElementAtOrDefault(0);
        var filePath = file?.TryGetLocalPath();

        IG_OpenPath(filePath);
    }


    /// <summary>
    /// Shows folder picker to open a photo folder. 
    /// </summary>
    public async Task IG_OpenFolderAsync()
    {
        var dirs = await _mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());

        var dir = dirs?.ElementAtOrDefault(0);
        var dirPath = dir?.TryGetLocalPath();

        IG_OpenPath(dirPath);
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


    /// <summary>
    /// Saves and overrides the current photo.
    /// </summary>
    public async Task IG_SaveAsync()
    {
        var srcFilePath = Core.Photos.CurrentFilePath;
        var isOpeningImageList = Core.Photos.CurrentIndex > -1;

        // use Save As if no image is opened
        if (!isOpeningImageList && Core.ClipboardImage is not null)
        {
            await IG_SaveAsAsync();
            return;
        }


        // show override warning
        if (Core.Config.ShowSaveOverrideConfirmation)
        {
            var modal = await ModalWindow.ShowWarningAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuSave],
                Heading = Core.Lang[LangId.FrmMain_MnuSave_Confirm],
                Description = srcFilePath,
                Note = Core.Lang[LangId.FrmMain_MnuSave_ConfirmDescription],
                IsRememberOptionVisible = true,
                Thumbnail = Core.Photos.Current?.GalleryThumbnail,
            }, ModalWindowButton.Yes_No);

            // update ShowSaveOverrideConfirmation setting
            Core.Config.ShowSaveOverrideConfirmation = !modal.IsRememberOptionChecked;

            if (modal.ExitCode != DialogExitCode.OK) return;
        }

        await SaveImageAsync(srcFilePath);
    }


    /// <summary>
    /// Shows save file dialog to save photo to file.
    /// </summary>
    public async Task IG_SaveAsAsync()
    {
        var srcFilePath = string.Empty;
        var srcExt = ".png";
        IStorageFolder? initSaveDir = null;


        // 1. get dest file name
        if (Core.ClipboardImage is null)
        {
            srcFilePath = Core.Photos.CurrentFilePath;
            srcExt = Core.Photos.Current?.Extension?.ToLowerInvariant() ?? srcExt;


            if (Core.Config.OpenSaveAsDialogInTheCurrentImageDir)
            {
                var initSaveDirPath = Path.GetDirectoryName(srcFilePath);

                if (!string.IsNullOrWhiteSpace(initSaveDirPath))
                {
                    initSaveDir = await _mainWindow.StorageProvider.TryGetFolderFromPathAsync(initSaveDirPath);
                }
            }
        }

        var destFileName = string.IsNullOrEmpty(srcFilePath)
            ? $"untitle{srcExt}"
            : Path.GetFileNameWithoutExtension(srcFilePath);


        // 2. create file save picker
        var result = await _mainWindow.StorageProvider.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuSaveAs],
            FileTypeChoices = SavingExts.FilePickerFileTypeChoices,
            ShowOverwritePrompt = !Core.Config.ShowSaveOverrideConfirmation, // only show 1 prompt
            SuggestedStartLocation = initSaveDir,
            SuggestedFileName = destFileName,
            SuggestedFileType = SavingExts.LastSavedFileType,
        });

        SavingExts.LastSavedFileType = result.SelectedFileType;

        var destFilePath = result.File?.TryGetLocalPath() ?? string.Empty;
        if (string.IsNullOrEmpty(destFilePath)) return;


        // 3. show override warning
        if (File.Exists(destFilePath) && Core.Config.ShowSaveOverrideConfirmation)
        {
            var fi = new FileInfo(destFilePath);

            // show confirm dialog
            var modal = await ModalWindow.ShowWarningAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuSaveAs],
                Heading = Core.Lang[LangId.FrmMain_MnuSave_Confirm],
                Description = $"""
                {destFilePath}
                {BHelper.FormatSize(fi.Length)}
                """,
                Note = Core.Lang[LangId.FrmMain_MnuSave_ConfirmDescription],
                IsRememberOptionVisible = true,
            }, ModalWindowButton.Yes_No);

            // update ShowSaveOverrideConfirmation setting
            Core.Config.ShowSaveOverrideConfirmation = !modal.IsRememberOptionChecked;

            if (modal.ExitCode != DialogExitCode.OK) return;
        }


        // save file
        await SaveImageAsync(destFilePath);
    }


    /// <summary>
    /// Save the viewing image to file.
    ///   <para>
    ///     The source image is checked by this order:
    ///     <list type="number">
    ///       <item>Selected image area.</item>
    ///       <item><see cref="Core.ClipboardImage"/>.</item>
    ///       <item>Source <paramref name="srcFilePath"/> file.</item>
    ///     </list>
    ///   </para>
    /// </summary>
    /// <param name="destFilePath">Destination file path</param>
    /// <param name="srcFilePath">
    ///   Source file path.
    ///   <para>
    ///     <c>Note:**</c>
    ///     If it's empty, ImageGlass will check for the selection and clipboard image.
    ///   </para>
    /// </param>
    public async Task<bool> SaveImageAsync(string destFilePath)
    {
        var saveSource = ImageSaveSource.Undefined;
        var hasSrcPath = !string.IsNullOrEmpty(Core.Photos.CurrentFilePath);
        Exception? error = null;

        _ = Message.ShowAsync(destFilePath, Core.Lang[LangId.FrmMain_MnuSave_Saving]);


        // 1. save photo
        // 1.1 save the selection
        var hasSelection = Viewer.EnableSelection && !Viewer.SourceSelection.IsEmpty;
        if (hasSelection)
        {
            try
            {
                using var selectedBmp = Viewer.GetRenderedBitmap(true);
                var selectedImg = SkiaCodec.ToSKImage(selectedBmp)!;
                using var photo = new Photo(selectedImg);

                await photo.SaveAsAsync(destFilePath, new ImgTransform(), Core.Config.ImageEditQuality);
                saveSource = ImageSaveSource.SelectedArea;
            }
            catch (Exception ex) { error = ex; }
        }

        // 1.2 save the clipboard image
        else if (Core.ClipboardImage is not null)
        {
            try
            {
                await Core.ClipboardImage.SaveAsAsync(destFilePath, Core.ImageTransform, Core.Config.ImageEditQuality);
                saveSource = ImageSaveSource.Clipboard;
            }
            catch (Exception ex) { error = ex; }
        }

        // 1.3 save the image in the list
        else if (Core.Photos.Current is not null)
        {
            try
            {
                await Core.Photos.Current.SaveAsAsync(destFilePath, Core.ImageTransform, Core.Config.ImageEditQuality);
                saveSource = ImageSaveSource.CurrentFile;
            }
            catch (Exception ex) { error = ex; }
        }

        // 1.4 image is empty
        else
        {
            return false;
        }


        // 2. check for error
        if (error is not null)
        {
            await Message.ClearAsync();

            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuSave],
                Heading = Core.Lang[LangId.FrmMain_MnuSave_Error],
                Description = $"""
                {error.Source}:
                {error.Message}

                {destFilePath}
                """
            });

            return false;
        }


        // 3. check for success
        var newPhotoIndex = Core.Photos.IndexOf(destFilePath);
        if (newPhotoIndex == Core.Photos.CurrentIndex)
        {
            if (saveSource == ImageSaveSource.SelectedArea)
            {
                // reload to view the updated image
                IG_Reload();
            }
            else if (saveSource == ImageSaveSource.Clipboard)
            {
                // clear the clipboard image
                await LoadClipboardPhotoAsync(null);

                // reload to view the updated image
                IG_Reload();
            }
        }

        _ = Message.ShowAsync(destFilePath, Core.Lang[LangId.FrmMain_MnuSave_Success]);


        // 4. update thumbnail & metadata if file in the list was overriden
        var destPhoto = Core.Photos.Get(destFilePath);
        if (destPhoto is not null)
        {
            // reload thumbnail
            Gallery.LoadThumbnail(newPhotoIndex, false);
        }


        // 5. emits saved event
        Core.OnPhotoSaved(new(Core.Photos.CurrentFilePath, destFilePath, saveSource));

        return true;
    }


    /// <summary>
    /// Shows Open With window.
    /// </summary>
    public async Task IG_OpenWithAsync()
    {
        string? filePath = null;
        var isClipboardPhoto = Core.ClipboardImage is not null;


        if (isClipboardPhoto)
        {
            _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);

            // save clipboard photo as temp PNG file
            filePath = await Core.SavePhotoAsTempFileAsync();
        }
        else
        {
            filePath = Core.Photos.CurrentFilePath;
        }


        await Message.ClearAsync();
        if (!File.Exists(filePath))
        {
            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuOpenWith],
                Description = Core.Lang[LangId._CreatingFileError],
            });
        }
        else
        {
            try
            {
                if (BHelper.OS == OSType.Windows)
                {
                    // Uses the system shell32.dll 'OpenAs_RunDLL' entry point
                    var args = $"shell32.dll,OpenAs_RunDLL {filePath}";
                    _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = "rundll32.exe",
                        Arguments = args,
                        UseShellExecute = true,
                    });
                }
                else if (BHelper.OS == OSType.Mac)
                {
                    var script = $"tell application \"Finder\" to open POSIX file \"{filePath}\" using (choose application)";
                    var escapedScript = script.Replace("\"", "\\\"");

                    _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e \"{escapedScript}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    });
                }
                else if (BHelper.OS == OSType.Linux)
                {
                    // -n forces a menu choice if multiple apps exist
                    _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = "mimeopen",
                        Arguments = $"-n \"{filePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = false, // Often needs a terminal context for choice
                    });
                }
            }
            catch { }
        }
    }


    /// <summary>
    /// Shows Share dialog.
    /// </summary>
    public async Task IG_ShareAsync()
    {
        if (Core.ShareProvider is null) return;

        var filePath = Core.Photos.CurrentFilePath;

        // print clipboard image
        if (Core.ClipboardImage is not null)
        {
            _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);

            // save image to temp file
            filePath = await Core.SavePhotoAsTempFileAsync();
        }

        await Message.ClearAsync();

        if (!File.Exists(filePath))
        {
            _ = ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuShare],
                Description = Core.Lang[LangId._CreatingFileError],
            });
        }
        else
        {
            try
            {
                Core.ShareProvider.ShowShare(_mainWindow.Handle, [filePath]);
            }
            catch (Exception ex)
            {
                _ = ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
                {
                    Title = Core.Lang[LangId.FrmMain_MnuShare],
                    Description = $"{Core.Lang[LangId.FrmMain_MnuShare_Error]}\r\n\r\n{ex.Message}",
                });
            }
        }
    }


    /// <summary>
    /// Opens photo file location.
    /// </summary>
    public static void IG_OpenLocation()
    {
        BHelper.OpenFilePath(Core.Photos.CurrentFilePath);
    }


    /// <summary>
    /// Opens a popup to rename the current photo.
    /// </summary>
    public async Task IG_RenameAsync()
    {
        var oldFilePath = Core.Photos.CurrentFilePath;
        if (!File.Exists(oldFilePath)) return;

        var currentFolder = Path.GetDirectoryName(oldFilePath) ?? string.Empty;
        var ext = Path.GetExtension(oldFilePath);
        var newName = Path.GetFileNameWithoutExtension(oldFilePath);
        var title = Core.Lang[LangId.FrmMain_MnuRename];

        // 2. show popup
        var result = await ModalWindow.ShowInputAsync(_mainWindow, new ModalWindowOptions
        {
            Title = title,
            Description = $"""
            {oldFilePath}
            
            {Core.Lang[LangId.FrmMain_MnuRename_Description]}
            """,
            InputValue = newName,
            AcceptValue = TextBoxAcceptValue.FileNameValueOnly,
            ThumbnailIcon = StockIconId.Rename,
            Thumbnail = Core.Photos.Current?.GalleryThumbnail,
        });

        if (result.ExitCode != DialogExitCode.OK || string.IsNullOrWhiteSpace(result.InputValue)) return;

        // 3. get new photo name
        newName = $"{result.InputValue.Trim()}{ext}";
        var newFilePath = Path.Combine(currentFolder, newName);


        // 4. perform renaming
        try
        {
            // Issue 73: Windows ignores case-only changes
            if (string.Equals(oldFilePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            {
                // user changing only the case of the filename. Need to perform a trick.
                File.Move(oldFilePath, oldFilePath + "_temp");
                File.Move(oldFilePath + "_temp", newFilePath);
            }
            else
            {
                File.Move(oldFilePath, newFilePath);
            }


            // TODO:
            Core.Photos.SetFilePath(Core.Photos.CurrentIndex, newFilePath);

            //// manually update the change if FileWatcher is not enabled
            //if (!Core.Config.EnableRealTimeFileUpdate)
            //{
            //    Core.Photos.SetFileName(Core.Photos.CurrentIndex, newFilePath);

            //    Gallery.Items[Local.CurrentIndex].Rename(newFilePath);
            //    LoadImageInfo(ImageInfoUpdateTypes.Name | ImageInfoUpdateTypes.Path);
            //}
        }
        catch (Exception ex)
        {
            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Description = ex.Message,
            });
        }
    }


    /// <summary>
    /// Sends or permenantly deletes the current image.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public async Task IG_DeleteAsync(string? moveToRecycleBinStr = "true")
    {
        var moveToRecycleBin = BHelper.ConvertStringToBool(moveToRecycleBinStr) ?? true;
        await IG_DeleteAsync(moveToRecycleBin);
    }


    /// <summary>
    /// Sends or permenantly deletes the current image.
    /// </summary>
    public async Task IG_DeleteAsync(bool moveToRecycleBin = true)
    {
        var filePath = Core.Photos.CurrentFilePath;
        if (!File.Exists(filePath)) return;

        var canDelete = true;
        var title = moveToRecycleBin
            ? Core.Lang[LangId.FrmMain_MnuMoveToRecycleBin]
            : Core.Lang[LangId.FrmMain_MnuDeleteFromHardDisk];


        // 1. show confirm dialog
        if (Core.Config.ShowDeleteConfirmation)
        {
            var heading = moveToRecycleBin
                ? Core.Lang[LangId.FrmMain_MnuMoveToRecycleBin_Description]
                : Core.Lang[LangId.FrmMain_MnuDeleteFromHardDisk_Description];
            var thumbnailIcon = moveToRecycleBin
                ? StockIconId.RecycleBin
                : StockIconId.Delete;

            var modal = await ModalWindow.ShowWarningAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Heading = heading,
                Description = $"""
                {filePath}
                {Core.Photos.Current?.Metadata.FileSizeFormatted}
                """,
                IsRememberOptionVisible = true,
                ThumbnailIcon = thumbnailIcon,
                Thumbnail = Core.Photos.Current?.GalleryThumbnail,
            }, ModalWindowButton.Yes_No);

            // update remember confirm setting
            Core.Config.ShowDeleteConfirmation = !modal.IsRememberOptionChecked;

            canDelete = modal.ExitCode == DialogExitCode.OK;
        }


        // 2. delete photo
        if (!canDelete) return;
        Core.IsBusy = true;

        try
        {
            await IG_UnloadAsync();
            BHelper.DeleteFile(filePath, moveToRecycleBin);

            // manually update the change because FileWatcher is disabled when IsBusy = true
            Core.Photos.Remove(Core.Photos.CurrentFilePath);
            var nextIndex = (int)Math.Min(Core.Photos.Count - 1, Core.Photos.CurrentIndex);
            var nextPhoto = Core.Photos.Select(nextIndex);
            _ = _mainWindow.PART_MainView.ViewPhotoAsync(nextPhoto);
        }
        catch (Exception ex)
        {
            await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Description = ex.Message,
            });
        }

        Core.IsBusy = false;
    }


    /// <summary>
    /// Opens photo's Properties dialog.
    /// </summary>
    public void IG_OpenProperties()
    {
        Core.ShellProvider?.ShowFileProperties(Core.Photos.CurrentFilePath, _mainWindow.Handle);
    }

    #endregion // File APIs



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



    #region Zoom APIs

    /// <summary>
    /// Shows input dialog for custom zoom.
    /// </summary>
    public async Task IG_CustomZoomAsync()
    {
        var oldZoom = Math.Round(Viewer.ZoomFactor * 100f, 3);

        var result = await ModalWindow.ShowInputAsync(_mainWindow, new ModalWindowOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuCustomZoom],
            Description = Core.Lang[LangId.FrmMain_MnuCustomZoom_Description],
            InputValue = oldZoom.ToString(),
            AcceptValue = TextBoxAcceptValue.UnsignedFloatValueOnly,
            ThumbnailIcon = StockIconId.Find,
        });

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
        if (mode == Core.Config.ZoomMode)
        {
            IG_Refresh();
        }
        else
        {
            Core.Config.ZoomMode = mode;
        }
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
        Viewer.StartDrawingAnimation(AnimationSources.ZoomIn, 100);
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
        Viewer.StartDrawingAnimation(AnimationSources.ZoomOut, 100);
    }

    #endregion // Zoom APIs



    #region Panning APIs

    /// <summary>
    /// Pans the viewing image to left.
    /// </summary>
    public void IG_PanLeft()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanLeft, 100);
    }


    /// <summary>
    /// Pans the viewing image to right.
    /// </summary>
    public void IG_PanRight()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanRight, 100);
    }


    /// <summary>
    /// Pans the viewing image to top.
    /// </summary>
    public void IG_PanUp()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanUp, 100);
    }


    /// <summary>
    /// Pans the viewing image to bottom.
    /// </summary>
    public void IG_PanDown()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanDown, 100);
    }


    /// <summary>
    /// Pans the viewing image to left side.
    /// </summary>
    public void IG_PanToLeft()
    {
        var distanceX = Viewer.SrcRect.X * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceX / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanLeft, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to right side.
    /// </summary>
    public void IG_PanToRight()
    {
        var x = Viewer.BitmapSize.Width - Viewer.SrcRect.Width;
        var distanceX = (x + Viewer.SrcRect.X) * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceX / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanRight, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to top.
    /// </summary>
    public void IG_PanToTop()
    {
        var distanceY = Viewer.SrcRect.Y * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceY / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanUp, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to bottom.
    /// </summary>
    public void IG_PanToBottom()
    {
        var y = Viewer.BitmapSize.Height - Viewer.SrcRect.Height;
        var distanceY = (y + Viewer.SrcRect.Y) * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceY / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanDown, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }

    #endregion // Panning APIs



    #region Image APIs

    /// <summary>
    /// Refreshes image viewport.
    /// </summary>
    public void IG_Refresh()
    {
        Viewer.Refresh(true, false, Core.Config.EnableWindowFit);
    }


    /// <summary>
    /// Reloads image file.
    /// </summary>
    public void IG_Reload()
    {
        _ = _mainWindow.PART_MainView.ViewPhotoAsync(Core.Photos.Current, useCache: false);

        // reload thumbnail
        Gallery.LoadThumbnail(Core.Photos.CurrentIndex, false);
    }


    /// <summary>
    /// Reloads images list.
    /// </summary>
    public void IG_ReloadList()
    {
        _mainWindow.PART_MainView.PrepareLoadPhotoList(Core.Photos.DistinctDirs,
            Core.Photos.CurrentFilePath, disposeForegroundShell: false, loadInitPhoto: false);
    }


    /// <summary>
    /// Unloads the current photo.
    /// </summary>
    public async Task IG_UnloadAsync()
    {
        var args = new PhotoUnloadedEventArgs()
        {
            IsClipboardPhoto = Core.ClipboardImage is not null,
            Index = Core.Photos.CurrentIndex,
            FilePath = Core.Photos.CurrentFilePath,
        };


        // 1. unload clipboard photo
        if (args.IsClipboardPhoto)
        {
            await LoadClipboardPhotoAsync(null);

            // show the current photo in the list
            await _mainWindow.PART_MainView.ViewPhotoAsync(Core.Photos.Current);
        }

        // 2. unload photo from the list
        else
        {
            // cancel loading the current image
            Core.Photos.Current?.CancelLoading();

            await _mainWindow.PART_MainView.ViewPhotoAsync(null, false);
            Core.Photos.Current?.Unload();
        }

        // raise unloaded event
        Core.OnPhotoUnloaded(args);
    }


    /// <summary>
    /// Sets the viewing photo as desktop wallpaper.
    /// </summary>
    public async Task IG_SetDesktopBackgroundAsync()
    {
        //await SetSystemBackgroundAsync__(false, WallpaperStyle.Current);
    }


    /// <summary>
    /// Sets the viewing photo as lock screen image.
    /// </summary>
    public async Task IG_SetLockScreenImageAsync()
    {
        //await SetSystemBackgroundAsync__(true);
    }


    ///// <summary>
    ///// Sets the current photo as system background.
    ///// </summary>
    ///// <param name="forLockScreen">
    ///// <c>true</c>: For lock screen image, <c>false</c>: for desktop wallpaper
    ///// </param>
    ///// <param name="style">Desktop wallpaper style</param>
    //private async Task SetSystemBackgroundAsync__(bool forLockScreen, WallpaperStyle style = WallpaperStyle.Current)
    //{
    //    if (Viewer.SourceKind == PhotoSource.None) return;

    //    var filePath = Core.Photos.CurrentFilePath;
    //    var ext = Core.Photos.Current?.Extension.ToLowerInvariant() ?? string.Empty;
    //    _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);

    //    var title = forLockScreen
    //        ? Core.Lang[LangId.FrmMain_MnuSetLockScreen]
    //        : Core.Lang[LangId.FrmMain_MnuSetDesktopBackground];



    //    // 1. create temp image if needed
    //    if (Core.ClipboardImage is not null || !_desktopNativeFormats.Contains(ext))
    //    {
    //        // save image to temp file
    //        filePath = await Core.SavePhotoAsTempFileAsync(".jpg");
    //    }
    //    await Message.ClearAsync();


    //    // 2. check if file path is valid
    //    if (!File.Exists(filePath))
    //    {
    //        _ = await ModalWindow.ShowErrorAsync(this,
    //            title: title,
    //            description: Core.Lang[LangId._CreatingFileError]);

    //        return;
    //    }


    //    // 3. set background
    //    try
    //    {
    //        if (forLockScreen)
    //        {
    //            var sFile = await StorageFile.GetFileFromPathAsync(filePath);
    //            await LockScreen.SetImageFileAsync(sFile);
    //        }
    //        else
    //        {
    //            DesktopApi.SetWallpaper(filePath, style);
    //        }


    //        var successMsg = forLockScreen
    //            ? Core.Lang[LangId.FrmMain_MnuSetLockScreen_Success]
    //            : Core.Lang[LangId.FrmMain_MnuSetDesktopBackground_Success];

    //        _ = Message.ShowAsync(successMsg);
    //    }
    //    catch (Exception ex)
    //    {
    //        var heading = forLockScreen
    //            ? Core.Lang[LangId.FrmMain_MnuSetLockScreen_Error]
    //            : Core.Lang[LangId.FrmMain_MnuSetDesktopBackground_Error];

    //        _ = await ModalWindow.ShowErrorAsync(this,
    //            title: title,
    //            description: ex.Message,
    //            heading: heading);
    //    }
    //}


    #endregion // Image APIs



    #region Clipboard APIs

    /// <summary>
    /// Opens image from clipboard.
    /// </summary>
    private async Task IG_PasteImageAsync()
    {
        if (_mainWindow.Clipboard is null) return;

        using var data = await _mainWindow.Clipboard.TryGetDataAsync();
        if (data is null) return;


        // 1. if clipboard contains a file
        if (data.Contains(DataFormat.File))
        {
            var fileItem = await data.TryGetFileAsync();
            var filePath = fileItem?.TryGetLocalPath();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                IG_OpenPath(filePath);
            }
            return;
        }


        // 2. if clipboard contains image pixels
        if (data.Contains(DataFormat.Bitmap))
        {
            using var abmp = await data.TryGetBitmapAsync();
            var skBmp = SkiaCodec.FromBitmap(abmp);
            if (skBmp is not null)
            {
                var photo = new Photo(skBmp);
                await LoadClipboardPhotoAsync(photo);
            }

            return;
        }


        // 3. if clipboard contains file path
        if (data.Contains(DataFormat.Text))
        {
            var text = await data.TryGetTextAsync();
            var path = BHelper.ResolvePath(text);

            // 3.1 try to get absolute path
            if (File.Exists(path) || Directory.Exists(path))
            {
                IG_OpenPath(path);
                return;
            }


            // 3.2 get photo from base64 string 
            var photo = await MagickCodec.DecodeBase64Async(text);
            if (photo is not null)
            {
                await LoadClipboardPhotoAsync(photo);
            }
        }

    }


    private async Task LoadClipboardPhotoAsync(Photo? photo)
    {
        // cancel the current loading image
        Core.Photos.Current?.CancelLoading();

        await _mainWindow.PART_MainView.ViewPhotoAsync(photo, true, false);

        Core.ClipboardImage = photo;
    }


    /// <summary>
    /// Copies image pixels.
    /// </summary>
    public async Task IG_CopyImagePixelsAsync()
    {
        if (Viewer.SourceKind == PhotoSource.None || _mainWindow.Clipboard is null) return;

        // 1. get rendered bitmap
        var bmp = Viewer.GetRenderedBitmap(!Viewer.SourceSelection.IsEmpty);
        if (bmp.IsDisposed()) return;


        // 2. show message
        await Message.ClearAsync();
        _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuCopyImagePixels_Copying], delayMs: 1000);


        // 3. copy to clipboard
        try
        {
            var abmp = SkiaCodec.ToWritableBitmap(bmp);
            await _mainWindow.Clipboard.SetBitmapAsync(abmp);

            _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuCopyImagePixels_Success]);
        }
        catch (Exception ex)
        {
            await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuCopyImagePixels],
                Description = ex.Message,
            });
        }
    }


    /// <summary>
    /// Copy the current image path.
    /// </summary>
    public void IG_CopyImagePathAsync()
    {
        if (string.IsNullOrWhiteSpace(Core.Photos.CurrentFilePath)) return;

        try
        {
            _mainWindow.Clipboard?.SetTextAsync(Core.Photos.CurrentFilePath);

            // show message
            _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuCopyPath_Success]);
        }
        catch { }
    }


    /// <summary>
    /// Copies the current photo file.
    /// </summary>
    public async Task IG_CopyFilesAsync()
    {
        await SetFileToClipboardAsync(Core.Photos.CurrentFilePath, false);
    }


    /// <summary>
    /// Cuts the current photo file.
    /// </summary>
    public async Task IG_CutFilesAsync()
    {
        await SetFileToClipboardAsync(Core.Photos.CurrentFilePath, true);
    }


    /// <summary>
    /// Sets file to clipboard
    /// </summary>
    private async Task SetFileToClipboardAsync(string? filePath, bool forCutting)
    {
        if (_mainWindow.Clipboard is null || !File.Exists(filePath)) return;

        // 1. cut/copy single file
        if (forCutting)
        {
            if (!Core.Config.EnableCutMultipleFiles)
            {
                Core.StringClipboard.Clear();
            }
        }
        else
        {
            if (!Core.Config.EnableCopyMultipleFiles)
            {
                Core.StringClipboard.Clear();
            }
        }


        // 2. try adding current photo path to clipboard paths
        _ = Core.StringClipboard.Add(filePath);


        // 3. set files to clipboard
        try
        {
            var dt = new DataTransfer();
            foreach (var path in Core.StringClipboard)
            {
                var fi = await _mainWindow.StorageProvider.TryGetFileFromPathAsync(path);
                if (fi is null) continue;

                var dti = new DataTransferItem();
                dti.SetFile(fi);
                dt.Add(dti);
            }


            // 4. perform copy/cut
            await _mainWindow.Clipboard.SetDataAsync(dt);

            // permanently adds the data that is on the Clipboard so that it is available
            // after the data's original application closes.
            await _mainWindow.Clipboard.FlushAsync();

            _ = Message.ShowAsync(Core.Lang[forCutting
                    ? LangId.FrmMain_MnuCutFile_Success
                    : LangId.FrmMain_MnuCopyFile_Success,
                Core.StringClipboard.Count]);
        }
        catch (Exception ex)
        {
            await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[forCutting ? LangId.FrmMain_MnuCutFile : LangId.FrmMain_MnuCopyFile],
                Description = ex.Message,
            });
        }
    }


    /// <summary>
    /// Clears clipboard.
    /// </summary>
    public async Task IG_ClearClipboardAsync()
    {
        // clear clipboard
        Core.StringClipboard.Clear();

        if (_mainWindow.Clipboard is not null)
        {
            await _mainWindow.Clipboard.ClearAsync();
        }


        // show message
        _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuClearClipboard_Success]);
    }

    #endregion // Clipboard APIs



    #region Window Mode APIs

    /// <summary>
    /// Toggles window fit mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public static void IG_ToggleWindowFit(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleWindowFit(enabled);
    }


    /// <summary>
    /// Toggles window fit mode.
    /// </summary>
    public static void IG_ToggleWindowFit(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableWindowFit;
        Core.Config.EnableWindowFit = enabled.Value;

        // TODO:
    }


    /// <summary>
    /// Toggles frameless mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleFrameless(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleFrameless(enabled);
    }


    /// <summary>
    /// Toggles frameless mode.
    /// </summary>
    public void IG_ToggleFrameless(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableFrameless;

        // set frameless mode
        SetFramelessMode__(enabled.Value, true);
    }
    private void SetFramelessMode__(bool enabled, bool showMessage)
    {
        Core.Config.EnableFrameless = enabled;


        // set frameless mode
        if (enabled)
        {
            // exit full screen
            if (Core.Config.EnableFullScreen) IG_ToggleFullScreen(false);

            _mainWindow.IsFrameless = true;
        }

        // restore frame
        else
        {
            _mainWindow.IsFrameless = false;
        }


        // show message
        if (showMessage && enabled)
        {
            _ = Message.ShowAsync(
                Core.Lang[LangId.FrmMain_MnuFrameless_EnableDescription],
                Core.Lang[LangId.FrmMain_MnuFrameless]);
        }
    }


    /// <summary>
    /// Toggles fullscreen mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleFullScreen(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleFullScreen(enabled);
    }


    /// <summary>
    /// Toggles fullscreen mode.
    /// </summary>
    public void IG_ToggleFullScreen(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableFullScreen;
        SetFullScreenMode__(enabled.Value, Core.Config.HideToolbarInFullscreen, Core.Config.HideGalleryInFullscreen);
    }
    private void SetFullScreenMode__(bool enabled, bool hideToolbar = false, bool hideThumbnails = false)
    {
        Core.Config.EnableFullScreen = enabled;

        // enable full screen mode
        if (enabled)
        {
            // exit window fit
            if (Core.Config.EnableWindowFit)
            {
                _isWindowFitBeforeFullscreen = true;
                IG_ToggleWindowFit(false);
            }

            // exit frameless
            if (Core.Config.EnableFrameless)
            {
                _isFramelessBeforeFullscreen = true;
                SetFramelessMode__(false, false);
            }

            // back up layout & window state
            _windowBound = _mainWindow.Bounds;
            _windowMaximized = _mainWindow.WindowState == WindowState.Maximized;
            _showToolbar = Core.Config.ShowToolbar;
            _showGallery = Core.Config.ShowGallery;


            // enable fullscreen
            _mainWindow.WindowState = WindowState.FullScreen;


            // hide toolbar & gallery
            if (hideToolbar) IG_ToggleToolbar(false);
            if (hideThumbnails) _ = IG_ToggleGalleryAsync(false);
        }

        // disable full screen mode
        else
        {
            // restore layout
            IG_ToggleToolbar(_showToolbar);
            _ = IG_ToggleGalleryAsync(_showGallery);


            // restore window state, size, position
            Core.Config.IsMainWindowMaximized = _windowMaximized;
            _mainWindow.WindowState = _windowMaximized
                ? WindowState.Maximized
                : WindowState.Normal;


            // restore frameless, window fit mode when exiting full screen
            if (_isFramelessBeforeFullscreen) SetFramelessMode__(true, false);
            if (_isWindowFitBeforeFullscreen) IG_ToggleWindowFit(true);
        }
    }


    /// <summary>
    /// Toggles slideshow mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public static void IG_ToggleSlideshow(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleSlideshow(enabled);
    }


    /// <summary>
    /// Toggles slideshow mode.
    /// </summary>
    public static void IG_ToggleSlideshow(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableSlideshow;
        Core.Config.EnableSlideshow = enabled.Value;

        // TODO:
    }


    #endregion // Window Modes APIs



    #region Layout APIs

    /// <summary>
    /// Toggles visibility of toolbar.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public static void IG_ToggleToolbar(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleToolbar(enabled);
    }


    /// <summary>
    /// Toggles visibility of toolbar.
    /// </summary>
    public static void IG_ToggleToolbar(bool? enabled = null)
    {
        enabled ??= !Core.Config.ShowToolbar;
        Core.Config.ShowToolbar = enabled.Value;

        // TODO: update window fit
    }


    /// <summary>
    /// Toggles visibility of gallery.
    /// </summary>
    public async Task IG_ToggleGalleryAsync(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        await IG_ToggleGalleryAsync(enabled);
    }


    /// <summary>
    /// Toggles visibility of gallery
    /// </summary>
    public async Task IG_ToggleGalleryAsync(bool? enabled = null)
    {
        enabled ??= !Core.Config.ShowGallery;
        Core.Config.ShowGallery = enabled.Value;

        if (enabled.Value)
        {
            Gallery.ScrollToItem(Core.Photos.CurrentIndex);
        }

        // TODO: update window fit
    }


    /// <summary>
    /// Toggles the viewer's checkerboard mode.
    /// </summary>
    public void IG_ToggleCheckerboard(string? mode = null)
    {
        var isValid = Enum.TryParse<CheckerboardType>(mode, out var wantedMode);

        IG_ToggleCheckerboard(isValid ? wantedMode : null);
    }


    /// <summary>
    /// Toggles the viewer's checkerboard mode.
    /// </summary>
    public static void IG_ToggleCheckerboard(CheckerboardType? mode = null)
    {
        var wantedMode = Core.Config.CheckerboardMode;

        if (mode is not null)
        {
            wantedMode = mode.Value;
        }
        else
        {
            switch (wantedMode)
            {
                case CheckerboardType.None:
                    wantedMode = CheckerboardType.Client;
                    break;
                case CheckerboardType.Client:
                    wantedMode = CheckerboardType.Image;
                    break;
                case CheckerboardType.Image:
                    wantedMode = CheckerboardType.None;
                    break;
                default:
                    break;
            }
        }

        Core.Config.CheckerboardMode = wantedMode;
    }


    /// <summary>
    /// Toggles window top most.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleWindowTopMost(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleWindowTopMost(enabled);
    }


    /// <summary>
    /// Toggles window top most.
    /// </summary>
    public void IG_ToggleWindowTopMost(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableWindowTopMost;
        Core.Config.EnableWindowTopMost = enabled.Value;

        _ = Message.ShowAsync(Core.Lang[enabled.Value
            ? LangId.FrmMain_MnuToggleTopMost_Enable
            : LangId.FrmMain_MnuToggleTopMost_Disable]);
    }


    #endregion // Layout APIs




}
