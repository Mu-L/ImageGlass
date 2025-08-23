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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;

namespace ImageGlass.Win64.UI;


public partial class IgClickable(ButtonBase control) : DisposableImpl
{
    private ButtonBase _control => control;

    private IgTheme _theme = new();
    private IgButtonStates _buttonStates = IgButtonStates.Normal;
    private bool _isCheckOnClick = false;
    private bool _isChecked = false;


    /// <summary>
    /// Gets, sets the theme instance.
    /// </summary>
    public IgTheme Theme
    {
        get => _theme;
        set
        {
            if (_theme != value)
            {
                _theme.PropertyChanged -= Theme_PropertyChanged;
                _theme = value;
                _theme.PropertyChanged += Theme_PropertyChanged;

                OnPropertyChanged();
                UpdateStyle();
            }
        }
    }

    private void Theme_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Theme.ColorBrushes))
        {
            UpdateStyle();
        }
    }


    /// <summary>
    /// Gets or sets the interaction states of the gallery button item.
    /// </summary>
    public IgButtonStates ButtonStates
    {
        get => _buttonStates;
        set
        {
            if (_buttonStates != value)
            {
                _buttonStates = value;
                UpdateStyle();
                OnPropertyChanged(nameof(ButtonStates));
            }
        }
    }


    /// <summary>
    /// Gets, sets the value indicating that the control is checked on click.
    /// </summary>
    public bool IsCheckOnClick
    {
        get => _isCheckOnClick;
        set
        {
            if (_isCheckOnClick != value)
            {
                _isCheckOnClick = value;
                UpdateStyle();
                OnPropertyChanged(nameof(IsCheckOnClick));
            }
        }
    }


    /// <summary>
    /// Gets or sets the check state of the control.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                UpdateStyle();
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }


    /// <summary>
    /// Update the style of control.
    /// </summary>
    public void UpdateStyle()
    {
        // normal style
        SolidColorBrush bgBrush = new(); // must not be null for interaction
        SolidColorBrush borderBrush = new();


        // checked style for background
        if (IsChecked)
        {
            bgBrush.Color = Theme.ColorBrushes.ToolbarItemSelectedColor;
        }


        // hover style
        if (ButtonStates.HasFlag(IgButtonStates.Hovered))
        {
            bgBrush.Color
                = borderBrush.Color
                = Theme.ColorBrushes.ToolbarItemHoverColor;
        }

        // pressed style
        else if (ButtonStates.HasFlag(IgButtonStates.Pressed))
        {
            bgBrush.Color
                = borderBrush.Color
                = Theme.ColorBrushes.ToolbarItemActiveColor;
        }

        // checked style for border
        if (IsChecked)
        {
            borderBrush.Color = Theme.ColorBrushes.ToolbarItemSelectedColor;
        }

        _control.Background = bgBrush;
        _control.BorderBrush = borderBrush;
    }


    public void SetStateForPreviewKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Space
            || e.Key == Windows.System.VirtualKey.Enter)
        {
            ButtonStates ^= IgButtonStates.Hovered;
            ButtonStates |= IgButtonStates.Pressed;
            _control.ClickMode = ClickMode.Press;

            if (IsCheckOnClick) IsChecked = !IsChecked;
        }
    }

    public void SetStateForPreviewKeyUp(KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Space
            || e.Key == Windows.System.VirtualKey.Enter)
        {
            ButtonStates ^= IgButtonStates.Pressed;
            ButtonStates ^= IgButtonStates.Hovered;
            _control.ClickMode = ClickMode.Release;
        }
    }


    public void SetStateForPointerEntered()
    {
        ButtonStates ^= IgButtonStates.Normal;
        ButtonStates |= IgButtonStates.Hovered;
    }

    public void SetStateForPointerExited()
    {
        ButtonStates ^= IgButtonStates.Hovered;
        ButtonStates |= IgButtonStates.Normal;
    }

    public void SetStateForPointerPressed()
    {
        ButtonStates ^= IgButtonStates.Hovered;
        ButtonStates |= IgButtonStates.Pressed;

        if (IsCheckOnClick) IsChecked = !IsChecked;
    }

    public void SetStateForPointerReleased(PointerRoutedEventArgs e)
    {
        ButtonStates ^= IgButtonStates.Pressed;
        if (e.Pointer.IsInContact) ButtonStates |= IgButtonStates.Hovered;
        else ButtonStates ^= IgButtonStates.Hovered;
    }
}


[Flags]
public enum IgButtonStates
{
    Normal = 1 << 1,
    Hovered = 1 << 2,
    Pressed = 1 << 3,
}

