using D2Phap.Canvas2D;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using Vortice.Direct2D1;
using Vortice.WIC;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass.Common.WinOS;

public partial class VirtualViewerControl : SwapChainCanvas
{
    private IWICBitmapSource? _bmpWic;
    private ID2D1Bitmap1? _bmpD2d;
    private ID2D1BitmapBrush1? _checkerboardBrush;
    private Rect _srcRect = new();
    private Rect _destRect = new();


    private ZoomMode _zoomMode = ZoomMode.AutoZoom;
    private double _zoomSpeed = 0f;
    private double _minZoom = 0.01f; // 1%
    private double _maxZoom = 100f; // 10_000%
    private double[] _zoomLevels = [];
    private double _zoomFactor = 1f;
    private double _oldZoomFactor = 1f;
    private bool _isManualZoom = false;



    private Point _zoomedPoint = default;
    private bool _xOut = false;
    private bool _yOut = false;


    private Point _panHostFromPoint;
    private Point _panHostToPoint;
    private float _panDistance = 20f;

    //private bool _isMouseDragged = false;
    //private Point? _mouseDownPoint = null;
    //private Point? _mouseMovePoint = null;


    public int CheckerboardSize { get; set; } = 25;
    //public BitmapInterpolationMode Interpolation { get; set; } = BitmapInterpolationMode.None;

    public Rect DrawingArea => new(
        Padding.Left,
        Padding.Top,
        Math.Max(0, Bounds.Width - Padding.Left - Padding.Right),
        Math.Max(0, Bounds.Height - Padding.Top - Padding.Bottom));

    public double SourceWidth { get; private set; } = 0;
    public double SourceHeight { get; private set; } = 0;

    public double ScreenDpiScaling => CompositionScaleX;

    public double ZoomFactor
    {
        get => _zoomFactor;
        set => SetZoomFactor(value, true);
    }
    public double MinZoom
    {
        get
        {
            if (ZoomLevels.Length > 0) return ZoomLevels[0];
            return _minZoom;
        }
        set => _minZoom = Math.Min(Math.Max(0.001f, value), 1000);
    }
    public double MaxZoom
    {
        get
        {
            if (ZoomLevels.Length > 0) return ZoomLevels[^1];
            return _maxZoom;
        }
        set => _maxZoom = Math.Min(Math.Max(0.001f, value), 1000);
    }
    public double[] ZoomLevels
    {
        get => _zoomLevels;
        set => _zoomLevels = value.OrderBy(x => x)
            .Where(i => i > 0)
            .Distinct()
            .ToArray();
    }
    public double ZoomSpeed
    {
        get => _zoomSpeed;
        set
        {
            _zoomSpeed = Math.Min(value, 500f); // max 500f
            _zoomSpeed = Math.Max(value, -500f); // min -500f
        }
    }
    public Point ImageViewportCenterPoint => new(
        _destRect.X + _destRect.Width / 2.0,
        _destRect.Y + _destRect.Height / 2.0);





    public VirtualViewerControl()
    {
        ManipulationMode = ManipulationModes.Scale
            | ManipulationModes.TranslateX | ManipulationModes.TranslateY
            | ManipulationModes.TranslateInertia;
    }


    protected override void OnLoaded()
    {
        base.OnLoaded();

        ManipulationDelta += VirtualViewer_ManipulationDelta;
    }



    private void VirtualViewer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var isPinching = e.Delta.Expansion != 0;

        // Panning
        PanTo(-e.Delta.Translation.X, -e.Delta.Translation.Y, e.Position, !isPinching);

        // Zooming for Pinch gesture
        if (isPinching)
        {
            var scaleDelta = e.Delta.Expansion * 8;

            _ = ZoomByDeltaToPoint(scaleDelta, e.Position);
        }

