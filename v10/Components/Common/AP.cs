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
using D2Phap;
using ImageGlass.Common.Photoing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common;

public static class AP
{
    public static event EventHandler<ThemePackChangedEventArgs>? ThemeChanged;
    public static event EventHandler? LanguageChanged;
    public static event EventHandler<PhotoUnloadedEventArgs>? PhotoUnloaded;
    public static event EventHandler<PhotoSaveEventArgs>? PhotoSaved;

    private static ExplorerView? _foregroundShell;
    private static string _foregroundShellPath = "";
    private static string _initImagePathFromArgs = "";
    private static readonly Lazy<WindowColorProfileProvider> _colorProfileService = new(() => new WindowColorProfileProvider(), LazyThreadSafetyMode.ExecutionAndPublication);


    #region Public Properties

    /// <summary>
    /// Gets the app settings.
    /// </summary>
    public static Config Config { get; set; } = new();


    /// <summary>
    /// Gets the arguments passed to the application.
    /// </summary>
    public static string[] Args { get; set; } = [];


    /// <summary>
    /// Gets the photo manager.
    /// </summary>
    public static PhotoManager Photos { get; set; } = new();


    /// <summary>
    /// Gets, sets app busy state.
    /// </summary>
    public static bool IsBusy { get; set; } = false;


    /// <summary>
    /// Gets the path of the image file from the arguments.
    /// </summary>
    public static string InputImagePathFromArgs => _initImagePathFromArgs;


    /// <summary>
    /// Gets the Shell object of foreground window.
    /// </summary>
    public static ExplorerView? ForegroundShell
    {
        get => _foregroundShell;
        set
        {
            _foregroundShell?.Dispose();
            _foregroundShell = value;

            try
            {
                _foregroundShellPath = _foregroundShell?.GetTabViewPath() ?? "";
                UpdateInitImagePath();
            }
            catch
            {
                _foregroundShellPath = "";
                _foregroundShell?.Dispose();
                _foregroundShell = null;
            }
        }
    }


    /// <summary>
    /// Provides a singleton instance of the <see cref="WindowColorProfileProvider"/> class.
    /// </summary>
    public static WindowColorProfileProvider ColorProfileService => _colorProfileService.Value;


    /// <summary>
    /// Gets, sets the changes of the current viewing image.
    /// </summary>
    public static ImgTransform ImageTransform { get; set; } = new();


    /// <summary>
    /// Gets, sets copied filename collection (multi-copy).
    /// </summary>
    public static HashSet<string> StringClipboard { get; set; } = [];


    /// <summary>
    /// Gets, sets the clipboard photo.
    /// </summary>
    public static Photo? ClipboardImage { get; set; }


    /// <summary>
    /// Gets, sets the path of the temporary image
    /// (clipboard image, temp image for printing, background,...)
    /// </summary>
    public static string? TempImagePath { get; set; }


    #endregion // Public Properties



    #region Public Methods

    /// <summary>
    /// Disposes all singletons.
    /// </summary>
    public static void Dispose()
    {
        Config.CleanUpPropertyChangedEvents();

        ForegroundShell = null;
        Photos.Dispose();
        ColorProfileService.Dispose();
        DisposeClipboardPhoto();
    }


    /// <summary>
    /// Check if we can use the foreground shell folder for loading images.
    /// </summary>
    public static bool CanUseForegroundShell()
    {
        // check if we should load images from foreground window
        var inputImageDirPath = Path.GetDirectoryName(InputImagePathFromArgs) ?? "";
        var isFromSearchWindow = _foregroundShellPath.StartsWith(EggShell.SEARCH_MS_PROTOCOL, StringComparison.OrdinalIgnoreCase);
        var isFromSavedSearch = _foregroundShellPath.EndsWith(".search-ms", StringComparison.OrdinalIgnoreCase);
        var isFromSameDir = inputImageDirPath.Equals(_foregroundShellPath, StringComparison.OrdinalIgnoreCase);

        var useForegroundWindow = ForegroundShell != null
            && !string.IsNullOrEmpty(InputImagePathFromArgs)
            && (isFromSearchWindow || isFromSavedSearch || isFromSameDir);

        return useForegroundWindow;
    }


