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
    private IProgress<UIReportEventArgs> _uiReporter;
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


        _uiReporter = new Progress<UIReportEventArgs>(UIReporter_Reported);

        // register unhandled exception handlers
        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        CoreApplication.UnhandledErrorDetected += CoreApplication_UnhandledErrorDetected;

        _systemUI.ColorValuesChanged += UiSettings_ColorValuesChanged;
        AP.ThemeChanged += AP_ThemeChanged;

        // load initial settings
        LoadInitAppSettings();
    }

    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        _uiReporter.Report(new(UIReportType.ThemeChanged, e));
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
            var isContinue = await ModalWindow.ShowUnhandledErrorAsync(Config.LoadingException);
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
            _uiReporter.Report(new(UIReportType.SystemColorChanged, args));
        }, info);
    }


    private void UIReporter_Reported(UIReportEventArgs e)
    {
        // system color changed
        if (e.Type == UIReportType.SystemColorChanged && e.Data is SystemColorInfoChangedEventArgs data)
        {
            AP.Config.AccentColor = data.AccentColor;
            AP.Config.IsSystemDarkMode = data.IsDarkMode;
            return;
        }


        // theme changed
        if (e.Type == UIReportType.ThemeChanged)
        {
            ApplyMenuTheme();

            return;
        }
    }


    #region Unhandled Exception

    private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = await ModalWindow.ShowUnhandledErrorAsync(e.Exception);

#if DEBUG
        throw e.Exception;
#endif
    }

    private async void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _ = await ModalWindow.ShowUnhandledErrorAsync(e.Exception);

#if DEBUG
        throw e.Exception;
#endif
    }

    private async void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;

        _ = await ModalWindow.ShowUnhandledErrorAsync(ex);

#if DEBUG
        throw ex;
#endif
    }

    private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        //var ex = e.Exception;
        //_ = await ModalWindow.ShowUnhandledErrorAsync(e.Exception);
    }

    private void CoreApplication_UnhandledErrorDetected(object? sender, UnhandledErrorDetectedEventArgs e)
    {
        e.UnhandledError.Propagate();
    }

    #endregion // Unhandled Exception


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
    /// Update flyout menu according to theme pack.
    /// </summary>
    private static void ApplyMenuTheme()
    {
        var bgHover = AP.Config.Theme.ComputedColors.MenuBgHoverColor;
        var bgPressed = AP.Config.Theme.ComputedColors.MenuBgActiveColor;

        var textNormal = AP.Config.Theme.ComputedColors.MenuTextColor;
        var textHover = AP.Config.Theme.ComputedColors.MenuTextHoverColor;
        var textPressed = textHover;
        var textDisabled = textNormal.Blend(AP.Config.Theme.BaseColor, 0.5f, textNormal.A);


        // 1. menu dropdown
        Application.Current.Resources["MenuFlyoutPresenterBackground"] = AP.Config.Theme.ComputedColors.MenuBgColor.ToBrush();


        // 2. menu separator
        Application.Current.Resources["MenuFlyoutSeparatorBackground"] = textNormal.WithAlpha(10).ToBrush();


        // 4. menu item
        // background
        Application.Current.Resources["MenuFlyoutItemBackgroundPointerOver"] = bgHover.ToBrush();
        Application.Current.Resources["MenuFlyoutItemBackgroundPressed"] = bgPressed.ToBrush();
        // foreground
        Application.Current.Resources["MenuFlyoutItemForeground"] = textNormal.ToBrush();
        Application.Current.Resources["MenuFlyoutItemForegroundPointerOver"] = textHover.ToBrush();
        Application.Current.Resources["MenuFlyoutItemForegroundPressed"] = textPressed.ToBrush();
        Application.Current.Resources["MenuFlyoutItemForegroundDisabled"] = textDisabled.ToBrush();
        // hotkey
        Application.Current.Resources["MenuFlyoutItemKeyboardAcceleratorTextForeground"] = textNormal.WithAlpha(150).ToBrush();
        Application.Current.Resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver"] = textHover.WithAlpha(150).ToBrush();
        Application.Current.Resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed"] = textPressed.WithAlpha(150).ToBrush();
        Application.Current.Resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled"] = textDisabled.WithAlpha(150).ToBrush();


        // 3. menu subitem
        // background
        Application.Current.Resources["MenuFlyoutSubItemBackgroundPointerOver"] = bgHover.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemBackgroundSubMenuOpened"] = bgHover.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemBackgroundPressed"] = bgPressed.ToBrush();
        // foreground
        Application.Current.Resources["MenuFlyoutSubItemForeground"] = textNormal.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemForegroundPointerOver"] = textHover.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemForegroundPressed"] = textPressed.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemForegroundSubMenuOpened"] = textHover.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemForegroundDisabled"] = textDisabled.ToBrush();
        // chevron
        Application.Current.Resources["MenuFlyoutSubItemChevron"] = textNormal.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemChevronPointerOver"] = textHover.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemChevronPressed"] = textPressed.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemChevronSubMenuOpened"] = textHover.ToBrush();
        Application.Current.Resources["MenuFlyoutSubItemChevronDisabled"] = textDisabled.ToBrush();


        // 5. toggle menu item
        // hotkey
        Application.Current.Resources["ToggleMenuFlyoutItemKeyboardAcceleratorTextForeground"] = textNormal.WithAlpha(180).ToBrush();
        Application.Current.Resources["ToggleMenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver"] = textHover.WithAlpha(180).ToBrush();
        Application.Current.Resources["ToggleMenuFlyoutItemKeyboardAcceleratorTextForegroundPressed"] = textPressed.WithAlpha(180).ToBrush();
        Application.Current.Resources["ToggleMenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled"] = textDisabled.WithAlpha(180).ToBrush();

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


public enum UIReportType
{
    ThemeChanged,
    SystemColorChanged,
}



public class UIReportEventArgs(UIReportType type, object? data) : EventArgs
{
    public UIReportType Type => type;
    public object? Data => data;
}


public class SystemColorInfoChangedEventArgs : EventArgs
{
    public Color AccentColor { get; set; }
    public bool IsDarkMode { get; set; }
}
