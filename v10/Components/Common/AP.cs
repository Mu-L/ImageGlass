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

namespace ImageGlass.Common;

public static class AP
{
    public static event EventHandler<ThemePackChangedEventArgs>? ThemeChanged;
    public static event EventHandler? LanguageChanged;

    private static ExplorerView? _foregroundShell;
    private static string _foregroundShellPath = "";
    private static string _inputImagePathFromArgs = "";
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
    /// Gets the path of the image file from the arguments.
    /// </summary>
    public static string InputImagePathFromArgs => _inputImagePathFromArgs;


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
                UpdateInputImagePath();
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
    /// Gets, sets copied filename collection (multi-copy).
    /// </summary>
    public static HashSet<string> StringClipboard { get; set; } = [];

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
    public static void UpdateInputImagePath(string? path = null)
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

        _inputImagePathFromArgs = pathToLoad;
    }


    /// <summary>
    /// Triggers event <see cref="ThemeChanged"/>.
    /// </summary>
    public static void RaiseThemeChangedEvent(string propName = "")
    {
        ThemeChanged?.Invoke(null, new ThemePackChangedEventArgs(propName));
    }


    /// <summary>
    /// Triggers event <see cref="ThemeChanged"/>.
    /// </summary>
    public static void RaiseLanguageChangedEvent()
    {
        LanguageChanged?.Invoke(null, new());
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

