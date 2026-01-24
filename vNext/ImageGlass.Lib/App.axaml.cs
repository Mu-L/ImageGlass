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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using ImageGlass.Common.Photoing;
using ImageGlass.Lib.UI.Windowing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ImageGlass.Common;

public partial class App : Application
{
    private IgWindow? _mainWindow = null;


    /// <summary>
    /// Gets the main window.
    /// </summary>
    public IgWindow MainWindow => _mainWindow!;


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
    public override void OnFrameworkInitializationCompleted()
    {
        ApplyUIConfigs();

        PlatformSettings?.ColorValuesChanged += PlatformSettings_ColorValuesChanged;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }


    /// <summary>
    /// Create a new main window.
    /// </summary>
    public void CreateMainWindowIfNotExist(IgWindow window)
    {
        if (_mainWindow is not null) return;

        _mainWindow = window;
    }



    private static void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {

    }


    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {

    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {

    }


    private async void PlatformSettings_ColorValuesChanged(object? sender, PlatformColorValues e)
    {
        Core.IsSystemDarkMode = e.ThemeVariant == PlatformThemeVariant.Dark;

        // load theme
        await Core.Config.LoadCurrentThemeAsync(Core.IsSystemDarkMode, true, true, false);

        // load & compute accent colors
        Core.AccentColor = Core.Config.Theme.UseSystemAccent
            ? e.AccentColor1
            : Core.Config.Theme.AccentColor;
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
            await Core.Config.LoadCurrentThemeAsync(isSystemDarkMode, true, true, false);

            // load & compute accent colors
            Core.AccentColor = Core.Config.Theme.UseSystemAccent
                ? info.AccentColor1
                : Core.Config.Theme.AccentColor;
        });



        // initialize Magick decoder
        MagickCodec.Initialize();

        // load app language
        _ = Core.Config.LoadCurrentLanguageAsync();
    }

}