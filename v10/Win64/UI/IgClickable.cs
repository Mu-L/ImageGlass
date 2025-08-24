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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace ImageGlass.Win64.UI;


public partial class IgClickable : DisposableImpl
{
    protected ButtonBase _control;
    protected IgTheme _theme = new();


    protected long _tokenIsPointerOverChanged = 0;
    protected long _tokenIsPressedChanged = 0;

    protected bool _isPointerOver = false;
    protected bool _isPressed = false;

    protected bool _isToggle = false;
    protected bool _isChecked = false;



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
    /// Gets a value that indicates whether a pointer is located over this control.
    /// </summary>
    public bool IsPointerOver => _isPointerOver;


    /// <summary>
    /// Gets a value that indicates whether this control is currently in a pressed state.
    /// </summary>
    public bool IsPressed => _isPressed;


    /// <summary>
    /// Gets, sets the value indicating that the control is checked on click.
    /// </summary>
    public bool IsToggle
    {
        get => _isToggle;
        set
        {
            if (_isToggle != value)
            {
                _isToggle = value;
                UpdateStyle();
                OnPropertyChanged(nameof(IsToggle));
            }
        }
    }


    /// <summary>
    /// Gets, sets check state for this control.
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


    public IgClickable(ButtonBase control)
    {
        _control = control;

        _tokenIsPointerOverChanged = _control.RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, IsPointerOver_Changed);
        _tokenIsPressedChanged = _control.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, IsPressed_Changed);

        _control.Unloaded += Control_Unloaded;
        _control.Click += Control_Click;
    }


    private void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        _control.Unloaded -= Control_Unloaded;
        _control.Click -= Control_Click;

        _control.UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, _tokenIsPointerOverChanged);
        _control.UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, _tokenIsPressedChanged);
    }


    private void IsPointerOver_Changed(DependencyObject sender, DependencyProperty dp)
    {
        if (sender is not ButtonBase btn) return;

        _isPointerOver = btn.IsPointerOver;
        OnPropertyChanged(nameof(IsPointerOver));
        UpdateStyle();
    }


    private void IsPressed_Changed(DependencyObject sender, DependencyProperty dp)
    {
        if (sender is not ButtonBase btn) return;

        _isPressed = btn.IsPressed;
        OnPropertyChanged(nameof(IsPressed));
        UpdateStyle();
    }


    private void Control_Click(object sender, RoutedEventArgs e)
    {
        if (IsToggle) IsChecked = !IsChecked;
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
        if (IsPointerOver)
        {
            bgBrush.Color
                = borderBrush.Color
                = Theme.ColorBrushes.ToolbarItemHoverColor;
        }

        // pressed style
        else if (IsPressed)
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

}

