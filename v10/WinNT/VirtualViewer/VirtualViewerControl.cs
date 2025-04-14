using D2Phap.Canvas2D;
using ImageGlass.WinNT.Common;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.WIC;
using Windows.Foundation;
using Windows.UI;
using WinRT;


namespace ImageGlass.WinNT;

public partial class VirtualViewerControl : SwapChainCanvas
{
    // drawing image
    private Photo? _photo;
    private ID2D1Bitmap1? _bmpD2d;
    private Rect _srcRect = new();
    private Rect _destRect = new();

    private bool _isPreviewing = false;
    private ImageInterpolation _interpolationScaleDown = ImageInterpolation.MultiSampleLinear;
    private ImageInterpolation _interpolationScaleUp = ImageInterpolation.NearestNeighbor;




    // control
    private Color _accentColor = Colors.Blue;
    private InputSystemCursorShape _cursor = InputSystemCursorShape.Arrow;





    public event EventHandler<EventArgs>? Error;


    public double FontSize { get; set; } = 13;
    public double FontSize_Dpi => this.DpiScale(FontSize);




    public Rect DrawingArea => new(
        Padding.Left,
        Padding.Top,
        Math.Max(0, Bounds_Dpi.Width - Padding.Left - Padding.Right),
        Math.Max(0, Bounds_Dpi.Height - Padding.Top - Padding.Bottom));


    public double SourceWidth { get; private set; } = 0;

    public double SourceHeight { get; private set; } = 0;



    /// <summary>
    /// Gets, sets interpolation mode used when the
    /// <see cref="ZoomFactor"/> is less than or equal <c>1.0f</c>.
    /// </summary>
    public ImageInterpolation InterpolationScaleDown
    {
        get => _interpolationScaleDown;
        set
        {
            if (_interpolationScaleDown != value)
            {
                _interpolationScaleDown = value;
                Invalidate();
            }
        }
    }


    /// <summary>
    /// Gets, sets interpolation mode used when the
    /// <see cref="ZoomFactor"/> is greater than <c>1.0f</c>.
    /// </summary>
    public ImageInterpolation InterpolationScaleUp
    {
        get => _interpolationScaleUp;
        set
        {
            if (_interpolationScaleUp != value)
            {
                _interpolationScaleUp = value;
                Invalidate();
            }
        }
    }


    /// <summary>
    /// Gets the current <see cref="ImageInterpolation"/> mode.
    /// </summary>
    public ImageInterpolation CurrentInterpolation
    {
        get
        {
            if (ZoomFactor < 1f) return _interpolationScaleDown;
            if (ZoomFactor > 1f) return _interpolationScaleUp;

            return ImageInterpolation.NearestNeighbor;
        }
    }


    /// <summary>
    /// Gets or sets the cursor shape for the input system.
    /// </summary>
    public InputSystemCursorShape Cursor
    {
        get => _cursor;
        set
        {
            if (_cursor != value)
            {
                _cursor = value;

                ProtectedCursor?.Dispose();
                ProtectedCursor = null;

                ProtectedCursor = InputSystemCursor.Create(_cursor);
            }
        }
    }


