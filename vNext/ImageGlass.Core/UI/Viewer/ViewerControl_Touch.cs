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
using Avalonia.Interactivity;
using ImageGlass._UI.Viewer.ZoomAndPan;

namespace ImageGlass._UI.Viewer;

public partial class ViewerControl
{

    private void EnableTouchGestures__()
    {
        // add support for gestures
        GestureRecognizers.Add(new PinchGestureRecognizer());
        //GestureRecognizers.Add(new ScrollGestureRecognizer()
        //{
        //    CanHorizontallyScroll = true,
        //    CanVerticallyScroll = true,
        //    IsScrollInertiaEnabled = true,
        //});

        var panGesture = new PanGestureRecognizer();
        panGesture.Panning += TouchScreen_Panning;
        GestureRecognizers.Add(panGesture);

        // touch screen gestures
        Gestures.AddDoubleTappedHandler(this, TouchScreen_DoubleTapped);
        //Gestures.AddScrollGestureHandler(this, TouchScreen_Scrolled);
        Gestures.AddPinchHandler(this, TouchScreen_Pinched);

        // touchpad gestures
        Gestures.AddPointerTouchPadGestureMagnifyHandler(this, TouchPad_Pinched);
    }


    private void DisableTouchGestures__()
    {
        Gestures.RemoveDoubleTappedHandler(this, TouchScreen_DoubleTapped);
        //Gestures.RemoveScrollGestureHandler(this, TouchScreen_Scrolled);
        Gestures.RemovePinchHandler(this, TouchScreen_Pinched);

        Gestures.RemovePointerTouchPadGestureMagnifyHandler(this, TouchPad_Pinched);
    }



    private void TouchScreen_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        // Touch double-tap:
        // enable double-tapping for drawing selection

        // TODO:
        //OnSelectionBeginWithTouch(e);
    }


    private void TouchScreen_Panning(object? sender, PanUpdatedEventArgs e)
    {
        PanTo(e.TotalX, e.TotalY, null);
    }


    //private void TouchScreen_Scrolled(object? sender, RoutedEventArgs e)
    //{
    //    var e = (ScrollGestureEventArgs)args;

    //    PanTo(e.Delta.X, e.Delta.Y);
    //    e.Handled = true;
    //}


    private void TouchScreen_Pinched(object? sender, PinchEventArgs e)
    {

    }


    private void TouchPad_Pinched(object? sender, PointerDeltaEventArgs e)
    {
        var position = e.GetPosition(this);

        ZoomByDeltaToPoint(e.Delta.X, position);
        e.Handled = true;
    }




}
