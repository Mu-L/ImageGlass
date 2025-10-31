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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ImageGlass.UI;


/// <summary>
/// Provides a base content control with theme and reactivity support.
/// </summary>
public partial class IgControl : ContentControl, INotifyPropertyChanged
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


    /// <summary>
    /// Gets, sets the visibility of the content of this control.
    /// </summary>
    public bool IsContentVisible
    {
        get; set
        {
            if (field != value)
            {
                field = value;

                if (Content is FrameworkElement fe)
                {
                    fe.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
                _ = OnPropertyChanged();
            }
        }
    } = true;


    /// <summary>
    /// Gets DPI scaling.
    /// </summary>
    public double DpiScale => this.XamlRoot.RasterizationScale;


    public IgControl()
    {
        DefaultStyleKey = typeof(IgControl);

        Loaded += IgControl_Loaded;
    }


    #region Control Events

    private void IgControl_Loaded(object sender, RoutedEventArgs e)
    {
        OnIgLanguageChanged();
        OnIgLoaded((FrameworkElement)sender);

        Unloaded += IgControl_Unloaded;
        SizeChanged += IgControl_SizeChanged;
        DataContextChanged += IgControl_DataContextChanged;

        AP.ThemeChanged += AP_ThemeChanged;
        AP.LanguageChanged += AP_LanguageChanged;
    }


    private void IgControl_Unloaded(object sender, RoutedEventArgs e)
    {
        CleanUpPropertyChangedEvents();

        AP.ThemeChanged -= AP_ThemeChanged;
        AP.LanguageChanged -= AP_LanguageChanged;

        Unloaded -= IgControl_Unloaded;
        SizeChanged -= IgControl_SizeChanged;
        DataContextChanged -= IgControl_DataContextChanged;

        OnIgUnloaded((FrameworkElement)sender);
    }


    private void IgControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        OnIgSizeChanged((FrameworkElement)sender, e);
    }


    private void IgControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
    {
        OnIgDataContextChanged(sender, e);
    }


    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        OnIgThemeChanged(e);
    }


    private void AP_LanguageChanged(object? sender, EventArgs e)
    {
        OnIgLanguageChanged();
    }

    #endregion // Control Events


    #region Virtual Methods

    /// <summary>
    /// Occurs when the control is loaded.
    /// </summary>
    protected virtual void OnIgLoaded(FrameworkElement fe) { }


    /// <summary>
    /// Occurs when the control is unloaded.
    /// </summary>
    protected virtual void OnIgUnloaded(FrameworkElement fe) { }


    /// <summary>
    /// Occurs when the control size is changed.
    /// </summary>
    protected virtual void OnIgSizeChanged(FrameworkElement fe, SizeChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the control data context is changed.
    /// </summary>
    protected virtual void OnIgDataContextChanged(FrameworkElement fe, DataContextChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the app theme is changed.
    /// </summary>
    protected virtual void OnIgThemeChanged(ThemePackChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the app language is changed.
    /// </summary>
    protected virtual void OnIgLanguageChanged() { }

    #endregion // Virtual Methods



}

