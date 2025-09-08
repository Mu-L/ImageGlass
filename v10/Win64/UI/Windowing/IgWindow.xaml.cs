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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinRT.Interop;

namespace ImageGlass.Win64.UI;


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


    protected IgWindowHook _winHook;


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
    /// Gets the titlebar control.
    /// </summary>
    public TitlebarControl TitleBar => PART_Titlebar;


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
    public object DialogContentDataContext
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


    #endregion // Control Properties



    public IgWindow()
    {
        InitializeComponent();

        _winHook = new(this, PART_Titlebar);
        _winHook.PropertyChanged += WinHook_PropertyChanged;

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

        _winHook.PropertyChanged -= WinHook_PropertyChanged;
        _winHook.Dispose();
    }


    private void IgWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
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
        OnIgThemeChanged(e);
    }


    private void WinHook_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
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




}
