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
using Microsoft.UI.Xaml.Media;
using System;

namespace ImageGlass.WinNT;


public partial class GalleryButtonItem : Button
{

    /// <summary>
    /// Gets or sets the interaction state of the gallery button item.
    /// </summary>
    public GalleryButtonItemStates State
    {
        get => (GalleryButtonItemStates)GetValue(StateProperty);
        set
        {
            SetValue(StateProperty, value);
            UpdateStyle();
        }
    }
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(
            nameof(State),
            typeof(GalleryButtonItemStates),
            typeof(GalleryButtonItem),
            new PropertyMetadata(GalleryButtonItemStates.Normal));


    /// <summary>
    /// Gets or sets the associated file path of the gallery button item.
    /// </summary>
    public string FilePath
    {
        get => (string)GetValue(FilePathProperty);
        set
        {
            SetValue(FilePathProperty, value);
            UpdateStyle();
        }
    }
    public static readonly DependencyProperty FilePathProperty =
        DependencyProperty.Register(
            nameof(FilePath),
            typeof(string),
            typeof(GalleryButtonItem),
            new PropertyMetadata(default));


    /// <summary>
    /// Gets or sets the selection state of the gallery button item.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set
        {
            SetValue(IsSelectedProperty, value);
            UpdateStyle();
        }
    }
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(GalleryButtonItem),
            new PropertyMetadata(default));



    public GalleryButtonItem()
    {
        DefaultStyleKey = typeof(GalleryButtonItem);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateStyle();
    }


    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        State ^= GalleryButtonItemStates.Normal;
        State |= GalleryButtonItemStates.Hovered;
        base.OnPointerEntered(e);

        UpdateStyle();
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        State ^= GalleryButtonItemStates.Hovered;
        State |= GalleryButtonItemStates.Normal;
        base.OnPointerExited(e);

        UpdateStyle();
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        State ^= GalleryButtonItemStates.Hovered;
        State |= GalleryButtonItemStates.Pressed;
        base.OnPointerPressed(e);

        UpdateStyle();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        State ^= GalleryButtonItemStates.Pressed;

        if (e.Pointer.IsInContact)
        {
            State |= GalleryButtonItemStates.Hovered;
        }
        else
        {
            State ^= GalleryButtonItemStates.Hovered;
        }

        base.OnPointerReleased(e);

        UpdateStyle();
    }


    /// <summary>
    /// Updates the visual style of the item based on its current state.
    /// </summary>
    public void UpdateStyle()
    {
        // normal style
        Brush bgBrush = new SolidColorBrush();
        Brush borderBrush = new SolidColorBrush();


        // selected style
        if (IsSelected)
        {
            bgBrush = (Brush)(Application.Current.Resources["IgButtonBackgroundSelected"]);
            borderBrush = (Brush)(Application.Current.Resources["IgButtonBorderSelected"]);
        }


        // hover style
        if (State.HasFlag(GalleryButtonItemStates.Hovered))
        {
            bgBrush = (Brush)(Application.Current.Resources["IgButtonBackgroundHovered"]);
        }

        // pressed style
        else if (State.HasFlag(GalleryButtonItemStates.Pressed))
        {
            bgBrush = (Brush)(Application.Current.Resources["IgButtonBackgroundPressed"]);
        }


        Background = bgBrush;
        BorderBrush = borderBrush;
    }

}



[Flags]
public enum GalleryButtonItemStates
{
    Normal = 1 << 1,
    Hovered = 1 << 2,
    Pressed = 1 << 3,
}
