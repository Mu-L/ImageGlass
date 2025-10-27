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
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Windows.UI;

namespace ImageGlass.UI;

public partial class IgButton : Button, INotifyPropertyChanged
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
    [JsonIgnore]
    public bool SuspendReactivity { get; set; } = false;

    #endregion // IgReactive > Properties & Events


    #region IgReactive > Methods

    /// <summary>
    /// Raises event <see cref="PropertyChanged"/>,
    /// returns <c>False</c> if the event is suspended.
    /// </summary>
    public bool OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        return OnPropertyChanged(null, null, propertyName);
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


    protected string _id = "";
    protected string _text = "";
    protected bool _isToggle = false;
    protected bool _isChecked = false;

    protected long _tokenIsPointerOverChanged = 0;
    protected long _tokenIsPressedChanged = 0;


    // Public Properties
    #region Public Properties

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
                UpdateStyle();
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
        CornerRadius = Const.BORDER_RADIUS;

        _tokenIsPointerOverChanged = RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, IgButton_StateChanged);
        _tokenIsPressedChanged = RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, IgButton_StateChanged);

        Loaded += IgButton_Loaded;
        Unloaded += IgButton_Unloaded;
        Click += IgButton_Click;

        AP.ThemeChanged += AP_ThemeChanged;
        AP.LanguageChanged += AP_LanguageChanged;
    }



    #region Control Events

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateStyle();
    }


    private void IgButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (Flyout is not null)
        {
            Flyout.Opened += Flyout_Opened;
            Flyout.Closed += Flyout_Closed;
        }

        OnIgLanguageChanged();
    }

    private void IgButton_Unloaded(object sender, RoutedEventArgs e)
    {
        AP.ThemeChanged -= AP_ThemeChanged;
        AP.LanguageChanged -= AP_LanguageChanged;

        Loaded -= IgButton_Loaded;
        Unloaded -= IgButton_Unloaded;
        Click -= IgButton_Click;

        CleanUpPropertyChangedEvents();
        UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, _tokenIsPointerOverChanged);
        UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, _tokenIsPressedChanged);
    }

    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        UpdateStyle();
        OnIgThemeChanged(e);
    }

    private void AP_LanguageChanged(object? sender, EventArgs e)
    {
        OnIgLanguageChanged();
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



    #region Virtual Methods

    /// <summary>
    /// Occurs when the app theme is changed.
    /// </summary>
    protected virtual void OnIgThemeChanged(ThemePackChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the app language is changed.
    /// </summary>
    protected virtual void OnIgLanguageChanged() { }


    protected virtual Color GetColorForText()
    {
        return AP.Config.Theme.ComputedColors.ToolbarTextColor;
    }

    protected virtual Color GetColorForDefault()
    {
        return new Color();
    }

    protected virtual Color GetColorForHovered()
    {
        return AP.Config.Theme.ComputedColors.ToolbarItemHoverColor;
    }

    protected virtual Color GetColorForPressed()
    {
        return AP.Config.Theme.ComputedColors.ToolbarItemActiveColor;
    }

    protected virtual Color GetColorForChecked()
    {
        return AP.Config.Theme.ComputedColors.ToolbarItemSelectedColor;
    }

    #endregion // Virtual Methods



    /// <summary>
    /// Update the style of control.
    /// </summary>
    public void UpdateStyle()
    {
        // normal style: must not be null for interaction
        SolidColorBrush bgBrush = new(GetColorForDefault());
        SolidColorBrush borderBrush = new(GetColorForDefault());


        // checked style for background
        if (IsChecked)
        {
            bgBrush.Color = GetColorForChecked();
        }


        // pressed style
        if (IsPressed)
        {
            bgBrush.Color = GetColorForPressed();
        }
        // hover style
        else if (IsPointerOver)
        {
            bgBrush.Color = GetColorForHovered();
        }


        // border style
        if (IsChecked || IsPointerOver || IsPressed)
        {
            var alpha = bgBrush.Color.A + (int)(bgBrush.Color.A / 1.5f);
            alpha = Math.Clamp(alpha, 0, 255);
            alpha = Math.Max(50, alpha);

            borderBrush.Color = bgBrush.Color.WithAlpha(alpha);
        }

        Background = bgBrush;
        BorderBrush = borderBrush;
        Foreground = GetColorForText().ToBrush();
    }


}
