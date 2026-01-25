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
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ImageGlass.Common.AppThemes;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageGlass.Common;


public static class Core
{
    public static readonly AppInstance AppInstance = new AppInstance("{f2a83de1-b9ac-4461-81d0-cc4547b0b27b}");

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
    /// Gets, sets the current color mode of OS.
    /// </summary>
    public static bool IsSystemDarkMode { get; set; } = true;


    /// <summary>
    /// Gets the system accent color.
    /// </summary>
    public static Color AccentColor
    {
        get => field;
        set
        {
            if (field == value) return;

            var oldValue = field;
            field = value;

            Core.UpdateAccentColorResources();
            Core.Theme.LoadColors(value);
            Core.OnThemeChanged(nameof(IgTheme.ComputedColors));
        }
    } = new();


    /// <summary>
    /// Gets, sets the current app theme pack.
    /// </summary>
    public static IgTheme Theme
    {
        get; set
        {
            if (field == value) return;

            var oldValue = field;
            field = value;
            Core.OnThemeChanged();
        }
    } = new();


    /// <summary>
    /// Gets, sets the current language.
    /// </summary>
    public static IgLang Lang
    {
        get; set
        {
            if (field == value) return;

            var oldValue = field;
            field = value;
            Core.OnLanguageChanged();
        }
    } = new();


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
    /// Updates base controls resources
    /// </summary>
    public static void UpdateBaseResources()
    {
        if (Application.Current is not App app) return;

        // 1. use modern Segoe UI font
        if (FontManager.Current.DefaultFontFamily.Name.Equals("Segoe UI"))
        {
            var fm = FontFamily.Parse("Segoe UI Variable Text");
            Resx.Set(ResxId.ContentControlThemeFontFamily, fm);
        }


        // 2. update control styles
        Resx.Set(ResxId.ControlCornerRadius, new CornerRadius(6));
    }


    /// <summary>
    /// Updates the app resources according to current color mode.
    /// </summary>
    public static void UpdateAppThemedColorResources()
    {
        if (Application.Current is not App app) return;

        Dispatcher.UIThread.Post(() =>
        {
            // update situational colors
            if (Core.Theme.Settings.IsDarkMode)
            {
                Resx.Set(ResxId.IG_BackgroundInfoBrush, IgTheme.BackgroundInfoDark.ToBrush());
                Resx.Set(ResxId.IG_BackgroundSuccessBrush, IgTheme.BackgroundSuccessDark.ToBrush());
                Resx.Set(ResxId.IG_BackgroundWarningBrush, IgTheme.BackgroundWarningDark.ToBrush());
                Resx.Set(ResxId.IG_BackgroundDangerBrush, IgTheme.BackgroundDangerDark.ToBrush());
            }
            else
            {
                Resx.Set(ResxId.IG_BackgroundInfoBrush, IgTheme.BackgroundInfoLight.ToBrush());
                Resx.Set(ResxId.IG_BackgroundSuccessBrush, IgTheme.BackgroundSuccessLight.ToBrush());
                Resx.Set(ResxId.IG_BackgroundWarningBrush, IgTheme.BackgroundWarningLight.ToBrush());
                Resx.Set(ResxId.IG_BackgroundDangerBrush, IgTheme.BackgroundDangerLight.ToBrush());
            }

            var bgNeutralAlpha = Core.Theme.Settings.IsDarkMode ? 100 : 150;
            var bgColor = Core.Theme.ComputedColors.BgColor.NoAlpha();
            var bgNeutral = bgColor.Blend(Core.Theme.InvertedBaseColor, 0.9f, bgNeutralAlpha);
            var borderNeutral = bgColor.Blend(Core.Theme.InvertedBaseColor, 0.8f, bgNeutralAlpha);
            var borderControl = bgColor.Blend(Core.Theme.InvertedBaseColor, 0.5f, bgNeutralAlpha);

            Resx.Set(ResxId.IG_BackgroundNeutralBrush, bgNeutral.ToBrush());
            Resx.Set(ResxId.IG_BorderNeutralBrush, borderNeutral.ToBrush());
            Resx.Set(ResxId.IG_BorderControlBrush, borderControl.ToBrush());


            // update text color
            var textBrush = Theme.ComputedColors.TextColor.ToBrush();
            Resx.Set(ResxId.SystemControlForegroundBaseHighBrush, textBrush);
            Resx.Set(ResxId.TextControlForeground, textBrush);
            Resx.Set(ResxId.CheckBoxForegroundChecked, textBrush);
            Resx.Set(ResxId.CheckBoxForegroundCheckedPointerOver, textBrush);
            Resx.Set(ResxId.CheckBoxForegroundUnchecked, textBrush);
            Resx.Set(ResxId.CheckBoxForegroundUncheckedPointerOver, textBrush);

            // update border color
            Resx.Set(ResxId.TextControlBorderBrush, borderControl);
            Resx.Set(ResxId.CheckBoxCheckBackgroundStrokeUnchecked, borderControl);
        });
    }


