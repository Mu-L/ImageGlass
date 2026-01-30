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
using Avalonia.Threading;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.OsApi;
using ImageGlass.Common.Types;
using System;
using System.Threading;

namespace ImageGlass.UI.Viewer;


public partial class ViewerControl : PhControl
{
    // loading
    private IDisposable? _photo; // TODO
    private CancellationTokenSource? _cancelPreview;
    private InterlockedBool _isPreviewing = new();


    /// <summary>
    /// Gets the drawing area.
    /// </summary>
    public Rect DrawingArea { get; private set; }


    /// <summary>
    /// Gets the bitmap size.
    /// </summary>
    public Size BitmapSize { get; private set; }



    #region Override Methods

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        EnableTouchGestures__();
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        DisableTouchGestures__();
        DisposeCheckerboard();

        _photoRenderer?.Dispose();
        _photoRenderer = null;

        _bmpSource?.Dispose();
        _bmpSource = null;

        _bmpPreview?.Dispose();
        _bmpPreview = null;
    }


    protected override void OnIgDpiChanged()
    {
        base.OnIgDpiChanged();

        DisposeCheckerboard();
        InvalidateVisual();
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // update drawing area
        if (e.Property == PaddingProperty || e.Property == BoundsProperty)
        {
            DrawingArea = Bounds.Deflate(Padding);
        }
    }


    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (e.NewSize.IsEmpty) return;

        // update drawing regions
        CalculateDrawingRegion();

        // redraw the control on resizing if it's not manual zoom
        if (_photo is not null && !_zooming.IsManual)
        {
            Refresh(true, false, true);
        }
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var p = e.GetCurrentPoint(this);
        var requestRerender = OnSelectionBegin(p);

        // request re-render control
        if (requestRerender) InvalidateVisual();
    }


    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var p = e.GetCurrentPoint(this);
        var requestRerender = OnSelectionUpdating(p);

        // request re-render control
        if (requestRerender) InvalidateVisual();
    }


    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _ = OnSelectionEnd(true);
    }


    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) InvalidateVisual();

        base.OnPointerReleased(e);
    }


    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) InvalidateVisual();

        base.OnPointerCaptureLost(e);
    }


    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var delta = e.Delta.Y;
        var position = e.GetPosition(this);
        var isUsingTouchpad = Math.Abs(e.Delta.Y) != 1;


        // Touchpad scrolling
        if (isUsingTouchpad)
        {
            // Scroll Left/Right: Pan horizontally
            if (Math.Abs(e.Delta.X) > 0 && Math.Abs(e.Delta.Y) < 0.5)
            {
                PanTo(e.Delta.X * -50, e.Delta.Y * -50, position);
                return;
            }

            // Scroll Up/Down: Zoom
            delta *= 70;
        }
        // Mouse wheel
        else
        {
            delta *= SystemInfo.MouseWheelScrollDelta;
        }

        // Zooming
        _ = ZoomByDeltaToPoint(delta, position);
    }


    #endregion // Override Methods



    #region Public Methods

    /// <summary>
    /// Forces the control to reset zoom mode and invalidate itself.
    /// </summary>
    public void Refresh()
    {
        Refresh(true);
    }


    /// <summary>
    /// Forces the control to invalidate itself.
    /// </summary>
    public void Refresh(bool resetZoom = true, bool isManualZoom = false, bool zoomedByResizing = false)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (resetZoom)
            {
                SetZoomMode(null, isManualZoom, zoomedByResizing);
            }

            InvalidateVisual();
        });
    }

    #endregion // Public Methods


}
