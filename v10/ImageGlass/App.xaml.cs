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
using ImageGlass.Win64.Common;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application, INotifyPropertyChanged
{
    private MainWindow? _winMain;

    private IProgress<AppUpdatedEventArgs> _uiReporter;
    private static UISettings _uiSystem = new UISettings();


    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow WinMain => _winMain!;



    /// <summary>
    /// Gets the arguments passed to the application.
    /// </summary>
    public static string[] Args { get; set; } = [];




    /// <summary>
    /// Gets the app settings.
    /// </summary>
    public static AppSettings Config { get; set; } = new();



    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        _uiReporter = new Progress<AppUpdatedEventArgs>(UIReporter_Reported);

        Application.Current.UnhandledException += Current_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;


        _uiSystem.ColorValuesChanged += UiSettings_ColorValuesChanged;
    }

    private void UIReporter_Reported(AppUpdatedEventArgs e)
    {
        if (e.AccentColor != null) App.Config.AccentColor = e.AccentColor.Value;
        if (e.IsDarkMode != null) App.Config.IsDarkMode = e.IsDarkMode.Value;
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        var accent = sender.GetColorValue(UIColorType.Accent);

        _uiReporter.Report(new AppUpdatedEventArgs()
        {
            AccentColor = accent,
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO:
        throw e.Exception;
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // TODO:
        throw e.Exception;
    }


    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
    {
        Args = Environment.GetCommandLineArgs();

        _winMain = new MainWindow();
        _winMain.Closed += Window_Closed;

        // monitor color mode change event
        var root = (FrameworkElement)_winMain.Content;
        root.ActualThemeChanged += Root_ActualThemeChanged;


        // load app configs
        App.Config = AppSettings.Load(AppSettings.CONFIG_USER);

        // get accent color & color mode
        var accentColor = _uiSystem.GetColorValue(UIColorType.Accent);
        var isDarkMode = root.ActualTheme != ElementTheme.Light;

        // load theme for the first time
        Config.LoadCurrentTheme(isDarkMode, accentColor, true, true, false);


        // get foreground shell
        if (App.Config.ShouldUseExplorerSortOrder)
        {
            using var shell = new EggShell();
            Local.ForegroundShell = shell.GetForegroundWindowView();
        }


        // show the main window
        _winMain.Activate();

        // get current monitor profile
        _ = WindowColorProfileProvider.Instance.InitializeAsync(_winMain.AppWindow.Id);

        // initialize Magick decoder
        MagickDecoder.Initialize();
    }

    private void Root_ActualThemeChanged(FrameworkElement sender, object args)
    {
        _uiReporter.Report(new AppUpdatedEventArgs()
        {
            IsDarkMode = sender.ActualTheme != ElementTheme.Light,
        });
    }

    private async void Window_Closed(object sender, WindowEventArgs args)
    {
        // save configs
        await SaveConfigsOnClosing();

        // dispose foreground shell
        Local.ForegroundShell = null;

        Local.Photos.Dispose();
        WindowColorProfileProvider.Instance.Dispose();
    }

    public static async Task SaveConfigsOnClosing()
    {
        //// save FrmMain placement
        //if (!Config.EnableFullScreen)
        //{
        //    WindowSettings.SaveFrmMainPlacementToConfig(this);
        //}


        App.Config.LastSeenImagePath = Local.Photos.GetFilePath(Local.Photos.CurrentIndex);
        //Config.ZoomLockValue = PicMain.ZoomFactor * 100f;


        // save config to file
        await App.Config.SaveAsync();


        //// cleaning
        //try
        //{
        //    // delete trash
        //    Directory.Delete(Config.ConfigDir(PathType.Dir, Dir.Temporary), true);
        //}
        //catch { }
    }
}


public class AppUpdatedEventArgs : EventArgs
{
    public Color? AccentColor { get; set; }
    public bool? IsDarkMode { get; set; }
}
