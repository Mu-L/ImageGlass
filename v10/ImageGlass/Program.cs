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
using ImageGlass.Common;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Threading;
using WinRT;

namespace ImageGlass;

public class Program
{
    private static string APP_SINGLE_INSTANCE_ID => "{f2a83de1-b9ac-4461-81d0-cc4547b0b27b}";


    [STAThread]
    static int Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        // load app configs
        AP.Args = Environment.GetCommandLineArgs();
        AP.Config = Config.Load(Config.CONFIG_USER);


        // multiple instances
        if (AP.Config.EnableMultiInstances)
        {
            LaunchAppInstance();
        }
        // single instance
        else
        {
            var isRedirected = RegisterSingleInstance();
            if (!isRedirected)
            {
                LaunchAppInstance();
            }
        }

        return 0;
    }


    /// <summary>
    /// Launches the app instance.
    /// </summary>
    private static void LaunchAppInstance()
    {
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            _ = new App();
        });
    }


    /// <summary>
    /// Registers single instance event to the system.
    /// </summary>
    private static bool RegisterSingleInstance()
    {
        var isRedirected = false;
        var e = AppInstance.GetCurrent().GetActivatedEventArgs();

        try
        {
            var instance = AppInstance.FindOrRegisterForKey(APP_SINGLE_INSTANCE_ID);

            if (instance.IsCurrent)
            {
                instance.Activated += SingleInstance_Activated;
            }
            else
            {
                isRedirected = true;
                InstanceApi.RedirectActivationTo(instance, e);
            }
        }
        catch { }

        return isRedirected;
    }


    private static void SingleInstance_Activated(object? sender, AppActivationArguments args)
    {
        if (args.Kind != ExtendedActivationKind.Launch) return;

        // get instance arguments
        var e = args.Data.As<Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs>();
        var arguments = e.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        AP.Args = arguments;
        AP.UpdateInitImagePath();


        // load the image path
        var app = (App)Application.Current;
        app.WinMain?.IG_OpenPath(AP.InputImagePathFromArgs);


        // activate main window
        app.WinMain?.DispatcherQueue.TryEnqueue(() =>
        {
            app.WinMain.Activate();
        });
    }

}
