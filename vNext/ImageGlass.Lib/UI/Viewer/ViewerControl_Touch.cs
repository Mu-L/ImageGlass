/*
ImageGlass - A lightweight, versatile image viewer
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
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using ImageGlass.UI.Viewer.ZoomAndPan;

namespace ImageGlass.UI.Viewer;

public partial class ViewerControl
{
    private PhPinchGestureRecognizer _pinchGesture = new();


    /// <summary>
    /// Registers touch gestures.
    /// </summary>
    private void RegisterTouchGestures()
    {
        // add support for panning gesture
        GestureRecognizers.Add(new ScrollGestureRecognizer
        {
            CanHorizontallyScroll = true,
            CanVerticallyScroll = true,
            IsScrollInertiaEnabled = true,
        });


        // touch screen gestures
        _pinchGesture.Pinch += OnTouchPinched; // pinch gesture
        GestureRecognizers.Add(_pinchGesture);
        Gestures.AddScrollGestureHandler(this, OnTouchPanning);  // panning
        Gestures.AddScrollGestureEndedHandler(this, OnTouchPanningEnd);

        // suppress context menu during multi-touch gestures
        AddHandler(ContextRequestedEvent, OnContextRequestedForTouch, RoutingStrategies.Tunnel);

        // touchpad gestures
        Gestures.AddPointerTouchPadGestureMagnifyHandler(this, OnTouchPadPinched); // pinch
    }


    /// <summary>
    /// Removes the touch gestures.
    /// </summary>
    private void UnregisterTouchGestures()
    {
        // touch screen gestures
        _pinchGesture.Pinch -= OnTouchPinched; // pinch
        Gestures.RemoveScrollGestureHandler(this, OnTouchPanning); // panning
        Gestures.RemoveScrollGestureEndedHandler(this, OnTouchPanningEnd);
        RemoveHandler(ContextRequestedEvent, OnContextRequestedForTouch);

        // touchpad gestures
        Gestures.RemovePointerTouchPadGestureMagnifyHandler(this, OnTouchPadPinched); // pinch
    }


    /// <summary>
    /// Handles panning event for touch screen.
    /// </summary>
    private void OnTouchPanning(object? sender, RoutedEventArgs e)
    {
        var args = (ScrollGestureEventArgs)e;

        if (EnableSelection && CurrentSelectionAction != SelectionAction.None)
        {
            // TODO:
            // OnSelectionUpdating(position);
        }
        else
        {
            if (!_enablePanningVelocity) args.ShouldEndScrollGesture = true;

            // perform panning
            _ = PanTo(args.Delta.X, args.Delta.Y, null);
        }

        e.Handled = true;
    }


    private void OnTouchPanningEnd(object? sender, ScrollGestureEndedEventArgs e)
    {
        _enablePanningVelocity = true;
    }


    /// <summary>
    /// Handles pinch event for touch screen.
    /// </summary>
    private void OnTouchPinched(object? sender, PhPinchEventArgs e)
    {
        // 1. normalize expansion value to delta
        var delta = e.Expansion * ZoomInfo.MAX_ZOOM_SPEED;


        // 2. check if the manipulation is a pinch gesture
        var isPintching = delta != 0;
        if (!isPintching) return;


        // 3. perform panning
        PanTo(-e.Translation.X, -e.Translation.Y, e.Position, !isPintching);


        // 4. perform zooming for Pinch gesture
        if (isPintching)
        {
            _ = ZoomByDeltaToPoint(delta, e.Position);
        }

        e.Handled = true;
    }


    /// <summary>
    /// Suppresses context menu when a multi-touch gesture (pinch, 2-finger tap) is active or was recently active.
    /// </summary>
    private void OnContextRequestedForTouch(object? sender, ContextRequestedEventArgs e)
    {
        if (_pinchGesture.IsPinchingOrRecentlyPinched)
        {
            e.Handled = true;
        }
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
