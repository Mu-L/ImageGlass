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
using Avalonia;
using ImageGlass.Common.Photoing;
using System.Collections.Generic;

namespace ImageGlass.UI;

public partial class GalleryControl : PhControl
{
    public GalleryControlModel VM => (GalleryControlModel)DataContext!;


    #region Public Properties

    /// <summary>
    /// Gets, sets the items source.
    /// </summary>
    public IEnumerable<Photo> ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    public static readonly StyledProperty<IEnumerable<Photo>> ItemsSourceProperty =
        AvaloniaProperty.Register<GalleryControl, IEnumerable<Photo>>(nameof(ItemsSource), []);


    #endregion // Public Properties





    public GalleryControl()
    {
        InitializeComponent();
    }



}