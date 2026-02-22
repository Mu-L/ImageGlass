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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using ImageGlass.Common.AppThemes;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.UI.Windowing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ImageGlass.Common;

public partial class App : Application
{
    private PhWindow? _mainWindow = null;


    /// <summary>
    /// Gets the main window.
    /// </summary>
    public PhWindow MainWindow => _mainWindow!;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void Initialize()
    {
        // App-level exception handler for non-debugger
        if (!Debugger.IsAttached)
        {
            Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        AvaloniaXamlLoader.Load(this);
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override async void OnFrameworkInitializationCompleted()
    {
        ApplyUIConfigs();

        PlatformSettings?.ColorValuesChanged += PlatformSettings_ColorValuesChanged;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow;

            // get foreground shell
            if (Core.Config.ShouldUseExplorerSortOrder)
            {
                Core.ShellProvider?.ForegroundShell = Core.ShellProvider.GetForegroundWindowView();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }


    /// <summary>
    /// Create a new main window.
    /// </summary>
    public void CreateMainWindowIfNotExist(PhWindow window)
    {
        if (_mainWindow is not null) return;

        _mainWindow = window;
    }



    /// <summary>
    /// Handles app command-line arguments that should run without starting the UI.
    /// Returns <c>true</c> if the command was handled and the process should exit.
    /// </summary>
    public static async Task<bool> HandleCommandLineAsync(string[] args)
    {
        if (args.Length < 1) return false;

        var topCmd = args[0];

        // set / remove default photo viewer
        if (topCmd == ExeParams.SET_DEFAULT_PHOTO_VIEWER
            || topCmd == ExeParams.REMOVE_DEFAULT_PHOTO_VIEWER)
        {
            if (args.Length < 2) return false;

            var enable = topCmd == ExeParams.SET_DEFAULT_PHOTO_VIEWER;
            var extensions = args[1]
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (Core.ShellProvider is not null)
            {
                await Core.ShellProvider.SetDefaultPhotoViewerAsync(extensions, enable);
            }

            return true;
        }

        return false;
    }



    private static async void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = await ModalWindow.ShowUnhandledErrorAsync(e.Exception);

#if DEBUG
        throw e.Exception;
#endif
    }


    private static async void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        await ModalWindow.ShowUnhandledErrorAsync(e.Exception);

#if DEBUG
        throw e.Exception;
#endif
    }

    private async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        _ = await ModalWindow.ShowUnhandledErrorAsync(ex);

#if DEBUG
        throw ex;
#endif
    }


    private async void PlatformSettings_ColorValuesChanged(object? sender, PlatformColorValues e)
    {
        Core.IsSystemDarkMode = e.ThemeVariant == PlatformThemeVariant.Dark;

        await ApplyThemePackAsync(Core.IsSystemDarkMode, e.AccentColor1);
    }


    private void ApplyUIConfigs()
    {
        // update the base styles
        Core.UpdateBaseResources();


        // load theme for the first time
        var info = PlatformSettings!.GetColorValues();
        var isSystemDarkMode = info.ThemeVariant == PlatformThemeVariant.Dark;
        _ = Task.Run(async () =>
        {
            await ApplyThemePackAsync(isSystemDarkMode, info.AccentColor1);
        });



        // initialize Magick decoder
        MagickCodec.Initialize();

        // load app language
        _ = Core.Config.LoadCurrentLanguageAsync();
    }


    private static async Task ApplyThemePackAsync(bool isSystemDarkMode, Color systemAccentColor)
    {
        // load theme pack
        var hasThemeChanged = await Core.Config.LoadCurrentThemeAsync(isSystemDarkMode,
                useFallBackTheme: true,
                throwIfThemeInvalid: true,
                forceUpdateBackground: false);

        // load & compute accent colors
        var accent = Core.Theme.UseSystemAccent
            ? systemAccentColor
            : Core.Theme.AccentColor;
        var hasAccentChanged = Core.SetAccentColor(accent.WithBrightness(-0.125f));


        Dispatcher.UIThread.Post(() =>
        {
            // update color mode for app level
            Core.SetDarkMode(Core.Theme.Settings.IsDarkMode);

            if (hasAccentChanged || hasThemeChanged)
            {
                Core.UpdateAccentColorResources();
                AppThemeColors.Load(Core.Theme.Colors, accent);
                Core.UpdateAppThemedColorResources();
            }

            if (hasThemeChanged)
            {
                Core.OnThemeChanged();
            }
        });

    }

}