        e.Handled = true;
    }

    protected override void OnUnloaded()
    {
        base.OnUnloaded();

        _bmpD2d?.Dispose();
        _bmpD2d = null;

        _bmpWic?.Dispose();
        _bmpWic = null;

        _checkerboardBrush?.Dispose();
        _checkerboardBrush = null;
    }


    protected override void OnResize(SizeChangedEventArgs e)
    {
        base.OnResize(e);
        if (e.NewSize.IsEmpty) return;

        // update drawing regions
        CalculateDrawingRegion();

        // redraw the control on resizing if it's not manual zoom
        if (_bmpWic != null && !_isManualZoom)
        {
            Refresh(true, false, true);
        }
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


    protected override void OnRender(SwapChainCanvasRenderEventArgs e)
    {
        // draw checkerboard
        //context.DrawCheckerboardBrush(ref _checkerboardBitmap, CheckerboardSize, DrawingArea);


        // draw image
        if (_bmpD2d != null)
        {
            e.DrawBitmap(_bmpD2d, _destRect, _srcRect);
        }


        // debug
        e.DrawText(
            $"""
            Control Size: {ActualWidth} x {ActualHeight}
            DrawingArea: {DrawingArea}
            Image size: {SourceWidth} x {SourceHeight}
            _srcRect: {_srcRect}
            _destRect: {_destRect}
            """,
            "Consolas", 15d * ScreenDpiScaling, DrawingArea, Colors.Magenta);

        e.DrawRectangle(_destRect, 0, Colors.Cyan);

        // draw zoomed point
        var zoomX = _zoomedPoint.X * CompositionScaleX;
        var zoomY = _zoomedPoint.Y * CompositionScaleY;
        e.DrawEllipse(zoomX, zoomY, 8f, Colors.White, Colors.Red, 3f);


        // draw SwapChainSize
        e.DrawRectangle(e.Sender.Bounds, 0, Colors.Yellow, Colors.Transparent, 3f);

        // draw DrawingArea
        e.DrawRectangle(DrawingArea, 0, Colors.GreenYellow, Colors.Transparent, 3f);

        base.OnRender(e);
    }


    protected virtual void DrawCheckerboardLayer(SwapChainCanvasRenderEventArgs g)
    {
        // TODO:
    }



    public void Refresh()
    {
        Refresh(true);
    }


    public void Refresh(bool resetZoom = true, bool isManualZoom = false, bool zoomedByResizing = false)
    {
        if (resetZoom)
        {
            SetZoomMode(null, isManualZoom, zoomedByResizing);
        }

        Invalidate();
    }

    public virtual void CalculateDrawingRegion()
    {
        if (_bmpWic == null || DrawingArea.IsEmpty) return;

        var zoomX = _zoomedPoint.X * CompositionScaleX - Padding.Left;
        var zoomY = _zoomedPoint.Y * CompositionScaleY - Padding.Top;

        _xOut = false;
        _yOut = false;

        var controlW = DrawingArea.Width;
        var controlH = DrawingArea.Height;
        var scaledImgWidth = SourceWidth * _zoomFactor;
        var scaledImgHeight = SourceHeight * _zoomFactor;


        var srcX = _srcRect.X;
        var srcY = _srcRect.Y;
        var srcWidth = _srcRect.Width;
        var srcHeight = _srcRect.Height;

        var destX = _destRect.X;
        var destY = _destRect.Y;
        var destWidth = _destRect.Width;
        var destHeight = _destRect.Height;


        // image width < control width
        if (scaledImgWidth <= controlW)
        {
            srcX = 0;
            srcWidth = SourceWidth;

            destX = (controlW - scaledImgWidth) / 2.0f + DrawingArea.Left;
            destWidth = scaledImgWidth;
        }
        else
        {
            srcX += (controlW / _oldZoomFactor - controlW / _zoomFactor) / ((controlW + float.Epsilon) / zoomX);
            srcWidth = controlW / _zoomFactor;

            destX = DrawingArea.Left;
            destWidth = controlW;
        }


        // image height < control height
        if (scaledImgHeight <= controlH)
        {
            srcY = 0;
            srcHeight = SourceHeight;

            destY = (controlH - scaledImgHeight) / 2f + DrawingArea.Top;
            destHeight = scaledImgHeight;
        }
        else
        {
            srcY += (controlH / _oldZoomFactor - controlH / _zoomFactor) / ((controlH + float.Epsilon) / zoomY);
            srcHeight = controlH / _zoomFactor;

            destY = DrawingArea.Top;
            destHeight = controlH;
        }


        _oldZoomFactor = _zoomFactor;
        //------------------------

        if (srcX < 0)
        {
            _xOut = true;
            srcX = 0;
        }
        else if (srcX + srcWidth > SourceWidth)
        {
            _xOut = true;
            srcX = SourceWidth - srcWidth;
        }

        if (srcY + srcHeight > SourceHeight)
        {
            _yOut = true;
            srcY = SourceHeight - srcHeight;
        }

        if (srcY < 0)
        {
            _yOut = true;
            srcY = 0;
        }

        _srcRect = new(srcX, srcY, srcWidth, srcHeight);
        _destRect = new(destX, destY, destWidth, destHeight);
    }


    public void SetZoomFactor(double zoomValue, bool isManualZoom)
    {
        if (_zoomFactor == zoomValue) return;

        _zoomFactor = Math.Min(MaxZoom, Math.Max(zoomValue, MinZoom));
        _isManualZoom = isManualZoom;


        // update drawing regions
        CalculateDrawingRegion();

        Invalidate();
    }


    public void SetZoomMode(ZoomMode? mode = null, bool isManualZoom = false, bool zoomedByResizing = false)
    {
        // get zoom factor after applying the zoom mode
        _zoomMode = mode ?? _zoomMode;
        _zoomFactor = CalculateZoomFactor(_zoomMode, SourceWidth, SourceHeight);
        _isManualZoom = isManualZoom;

        // update drawing regions
        CalculateDrawingRegion();
    }


    public double CalculateZoomFactor(ZoomMode zoomMode, double srcWidth, double srcHeight)
    {
        return CalculateZoomFactor(zoomMode, srcWidth, srcHeight, DrawingArea.Width, DrawingArea.Height);
    }


    public double CalculateZoomFactor(ZoomMode zoomMode, double srcWidth, double srcHeight, double viewportW, double viewportH)
    {
        if (srcWidth == 0 || srcHeight == 0 || viewportW == 0 || viewportH == 0) return _zoomFactor;

        var widthScale = viewportW / srcWidth;
        var heightScale = viewportH / srcHeight;
        double zoomFactor;

        if (zoomMode == ZoomMode.ScaleToWidth)
        {
            zoomFactor = widthScale;
        }
        else if (zoomMode == ZoomMode.ScaleToHeight)
        {
            zoomFactor = heightScale;
        }
        else if (zoomMode == ZoomMode.ScaleToFit)
        {
            zoomFactor = Math.Min(widthScale, heightScale);
        }
        else if (zoomMode == ZoomMode.ScaleToFill)
        {
            zoomFactor = Math.Max(widthScale, heightScale);
        }
        else if (zoomMode == ZoomMode.LockZoom)
        {
            zoomFactor = ZoomFactor;
        }
        // AutoZoom
        else
        {
            // viewbox size >= image size
            if (widthScale >= 1 && heightScale >= 1)
            {
                zoomFactor = 1; // show original size
            }
            else
            {
                zoomFactor = Math.Min(widthScale, heightScale);
            }
        }

        return zoomFactor;
    }


    public bool ZoomByDeltaToPoint(double delta, Point? point = null, bool requestRerender = true)
    {
        var newZoomFactor = _zoomFactor;
        var isZoomingByMouseWheel = true; // Math.Abs(delta) == SystemInformation.MouseWheelScrollDelta;

        // use zoom levels
        if (ZoomLevels.Length > 0 && isZoomingByMouseWheel)
        {
            var minZoomLevel = ZoomLevels[0];
            var maxZoomLevel = ZoomLevels[^1];

            // zoom in
            if (delta > 0)
            {
                newZoomFactor = ZoomLevels.FirstOrDefault(i => i > _zoomFactor);
            }
            // zoom out
            else if (delta < 0)
            {
                newZoomFactor = ZoomLevels.LastOrDefault(i => i < _zoomFactor);
            }
            if (newZoomFactor == 0) return false;

            // limit zoom factor
            newZoomFactor = Math.Min(Math.Max(minZoomLevel, newZoomFactor), maxZoomLevel);

        }
        // use smooth zooming
        else
        {
            var speed = delta / (501f - ZoomSpeed);

            // zoom in
            if (delta > 0)
            {
                newZoomFactor = _zoomFactor * (1f + speed);
            }
            // zoom out
            else if (delta < 0)
            {
                newZoomFactor = _zoomFactor / (1f - speed);
            }

            // limit zoom factor
            newZoomFactor = Math.Min(Math.Max(MinZoom, newZoomFactor), MaxZoom);
        }


        if (newZoomFactor == _zoomFactor) return false;

        var location = point ?? new Point(-1, -1);

        // use the center point if the point is outside
        if (!Bounds.Contains(location))
        {
            location = ImageViewportCenterPoint;
        }


        _oldZoomFactor = _zoomFactor;
        _zoomFactor = newZoomFactor;
        _isManualZoom = true;
        _zoomedPoint = new(location.X, location.Y);


        // update drawing regions
        CalculateDrawingRegion();


        if (requestRerender)
        {
            Invalidate();
        }

        return true;
    }


    public bool PanTo(double hDistance, double vDistance, Point pointerPosition, bool requestRerender = true)
    {
        if (_bmpWic == null) return false;
        if (hDistance == 0 && vDistance == 0) return false;

        hDistance *= CompositionScaleX;
        vDistance *= CompositionScaleY;


        // horizontal
        if (hDistance != 0)
        {
            _srcRect.X += hDistance / _zoomFactor;
        }

        // vertical 
        if (vDistance != 0)
        {
            _srcRect.Y += vDistance / _zoomFactor;
        }

        _zoomedPoint = pointerPosition;
        _panHostToPoint = pointerPosition;


        // update drawing regions
        CalculateDrawingRegion();


        if (requestRerender)
        {
            Invalidate();
        }

        return true;
    }




    public void LoadImage(string path)
    {
        _bmpD2d?.Dispose();
        _bmpD2d = null;

        _bmpWic?.Dispose();
        _bmpWic = null;


        _bmpWic = WicBitmapSource.Load(path);
        SourceWidth = _bmpWic.Size.Width;
        SourceHeight = _bmpWic.Size.Height;

        var exceededMaxBitmapSize = SourceWidth > MAX_HARDWARE_BITMAP_DIMENSION
            || SourceHeight > MAX_HARDWARE_BITMAP_DIMENSION;
        UseHardwareAcceleration = !exceededMaxBitmapSize;


        _bmpD2d = D2dContext.CreateBitmapFromWicBitmap(_bmpWic);

        Refresh();
    }
}