    /// <summary>
    /// Gets, sets accent color.
    /// </summary>
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            //if (Web2 != null) Web2.AccentColor = _accentColor;
        }
    }



    public VirtualViewerControl()
    {
        ManipulationMode = ManipulationModes.Scale
            | ManipulationModes.TranslateX | ManipulationModes.TranslateY
            | ManipulationModes.TranslateInertia;
    }


    protected override void OnLoaded()
    {
        base.OnLoaded();
    }


    protected override void OnUnloaded()
    {
        base.OnUnloaded();

        _bmpD2d?.Dispose();
        _bmpD2d = null;

        _photo?.Dispose();
        _photo = null;

        _checkerboardBrush?.Dispose();
        _checkerboardBrush = null;
    }


    protected virtual void OnError(EventArgs e)
    {
        Error?.Invoke(this, e);
    }


    protected override void OnResize(SizeChangedEventArgs e)
    {
        base.OnResize(e);
        if (e.NewSize.IsEmpty()) return;

        // update drawing regions
        CalculateDrawingRegion();

        // redraw the control on resizing if it's not manual zoom
        if (_photo != null && !_zooming.IsManual)
        {
            Refresh(true, false, true);
        }
    }


    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        var cursor = e.GetCurrentPoint(this);
        var requestRerender = OnSelectionBegin(cursor);

        // request re-render control
        if (requestRerender) Invalidate();
    }


    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        var pointer = e.GetCurrentPoint(this);
        var requestRerender = OnSelectionUpdating(pointer);

        // request re-render control
        if (requestRerender) Invalidate();
    }


    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        OnSelectionEnd(true);
    }


    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) Invalidate();

        base.OnPointerReleased(e);
    }


    protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    {
        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) Invalidate();

        base.OnPointerCanceled(e);
    }


    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var pPoint = e.GetCurrentPoint(this);
        var delta = pPoint.Properties.MouseWheelDelta;
        var position = pPoint.Position;
        var isUsingTouchpad = Math.Abs(delta) != 120;


        // Touchpad scrolling
        if (isUsingTouchpad)
        {
            // Horizontal mouse wheel
            if (pPoint.Properties.IsHorizontalMouseWheel)
            {
                _ = PanTo(delta, 0, position);
                return;
            }
            //// Vertical mouse wheel
            //else
            //{
            //    _ = PanTo(0, -delta, position);
            //    return;
            //}
        }

        // Zooming
        _ = ZoomByDeltaToPoint(delta, position);
    }


    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        base.OnDoubleTapped(e);

        // Touch double-tap:
        // enable double-tapping for drawing selection
        OnSelectionBeginWithTouch(e);
    }


    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        // 1. check if the manipulation is a pinch gesture
        var isPintching = e.Delta.Expansion != 0;

        // 2. check if we're drawing selection
        if (CurrentSelectionAction != SelectionAction.None && !isPintching)
        {
            // stop velocity
            e.Complete();
        }
        else
        {
            // 2. perform panning
            PanTo(-e.Delta.Translation.X, -e.Delta.Translation.Y, e.Position, !isPintching);


            // 3. perform zooming for Pinch gesture
            if (isPintching)
            {
                var scaleDelta = e.Delta.Expansion * 8;
                _ = ZoomByDeltaToPoint(scaleDelta, e.Position);
            }
        }

        e.Handled = true;
    }


    protected override void OnRender(SwapChainCanvasRenderEventArgs e)
    {
        // draw checkerboard
        DrawCheckerboardLayer(e);


        // draw image
        DrawImageLayer(e);


        // Draw selection layer
        OnSelectionDrawing(e);


        // debug
        e.DrawText(
            $"""
            Control Size: {ActualWidth} x {ActualHeight}
            Image size: {SourceWidth} x {SourceHeight}
            _srcRect: {_srcRect}
            _destRect: {_destRect}
            _sourceSelection: {_selection.SourceRect}
            ClientSelection: {ClientSelection}
            CurrentTouchPoints: {TouchedPoints}
            """,
            "Consolas", FontSize_Dpi, DrawingArea, Colors.Magenta);

        e.DrawRectangle(_destRect, 0, Colors.Cyan);

        // draw zoomed point
        var zoomX = _zooming.ZoomedPoint.X * CompositionScaleX;
        var zoomY = _zooming.ZoomedPoint.Y * CompositionScaleY;
        e.DrawEllipse(zoomX, zoomY, 8f, Colors.White, Colors.Red, 3f);


        //// draw SwapChainSize
        //e.DrawRectangle(e.Sender.Bounds, 0, Colors.Yellow, Colors.Transparent, 3f);

        //// draw DrawingArea
        //e.DrawRectangle(DrawingArea, 0, Colors.GreenYellow, Colors.Transparent, 3f);

        base.OnRender(e);
    }


    protected virtual void DrawImageLayer(SwapChainCanvasRenderEventArgs e)
    {
        if (_bmpD2d == null) return;

        e.DrawBitmap(_bmpD2d, _destRect, _srcRect, (InterpolationMode)CurrentInterpolation);
    }





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
        if (resetZoom)
        {
            SetZoomMode(null, isManualZoom, zoomedByResizing);
        }

        Invalidate();
    }




    public async Task LoadImageAsync(string path)
    {
        // dispose current resources
        _bmpD2d?.Dispose();
        _bmpD2d = null;

        _photo?.Dispose();
        _photo = null;

        SetSourceSelection(Rect.Empty, false);


        // start loading new photo
        _photo = new Photo(path);
        await _photo.LoadAsync();

        SourceWidth = _photo?.Width ?? 0;
        SourceHeight = _photo?.Height ?? 0;

        if (_photo == null)
        {
            Refresh();
            return;
        }

        var exceededMaxBitmapSize = SourceWidth > MAX_HARDWARE_BITMAP_DIMENSION
            || SourceHeight > MAX_HARDWARE_BITMAP_DIMENSION;
        UseHardwareAcceleration = !exceededMaxBitmapSize;


        _bmpD2d = _photo.CreateDirect2dBitmap(D2dContext);

        ApplyColorManagementEffect();

        Refresh();
    }


    private void ApplyColorManagementEffect()
    {
        if (_bmpD2d == null || _photo == null) return;

        // create color management effect
        using var colorEffect = new ColorManagement(D2dContext);
        colorEffect.SetInput(0, _bmpD2d, false);


        // get color context
        if (_photo.ColorProfile?.Data is null) return;


        // create destination color context
        ID2D1ColorContext? destColorContext = null;
        if (WindowColorProfileProvider.Instance.Data != null)
        {
            destColorContext = D2dContext.CreateColorContext(ColorSpace.Custom, WindowColorProfileProvider.Instance.Data);
        }
        else if (_photo.PixelFormatInfo?.NumericRepresentation == PixelFormatNumericRepresentation.PixelFormatNumericRepresentationFloat)
        {
            destColorContext = D2dContext.CreateColorContext(ColorSpace.ScRgb, []);
        }
        else
        {
            destColorContext = D2dContext.CreateColorContext(ColorSpace.Srgb, []);
        }

        // create source color context
        using var srcColorContext = _photo.ColorProfile.Native != null
            ? D2dContext.CreateColorContextFromWicColorContext(_photo.ColorProfile.Native.As<IWICColorContext>())
            : null;


        // set color effect
        colorEffect.SourceColorContext = srcColorContext;
        colorEffect.DestinationColorContext = destColorContext;


        _bmpD2d?.Dispose();
        _bmpD2d = null;
        _bmpD2d = colorEffect.GetD2D1Bitmap1(D2dContext);

        srcColorContext?.Dispose();
        destColorContext?.Dispose();
    }


}
