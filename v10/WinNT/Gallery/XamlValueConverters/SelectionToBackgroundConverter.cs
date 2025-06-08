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
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace ImageGlass.WinNT;



public partial class SelectionToBackgroundConverter : IValueConverter
{
    public Brush DefaultBackground { get; set; } = new SolidColorBrush();

    public Brush SelectedBackground { get; set; } = (Brush)(Application.Current.Resources["ItemBackgroundSelected"]);


    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not bool isSelected) return DefaultBackground;

        return isSelected ? SelectedBackground : DefaultBackground;
    }


    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // ConvertBack is not needed and not implemented in this case
        throw new NotImplementedException();
    }
}

