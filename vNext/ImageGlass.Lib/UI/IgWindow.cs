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
using Avalonia.Media;
using ImageGlass.Common.Types;
using System;

namespace ImageGlass.Lib.UI;


public partial class IgWindow : Window
{

    #region Public Properties

    /// <summary>
    /// Gets the handle of this window.
    /// </summary>
    public nint Handle => GetTopLevel(this)?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;



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
        AvaloniaProperty.Register<Window, bool>(nameof(IsFrameless), true);


    #endregion // Public Properties



    public IgWindow()
    {
        OnBackdropStyleChanged(BackdropStyle);
        OnFramelessModeChanged(IsFrameless);
    }



    #region Override methods

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // BackdropStyle
        if (e.Property == BackdropStyleProperty)
        {
            OnBackdropStyleChanged((BackdropStyle)e.NewValue!);
        }

        // IsFrameless
        else if (e.Property == IsFramelessProperty)
        {
            OnFramelessModeChanged((bool)e.NewValue!);
        }
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);


        // check if the hotkey for closing window is pressed
        foreach (var hk in CloseWindowHotkeys)
        {
            if (hk.IsSame(e.Key, e.KeyModifiers))
            {
                Close();
                break;
            }
        }
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


    #endregion // Override methods




    /// <summary>
    /// Updates backdrop style of the window.
    /// </summary>
    protected virtual void OnBackdropStyleChanged(BackdropStyle style)
    {
        Background = style == BackdropStyle.None
            ? null
            : Brushes.Transparent;
    }


    protected virtual void OnFramelessModeChanged(bool enable)
    {
        if (enable)
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = 100;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        }
        else
        {
            ExtendClientAreaToDecorationsHint = false;
        }
    }



}
