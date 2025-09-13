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
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.UI;
using Microsoft.UI.Xaml;
using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace ImageGlass;


/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private MainWindow? _winMain;
    private IProgress<SystemColorInfoChangedEventArgs> _uiReporter;
    private static UISettings _systemUI = new UISettings();


    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // use independent culture for formatting or parsing a string
        CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture =
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;


        _uiReporter = new Progress<SystemColorInfoChangedEventArgs>(UIReporter_Reported);

        // register unhandled exception handlers
        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        CoreApplication.UnhandledErrorDetected += CoreApplication_UnhandledErrorDetected;

        _systemUI.ColorValuesChanged += UiSettings_ColorValuesChanged;

        // load initial settings
        LoadInitAppSettings();
    }



    // App Events
    #region App Events

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs e)
    {
        // check if the config has any error
        if (Config.LoadingException is not null)
        {
            var isContinue = await ShowUnhandledException(Config.LoadingException);
            if (!isContinue) return;
        }

        _winMain = new MainWindow();
        _winMain.Closed += MainWindow_Closed;


        // get foreground shell
        if (AP.Config.ShouldUseExplorerSortOrder)
        {
            using var shell = new EggShell();
            AP.ForegroundShell = shell.GetForegroundWindowView();
        }

        // load other app settings
        LoadOtherAppSettings();

        // show the main window
        _winMain.Activate();
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        // save configs
        await SaveConfigsOnClosing();

        // dispose the global singleton
        AP.Dispose();

        _systemUI.ColorValuesChanged -= UiSettings_ColorValuesChanged;
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        var info = GetSystemColorInfo(sender);

        BHelper.Debounce(200, (args) =>
        {
            _uiReporter.Report(args!);
        }, info);
    }

    private void UIReporter_Reported(SystemColorInfoChangedEventArgs e)
    {
        AP.Config.AccentColor = e.AccentColor;
        AP.Config.IsSystemDarkMode = e.IsDarkMode;
    }



    private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = await ShowUnhandledException(e.Exception);

#if DEBUG
        throw e.Exception;
#endif
    }

    private async void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _ = await ShowUnhandledException(e.Exception);

#if DEBUG
        throw e.Exception;
#endif
    }

    private async void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;

        _ = await ShowUnhandledException(ex);

#if DEBUG
        throw ex;
#endif
    }

    private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        //var ex = e.Exception;
        //_ = await ShowUnhandledException(e.Exception);
    }

    private void CoreApplication_UnhandledErrorDetected(object? sender, UnhandledErrorDetectedEventArgs e)
    {
        e.UnhandledError.Propagate();
    }


    #endregion // App Events


    /// <summary>
    /// Loads user settings and applies theme pack.
    /// </summary>
    private void LoadInitAppSettings()
    {
        AP.Args = Environment.GetCommandLineArgs();

        // load app configs
        AP.Config = Config.Load(Config.CONFIG_USER);

        // get accent, color mode & load theme for the first time
        var info = GetSystemColorInfo(_systemUI);
        BHelper.RunSync(() => AP.Config.LoadCurrentThemeAsync(info.IsDarkMode, info.AccentColor, true, true, false));

        // set the initial app color mode
        if (AP.Config.Theme.Settings.IsDarkMode) RequestedTheme = ApplicationTheme.Dark;
        else RequestedTheme = ApplicationTheme.Light;
    }


    /// <summary>
    /// Loads other app settings.
    /// </summary>
    private void LoadOtherAppSettings()
    {
        // load app language
        _ = AP.Config.LoadCurrentLanguageAsync();

        // get current monitor profile
        _ = AP.ColorProfileService.InitializeAsync(_winMain!.AppWindow.Id);

        // initialize Magick decoder
        MagickDecoder.Initialize();
    }


    /// <summary>
    /// Gets system color information.
    /// </summary>
    private static SystemColorInfoChangedEventArgs GetSystemColorInfo(UISettings settings)
    {
        var foreground = settings.GetColorValue(UIColorType.Foreground);
        var isDarkMode = foreground.IsLight(); // if text color is light => dark mode
        var accent = settings.GetColorValue(UIColorType.Accent);

        var info = new SystemColorInfoChangedEventArgs()
        {
            AccentColor = accent,
            IsDarkMode = isDarkMode,
        };

        return info;
    }


    /// <summary>
    /// Reports unhandled exception,
    /// returns <c>true</c> if user ignores the error to continue.
    /// </summary>
    private static async Task<bool> ShowUnhandledException(Exception ex)
    {
        var isContinue = false;

        var result = await ModalWindow.ShowErrorAsync(null,
            AP.Config.Lang["_._UnhandledException"],
            AP.Config.Lang["_._UnhandledException._Description"],
            ex.Message,
            ex.ToString(),
            ModalWindowButton.Continue_Quit);

        // user chooses 'Quit'
        if (result.ExitCode == DialogExitCode.Cancel)
        {
            Application.Current.Exit();
        }
        else if (result.ExitCode == DialogExitCode.OK)
        {
            isContinue = true;
        }

        return isContinue;
    }


    public static async Task SaveConfigsOnClosing()
    {
        AP.Config.LastSeenImagePath = AP.Photos.CurrentFilePath;
        //Config.ZoomLockValue = PicMain.ZoomFactor * 100f;


        // save config to file
        await AP.Config.SaveAsync();


        //// cleaning
        //try
        //{
        //    // delete trash
        //    Directory.Delete(Config.ConfigDir(PathType.Dir, Dir.Temporary), true);
        //}
        //catch { }
    }


}


public class SystemColorInfoChangedEventArgs : EventArgs
{
    public Color AccentColor { get; set; }
    public bool IsDarkMode { get; set; }
}
