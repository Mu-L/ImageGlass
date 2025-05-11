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
using ImageGlass.WinNT.Common;
using Microsoft.UI.Xaml;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;


    /// <summary>
    /// Gets the arguments passed to the application.
    /// </summary>
    public static string[] Args { get; set; } = [];


    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }


    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Args = Environment.GetCommandLineArgs();

        // get foreground shell
        using var shell = new EggShell();
        Local.ForegroundShell = shell.GetForegroundWindowView();


        _window = new MainWindow();
        _window.Closed += Window_Closed;
        _window.Activate();

        // get current monitor profile
        _ = WindowColorProfileProvider.Instance.InitializeAsync(_window.AppWindow.Id);

        // initialize Magick decoder
        MagickDecoder.Initialize();
    }


    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // dispose foreground shell
        Local.ForegroundShell = null;

        Local.Photos.Dispose();
        WindowColorProfileProvider.Instance.Dispose();
    }


}
