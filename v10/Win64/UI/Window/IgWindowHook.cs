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
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.Win64.Common;
using ImageMagick;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using WinRT.Interop;


namespace ImageGlass.Win64.UI;

public partial class IgWindowHook : DisposableImpl
{
    private Window _window;
    private WindowMessageMonitor _msgMonitor;
    private TitlebarControl? _titleBar = null;
    private BackdropStyle _backdropStyle = BackdropStyle.None;

    private readonly IProgress<AppIconChangedEventArgs> _uiReporter;
    private nint _appIconHandle = IntPtr.Zero;


    // Public Properties 
    #region Public Properties

    /// <summary>
    /// Gets the window handle.
    /// </summary>
    public nint WindowHandle => WindowNative.GetWindowHandle(_window);


    /// <summary>
    /// Gets DPI scale of the window.
    /// </summary>
    public double DpiScale => WindowApi.GetDpiScaleForWindow(WindowHandle);


    /// <summary>
    /// Gets, set the title of the window.
    /// </summary>
    public string? TitlebarText
    {
        get => _window.Title;
        set
        {
            if (value != _window.Title)
            {
                _window.Title = value;
                OnPropertyChanged(nameof(TitlebarText));
            }
        }
    }


    /// <summary>
    /// Gets the title bar of window.
    /// </summary>
    public TitlebarControl? Titlebar => _titleBar;


