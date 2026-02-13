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
using Avalonia.VisualTree;
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.UI;

public partial class GalleryControl : PhControl
{
    private CancellationTokenSource? _cancelScrollAnimation;
    private static readonly Thickness GALLERY_ITEM_MARGIN = new(1);
    private static readonly Thickness GALLERY_PADDING = new(4, 4, 4, 8);


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



    #region Override Methods

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        UpdateReservedSize();
        Core.Config.PropertyChanged += Config_PropertyChanged;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Core.Config.PropertyChanged -= Config_PropertyChanged;
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == OrientationProperty)
        {
            UpdateReservedSize();
        }
    }


    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Config.ThumbnailSize))
        {
            UpdateReservedSize();
        }
    }


    #endregion // Override Methods



    #region Control Events

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

        // raise event
        ItemClicked?.Invoke(itemEl, new GalleryItemClickEventArgs(itemEl.VM));
    }

    #endregion // Control Events



    #region Control Methods


    /// <summary>
    /// Loads the thumbnail.
    /// </summary>
    public void LoadThumbnail(int index, bool useCache)
    {
        var photo = Core.Photos.Get(index);
        if (photo is null) return;

        var thumbSize = Core.Config.ThumbnailSize * Dpi;
        _ = photo.LoadThumbnailAsync(thumbSize, useCache);
    }


    /// <summary>
    /// Scrolls the gallery to bring the specified item into the center of the view.
    /// </summary>
    public void ScrollToItem(int index, bool enableAnimation = false)
    {
        var svEl = FindScrollViewer();
        if (svEl is null || index < 0) return;

        var targetOffset = GetCenteredScrollOffset(svEl, index);
        if (enableAnimation)
        {
            _ = AnimateScrollAsync(svEl, targetOffset);
        }
        else
        {
            _cancelScrollAnimation?.Cancel();
            svEl.Offset = targetOffset;
        }
    }


    /// <summary>
    /// Calculates the scroll offset that centers the item at <paramref name="index"/>.
    /// </summary>
    private Vector GetCenteredScrollOffset(ScrollViewer svEl, int index)
    {
        var itemSize = (double)Core.Config.ThumbnailSize;
        var itemTotalSize = itemSize + 2; // Margin="1" = 1px on each side

        if (Orientation == Orientation.Horizontal)
        {
            var itemCenter = 4 + (index * itemTotalSize) + (itemTotalSize / 2); // 4 = Border left padding
            var target = itemCenter - (svEl.Viewport.Width / 2);
            var maxOffset = Math.Max(0, svEl.Extent.Width - svEl.Viewport.Width);

            return new Vector(Math.Clamp(target, 0, maxOffset), svEl.Offset.Y);
        }
        else
        {
            var itemCenter = 4 + (index * itemTotalSize) + (itemTotalSize / 2); // 4 = Border top padding
            var target = itemCenter - (svEl.Viewport.Height / 2);
            var maxOffset = Math.Max(0, svEl.Extent.Height - svEl.Viewport.Height);

            return new Vector(svEl.Offset.X, Math.Clamp(target, 0, maxOffset));
        }
    }


    /// <summary>
    /// Smoothly animates the <see cref="ScrollViewer"/> offset
    /// from its current position to <paramref name="targetOffset"/> using ease-out cubic.
    /// </summary>
    private async Task AnimateScrollAsync(ScrollViewer svEl, Vector targetOffset, int durationMs = 300)
    {
        _cancelScrollAnimation?.Cancel();
        _cancelScrollAnimation = new CancellationTokenSource();
        var token = _cancelScrollAnimation.Token;

        var startOffset = svEl.Offset;
        var startTime = Environment.TickCount64;

        while (!token.IsCancellationRequested)
        {
            var elapsed = Environment.TickCount64 - startTime;
            var progress = Math.Clamp((double)elapsed / durationMs, 0, 1);

            // ease-out cubic: f(t) = 1 - (1 - t)^3
            var eased = 1 - Math.Pow(1 - progress, 3);

            var x = startOffset.X + (targetOffset.X - startOffset.X) * eased;
            var y = startOffset.Y + (targetOffset.Y - startOffset.Y) * eased;
            svEl.Offset = new Vector(x, y);

            if (progress >= 1) break;

            await Task.Delay(16, token); // ~60 fps
        }
    }


    /// <summary>
    /// Finds the <see cref="ScrollViewer"/> inside the <see cref="ItemsControl"/> template.
    /// </summary>
    private ScrollViewer? FindScrollViewer()
    {
        return PART_ItemsControl
            .GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
    }


    /// <summary>
    /// Updates the minimum size of gallery control.
    /// </summary>
    private void UpdateReservedSize()
    {
        var itemSize = (double)Core.Config.ThumbnailSize;
        var totalWidth = itemSize
            + GALLERY_ITEM_MARGIN.Left + GALLERY_ITEM_MARGIN.Right
            + GALLERY_PADDING.Left + GALLERY_PADDING.Right;
        var totalHeight = itemSize
            + GALLERY_ITEM_MARGIN.Top + GALLERY_ITEM_MARGIN.Bottom
            + GALLERY_PADDING.Top + GALLERY_PADDING.Bottom;

        if (Orientation == Orientation.Horizontal)
        {
            PART_ItemsControl.MinHeight = totalHeight;
            PART_ItemsControl.MinWidth = 0;
        }
        else
        {
            PART_ItemsControl.MinWidth = totalWidth;
            PART_ItemsControl.MinHeight = 0;
        }
    }


    #endregion // Control Methods


}


public class GalleryItemClickEventArgs(Photo vm) : RoutedEventArgs
{
    public Photo VM => vm;
}
