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
using D2Phap;
using ImageGlass.Common;
using ImageGlass.Common.Types;
using ImageGlass.Win32.Common.Photoing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ImageGlass.Win32.Common;

public static class CoreWin32
{
    private static ExplorerView? _foregroundShell;
    private static string _foregroundShellPath = string.Empty;



    #region Public Properties

    /// <summary>
    /// Gets the photo manager.
    /// </summary>
    public static PhotoManager Photos { get; set; } = new();


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
                Core.UpdateInitImagePath();
            }
            catch
            {
                _foregroundShellPath = "";
                _foregroundShell?.Dispose();
                _foregroundShell = null;
            }
        }
    }

    #endregion // Public Properties




    #region Public Methods

    /// <summary>
    /// Disposes all singletons.
    /// </summary>
    public static void Dispose()
    {
        ForegroundShell = null;
        Photos.Dispose();
    }


    /// <summary>
    /// Check if we can use the foreground shell folder for loading images.
    /// </summary>
    public static bool CanUseForegroundShell()
    {
        // check if we should load images from foreground window
        var inputImageDirPath = Path.GetDirectoryName(Core.InputImagePathFromArgs) ?? "";
        var isFromSearchWindow = _foregroundShellPath.StartsWith(EggShell.SEARCH_MS_PROTOCOL, StringComparison.OrdinalIgnoreCase);
        var isFromSavedSearch = _foregroundShellPath.EndsWith(".search-ms", StringComparison.OrdinalIgnoreCase);
        var isFromSameDir = inputImageDirPath.Equals(_foregroundShellPath, StringComparison.OrdinalIgnoreCase);

        var useForegroundWindow = ForegroundShell != null
            && !string.IsNullOrEmpty(Core.InputImagePathFromArgs)
            && (isFromSearchWindow || isFromSavedSearch || isFromSameDir);

        return useForegroundWindow;
    }


    /// <summary>
    /// Quickly save the viewing photo as a temporary file.
    /// </summary>
    public static async Task<string?> SavePhotoAsTempFileAsync(string ext = ".png")
    {
        // 1. check if we can use the current clipboard image path
        if (File.Exists(Core.TempImagePath))
        {
            var extension = Path.GetExtension(Core.TempImagePath);

            if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                return Core.TempImagePath;
            }
        }


        // 2. create temp file path
        var tempDir = BHelper.ConfigDir(Dir.Temporary);
        Directory.CreateDirectory(tempDir);
        var tempFilePath = Path.Combine(tempDir, $"ig_temp_{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}{ext}");


        // 3. save the photo to file
        var photo = Core.ClipboardImage ?? CoreWin32.Photos.Current;
        if (photo is not null)
        {
            try
            {
                // save photo as file
                await photo.SaveAsAsync(tempFilePath, Core.ImageTransform, 85);

                Core.TempImagePath = tempFilePath;
            }
            catch
            {
                Core.TempImagePath = null;
            }
        }

        return Core.TempImagePath;
    }

    #endregion // Public Methods




}