    /// <summary>
    /// Gets, sets the title bar's right inset width of <see cref="MainWindow"/>.
    /// </summary>
    public double TitleBarRightInset
    {
        get => field / DpiScale;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TitleBarPadding));
            }
        }
    } = 0;


    /// <summary>
    /// Gets the title bar padding of <see cref="MainWindow"/>.
    /// </summary>
    public Thickness TitleBarPadding => new Thickness(0, 0, TitleBarRightInset, 0);

    #endregion // Public Properties


    public IgWindowHook(Window window, TitlebarControl? customTitleBar = null)
    {
        _window = window;
        _uiReporter = new Progress<AppIconChangedEventArgs>(UIReporter_Report);

        // set title bar
        if (customTitleBar != null) SetTitlebar(customTitleBar);

        AP.ThemeChanged += AP_ThemeChanged;
        _window.Activated += Window_Activated;
        _msgMonitor = new WindowMessageMonitor(WindowHandle);

        var root = (FrameworkElement)_window.Content;
        root.Loaded += Root_Loaded;
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();

        AP.ThemeChanged -= AP_ThemeChanged;
        _window.Activated -= Window_Activated;
        _msgMonitor.Dispose();

        IconApi.DestroyHIcon(_appIconHandle);
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTitleBarSize();

        UpdateWindowColorMode();
        UpdateWindowIcon();
        UpdateWindowBackdrop();
    }

    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        // a new theme just loaded
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            // update app color mode according to the theme's color mode
            UpdateWindowColorMode();

            // update app icon
            UpdateWindowIcon();

            // update backdrop
            UpdateWindowBackdrop();
        }
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (_titleBar?.FindName(TitlebarControl._PART_TitleBar_Text) is not TextBlock txtEl) return;

        // change title bar text opacity according to window activation state
        if (e.WindowActivationState == WindowActivationState.Deactivated)
        {
            txtEl.Opacity = 0.5;
        }
        else
        {
            txtEl.Opacity = 1;
        }
    }

    private void UIReporter_Report(AppIconChangedEventArgs e)
    {
        if (e.Bytes == null) return;

        // create new icon handle
        _appIconHandle = IconApi.CreateHIcon(e.Bytes, e.Size, e.Size);

        // update taskbar icon
        IconApi.SetTaskbarIcon(WindowHandle, _appIconHandle);

    }


    /// <summary>
    /// Set titlebar for window.
    /// </summary>
    public void SetTitlebar(TitlebarControl? titlebar = null)
    {
        _titleBar = titlebar;

        // set title bar
        _window.ExtendsContentIntoTitleBar = _titleBar != null;
        _window.SetTitleBar(_titleBar);

        UpdateTitleBarSize();
    }


    /// <summary>
    /// Updates the title bar size of this window.
    /// </summary>
    public void UpdateTitleBarSize()
    {
        // update title bar size according to API
        TitleBarRightInset = _window.AppWindow.TitleBar.RightInset;
    }


    /// <summary>
    /// Updates the color mode of this window.
    /// </summary>
    public void UpdateWindowColorMode()
    {
        var root = (FrameworkElement)_window.Content;

        if (AP.Config.Theme.Settings.IsDarkMode)
        {
            root.RequestedTheme = ElementTheme.Dark;
            _window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Dark;
        }
        else
        {
            root.RequestedTheme = ElementTheme.Light;
            _window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Light;
        }
    }


    /// <summary>
    /// Updates icon for window, optional for taskbar.
    /// </summary>
    public void UpdateWindowIcon()
    {
        // 1. get full path of icon
        var iconPath = AP.Config.Theme.GetIconPath(IgThemeIcon.AppLogo);
        var useDefaultIcon = !File.Exists(iconPath);

        if (useDefaultIcon)
        {
            // get default logo icon if theme's app logo does not exist
            iconPath = BHelper.BaseDir(Dir.Assets, "icon256.ico");
            _window.AppWindow.SetIcon(iconPath);
        }


        // 2. set custom title bar icon
        if (_titleBar?.FindName(TitlebarControl._PART_TitleBar_Icon) is ImageIcon iconEl)
        {
            try
            {
                // try to get icon URI from the given path
                var iconUri = new Uri(iconPath);

                if (iconPath.EndsWith(".SVG", StringComparison.OrdinalIgnoreCase))
                {
                    iconEl.Source = new SvgImageSource(iconUri);
                }
                else
                {
                    iconEl.Source = new BitmapImage(iconUri)
                    {
                        DecodePixelWidth = 32,
                        DecodePixelHeight = 32,
                    };
                }
            }
            catch { }
        }
        if (useDefaultIcon) return;


        // 3. set icon for taskbar & native titlebar
        var size = (int)DpiScale * 32;
        _ = Task.Run(async () =>
        {
            var bytes = await MagickDecoder.QuickDecodeAsync(iconPath, size, size, MagickFormat.Bgra);
            _uiReporter.Report(new AppIconChangedEventArgs(bytes, size));
        });
    }


    /// <summary>
    /// Updates window backdrop according to user config.
    /// </summary>
    public void UpdateWindowBackdrop()
    {
        // check if background has transparency
        var isTransparentToolbar = AP.Config.Theme.ComputedColors.ToolbarBgColor.A < 255;
        var isTransparentViewer = AP.Config.Theme.ComputedColors.BgColor.A < 255;
        var isTransparentGallery = AP.Config.Theme.ComputedColors.GalleryBgColor.A < 255;
        // TODO: check for Config.BackgroundColor

        var hasTransparency = isTransparentToolbar || isTransparentViewer || isTransparentGallery;

        // has transparency => support all backdrop
        if (hasTransparency)
        {
            SetWindowBackdrop(AP.Config.WindowBackdrop);
        }
        // no transparency => no backdrop
        else
        {
            SetWindowBackdrop(BackdropStyle.None);
        }
    }


    /// <summary>
    /// Sets window backdrop style.
    /// </summary>
    public void SetWindowBackdrop(BackdropStyle style)
    {
        if (_backdropStyle == style) return;

        // get backdrop
        SystemBackdrop? backdrop = style switch
        {
            BackdropStyle.Mica => new MicaBackdrop(),
            BackdropStyle.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
            BackdropStyle.Acrylic => new DesktopAcrylicBackdrop(),
            BackdropStyle.AcrylicThin => new AcrylicBackdrop(DesktopAcrylicKind.Thin),
            BackdropStyle.Transparent => new TransparentBackdrop(_msgMonitor),
            _ => null,
        };


        // set backdrop
        _backdropStyle = style;
        _window.SystemBackdrop = backdrop;
    }


    /// <summary>
    /// Sets the owner of this window.
    /// </summary>
    public void SetWindowOwner(Window owner)
    {
        var ownerHandle = WindowNative.GetWindowHandle(owner);

        WindowApi.SetWindowOwner(WindowHandle, ownerHandle);
    }

}


public class AppIconChangedEventArgs(byte[]? bytes, int size) : EventArgs
{
    public byte[]? Bytes { get; set; } = bytes;
    public int Size { get; set; } = size;
}

