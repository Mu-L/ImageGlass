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
using Avalonia.Input;
using ImageGlass.Common;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.ServiceProviders.FileSearchService;
using ImageGlass.Common.Windows;
using ImageGlass.Linux.Common.ServiceProviders;
using ImageGlass.ViewModels;
using System;

namespace ImageGlass.Linux;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var isHandled = App.InitializeAppInstance(args, () =>
        {
            // initialize service providers
            Core.FileSearchProvider = new FileSearchProvider();
            Core.PreviewProvider = new PhotoPreviewProvider();
            Core.ShellProvider = new LinuxShellProvider();
            //Core.ShareProvider = new Win32ShareProvider();
            //Core.PrintProvider = new Win32PrintProvider();
        });

        if (isHandled) return 0;

        return BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }



    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
#if DEBUG
        .LogToTrace()
        .WithDeveloperTools(o =>
        {
            o.ApplicationName = BHelper.AppName;
            o.Gesture = new KeyGesture(Key.I, KeyModifiers.Control | KeyModifiers.Shift);
        })
        .UsePlatformDetect()
#else
        .UseX11()
#endif
        .UseSkia()
        .With(new SkiaOptions
        {
            MaxGpuResourceSizeBytes = long.MaxValue,
        })
        .AfterSetup(builder =>
        {
            var app = (App?)builder.Instance;

            // create main window
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainWindowModel(mainWindow);

            app?.CreateMainWindowIfNotExist(mainWindow);
        });
}
