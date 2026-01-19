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
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageGlass.Common;


public static class Core
{
    public static event EventHandler? LanguageChanged;
    public static event EventHandler<ThemePackChangedEventArgs>? ThemeChanged;
    public static event EventHandler<PhotoUnloadedEventArgs>? PhotoUnloaded;
    public static event EventHandler<PhotoSaveEventArgs>? PhotoSaved;

    private static string _initImagePathFromArgs = "";



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
    /// Gets, sets app busy state.
    /// </summary>
    public static bool IsBusy { get; set; } = false;


    /// <summary>
    /// Provides a singleton instance of the <see cref="WindowColorProfileProvider"/> class.
    /// </summary>
    public static IWindowColorProfileProvider? ColorProfileService
    {
        get; set
        {
            if (field is not null) return;
            field = value;
        }
    } = null;


    /// <summary>
    /// Gets the path of the image file from the arguments.
    /// </summary>
    public static string InputImagePathFromArgs => _initImagePathFromArgs;


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

        DisposeClipboardPhoto();

        ColorProfileService?.Dispose();
        ColorProfileService = null;
    }


    /// <summary>
    /// Update input path from arguments.
    /// </summary>
    public static void UpdateInitImagePath(string? path = null)
    {
        var pathToLoad = path ?? string.Empty;

        if (string.IsNullOrWhiteSpace(pathToLoad) && Core.Args.Length >= 2)
        {
            // get path from params
            var cmdPath = Core.Args
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
        Core.ClipboardImage?.Dispose();
        Core.ClipboardImage = null;
        Core.TempImagePath = null;
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
