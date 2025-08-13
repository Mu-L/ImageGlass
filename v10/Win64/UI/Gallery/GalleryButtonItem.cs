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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ImageGlass.Win64.UI;


public partial class GalleryButtonItem : Button
{
    private readonly IgClickable _clickable;

    /// <summary>
    /// Gets or sets the associated file path of the gallery button item.
    /// </summary>
    public string FilePath
    {
        get => (string)GetValue(FilePathProperty);
        set
        {
            SetValue(FilePathProperty, value);
            _clickable.UpdateStyle();
        }
    }
    public static readonly DependencyProperty FilePathProperty =
        DependencyProperty.Register(
            nameof(FilePath),
            typeof(string),
            typeof(GalleryButtonItem),
            new PropertyMetadata(default));


    /// <summary>
    /// Gets or sets the check state of the gallery button item.
    /// </summary>
    public bool IsChecked
    {
        get => _clickable.IsChecked;
        set => _clickable.IsChecked = value;
    }


    public GalleryButtonItem()
    {
        _clickable = new IgClickable(this);
        DefaultStyleKey = typeof(GalleryButtonItem);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _clickable.UpdateStyle();
    }


    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerEntered();
        base.OnPointerEntered(e);

        _clickable.UpdateStyle();
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerExited();
        base.OnPointerExited(e);

        _clickable.UpdateStyle();
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerPressed();
        base.OnPointerPressed(e);

        _clickable.UpdateStyle();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerReleased(e);
        base.OnPointerReleased(e);

        _clickable.UpdateStyle();
    }

}

