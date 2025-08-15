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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;

namespace ImageGlass.Win64.UI;


public class IgClickable(ButtonBase control) : DependencyObject
{
    private ButtonBase _control => control;


    /// <summary>
    /// Gets or sets the interaction state of the gallery button item.
    /// </summary>
    public IgButtonStates State
    {
        get => (IgButtonStates)GetValue(StateProperty);
        set
        {
            SetValue(StateProperty, value);
            UpdateStyle();
        }
    }
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(
            nameof(State),
            typeof(IgButtonStates),
            typeof(GalleryButtonItem),
            new PropertyMetadata(IgButtonStates.Normal));


    /// <summary>
    /// Gets, sets the value indicating that the control is checkable.
    /// </summary>
    public bool IsCheckable
    {
        get => (bool)GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }
    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register(
            nameof(IsCheckable),
            typeof(bool),
            typeof(IgToolbarButton),
            new PropertyMetadata(default));


    /// <summary>
    /// Gets or sets the check state of the control.
    /// </summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set
        {
            SetValue(IsCheckedProperty, value);

            // set selected styles
            UpdateStyle();
        }
    }
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(IgToolbarButton),
            new PropertyMetadata(default));


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
        if (State.HasFlag(IgButtonStates.Hovered))
        {
            bgBrush
                = borderBrush
                = (Brush)(Application.Current.Resources["IgButtonBackgroundHovered"]);
        }

        // pressed style
        else if (State.HasFlag(IgButtonStates.Pressed))
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
            State ^= IgButtonStates.Hovered;
            State |= IgButtonStates.Pressed;
            _control.ClickMode = ClickMode.Press;

            if (IsCheckable) IsChecked = !IsChecked;
        }
    }

    public void SetStateForPreviewKeyUp(KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Space
            || e.Key == Windows.System.VirtualKey.Enter)
        {
            State ^= IgButtonStates.Pressed;
            State ^= IgButtonStates.Hovered;
            _control.ClickMode = ClickMode.Release;
        }
    }


    public void SetStateForPointerEntered()
    {
        State ^= IgButtonStates.Normal;
        State |= IgButtonStates.Hovered;
    }

    public void SetStateForPointerExited()
    {
        State ^= IgButtonStates.Hovered;
        State |= IgButtonStates.Normal;
    }

    public void SetStateForPointerPressed()
    {
        State ^= IgButtonStates.Hovered;
        State |= IgButtonStates.Pressed;

        if (IsCheckable) IsChecked = !IsChecked;
    }

    public void SetStateForPointerReleased(PointerRoutedEventArgs e)
    {
        State ^= IgButtonStates.Pressed;
        if (e.Pointer.IsInContact) State |= IgButtonStates.Hovered;
        else State ^= IgButtonStates.Hovered;
    }
}


[Flags]
public enum IgButtonStates
{
    Normal = 1 << 1,
    Hovered = 1 << 2,
    Pressed = 1 << 3,
}

