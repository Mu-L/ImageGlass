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
using ImageGlass.Win64.Common;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;


namespace ImageGlass.Win64.UI;

public partial class IgWindowHook : DisposableImpl
{
    private Window _window;
    private double _titleBarHeight = 0;
    private double _titleBarRightInset = 0;


    // Public Properties 
    #region Public Properties

    /// <summary>
    /// Gets the window handle.
    /// </summary>
    public nint WindowHandle => WindowNative.GetWindowHandle(_window);


    /// <summary>
    /// Gets DPI scale of the window.
    /// </summary>
    public double DpiScale => _window.Content.XamlRoot?.RasterizationScale ?? 1;


    /// <summary>
    /// Gets, set the title of <see cref="MainWindow"/>.
    /// </summary>
    public string? Title
    {
        get => _window.Title;
        set
        {
            if (value != _window.Title)
            {
                _window.Title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }


    /// <summary>
    /// Gets, sets the title bar height of <see cref="MainWindow"/>.
    /// </summary>
    public double TitleBarHeight
    {
        get => _titleBarHeight / DpiScale;
        set
        {
            if (_titleBarHeight != value)
            {
                _titleBarHeight = value;
                OnPropertyChanged(nameof(TitleBarHeight));
            }
        }
    }


    /// <summary>
    /// Gets, sets the title bar's right inset width of <see cref="MainWindow"/>.
    /// </summary>
    public double TitleBarRightInset
    {
        get => _titleBarRightInset / DpiScale;
        set
        {
            if (_titleBarRightInset != value)
            {
                _titleBarRightInset = value;
                OnPropertyChanged(nameof(TitleBarRightInset));
                OnPropertyChanged(nameof(TitleBarPadding));
            }
        }
    }


    /// <summary>
    /// Gets the title bar padding of <see cref="MainWindow"/>.
    /// </summary>
    public Thickness TitleBarPadding => new Thickness(0, 0, TitleBarRightInset, 0);


    public static SystemBackdrop? WindowBackdrop
    {
        get
        {
            if (AP.Config.WindowBackdrop == BackdropStyle.None) return null;
            if (AP.Config.WindowBackdrop == BackdropStyle.Acrylic)
            {
                return new DesktopAcrylicBackdrop();
            }
            else
            {
                return new MicaBackdrop()
                {
                    Kind = AP.Config.WindowBackdrop == BackdropStyle.MicaAlt
                        ? MicaKind.BaseAlt
                        : MicaKind.Base
                };
            }
        }
    }

    #endregion // Public Properties


    public IgWindowHook(Window window, UIElement? customTitleBar = null)
    {
        _window = window;

        // set title bar
        if (customTitleBar != null)
        {
            _window.ExtendsContentIntoTitleBar = true;
            _window.SetTitleBar(customTitleBar);
        }

        AP.ThemeChanged += AP_ThemeChanged;

        var root = (FrameworkElement)_window.Content;
        root.Loaded += Root_Loaded;
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();

        AP.ThemeChanged -= AP_ThemeChanged;
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateWindowColorMode();
        UpdateTitleBarSize();
    }

    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        // a new theme just loaded
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            // update app color mode according to the theme's color mode
            UpdateWindowColorMode();
        }
    }


    /// <summary>
    /// Updates the title bar size of this window.
    /// </summary>
    public void UpdateTitleBarSize()
    {
        // update title bar size according to API
        TitleBarHeight = _window.AppWindow.TitleBar.Height;
        TitleBarRightInset = _window.AppWindow.TitleBar.RightInset;
    }


    /// <summary>
    /// Updates the color mode of this window.
    /// </summary>
    public void UpdateWindowColorMode()
    {
        // update app color mode according to the theme's color mode
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

}
