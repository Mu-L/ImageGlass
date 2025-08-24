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
using ImageGlass.Win64.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageGlass.Win64.UI;

public partial class IgButton : Button, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected IgTheme _theme = new();
    protected string _id = "";
    protected string _text = "";
    protected bool _isToggle = false;
    protected bool _isChecked = false;

    protected long _tokenIsPointerOverChanged = 0;
    protected long _tokenIsPressedChanged = 0;


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets, sets the theme instance.
    /// </summary>
    public IgTheme Theme
    {
        get => (IgTheme)GetValue(ThemeProperty);
        set
        {
            value.PropertyChanged -= Theme_PropertyChanged;
            SetValue(ThemeProperty, value);
            value.PropertyChanged += Theme_PropertyChanged;

            UpdateStyle();
        }
    }
    public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(nameof(Theme), typeof(IgTheme), typeof(IgButton), new PropertyMetadata(new IgTheme()));
    private void Theme_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Theme.ColorBrushes))
        {
            UpdateStyle();
        }
    }



    /// <summary>
    /// Gets, sets the value indicating that the control is checked on click.
    /// </summary>
    public bool IsToggle
    {
        get => _isToggle;
        set
        {
            if (value != _isToggle)
            {
                _isToggle = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets or sets the ID of this control.
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets or sets the text of this control.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion // Public Properties


    public IgButton()
    {
        DefaultStyleKey = typeof(IgButton);

        _tokenIsPointerOverChanged = RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, IgButton_StateChanged);
        _tokenIsPressedChanged = RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, IgButton_StateChanged);

        Loaded += IgButton_Loaded;
        Unloaded += IgButton_Unloaded;
        Click += IgButton_Click;
    }


    // Control Events
    #region Control Events

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateStyle();
    }

    /// <summary>
    /// Emits event <see cref="PropertyChanged"/>.
    /// </summary>
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void IgButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (Flyout is null) return;

        Flyout.Opened += Flyout_Opened;
        Flyout.Closed += Flyout_Closed;
    }

    private void IgButton_Unloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= IgButton_Loaded;
        Unloaded -= IgButton_Unloaded;
        Click -= IgButton_Click;

        UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, _tokenIsPointerOverChanged);
        UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, _tokenIsPressedChanged);
    }

    private void IgButton_StateChanged(DependencyObject sender, DependencyProperty dp)
    {
        UpdateStyle();
    }

    private void IgButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsToggle) IsChecked = !IsChecked;
    }

    private void Flyout_Opened(object? sender, object e)
    {
        IsChecked = true;
    }

    private void Flyout_Closed(object? sender, object e)
    {
        IsChecked = false;
    }

    #endregion // Control Events


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

        Background = bgBrush;
        BorderBrush = borderBrush;
    }


}
