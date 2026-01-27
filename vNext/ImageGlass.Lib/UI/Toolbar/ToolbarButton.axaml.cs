/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
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
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ImageGlass.Common;
using System;

namespace ImageGlass.UI;

public partial class ToolbarButton : ToggleButton, IToolbarItem
{
    protected override Type StyleKeyOverride => typeof(Button);
    public ToolbarItemModel VM => (ToolbarItemModel)DataContext!;


    public ToolbarButton()
    {
        InitializeComponent();
    }


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Core.Config.PropertyChanged += Config_PropertyChanged;
        Core.ThemeChanged += Core_ThemeChanged;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Core.Config.PropertyChanged -= Config_PropertyChanged;
        Core.ThemeChanged -= Core_ThemeChanged;
    }


    private void Core_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.ImagePath));
        }
    }


    private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (nameof(Core.Config.ToolbarIconHeight).Equals(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.InnerSpacing));
            _ = VM.OnPropertyChanged(nameof(VM.ItemPadding));
            _ = VM.OnPropertyChanged(nameof(VM.SeparatorEndPoint));
        }
    }



}