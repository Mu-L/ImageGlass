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
using Microsoft.UI.Xaml.Controls;


namespace ImageGlass.Win64.UI;

public sealed partial class IgToolbarItemSeparator : UserControl, IIgToolbarItem
{

    // Dependency Properties
    #region Dependency Properties

    /// <summary>
    /// Gets, sets view model for the control.
    /// </summary>
    public ToolbarItemModel ViewModel
    {
        get => (ToolbarItemModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(ToolbarItemModel), typeof(IgToolbarItemButton), new PropertyMetadata(new ToolbarItemModel()));


    /// <summary>
    /// Gets, sets the theme instance.
    /// </summary>
    public IgTheme Theme
    {
        get => (IgTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }
    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(nameof(Theme), typeof(IgTheme), typeof(IgToolbarItemSeparator),
            new PropertyMetadata(new IgTheme()));

    #endregion // Dependency Properties


    public IgToolbarItemSeparator()
    {
        InitializeComponent();
    }
}
