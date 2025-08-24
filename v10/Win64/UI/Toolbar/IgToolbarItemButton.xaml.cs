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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;


namespace ImageGlass.Win64.UI;

public partial class IgToolbarItemButton : UserControl, IIgToolbarItem
{
    public static string _PART_ButtonIcon => "PART_ButtonIcon";
    public static string _PART_ButtonText => "PART_ButtonText";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected IgTheme _theme = new();
    protected ToolbarItemModel _vm = new();


    // Public Properties
    #region Public Properties

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
                _theme = value;
                OnPropertyChanged();
                UpdateIcon();
            }
        }
    }


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
        get => BtnActivator.Flyout;
        set => BtnActivator.Flyout = value;
    }

    #endregion // Public Properties


    public IgToolbarItemButton()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Emits event <see cref="PropertyChanged"/>.
    /// </summary>
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void BtnActivator_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
            var themeIconName = Theme.GetToolbarIconPath(themeIconNameEnum);
            if (string.IsNullOrWhiteSpace(themeIconName)) return;

            // theme icon path
            svgPath = Path.Combine(Theme.FolderPath, themeIconName);
            if (!File.Exists(svgPath)) return;

            // set icon
            PART_ButtonIcon.Source = new SvgImageSource(new Uri(svgPath));
        }
        catch { }
    }


}
