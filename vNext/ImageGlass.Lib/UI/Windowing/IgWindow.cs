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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using ImageGlass.Common;
using ImageGlass.Common.AppThemes;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ImageGlass.Lib.UI.Windowing;


public partial class IgWindow : Window
{
    #region Public Properties

    /// <summary>
    /// Gets the handle of this window.
    /// </summary>
    public nint Handle => GetTopLevel(this)?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;


    /// <summary>
    /// Gets the DPI scale of this window.
    /// </summary>
    public double DpiScale => VisualRoot?.RenderScaling ?? 1.0;



    /// <summary>
    /// Gets, sets the window backdrop style.
    /// </summary>
    public BackdropStyle BackdropStyle
    {
        get => GetValue(BackdropStyleProperty);
        set => SetValue(BackdropStyleProperty, value);
    }
    public static readonly StyledProperty<BackdropStyle> BackdropStyleProperty =
        AvaloniaProperty.Register<Window, BackdropStyle>(nameof(BackdropStyle), BackdropStyle.Mica);



    /// <summary>
    /// Gets, sets the hotkey to close the window with.
    /// </summary>
    public Hotkey[] CloseWindowHotkeys
    {
        get => GetValue(CloseWindowHotkeysProperty);
        set => SetValue(CloseWindowHotkeysProperty, value);
    }
    public static readonly StyledProperty<Hotkey[]> CloseWindowHotkeysProperty =
        AvaloniaProperty.Register<Window, Hotkey[]>(nameof(CloseWindowHotkeys), []);



    /// <summary>
    /// Gets, sets the frameless mode.
    /// </summary>
    public bool IsFrameless
    {
        get => GetValue(IsFramelessProperty);
        set => SetValue(IsFramelessProperty, value);
    }
    public static readonly StyledProperty<bool> IsFramelessProperty =
        AvaloniaProperty.Register<Window, bool>(nameof(IsFrameless), false);


    #endregion // Public Properties



    public IgWindow()
    {
        OnIgBackdropStyleChanged(BackdropStyle);
        OnIgFramelessModeChanged(IsFrameless);

        Core.ThemeChanged += Core_ThemeChanged;
        Core.LanguageChanged += Core_LanguageChanged;
    }



    #region Events & Override methods

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);


    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);


    }


    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        Core.ThemeChanged -= Core_ThemeChanged;
        Core.LanguageChanged -= Core_LanguageChanged;
    }


    private void Core_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        // a new theme just loaded
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            // update app icon
            _ = UpdateWindowIconAsync();
        }

        OnIgThemeChanged(e);
    }


    private void Core_LanguageChanged(object? sender, EventArgs e)
    {
        OnIgLanguageChanged();
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // BackdropStyle
        if (e.Property == BackdropStyleProperty)
        {
            OnIgBackdropStyleChanged((BackdropStyle)e.NewValue!);
        }

        // IsFrameless
        else if (e.Property == IsFramelessProperty)
        {
            OnIgFramelessModeChanged((bool)e.NewValue!);
        }
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        // check if the hotkey for closing window is pressed
        foreach (var hk in CloseWindowHotkeys)
        {
            if (hk.IsSame(e.Key, e.KeyModifiers))
            {
                OnIgCloseWindowHotkeyPressed(e);
                if (!e.Handled)
                {
                    e.Handled = true;
                    Close();
                    return;
                }

                break;
            }
        }


        base.OnKeyDown(e);
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Frameless mode: enable window dragging at top border
        if (IsFrameless)
        {
            var p = e.GetCurrentPoint(this);
            if (p.Position.Y < 15) BeginMoveDrag(e);
        }
    }


    #endregion // Events & Override methods


    /// <summary>
    /// Occurs when one of the hotkey for closing window is pressed.
    /// </summary>
    protected virtual void OnIgCloseWindowHotkeyPressed(KeyEventArgs e) { }


    /// <summary>
    /// Occurs whenthe backdrop style is changed.
    /// </summary>
    protected virtual void OnIgBackdropStyleChanged(BackdropStyle style)
    {
        Background = style == BackdropStyle.None
            ? null
            : Brushes.Transparent;
    }


    /// <summary>
    /// Occurs when the frameless mode is changed.
    /// </summary>
    /// <param name="enable"></param>
    protected virtual void OnIgFramelessModeChanged(bool enable)
    {
        if (enable)
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        }
        else
        {
            ExtendClientAreaToDecorationsHint = false;
        }
    }


    /// <summary>
    /// Occurs when the app theme is changed.
    /// </summary>
    protected virtual void OnIgThemeChanged(ThemePackChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the app language is changed.
    /// </summary>
    protected virtual void OnIgLanguageChanged() { }





    #region Internal Methods

    /// <summary>
    /// Updates icon for window and taskbar.
    /// </summary>
    protected async Task UpdateWindowIconAsync()
    {
        // 1. get full path of icon
        var iconPath = Core.Config.Theme.GetIconPath(IgThemeIcon.AppLogo);
        var useDefaultIcon = !File.Exists(iconPath);


        // 2. use default icon as logo
        if (useDefaultIcon)
        {
            // get default logo icon if theme's app logo does not exist
            using var stream = AssetLoader.Open(new Uri("avares://ImageGlass.Lib/Assets/icon256.ico"));
            Icon = new WindowIcon(stream);

            return;
        }


        // 2. use theme icon as logo
        // decode the logo
        var size = (int)DpiScale * 64;
        var bytes = await MagickCodec.QuickDecodeAsync(iconPath, ImageMagick.MagickFormat.Ico, size, size);
        if (bytes is null) return;

        // update icon
        using var ms = new MemoryStream(bytes);
        Icon = new WindowIcon(ms);
    }


    #endregion // Internal Methods


}
