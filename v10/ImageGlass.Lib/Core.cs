/*
ImageGlass - A lightweight, versatile image viewer
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
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using ImageGlass.Plugins;
using ImageGlass.Plugins.External;
using ImageGlass.SDK;
using ImageGlass.UI.Viewer;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Common;

public static class Core
{
    public static readonly AppInstance AppInstance = new AppInstance("IG_APP"); // MacOS has length limit

    public static event EventHandler? LanguageChanged;
    public static event EventHandler<ThemePackChangedEventArgs>? ThemeChanged;
    public static event EventHandler<PhotoUnloadedEventArgs>? PhotoUnloaded;
    public static event EventHandler<PhotoSaveEventArgs>? PhotoSaved;
    public static event EventHandler? ColorProfileChanged;

    private static string _initImagePathFromArgs = string.Empty;



    #region Platform Service Provider

    /// <summary>
    /// Gets the build information.
    /// </summary>
    /// <remarks>
    /// <c>**NOTE:</c> Set this from the platform project's <c>Program.Main()</c>
    /// using the MSBuild-generated <c>AppBuildInfo</c> class.
    /// </remarks>
    public static IAppBuildInfo BuildInfo { get; set; } = null!;


    /// <summary>
    /// Provides a singleton instance to detect color profile of monitor.
    /// </summary>
    public static IWindowColorProfileProvider? ColorProfileProvider { get; set; } = null;


    /// <summary>
    /// Provides a singleton instance to retrieve photo preview & thumbnail.
    /// </summary>
    public static IPhotoPreviewProvider? PreviewProvider { get; set; } = null;


    /// <summary>
    /// Provides a singleton instance to search photo files.
    /// </summary>
    public static IFileSearchProvider? FileSearchProvider { get; set; } = null;


    /// <summary>
    /// Provides a singleton instance to manage OS shell & file path.
    /// </summary>
    public static IShellProvider? ShellProvider { get; set; } = null;


    /// <summary>
    /// Provides a singleton instance to manage Print service.
    /// </summary>
    public static IPrintProvider? PrintProvider { get; set; } = null;


    /// <summary>
    /// Provides a singleton instance to manage Share dialog.
    /// </summary>
    public static IShareProvider? ShareProvider { get; set; } = null;


    /// <summary>
    /// Provides a singleton instance to access app APIs.
    /// </summary>
    public static AppAPIProvider? API { get; set; } = null;


    /// <summary>
    /// Provides the slideshow service for managing slideshow playback.
    /// </summary>
    public static SlideshowProvider? Slideshow { get; set; } = null;


    /// <summary>
    /// Provides the update service for checking and downloading app updates.
    /// </summary>
    public static UpdateProvider? Update { get; set; } = null;

    #endregion // Platform Service Provider



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
    /// Gets, sets the current color mode of OS.
    /// </summary>
    public static bool IsSystemDarkMode { get; set; } = true;


    /// <summary>
    /// Gets the system accent color.
    /// </summary>
    public static Color AccentColor { get; private set; } = new();


    /// <summary>
    /// Gets the current app theme pack.
    /// </summary>
    public static IgTheme Theme { get; private set; } = new();


    /// <summary>
    /// Gets, sets the current language.
    /// </summary>
    public static Lang Lang
    {
        get; set
        {
            if (field == value) return;

            field = value;
            Core.OnLanguageChanged();
        }
    } = new();


    /// <summary>
    /// Gets or sets the HDR tone mapping options used when rendering high dynamic range images.
    /// </summary>
    public static HdrToneMappingOptions HdrToneMappingConfig { get; set; } = new();


    /// <summary>
    /// Gets the central registry for all plugins (hosted and non-hosted).
    /// Built-in plugins are registered during <see cref="ServiceProviders.AppAPIProvider"/> construction.
    /// </summary>
    public static PluginRegistry PluginRegistry { get; } = new();


    /// <summary>
    /// Gets the process manager for external (out-of-process) plugins.
    /// </summary>
    public static PluginProcessManager ExternalPlugins { get; } = new();


    /// <summary>
    /// Gets the registry for built-in and future external photo codecs.
    /// </summary>
    public static CodecRegistry CodecRegistry { get; } = new();


    /// <summary>
    /// Gets the path of the image file from the arguments.
    /// </summary>
    public static string InputImagePathFromArgs => _initImagePathFromArgs;


    /// <summary>
    /// Gets, sets the changes of the current viewing image.
    /// </summary>
    public static PhotoTransform ImageTransform { get; set; } = new();


    /// <summary>
    /// Gets, sets copied filename collection (multi-copy).
    /// </summary>
    public static HashSet<string> StringClipboard { get; set; } = [];


    /// <summary>
    /// Gets, sets the clipboard photo.
    /// </summary>
    public static Photo? ClipboardImage { get; set; }


    /// <summary>
    /// Gets, sets the path of the temporary image (clipboard image, temp image for printing, background,...)
    /// </summary>
    public static string? TempImagePath { get; set; }


    /// <summary>
    /// Gets the color profile of current photo.
    /// </summary>
    public static SKColorSpace? DestColorProfile { get; private set; }


    /// <summary>
    /// Checks if the current color profile is supported by Skia.
    /// </summary>
    public static bool IsDestColorProfileSupported { get; private set; } = true;


    /// <summary>
    /// Gets the color channels setting.
    /// </summary>
    public static ColorChannels ColorChannels { get; set; } = ColorChannels.RGBA;

    #endregion // Public Properties



    #region Public Methods


    /// <summary>
    /// Disposes all singletons.
    /// </summary>
    public static void Dispose()
    {
        Config.CleanUpPropertyChangedEvents();

        // Dispose external plugins (StopAllAsync already called in OnClosing)
        ExternalPlugins.Dispose();
        NativeCodecPluginLoader.Dispose();
        CodecRegistry.Dispose();

        DisposeClipboardPhoto();

        Core.Slideshow?.Dispose();
        Core.Slideshow = null;

        Core.Update = null;

        Core.Photos.Dispose();
        Core.ColorProfileProvider?.Dispose();
        Core.ColorProfileProvider = null;

        Core.FileSearchProvider?.Dispose();
        Core.FileSearchProvider = null;

        Core.ShellProvider?.Dispose();
        Core.ShellProvider = null;
    }


    public static bool SetTheme(IgTheme theme)
    {
        if (Core.Theme == theme) return false;

        Core.Theme = theme;
        return true;
    }


    public static bool SetAccentColor(Color accent)
    {
        if (Core.AccentColor == accent) return false;

        Core.AccentColor = accent;
        return true;
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
    }


    /// <summary>
    /// Updates the app resources according to current color mode.
    /// </summary>
    public static void UpdateAppThemedColorResources()
    {
        if (Application.Current is not App app) return;

        // update theme colors
        Resx.Set(ResxId.IG_ThemeBackgroundBrush, AppThemeColors.BgBrush);
        Resx.Set(ResxId.IG_ThemeForegroundBrush, AppThemeColors.TextColorBrush);
        Resx.Set(ResxId.IG_ThemeToolbarBackgroundBrush, AppThemeColors.ToolbarBgBrush);
        Resx.Set(ResxId.IG_ThemeGalleryBackgroundBrush, AppThemeColors.GalleryBgBrush);
        Resx.Set(ResxId.IG_ThemeMenuBackgroundBrush, AppThemeColors.MenuBgBrush);

        // update situational colors
        if (Core.Theme.Settings.IsDarkMode)
        {
            Resx.Set(ResxId.IG_BackgroundInfoBrush, AppThemeColors.BackgroundInfoDark.ToBrush());
            Resx.Set(ResxId.IG_BackgroundSuccessBrush, AppThemeColors.BackgroundSuccessDark.ToBrush());
            Resx.Set(ResxId.IG_BackgroundWarningBrush, AppThemeColors.BackgroundWarningDark.ToBrush());
            Resx.Set(ResxId.IG_BackgroundDangerBrush, AppThemeColors.BackgroundDangerDark.ToBrush());
        }
        else
        {
            Resx.Set(ResxId.IG_BackgroundInfoBrush, AppThemeColors.BackgroundInfoLight.ToBrush());
            Resx.Set(ResxId.IG_BackgroundSuccessBrush, AppThemeColors.BackgroundSuccessLight.ToBrush());
            Resx.Set(ResxId.IG_BackgroundWarningBrush, AppThemeColors.BackgroundWarningLight.ToBrush());
            Resx.Set(ResxId.IG_BackgroundDangerBrush, AppThemeColors.BackgroundDangerLight.ToBrush());
        }

        var bgNeutralAlpha = Core.Theme.Settings.IsDarkMode ? 100 : 150;
        var bgColor = AppThemeColors.BgBrush.Color.NoAlpha();
        var bgNeutral = bgColor.Blend(Core.Theme.InvertedBaseColor, 0.9f, bgNeutralAlpha);
        var borderNeutral = bgColor.Blend(Core.Theme.InvertedBaseColor, 0.8f, bgNeutralAlpha);
        var borderControl = bgColor.Blend(Core.Theme.InvertedBaseColor, 0.5f, bgNeutralAlpha);

        Resx.Set(ResxId.IG_BackgroundNeutralBrush, bgNeutral.ToBrush());
        Resx.Set(ResxId.IG_BorderNeutralBrush, borderNeutral.ToBrush());
        Resx.Set(ResxId.IG_BorderControlBrush, borderControl.ToBrush());
        Resx.Set(ResxId.IG_MessageBackgroundBrush, bgColor.A(200).ToBrush());


        // update text color
        var textBrush = AppThemeColors.TextColorBrush.Color.ToBrush();
        var textDisabled = AppThemeColors.TextColorBrush.Color.Blend(Core.Theme.BaseColor, 0.5f, AppThemeColors.TextColorBrush.A);

        Resx.Set(ResxId.SystemControlForegroundBaseHighBrush, textBrush);
        Resx.Set(ResxId.TextControlForeground, textBrush);
        Resx.Set(ResxId.CheckBoxForegroundChecked, textBrush);
        Resx.Set(ResxId.CheckBoxForegroundCheckedPointerOver, textBrush);
        Resx.Set(ResxId.CheckBoxForegroundUnchecked, textBrush);
        Resx.Set(ResxId.CheckBoxForegroundUncheckedPointerOver, textBrush);

        // update border color
        Resx.Set(ResxId.TextControlBorderBrush, borderControl);
        Resx.Set(ResxId.TextControlBorderBrushDisabled, borderControl);
        Resx.Set(ResxId.ComboBoxBorderBrush, borderControl);
        Resx.Set(ResxId.CheckBoxCheckBackgroundStrokeUnchecked, borderControl);


        // update dropdown menu =======
        var menuBg = AppThemeColors.MenuBgBrush.Color.NoAlpha(); // no alpha support
        var menuBorder = Core.Theme.InvertedBaseColor.WithAlpha(30);
        var menuText = AppThemeColors.TextColorBrush.Color;
        var menuTextDisabled = menuText.Blend(Core.Theme.InvertedBaseColor, 0.8f, 100);

        Resx.Set(ResxId.MenuFlyoutPresenterBackground, menuBg);
        Resx.Set(ResxId.MenuFlyoutPresenterBorderBrush, menuBorder);
        Resx.Set(ResxId.IG_MenuSeparatorBackground, menuText.A(20));


        // menu text
        Resx.Set(ResxId.MenuFlyoutItemForeground, menuText);
        Resx.Set(ResxId.MenuFlyoutItemForegroundPointerOver, menuText);
        Resx.Set(ResxId.MenuFlyoutItemForegroundPressed, menuText);
        Resx.Set(ResxId.MenuFlyoutItemForegroundDisabled, menuTextDisabled);

        // menu hotkey text
        var hotkeyTextColor = menuText.A(200);
        Resx.Set(ResxId.MenuFlyoutItemKeyboardAcceleratorTextForeground, hotkeyTextColor);
        Resx.Set(ResxId.MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver, hotkeyTextColor);
        Resx.Set(ResxId.MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed, hotkeyTextColor);
        Resx.Set(ResxId.MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled, menuTextDisabled);

        // menu chevron
        Resx.Set(ResxId.MenuFlyoutSubItemChevron, menuText);
        Resx.Set(ResxId.MenuFlyoutSubItemChevronPointerOver, menuText);
        Resx.Set(ResxId.MenuFlyoutSubItemChevronPressed, menuText);
        Resx.Set(ResxId.MenuFlyoutSubItemChevronDisabled, menuTextDisabled);
        Resx.Set(ResxId.MenuFlyoutSubItemChevronSubMenuOpened, menuText);

        // tooltip
        Resx.Set(ResxId.ToolTipForeground, menuText);
        Resx.Set(ResxId.ToolTipBackground, menuBg);
        Resx.Set(ResxId.ToolTipBorder, menuBorder);


        // combobox ===========
        Resx.Set(ResxId.ComboBoxForeground, textBrush);
        Resx.Set(ResxId.ComboBoxDropDownBackground, menuBg);
        Resx.Set(ResxId.ComboBoxDropDownBorderBrush, menuBorder);

        Resx.Set(ResxId.ComboBoxItemForeground, menuText);
        Resx.Set(ResxId.ComboBoxItemForegroundPointerOver, menuText);
        Resx.Set(ResxId.ComboBoxItemForegroundPressed, menuText);
        Resx.Set(ResxId.ComboBoxItemForegroundDisabled, menuTextDisabled);
    }


    /// <summary>
    /// Updates the app resources according to current accent color
    /// </summary>
    public static void UpdateAccentColorResources()
    {
        if (Application.Current is not App app) return;

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
        Resx.Set(ResxId.ComboBoxBorderBrushPointerOver, borderHoverBrush);
        Resx.Set(ResxId.CheckBoxCheckBackgroundStrokeUncheckedPointerOver, borderHoverBrush);


        // tool buttons
        var btnBgAlphaGap = Core.Theme.Settings.IsDarkMode ? 0 : -50;
        var btnBg = accent.A(0);
        var btnBgHover = accent.A((byte)(90 + btnBgAlphaGap));
        var btnBgPressed = accent.A((byte)(130 + btnBgAlphaGap));
        var btnBgChecked = accent.A((byte)(150 + btnBgAlphaGap));

        Resx.Set(ResxId.IG_ToolButtonBackground, btnBg);
        Resx.Set(ResxId.IG_ToolButtonBackgroundHover, btnBgHover);
        Resx.Set(ResxId.IG_ToolButtonBackgroundPressed, btnBgPressed);
        Resx.Set(ResxId.IG_ToolButtonBackgroundChecked, btnBgChecked);

        // menu item background
        Resx.Set(ResxId.MenuFlyoutItemBackground, btnBg);
        Resx.Set(ResxId.MenuFlyoutItemBackgroundPointerOver, btnBgHover);
        Resx.Set(ResxId.MenuFlyoutItemBackgroundPressed, btnBgPressed);

        // combobox item background
        Resx.Set(ResxId.ComboBoxItemBackground, btnBg);
        Resx.Set(ResxId.ComboBoxItemBackgroundPointerOver, btnBgHover);
        Resx.Set(ResxId.ComboBoxItemBackgroundPressed, btnBgPressed);
        Resx.Set(ResxId.ComboBoxItemBackgroundSelected, btnBgChecked);
    }


    /// <summary>
    /// Sets dark mode.
    /// </summary>
    public static void SetAppDarkThemeVariant(bool enable)
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
        var pathToLoad = path;

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

        _initImagePathFromArgs = pathToLoad ?? string.Empty;
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
        var tempFilePath = BHelper.ConfigDir(Dir.Temporary, $"ig_temp_{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}{ext}");


        // 3. save the photo to file
        var photo = Core.ClipboardImage ?? Core.Photos.Current;
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
    /// Updates the current destination color space.
    /// </summary>
    public static void UpdateDestColorProfile()
    {
        var results = SkiaCodec.GetColorProfileByName(Core.Config.ColorProfile);

        Core.IsDestColorProfileSupported = results.IsSupported;

        // no change
        if (Core.DestColorProfile == results.ColorSpace) return;

        // color profile change
        Core.DestColorProfile?.Dispose();
        Core.DestColorProfile = results.ColorSpace;
        Core.OnColorProfileChanged();
    }


    /// <summary>
    /// Loads a photo as the clipboard image in the viewer.
    /// </summary>
    public static async Task LoadClipboardPhotoAsync(Photo? photo)
    {
        if (API is null) return;
        await API.LoadClipboardPhotoAsync(photo);
    }


    #endregion // Public Methods



    #region Methods to raise Events


    /// <summary>
    /// Raises ColorProfileChanged event on UI thread.
    /// </summary>
    public static void OnColorProfileChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            ColorProfileChanged?.Invoke(null, new());
        });

        ExternalPlugins.BroadcastToAll(SDK.MessageTypes.COLOR_PROFILE_CHANGED);
    }


    /// <summary>
    /// Raises <see cref="ThemeChanged"/> event on UI thread.
    /// </summary>
    public static void OnThemeChanged(string propName = "")
    {
        Dispatcher.UIThread.Post(() =>
        {
            ThemeChanged?.Invoke(null, new ThemePackChangedEventArgs(propName));
        });

        ExternalPlugins.BroadcastToAll(SDK.MessageTypes.THEME_CHANGED, new SDK.ThemeInfo
        {
            IsDarkMode = Theme.Settings.IsDarkMode,
            AccentColor = AccentColor.ToString(),
            BackgroundColor = Config.BackgroundColor,
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

        ExternalPlugins.BroadcastToAll(SDK.MessageTypes.LANGUAGE_CHANGED, new SDK.LanguageChangedEventArgs
        {
            Code = Lang.Metadata.Code,
            EnglishName = Lang.Metadata.EnglishName,
            LocalName = Lang.Metadata.LocalName,
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


    /// <summary>
    /// Broadcasts a photo change event to all running external plugins.
    /// </summary>
    public static void BroadcastPhotoChanged(Photo? photo)
    {
        if (photo is null) return;

        ExternalPlugins.BroadcastToAll(SDK.MessageTypes.PHOTO_CHANGED, new SDK.PhotoChangedEventArgs
        {
            FilePath = photo.FilePath,
            Width = (int)(photo.Metadata?.OriginalWidth ?? 0),
            Height = (int)(photo.Metadata?.OriginalHeight ?? 0),
            Format = photo.Metadata?.FileExtension,
            FrameCount = (int)(photo.Metadata?.FrameCount ?? 1),
            CanAnimate = photo.Metadata?.CanAnimate ?? false,
        });
    }


    /// <summary>
    /// Singleton quarantine tracker for native codec plugins.
    /// </summary>
    public static Plugins.Native.NativePluginQuarantine NativeCodecPluginQuarantine { get; } = new();


    /// <summary>
    /// Singleton loader that owns every native codec plugin shared library for the process lifetime.
    /// </summary>
    public static Plugins.Native.NativePluginLoader NativeCodecPluginLoader { get; } =
        new(NativeCodecPluginQuarantine);


    /// <summary>
    /// Discovers external plugins from the <c>_plugins</c> directory and registers them.
    /// Runs on a background thread to avoid blocking app startup.
    /// OOP plugins are registered as user-facing tool plugins; native codec plugins
    /// are loaded in-process and their codecs are registered into <see cref="CodecRegistry"/>.
    /// </summary>
    public static void DiscoverExternalPlugins()
    {
        _ = Task.Run(() =>
        {
            var pluginsDir = BHelper.ConfigDir(Dir.Plugins);
            var discovered = PluginProcessManager.DiscoverPlugins(pluginsDir);

            foreach (var (manifest, dir) in discovered)
            {
                try
                {
                    if (manifest.Kind == SDK.Native.IGPluginKind.Native)
                    {
                        DiscoverNativeCodecPlugin(manifest, dir);
                    }
                    else
                    {
                        var proxy = new ExternalPluginProxy(manifest, ExternalPlugins, dir);
                        PluginRegistry.Register(manifest.Id, proxy);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Core.DiscoverExternalPlugins] '{manifest.Id}' failed: {ex.Message}");
                }
            }
        });
    }


    /// <summary>
    /// Loads a single native codec plugin and registers all of its codecs into the registry.
    /// </summary>
    private static void DiscoverNativeCodecPlugin(SDK.PluginManifest manifest, string pluginDir)
    {
        var handle = NativeCodecPluginLoader.LoadAndProbe(manifest, pluginDir);
        if (handle is null) return;

        foreach (var proxy in NativeCodecPluginLoader.CreateProxies(handle))
        {
            try
            {
                CodecRegistry.Register(proxy);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Core.DiscoverNativeCodecPlugin] register '{proxy.CodecId}' failed: {ex.Message}");
            }
        }
    }


    internal static void Viewer_PhotoLoadingForPlugins(ViewerControl sender, PhotoLoadingEventArgs e)
    {
        if (e.State == PhotoState.Loaded)
        {
            BroadcastPhotoChanged(e.Photo);
        }
    }

    internal static void Viewer_PointerMovedForPlugins(ViewerControl sender, ViewerPointerEventArgs e)
    {
        ExternalPlugins.BroadcastToSubscribed(
            MessageTypes.POINTER_MOVED,
            new PointerEventArgs
            {
                SourceX = e.SourcePoint.X,
                SourceY = e.SourcePoint.Y,
                ClientX = (float)e.Point.Position.X,
                ClientY = (float)e.Point.Position.Y,
            },
            s => s.PointerMoved);
    }

    internal static void Viewer_PointerPressedForPlugins(ViewerControl sender, ViewerPointerEventArgs e)
    {
        ExternalPlugins.BroadcastToSubscribed(
            MessageTypes.POINTER_PRESSED,
            new PointerEventArgs
            {
                SourceX = e.SourcePoint.X,
                SourceY = e.SourcePoint.Y,
                ClientX = (float)e.Point.Position.X,
                ClientY = (float)e.Point.Position.Y,
            },
            s => s.PointerPressed);
    }

    internal static void Viewer_SelectionChangedForPlugins(ViewerControl sender, ViewerSelectionChangedEventArgs e)
    {
        var src = e.SourceSelection;
        ExternalPlugins.BroadcastToSubscribed(
            MessageTypes.SELECTION_CHANGED,
            src == default ? null : new SelectionEventArgs
            {
                X = (float)src.X,
                Y = (float)src.Y,
                Width = (float)src.Width,
                Height = (float)src.Height,
            },
            s => s.SelectionChanged);
    }

    internal static void Viewer_FrameChangedForPlugins(ViewerControl sender, PhotoFrameChangedEventArgs e)
    {
        ExternalPlugins.BroadcastToSubscribed(
            MessageTypes.FRAME_CHANGED,
            (int)e.CurrentFrame,
            s => s.FrameChanged);
    }


    #endregion // Methods to raise Events


}
