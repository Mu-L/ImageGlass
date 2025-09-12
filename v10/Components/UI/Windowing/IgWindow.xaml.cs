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
using ImageMagick;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WinRT.Interop;

namespace ImageGlass.UI;


/// <summary>
/// An empty window that suppors theme and reactivity.
/// </summary>
public partial class IgWindow : Window, INotifyPropertyChanged
{
    #region INotifyPropertyChanged Implementation

    // to manage PropertyChanged events
    private List<PropertyChangedEventHandler> _propertyChangedEvents = [];
    private event PropertyChangedEventHandler? _propertyChanged;


    #region IgReactive > Properties & Events

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            if (value is not null)
            {
                _propertyChanged += value;
                _propertyChangedEvents.Add(value);
            }
        }
        remove
        {
            if (value is not null)
            {
                _propertyChanged -= value;
                _propertyChangedEvents.Remove(value);
            }
        }
    }


    /// <summary>
    /// Suspends the <see cref="PropertyChanged"/> event.
    /// </summary>
    public bool SuspendReactivity { get; set; } = false;

    #endregion // IgReactive > Properties & Events


    #region IgReactive > Methods

    /// <summary>
    /// Raises event <see cref="PropertyChanged"/>,
    /// returns <c>False</c> if the event is suspended.
    /// </summary>
    public bool OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        return OnPropertyChanged(propertyName, null, null);
    }


    /// <summary>
    /// Raises event <see cref="PropertyChanged"/>,
    /// returns <c>False</c> if the event is suspended.
    /// </summary>
    public bool OnPropertyChanged(object? value, object? oldValue, [CallerMemberName] string? propertyName = null)
    {
        if (SuspendReactivity) return false;

        _propertyChanged?.Invoke(this, new ReactiveEventArgs(propertyName, value, oldValue));
        return true;
    }


    /// <summary>
    /// Clears event handlers list of <see cref="PropertyChanged"/>.
    /// </summary>
    public void CleanUpPropertyChangedEvents()
    {
        // remove PropertyChanged events
        foreach (var eventHandler in _propertyChangedEvents)
        {
            _propertyChanged -= eventHandler;
        }
        _propertyChangedEvents.Clear();
    }


    /// <summary>
    /// Runs an action without triggering <see cref="PropertyChanged"/> event.
    /// </summary>
    public void WithNoReactive(Action fn)
    {
        SuspendReactivity = true;
        fn();
        SuspendReactivity = false;
    }

    #endregion IgReactive > Methods


    #endregion // INotifyPropertyChanged Implementation


    protected readonly WindowMessageMonitor _msgMonitor;
    protected readonly IProgress<AppIconChangedEventArgs> _uiReporter;

    protected BackdropStyle _actualBackdropStyle = BackdropStyle.None;
    protected nint _windowIconHandle = IntPtr.Zero;


    #region Control Properties

    /// <summary>
    /// Gets window handle.
    /// </summary>
    public nint Handle => WindowNative.GetWindowHandle(this);


    /// <summary>
    /// Gets DPI scale of the window.
    /// </summary>
    public double DpiScale => WindowApi.GetDpiScaleForWindow(Handle);


    /// <summary>
    /// Gets, sets the content of window.
    /// </summary>
    public FrameworkElement WindowContent
    {
        get => (FrameworkElement)PART_WindowContent.Content;
        set => PART_WindowContent.Content = value;
    }


    /// <summary>
    /// Gets, sets the data context for <see cref="WindowContent"/>.
    /// </summary>
    public object WindowContentDataContext
    {
        get => WindowContent.DataContext;
        set
        {
            if (WindowContent.DataContext != value)
            {
                // update view model in content dialog
                WindowContent.DataContext = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the title of window.
    /// </summary>
    public string? WindowTitle
    {
        get => Title;
        set
        {
            if (value != Title)
            {
                Title = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets the titlebar control.
    /// </summary>
    public TitlebarControl TitleBar => PART_Titlebar;


    /// <summary>
    /// Gets, sets the title bar's right inset width of <see cref="MainWindow"/>.
    /// </summary>
    protected double TitleBarRightInset
    {
        get => field / DpiScale;
        set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(TitleBarPadding));
            }
        }
    } = 0;


    /// <summary>
    /// Gets the title bar padding of <see cref="MainWindow"/>.
    /// </summary>
    protected Thickness TitleBarPadding => new Thickness(0, 0, TitleBarRightInset, 0);


    /// <summary>
    /// Gets, sets the value indicates that the window backdrop
    /// should be used only if the window background color has transparency.
    /// </summary>
    public bool UseBackdropForTransparentWindowOnly
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = true;


    /// <summary>
    /// Gets, sets the window backdrop style.
    /// </summary>
    public BackdropStyle BackdropStyle
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();

                UpdateWindowBackdrop();
            }
        }
    } = AP.Config.WindowBackdrop;


    #endregion // Control Properties



    public IgWindow()
    {
        InitializeComponent();

        // setup window style
        SetupWindowTitlebar();
        _msgMonitor = new WindowMessageMonitor(Handle);
        _uiReporter = new Progress<AppIconChangedEventArgs>(UIReporter_Report);

        // setup events
        AP.ThemeChanged += AP_ThemeChanged;
        PART_WindowContent.Loaded += PART_WindowContent_Loaded;
        Closed += IgWindow_Closed;
        VisibilityChanged += IgWindow_VisibilityChanged;
        Activated += IgWindow_Activated;
        SizeChanged += IgWindow_SizeChanged;
    }



    #region Window Events

    private void PART_WindowContent_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTitleBarSize();
        UpdateWindowColorMode();
        UpdateWindowIcon();
        UpdateWindowBackdrop();

        OnIgLoaded((FrameworkElement)sender);
    }


    private void IgWindow_Closed(object sender, WindowEventArgs e)
    {
        OnIgClosed(e);


        CleanUpPropertyChangedEvents();

        AP.ThemeChanged -= AP_ThemeChanged;
        PART_WindowContent.Loaded -= PART_WindowContent_Loaded;
        Closed -= IgWindow_Closed;
        VisibilityChanged -= IgWindow_VisibilityChanged;
        Activated -= IgWindow_Activated;
        SizeChanged -= IgWindow_SizeChanged;

        _msgMonitor.Dispose();
        IconApi.DestroyHIcon(_windowIconHandle);

        if (WindowContentDataContext is IDisposable dc) dc.Dispose();
    }


    private void IgWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (TitleBar.FindName(TitlebarControl._PART_TitleBar_Text) is not TextBlock txtEl) return;

        // change title bar text opacity according to window activation state
        if (e.WindowActivationState == WindowActivationState.Deactivated)
        {
            txtEl.Opacity = 0.5;
        }
        else
        {
            txtEl.Opacity = 1;
        }

        OnIgActivated(e);
    }


    private void IgWindow_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs e)
    {
        OnIgVisibilityChanged(e);
    }


    private void IgWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        OnIgSizeChanged(e);
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


        OnIgThemeChanged(e);
    }


    private void WinHook_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }


    private void UIReporter_Report(AppIconChangedEventArgs e)
    {
        if (e.IconData == null) return;

        // create new icon handle
        _windowIconHandle = IconApi.CreateHIcon(e.IconData, e.Size, e.Size);

        // update taskbar icon
        IconApi.SetTaskbarIcon(Handle, _windowIconHandle);

    }


    #endregion // Window Events



    #region Virtual Methods

    /// <summary>
    /// Occurs when the window is loaded.
    /// </summary>
    protected virtual void OnIgLoaded(FrameworkElement fe) { }


    /// <summary>
    /// Occurs when the window is closed.
    /// </summary>
    protected virtual void OnIgClosed(WindowEventArgs e) { }


    /// <summary>
    /// Occurs when the window visibility is changed.
    /// </summary>
    protected virtual void OnIgVisibilityChanged(WindowVisibilityChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the window is activated.
    /// </summary>
    protected virtual void OnIgActivated(WindowActivatedEventArgs e) { }


    /// <summary>
    /// Occurs when the window size is changed.
    /// </summary>
    protected virtual void OnIgSizeChanged(WindowSizeChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the app theme is changed.
    /// </summary>
    protected virtual void OnIgThemeChanged(ThemePackChangedEventArgs e) { }

    #endregion // Virtual Methods



    #region Internal Methods

    /// <summary>
    /// Updates the title bar size of this window.
    /// </summary>
    protected void UpdateTitleBarSize()
    {
        // update title bar size according to API
        TitleBarRightInset = AppWindow.TitleBar.RightInset;
    }


    /// <summary>
    /// Set titlebar for window.
    /// </summary>
    protected void SetupWindowTitlebar()
    {
        // set title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(PART_Titlebar);

        UpdateTitleBarSize();
    }


    /// <summary>
    /// Updates the color mode of this window.
    /// </summary>
    protected void UpdateWindowColorMode()
    {
        try
        {
            var root = (FrameworkElement)Content;

            if (AP.Config.Theme.Settings.IsDarkMode)
            {
                root.RequestedTheme = ElementTheme.Dark;
                AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Dark;
            }
            else
            {
                root.RequestedTheme = ElementTheme.Light;
                AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Light;
            }
        }
        catch (Exception)
        {
            // COMException: DCOMPOSITION_ERROR_SURFACE_BEING_RENDERED
        }
    }


    /// <summary>
    /// Updates icon for window, optional for taskbar.
    /// </summary>
    protected void UpdateWindowIcon()
    {
        // 1. get full path of icon
        var iconPath = AP.Config.Theme.GetIconPath(IgThemeIcon.AppLogo);
        var useDefaultIcon = !File.Exists(iconPath);

        if (useDefaultIcon)
        {
            // get default logo icon if theme's app logo does not exist
            iconPath = BHelper.BaseDir(Dir.Assets, "icon256.ico");
            AppWindow.SetIcon(iconPath);
        }


        // 2. set custom title bar icon
        if (TitleBar.FindName(TitlebarControl._PART_TitleBar_Icon) is ImageIcon iconEl)
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
    protected void UpdateWindowBackdrop()
    {
        if (!UseBackdropForTransparentWindowOnly)
        {
            SetWindowBackdrop(BackdropStyle);
            return;
        }


        // check if background has transparency
        var isTransparentToolbar = AP.Config.Theme.ComputedColors.ToolbarBgColor.A < 255;
        var isTransparentViewer = AP.Config.Theme.ComputedColors.BgColor.A < 255;
        var isTransparentGallery = AP.Config.Theme.ComputedColors.GalleryBgColor.A < 255;
        // TODO: check for Config.BackgroundColor

        var hasTransparency = isTransparentToolbar || isTransparentViewer || isTransparentGallery;

        // has transparency => support all backdrop
        if (hasTransparency)
        {
            SetWindowBackdrop(BackdropStyle);
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
    protected void SetWindowBackdrop(BackdropStyle style)
    {
        if (_actualBackdropStyle == style) return;

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
        _actualBackdropStyle = style;
        SystemBackdrop = backdrop;
    }


    #endregion // Internal Methods



}



public class AppIconChangedEventArgs(byte[]? iconData, int size) : EventArgs
{
    public byte[]? IconData { get; set; } = iconData;
    public int Size { get; set; } = size;
}
