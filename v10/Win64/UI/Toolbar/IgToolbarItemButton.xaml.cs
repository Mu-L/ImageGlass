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
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;


namespace ImageGlass.Win64.UI;

public partial class IgToolbarItemButton : UserControl, IIgToolbarItem
{
    #region INotifyPropertyChanged Implementation

    // to manage PropertyChanged events
    private List<PropertyChangedEventHandler> _propertyChangedEvent = new();
    private event PropertyChangedEventHandler? _propertyChangedHandler;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            if (value != null)
            {
                _propertyChangedHandler += value;
                _propertyChangedEvent.Add(value);
            }
        }

        remove
        {
            if (value != null)
            {
                _propertyChangedHandler -= value;
                _propertyChangedEvent.Remove(value);
            }
        }
    }


    /// <summary>
    /// Emits event <see cref="PropertyChanged"/>.
    /// </summary>
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        _propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    /// Clears event handlers list of <see cref="PropertyChanged"/>.
    /// </summary>
    public void ClearPropertyChangedEvents()
    {
        // remove PropertyChanged events
        foreach (var eventHandler in _propertyChangedEvent)
        {
            _propertyChangedHandler -= eventHandler;
        }
        _propertyChangedEvent.Clear();
    }

    #endregion // INotifyPropertyChanged Implementation


    public static string _PART_Button => "PART_Button";
    public static string _PART_ButtonIcon => "PART_ButtonIcon";
    public static string _PART_ButtonText => "PART_ButtonText";
    public static double InnerSpacing => AP.Config.ToolbarIconHeight / 6f; // 4
    public static Thickness ItemPadding => new(AP.Config.ToolbarIconHeight / 4.8f); // 5


    protected FlyoutBase? _flyout = null;
    protected ToolbarItemModel _vm = new();


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets, sets view model for the control.
    /// </summary>
    public ToolbarItemModel VM
    {
        get => _vm;
        set
        {
            if (_vm != value)
            {
                _vm = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets or sets the flyout associated with this button.
    /// </summary>
    public FlyoutBase? Flyout
    {
        get => _flyout;
        set
        {
            if (_flyout != value)
            {
                _flyout = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion // Public Properties


    public IgToolbarItemButton()
    {
        InitializeComponent();

        AP.ThemeChanged += AP_ThemeChanged;
        Loaded += IgToolbarItemButton_Loaded;
        Unloaded += IgToolbarItemButton_Unloaded;
    }


    private void IgToolbarItemButton_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateIcon();
    }


    private void IgToolbarItemButton_Unloaded(object sender, RoutedEventArgs e)
    {
        AP.ThemeChanged -= AP_ThemeChanged;
        Unloaded -= IgToolbarItemButton_Unloaded;

        ClearPropertyChangedEvents();
    }


    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        UpdateIcon();
    }


    /// <summary>
    /// Updates icon.
    /// </summary>
    private void UpdateIcon()
    {
        if (string.IsNullOrWhiteSpace(VM.Image)) return;
        var svgPath = "";

        try
        {
            // absolute path
            if (File.Exists(VM.Image)) return;

            // get toolbar icon enum from theme
            if (!Enum.TryParse<IgThemeIcon>(VM.Image, out var themeIconNameEnum)) return;

            // get icon file name from theme
            var themeIconName = AP.Config.Theme.GetToolbarIconPath(themeIconNameEnum);
            if (string.IsNullOrWhiteSpace(themeIconName)) return;

            // theme icon path
            svgPath = Path.Combine(AP.Config.Theme.FolderPath, themeIconName);
            if (!File.Exists(svgPath)) return;

            // set icon
            PART_ButtonIcon.Source = new SvgImageSource(new Uri(svgPath));
        }
        catch { }
    }


}
