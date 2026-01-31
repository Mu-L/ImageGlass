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
using Avalonia.Input;
using ImageGlass.Common;
using ImageGlass.Common.Types;
using ImageGlass.ViewModels;
using ImageGlass.Win32.Common.ServiceProviders;
using ImageGlass.Win32.Windows;
using System;
using System.Globalization;
using System.Threading;

namespace ImageGlass.Win32;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // use independent culture for formatting or parsing a string
        CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture =
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;


        // load app configs
        Core.Args = Environment.GetCommandLineArgs();
        Core.Config = Config.Load(Config.CONFIG_USER);


        // handle single instance
        if (!Core.Config.EnableMultiInstances)
        {
            if (!Core.AppInstance.IsFirstInstance)
            {
                Core.AppInstance.SendArgsToExistingInstances(ExeParams.SINGLE_INSTANCE, args);
                return 0;
            }
        }

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
#endif
        .UseWin32()
        .UseSkia()
        .With(new SkiaOptions
        {
            MaxGpuResourceSizeBytes = long.MaxValue,
        })
        .AfterSetup(builder =>
        {
            var app = (App?)builder.Instance;

            // initialize service providers
            Core.PreviewProvider = new Win32PhotoPreviewProvider();

            // create main window
            var mainWindow = new MainWindow32();
            mainWindow.DataContext = new MainWindowModel(mainWindow);
            app?.CreateMainWindowIfNotExist(mainWindow);
        });


}