    /// <summary>
    /// Update input path from arguments.
    /// </summary>
    public static void UpdateInitImagePath(string? path = null)
    {
        var pathToLoad = path ?? string.Empty;

        if (string.IsNullOrWhiteSpace(pathToLoad) && AP.Args.Length >= 2)
        {
            // get path from params
            var cmdPath = AP.Args
                .Skip(1)
                .FirstOrDefault(i => !i.StartsWith(Const.CONFIG_CMD_PREFIX, StringComparison.Ordinal));

            if (!string.IsNullOrEmpty(cmdPath))
            {
                pathToLoad = cmdPath;
            }
        }

        _initImagePathFromArgs = pathToLoad;
    }


    /// <summary>
    /// Disposes the clipboard photo.
    /// </summary>
    public static void DisposeClipboardPhoto()
    {
        AP.ClipboardImage?.Dispose();
        AP.ClipboardImage = null;
        AP.TempImagePath = null;
    }


    /// <summary>
    /// Quickly save the viewing photo as a temporary file.
    /// </summary>
    public static async Task<string?> SavePhotoAsTempFileAsync(string ext = ".png")
    {
        // 1. check if we can use the current clipboard image path
        if (File.Exists(AP.TempImagePath))
        {
            var extension = Path.GetExtension(AP.TempImagePath);

            if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                return AP.TempImagePath;
            }
        }


        // 2. create temp file path
        var tempDir = BHelper.ConfigDir(Dir.Temporary);
        Directory.CreateDirectory(tempDir);
        var tempFilePath = Path.Combine(tempDir, $"ig_temp_{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}{ext}");


        // 3. save the photo to file
        var photo = AP.ClipboardImage ?? AP.Photos.Current;
        if (photo is not null)
        {
            try
            {
                // save photo as file
                await photo.SaveAsAsync(tempFilePath, AP.ImageTransform, 85);

                AP.TempImagePath = tempFilePath;
            }
            catch
            {
                AP.TempImagePath = null;
            }
        }

        return AP.TempImagePath;
    }


    /// <summary>
    /// Raises <see cref="ThemeChanged"/> event.
    /// </summary>
    public static void OnThemeChanged(string propName = "")
    {
        ThemeChanged?.Invoke(null, new ThemePackChangedEventArgs(propName));
    }


    /// <summary>
    /// Raises <see cref="ThemeChanged"/> event.
    /// </summary>
    public static void OnLanguageChanged()
    {
        LanguageChanged?.Invoke(null, new());
    }


    /// <summary>
    /// Raises <see cref="PhotoUnloadedEventArgs"/> event.
    /// </summary>
    public static void OnPhotoUnloaded(PhotoUnloadedEventArgs e)
    {
        PhotoUnloaded?.Invoke(null, e);
    }


    /// <summary>
    /// Raises <see cref="PhotoSaved"/> event.
    /// </summary>
    public static void OnPhotoSaved(PhotoSaveEventArgs e)
    {
        PhotoSaved?.Invoke(null, e);
    }

    #endregion // Public Methods


}


public class ThemePackChangedEventArgs(string propName = "") : EventArgs
{
    /// <summary>
    /// Gets the property that triggered the event.
    /// If it's empty, the new theme pack is loaded.
    /// </summary>
    public string PropertyName => propName;
}


public class PhotoUnloadedEventArgs : EventArgs
{
    public bool IsClipboardPhoto { get; set; } = false;
    public int Index { get; set; } = -1;
    public string FilePath { get; set; } = string.Empty;
}


public class PhotoSaveEventArgs(string srcFilePath, string destFilePath, ImageSaveSource saveSource) : EventArgs
{
    public string SrcFilePath { get; init; } = srcFilePath;
    public string DestFilePath { get; init; } = destFilePath;
    public bool IsSaveAsNewFile => !SrcFilePath.Equals(DestFilePath, StringComparison.OrdinalIgnoreCase);
    public ImageSaveSource SaveSource { get; init; } = saveSource;
}