    /// <summary>
    /// Updates the app resources according to current accent color
    /// </summary>
    public static void UpdateAccentColorResources()
    {
        if (Application.Current is not App app) return;

        Dispatcher.UIThread.Post(() =>
        {
            // update app accent color
            var accent = Core.AccentColor;
            var accentLight1 = accent.WithBrightness(0.2f);
            var accentLight2 = accent.WithBrightness(0.3f);
            var accentLight3 = accent.WithBrightness(0.4f);
            var accentDark1 = accent.WithBrightness(-0.2f);
            var accentDark2 = accent.WithBrightness(-0.3f);
            var accentDark3 = accent.WithBrightness(-0.4f);


            // update all accent-related resources
            Resx.Set(ResxId.SystemAccentColor, accent);
            Resx.Set(ResxId.SystemAccentColorLight1, accentLight1);
            Resx.Set(ResxId.SystemAccentColorLight2, accentLight2);
            Resx.Set(ResxId.SystemAccentColorLight3, accentLight3);
            Resx.Set(ResxId.SystemAccentColorDark1, accentDark1);
            Resx.Set(ResxId.SystemAccentColorDark2, accentDark2);
            Resx.Set(ResxId.SystemAccentColorDark3, accentDark3);


            // border hover styles
            var borderHoverBrush = accentLight1.ToBrush();
            Resx.Set(ResxId.TextControlBorderBrushPointerOver, borderHoverBrush);
            Resx.Set(ResxId.CheckBoxCheckBackgroundStrokeUncheckedPointerOver, borderHoverBrush);
        });
    }


    /// <summary>
    /// Sets dark mode.
    /// </summary>
    public static void SetDarkMode(bool enable)
    {
        if (enable)
        {
            Application.Current?.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            Application.Current?.RequestedThemeVariant = ThemeVariant.Light;
        }
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
    /// Raises <see cref="ThemeChanged"/> event on UI thread.
    /// </summary>
    public static void OnThemeChanged(string propName = "")
    {
        Dispatcher.UIThread.Post(() =>
        {
            // update color mode for app level
            Core.SetDarkMode(Core.Theme.Settings.IsDarkMode);
            ThemeChanged?.Invoke(null, new ThemePackChangedEventArgs(propName));
        });
    }


    /// <summary>
    /// Raises <see cref="ThemeChanged"/> event on UI thread.
    /// </summary>
    public static void OnLanguageChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            LanguageChanged?.Invoke(null, new());
        });
    }


    /// <summary>
    /// Raises <see cref="PhotoUnloadedEventArgs"/> event on UI thread.
    /// </summary>
    public static void OnPhotoUnloaded(PhotoUnloadedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PhotoUnloaded?.Invoke(null, e);
        });
    }


    /// <summary>
    /// Raises <see cref="PhotoSaved"/> event on UI thread.
    /// </summary>
    public static void OnPhotoSaved(PhotoSaveEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PhotoSaved?.Invoke(null, e);
        });
    }


    #endregion // Public Methods


}
