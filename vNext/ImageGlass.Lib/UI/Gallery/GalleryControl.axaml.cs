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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using System.Collections.Generic;

namespace ImageGlass.UI;

public partial class GalleryControl : PhControl
{
    public GalleryControlModel VM => (GalleryControlModel)DataContext!;


    // events
    public event TEventHandler<GalleryItem, GalleryItemClickEventArgs>? ItemClicked;


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


    /// <summary>
    /// Gets, sets the layout direction.
    /// </summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<GalleryControl, Orientation>(nameof(Orientation), Orientation.Horizontal);


    #endregion // Public Properties





    public GalleryControl()
    {
        InitializeComponent();
    }



    private void PART_ScrollViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (Orientation == Orientation.Vertical) return;
        if (sender is not ScrollViewer svEl) return;
        if (svEl.Extent.Width <= svEl.Viewport.Width) return;

        // 1. Check if the user is scrolling vertically (Mouse Wheel)
        //    and NOT already scrolling horizontally (Shift + Wheel / Touchpad)
        if (e.Delta.X == 0 && e.Delta.Y == 0) return;

        // 2. Translate Vertical Delta (Y) to Horizontal Offset (X)
        // Multiply by 50 for reasonable scroll speed (adjust as needed)
        var scrollAmount = e.Delta.Y * 50d;

        // Subtract to match natural scroll direction (Wheel Down -> Scroll Right)
        svEl.Offset = new Vector(svEl.Offset.X - scrollAmount, 0);

        // 3. Mark event as handled to prevent bubbling
        e.Handled = true;

    }


    private void GalleryItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not GalleryItem itemEl) return;
        if (itemEl.VM.IsCurrent) return;

        // scroll the clicked item into the view
        // TODO:


        // raise event
        ItemClicked?.Invoke(itemEl, new GalleryItemClickEventArgs(itemEl.VM));
    }



    /// <summary>
    /// Scrolls the gallery to bring the specified item into view.
    /// </summary>
    public void ScrollToItem(int index, bool disableAnimation = true)
    {
        // TODO:
    }


    /// <summary>
    /// Loads the thumbnail.
    /// </summary>
    public void LoadThumbnail(int index, bool useCache)
    {
        // TODO:
    }


}


public class GalleryItemClickEventArgs(Photo vm) : RoutedEventArgs
{
    public Photo VM => vm;
}
