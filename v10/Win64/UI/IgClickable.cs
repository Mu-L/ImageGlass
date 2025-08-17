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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;

namespace ImageGlass.Win64.UI;


public partial class IgClickable(ButtonBase control) : DisposableImpl
{
    private ButtonBase _control => control;

    private IgButtonStates _buttonStates = IgButtonStates.Normal;
    private bool _isCheckOnClick = false;
    private bool _isChecked = false;


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
        Brush bgBrush = new SolidColorBrush(); // must not be null for interaction
        Brush? borderBrush = null;


        // checked style for background
        if (IsChecked)
        {
            bgBrush = (Brush)(Application.Current.Resources["IgButtonBackgroundSelected"]);
        }


        // hover style
        if (ButtonStates.HasFlag(IgButtonStates.Hovered))
        {
            bgBrush
                = borderBrush
                = (Brush)(Application.Current.Resources["IgButtonBackgroundHovered"]);
        }

        // pressed style
        else if (ButtonStates.HasFlag(IgButtonStates.Pressed))
        {
            bgBrush
                = borderBrush
                = (Brush)(Application.Current.Resources["IgButtonBackgroundPressed"]);
        }

        // checked style for border
        if (IsChecked)
        {
            borderBrush = (Brush)(Application.Current.Resources["IgButtonBorderSelected"]);
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

