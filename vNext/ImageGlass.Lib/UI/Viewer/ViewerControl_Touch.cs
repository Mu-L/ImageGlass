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
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using ImageGlass.UI.Viewer.ZoomAndPan;

namespace ImageGlass.UI.Viewer;

public partial class ViewerControl
{
    private double _lastPinchScale = 1.0;


    /// <summary>
    /// Registers touch gestures.
    /// </summary>
    private void RegisterTouchGestures()
    {
        // add support for gestures
        GestureRecognizers.Add(new PinchGestureRecognizer());
        GestureRecognizers.Add(new ScrollGestureRecognizer
        {
            CanHorizontallyScroll = true,
            CanVerticallyScroll = true,
            IsScrollInertiaEnabled = true,
        });

        // touch screen + touchpad gestures
        Gestures.AddScrollGestureHandler(this, OnTouchPanning);  // panning

        // touch screen gestures
        Gestures.AddPinchHandler(this, OnTouchPinched); // pinch
        Gestures.AddPinchEndedHandler(this, OnTouchPinchEnded); // pinch-end
        Gestures.AddDoubleTappedHandler(this, OnTouchDoubleTapped);  // double-tap

        // touchpad gestures
        Gestures.AddPointerTouchPadGestureMagnifyHandler(this, OnTouchPadPinched); // pinch
    }


    /// <summary>
    /// Removes the touch gestures.
    /// </summary>
    private void UnregisterTouchGestures()
    {
        // touch screen + touchpad gestures
        Gestures.RemoveScrollGestureHandler(this, OnTouchPanning); // panning

        // touch screen gestures
        Gestures.RemovePinchHandler(this, OnTouchPinched); // pinch
        Gestures.RemovePinchEndedHandler(this, OnTouchPinchEnded); // pinch-end
        Gestures.RemoveDoubleTappedHandler(this, OnTouchDoubleTapped); // double-tap

        // touchpad gestures
        Gestures.RemovePointerTouchPadGestureMagnifyHandler(this, OnTouchPadPinched); // pinch
    }


    /// <summary>
    /// Handles panning event for touch screen and touchpad.
    /// </summary>
    private void OnTouchPanning(object? sender, RoutedEventArgs e)
    {
        var args = (ScrollGestureEventArgs)e;

        // perform panning
        _ = PanTo(args.Delta.X, args.Delta.Y, null);
        e.Handled = true;
    }


    /// <summary>
    /// Handles pinch event for touch screen.
    /// </summary>
    private void OnTouchPinched(object? sender, PinchEventArgs e)
    {
        // normalize scale value to delta
        var scaleDiff = e.Scale / _lastPinchScale;
        var delta = (scaleDiff - 1.0) * ZoomInfo.MAX_ZOOM_SPEED;
        _lastPinchScale = e.Scale;

        // perform zooming
        _ = ZoomByDeltaToPoint(delta, e.ScaleOrigin);
        e.Handled = true;
    }


    /// <summary>
    /// Handles pinch-end event for touch screen.
    /// </summary>
    private void OnTouchPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        _lastPinchScale = 1.0;
    }


    /// <summary>
    /// Handles double-tap event for touch screen.
    /// </summary>
    private void OnTouchDoubleTapped(object? sender, RoutedEventArgs e)
    {
        // Touch double-tap:
        // enable double-tapping for drawing selection

        var args = (TappedEventArgs)e;
        OnSelectionBeginWithTouch(args);
        e.Handled = true;
    }


    /// <summary>
    /// Handles pinch event for touchpad.
    /// </summary>
    private void OnTouchPadPinched(object? sender, PointerDeltaEventArgs e)
    {
        var position = e.GetPosition(this);

        ZoomByDeltaToPoint(e.Delta.X, position);
        e.Handled = true;
    }




}
