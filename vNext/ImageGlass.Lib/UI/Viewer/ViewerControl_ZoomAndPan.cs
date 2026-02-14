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
using Avalonia.Threading;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.OsApi;
using ImageGlass.Common.Types;
using ImageGlass.UI.Viewer.ZoomAndPan;
using System;
using System.Linq;

namespace ImageGlass.UI.Viewer;

public partial class ViewerControl
{
    private readonly ZoomInfo _zooming = new();
    private double _panSpeed = 20f;
    private bool _enablePanningVelocity = true;


    /// <summary>
    /// Logical source position (can be negative when over-panning).
    /// This value is NOT clipped to image bounds, preserving the over-pan state across frames.
    /// </summary>
    private Point _logicalSrcPoint = new();


    // Public Events
    #region Public Events

    /// <summary>
    /// Occurs when <see cref="ZoomFactor"/> value changes.
    /// </summary>
    public event TEventHandler<ViewerControl, ViewerZoomEventArgs>? ZoomChanged;


    /// <summary>
    /// Occurs when the image is being panned.
    /// </summary>
    public event TEventHandler<ViewerControl, ViewerPanEventArgs>? Panning;

    #endregion // Public Events



    // Public Properies
    #region Public Properies

    /// <summary>
    /// Gets, sets zoom mode.
    /// </summary>
    public ZoomMode ZoomMode
    {
        get => GetValue(ZoomModeProperty);
        set => SetValue(ZoomModeProperty, value);
    }
    public static readonly StyledProperty<ZoomMode> ZoomModeProperty =
        AvaloniaProperty.Register<ViewerControl, ZoomMode>(nameof(ZoomMode), ZoomMode.AutoZoom,
            coerce: (sender, value) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var control = (ViewerControl)sender;
                    control._zooming.Mode = value;
                    control.Refresh();
                });

                return value;
            });


    /// <summary>
    /// Gets, sets current zoom factor (<c>1.0f = 100%</c>).
    /// This is a manual zooming.
    /// 
    /// <para>
    /// Use <see cref="SetZoomFactor(double, bool)"/> for more options.
    /// </para>
    /// </summary>
    public double ZoomFactor
    {
        get => _zooming.Factor;
        set => SetZoomFactor(value, true);
    }


    /// <summary>
    /// Gets, sets the minimum zoom factor (<c>1.0f = 100%</c>).
    /// Returns the first value of <see cref="ZoomLevels"/> if it is not empty.
    /// </summary>
    public double MinZoom
    {
        get
        {
            if (ZoomLevels.Length > 0) return ZoomLevels[0];
            return _zooming.Min;
        }
        set => _zooming.Min = Math.Min(Math.Max(0.001f, value), 1000);
    }


    /// <summary>
    /// Gets, sets the maximum zoom factor (<c>1.0f = 100%</c>).
    /// Returns the last value of <see cref="ZoomLevels"/> if it is not empty.
    /// </summary>
    public double MaxZoom
    {
        get
        {
            if (ZoomLevels.Length > 0) return ZoomLevels[^1];
            return _zooming.Max;
        }
        set => _zooming.Max = Math.Min(Math.Max(0.001f, value), 1000);
    }


    /// <summary>
    /// Gets or sets an array of zoom levels (ordered by ascending).
    /// </summary>
    public double[] ZoomLevels
    {
        get => _zooming.Levels;
        set => _zooming.Levels = value.OrderBy(x => x)
            .Where(i => i > 0)
            .Distinct()
            .ToArray();
    }


    /// <summary>
    /// Gets, sets the zoom speed. Value is from <c>-500f</c> to <c>500f</c>.
    /// </summary>
    public double ZoomSpeed
    {
        get => _zooming.Speed;
        set
        {
            _zooming.Speed = Math.Min(value, ZoomInfo.MAX_ZOOM_SPEED);
            _zooming.Speed = Math.Max(value, -ZoomInfo.MAX_ZOOM_SPEED);
        }
    }


    /// <summary>
    /// Gets, sets the speed for manual panning. Min value is <c>0</c>.
    /// </summary>
    public double PanSpeed
    {
        get => _panSpeed;
        set
        {
            _panSpeed = Math.Max(value, 0); // min 0
        }
    }


    /// <summary>
    /// Gets, sets a value indicating whether panning is allowed
    /// when the rendered image fits within the <see cref="DrawingArea"/>.
    /// </summary>
    public bool EnableFreePan
    {
        get => GetValue(EnableFreePanProperty);
        set => SetValue(EnableFreePanProperty, value);
    }
    public static readonly StyledProperty<bool> EnableFreePanProperty =
        AvaloniaProperty.Register<ViewerControl, bool>(nameof(EnableFreePan));


    /// <summary>
    /// Gets, sets the maximum panning margin in screen pixels beyond the image edge.
    /// </summary>
    public double PanMargin
    {
        get => GetValue(PanMarginProperty);
        set => SetValue(PanMarginProperty, value);
    }
    public static readonly StyledProperty<double> PanMarginProperty =
        AvaloniaProperty.Register<ViewerControl, double>(nameof(PanMargin), 30);


    #endregion // Public Properies



    // Public Methods
    #region Public Methods


    /// <summary>
    /// Calculates the drawing region for an image based on zoom level and control dimensions.
    /// Adjusts source and destination rectangles accordingly.
    /// </summary>
    public virtual void CalculateDrawingRegion()
    {
        if (DrawingArea.IsEmpty || BitmapSize.IsEmpty)
        {
            SrcRect = new();
            DestRect = new();
            _logicalSrcPoint = new();
            return;
        }


        // 1. scale the values according to DPI
        var currentZoomFactor = _zooming.Factor / Dpi;
        var oldZoomFactor = _zooming.OldFactor / Dpi;

        // 1.1 zoom point
        var zoomX = _zooming.ZoomedPoint.X - Padding.Left;
        var zoomY = _zooming.ZoomedPoint.Y - Padding.Top;


        // 1.2 source and viewport size
        var controlW = DrawingArea.Width;
        var controlH = DrawingArea.Height;

        var scaledImgWidth = BitmapSize.Width * currentZoomFactor;
        var scaledImgHeight = BitmapSize.Height * currentZoomFactor;


        // 1.3 initialize new values for source and destination rectangles
        var srcX = _logicalSrcPoint.X;
        var srcY = _logicalSrcPoint.Y;
        var srcWidth = SrcRect.Width;
        var srcHeight = SrcRect.Height;

        var destX = DestRect.X;
        var destY = DestRect.Y;
        var destWidth = DestRect.Width;
        var destHeight = DestRect.Height;


        // 2. calculate x-axis and width
        if (scaledImgWidth <= controlW)
        {
            srcX = 0;
            srcWidth = BitmapSize.Width;
            destWidth = scaledImgWidth;

            if (EnableFreePan)
            {
                // center the image, then apply pan offset
                // allow panning within the free space (image stays inside DrawingArea)
                var maxPanScreenX = (controlW - scaledImgWidth) / 2.0;
                var panOffsetX = Math.Clamp(_logicalSrcPoint.X * currentZoomFactor, -maxPanScreenX, maxPanScreenX);
                destX = (controlW - scaledImgWidth) / 2.0f + DrawingArea.Left - panOffsetX;
            }
            else
            {
                destX = (controlW - scaledImgWidth) / 2.0f + DrawingArea.Left;
            }
        }
        else
        {
            var oldControlW = controlW / oldZoomFactor;
            var newControlW = controlW / currentZoomFactor;

            srcX += (oldControlW - newControlW) / ((controlW + float.Epsilon) / zoomX);
            srcWidth = newControlW;

            destX = DrawingArea.Left;
            destWidth = controlW;
        }


        // 3. calculate y-axis and height
        if (scaledImgHeight <= controlH)
        {
            srcY = 0;
            srcHeight = BitmapSize.Height;
            destHeight = scaledImgHeight;

            if (EnableFreePan)
            {
                // center the image, then apply pan offset
                // allow panning within the free space (image stays inside DrawingArea)
                var maxPanScreenY = (controlH - scaledImgHeight) / 2.0;
                var panOffsetY = Math.Clamp(_logicalSrcPoint.Y * currentZoomFactor, -maxPanScreenY, maxPanScreenY);
                destY = (controlH - scaledImgHeight) / 2f + DrawingArea.Top - panOffsetY;
            }
            else
            {
                destY = (controlH - scaledImgHeight) / 2f + DrawingArea.Top;
            }
        }
        else
        {
            var oldControlH = controlH / oldZoomFactor;
            var newControlH = controlH / currentZoomFactor;

            srcY += (oldControlH - newControlH) / ((controlH + float.Epsilon) / zoomY);
            srcHeight = newControlH;

            destY = DrawingArea.Top;
            destHeight = controlH;
        }


        // 4. Panning to the edge:
        // Allow panning beyond image bounds by a margin (screen pixels converted to source coordinates).
        var panMarginSrc = DpiScale(PanMargin) / currentZoomFactor;

        // For overflow axes: clamp srcX/srcY with margin
        if (scaledImgWidth > controlW)
        {
            if (srcX < -panMarginSrc)
            {
                srcX = -panMarginSrc;
            }
            else if (srcX + srcWidth > BitmapSize.Width + panMarginSrc)
            {
                srcX = BitmapSize.Width - srcWidth + panMarginSrc;
            }
        }

        if (scaledImgHeight > controlH)
        {
            if (srcY + srcHeight > BitmapSize.Height + panMarginSrc)
            {
                srcY = BitmapSize.Height - srcHeight + panMarginSrc;
            }

            if (srcY < -panMarginSrc)
            {
                srcY = -panMarginSrc;
            }
        }

        // preserve the logical (unclipped) position for the next frame
        // - overflow axes: save from srcX/srcY (already margin-clamped)
        // - fits axes: clamp _logicalSrcPoint to the free space, or reset to 0 if free pan is disabled
        var logicalX = scaledImgWidth > controlW
            ? srcX
            : EnableFreePan
                ? Math.Clamp(_logicalSrcPoint.X,
                    -(controlW - scaledImgWidth) / 2.0 / currentZoomFactor,
                    (controlW - scaledImgWidth) / 2.0 / currentZoomFactor)
                : 0;
        var logicalY = scaledImgHeight > controlH
            ? srcY
            : EnableFreePan
                ? Math.Clamp(_logicalSrcPoint.Y,
                    -(controlH - scaledImgHeight) / 2.0 / currentZoomFactor,
                    (controlH - scaledImgHeight) / 2.0 / currentZoomFactor)
                : 0;
        _logicalSrcPoint = new(logicalX, logicalY);


        // 4.1 Clip source rect to valid image bounds
        // and proportionally adjust the destination rect to show a gap at the edge.
        if (srcX < 0)
        {
            var overPan = -srcX * currentZoomFactor;
            destX += overPan;
            destWidth -= overPan;
            srcWidth += srcX; // reduce by |srcX|
            srcX = 0;
        }

        if (srcX + srcWidth > BitmapSize.Width)
        {
            var excess = srcX + srcWidth - BitmapSize.Width;
            var excessScreen = excess * currentZoomFactor;
            destWidth -= excessScreen;
            srcWidth = BitmapSize.Width - srcX;
        }

        if (srcY < 0)
        {
            var overPan = -srcY * currentZoomFactor;
            destY += overPan;
            destHeight -= overPan;
            srcHeight += srcY; // reduce by |srcY|
            srcY = 0;
        }

        if (srcY + srcHeight > BitmapSize.Height)
        {
            var excess = srcY + srcHeight - BitmapSize.Height;
            var excessScreen = excess * currentZoomFactor;
            destHeight -= excessScreen;
            srcHeight = BitmapSize.Height - srcY;
        }


        // 5. get the final rectangles
        SrcRect = new(srcX, srcY, srcWidth, srcHeight);
        DestRect = new(destX, destY, destWidth, destHeight);

        _zooming.OldFactor = _zooming.Factor;
    }


    /// <summary>
    /// Sets the zoom factor for a view while ensuring it stays within defined limits.
    /// </summary>
    public void SetZoomFactor(double zoomValue, bool isManualZoom)
    {
        // reset viewport
        if (!isManualZoom)
        {
            SrcRect = new();
            _logicalSrcPoint = new();
            _zooming.ZoomedPoint = new();
        }

        _zooming.Factor = Math.Min(MaxZoom, Math.Max(zoomValue, MinZoom));
        _zooming.IsManual = isManualZoom;


        // update drawing regions
        CalculateDrawingRegion();
        InvalidateVisual();

        ZoomChanged?.Invoke(this, new ViewerZoomEventArgs()
        {
            ZoomFactor = _zooming.Factor,
            IsManualZoom = _zooming.IsManual,
            IsZoomModeChange = false,
            IsPreviewingImage = _isPreviewing.Value,
            ChangeSource = ZoomChangeSource.Unknown,
        });
    }


    /// <summary>
    /// Sets the zoom mode and updates the zoom factor based on the provided parameters.
    /// This <u><c>does not</c></u> redraw the viewing image.
    /// </summary>
    public void SetZoomMode(ZoomMode? mode = null, bool isManualZoom = false, bool zoomedByResizing = false)
    {
        // get zoom factor after applying the zoom mode
        _logicalSrcPoint = default;
        _zooming.ZoomedPoint = default;

        _zooming.Mode = mode ?? _zooming.Mode;
        _zooming.Factor = CalculateZoomFactor(_zooming.Mode, BitmapSize.Width, BitmapSize.Height);
        _zooming.IsManual = isManualZoom;


        // update drawing regions
        CalculateDrawingRegion();


        ZoomChanged?.Invoke(this, new ViewerZoomEventArgs()
        {
            ZoomFactor = _zooming.Factor,
            IsManualZoom = _zooming.IsManual,
            IsZoomModeChange = mode != _zooming.Mode,
            IsPreviewingImage = _isPreviewing.Value,
            ChangeSource = zoomedByResizing ? ZoomChangeSource.SizeChanged : ZoomChangeSource.ZoomMode,
        });
    }


    /// <summary>
    /// Calculates zoom factor by the input zoom mode, and source size.
    /// </summary>
    public double CalculateZoomFactor(ZoomMode zoomMode, double srcWidth, double srcHeight)
    {
        return CalculateZoomFactor(zoomMode, srcWidth, srcHeight, DrawingArea.Width, DrawingArea.Height);
    }


    /// <summary>
    /// Calculates zoom factor by the input zoom mode, and source size.
    /// </summary>
    public double CalculateZoomFactor(ZoomMode zoomMode, double srcWidth, double srcHeight, double viewportW, double viewportH)
    {
        if (srcWidth == 0 || srcHeight == 0 || viewportW == 0 || viewportH == 0) return _zooming.Factor;

        var widthScale = viewportW / srcWidth * Dpi;
        var heightScale = viewportH / srcHeight * Dpi;
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
            zoomFactor = _zooming.Factor;
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




    /// <summary>
    /// Zooms into the image.
    /// </summary>
    /// <param name="point">
    /// Client's cursor location to zoom into.
    /// </param>
    /// <returns>
    ///   <list type="table">
    ///     <item><c>true</c> if the viewport is changed.</item>
    ///     <item><c>false</c> if the viewport is unchanged.</item>
    ///   </list>
    /// </returns>
    public bool ZoomIn(Point? point = null, bool requestRerender = true)
    {
        return ZoomByDeltaToPoint(SystemInfo.MouseWheelScrollDelta, point, requestRerender);
    }


    /// <summary>
    /// Zooms out of the image.
    /// </summary>
    /// <param name="point">
    /// Client's cursor location to zoom out.
    /// </param>
    /// <returns>
    ///   <list type="table">
    ///     <item><c>true</c> if the viewport is changed.</item>
    ///     <item><c>false</c> if the viewport is unchanged.</item>
    ///   </list>
    /// </returns>
    public bool ZoomOut(Point? point = null, bool requestRerender = true)
    {
        return ZoomByDeltaToPoint(-SystemInfo.MouseWheelScrollDelta, point, requestRerender);
    }


    /// <summary>
    /// Scales the image using factor value.
    /// </summary>
    /// <param name="factor">Zoom factor (<c>1.0f = 100%</c>).</param>
    /// <param name="point">
    /// Client's cursor location to zoom out.
    /// If its value is <c>null</c> or outside of the <see cref="VirtualViewerControl"/> control,
    /// use DrawingArea center point.
    /// </param>
    /// <returns>
    ///   <list type="table">
    ///     <item><c>true</c> if the viewport is changed.</item>
    ///     <item><c>false</c> if the viewport is unchanged.</item>
    ///   </list>
    /// </returns>
    public bool ZoomToPoint(float factor, Point? point = null, bool requestRerender = true)
    {
        if (factor >= _zooming.Max || factor <= _zooming.Min) return false;

        var newZoomFactor = factor;
        var location = point ?? new Point(-1, -1);

        // use the center point if the point is outside
        if (!Bounds.Contains(location))
        {
            location = DrawingArea.Center;
        }

        // get the gap when the viewport is smaller than the control size
        var gapX = Math.Max(0, DestRect.X);
        var gapY = Math.Max(0, DestRect.Y);

        // the location after zoomed
        var zoomedLocation = new Point(
            (location.X - gapX) * newZoomFactor / ZoomFactor,
            (location.Y - gapY) * newZoomFactor / ZoomFactor);

        // the distance of 2 points after zoomed
        var zoomedDistance = new Size(
            Math.Max(0, zoomedLocation.X - location.X),
            Math.Max(0, zoomedLocation.Y - location.Y));

        // perform zoom if the new zoom factor is different
        if (_zooming.Factor != newZoomFactor)
        {
            _zooming.Factor = Math.Min(MaxZoom, Math.Max(newZoomFactor, MinZoom));
            _zooming.IsManual = true;

            // update drawing regions
            CalculateDrawingRegion();

            //// if using Webview2
            //if (UseWebview2)
            //{
            //    SetZoomFactorWeb2(_zoomFactor, _isManualZoom);
            //    return true;
            //}

            _ = PanTo(zoomedDistance.Width, zoomedDistance.Height, location, false);

            if (requestRerender) InvalidateVisual();

            // emit ZoomChanged event
            ZoomChanged?.Invoke(this, new ViewerZoomEventArgs()
            {
                ZoomFactor = _zooming.Factor,
                IsManualZoom = _zooming.IsManual,
                IsZoomModeChange = false,
                IsPreviewingImage = _isPreviewing.Value,
                ChangeSource = ZoomChangeSource.Unknown,
            });

            return true;
        }

        return false;
    }


    /// <summary>
    /// Scales the image using delta value.
    /// </summary>
    /// <param name="delta">Delta value.
    ///   <list type="table">
    ///     <item><c>delta <![CDATA[>]]> 0</c>: Zoom in.</item>
    ///     <item><c>delta <![CDATA[<]]> 0</c>: Zoom out.</item>
    ///   </list>
    /// </param>
    /// <param name="point">
    /// Client's cursor location to zoom out.
    /// </param>
    /// <returns>
    ///   <list type="table">
    ///     <item><c>true</c> if the viewport is changed.</item>
    ///     <item><c>false</c> if the viewport is unchanged.</item>
    ///   </list>
    /// </returns>
    public bool ZoomByDeltaToPoint(double delta, Point? point = null, bool requestRerender = true)
    {
        var newZoomFactor = _zooming.Factor;
        var isZoomingByMouseWheel = Math.Abs(delta) == SystemInfo.MouseWheelScrollDelta;

        // use zoom levels
        if (ZoomLevels.Length > 0 && isZoomingByMouseWheel)
        {
            var minZoomLevel = ZoomLevels[0];
            var maxZoomLevel = ZoomLevels[^1];

            // zoom in
            if (delta > 0)
            {
                newZoomFactor = ZoomLevels.FirstOrDefault(i => i > _zooming.Factor);
            }
            // zoom out
            else if (delta < 0)
            {
                newZoomFactor = ZoomLevels.LastOrDefault(i => i < _zooming.Factor);
            }
            if (newZoomFactor == 0) return false;

            // limit zoom factor
            newZoomFactor = Math.Min(Math.Max(minZoomLevel, newZoomFactor), maxZoomLevel);

        }
        // use smooth zooming
        else
        {
            var speed = delta / (ZoomInfo.MAX_ZOOM_SPEED - ZoomSpeed + 1);

            // zoom in
            if (delta > 0)
            {
                newZoomFactor = _zooming.Factor * (1f + speed);
            }
            // zoom out
            else if (delta < 0)
            {
                newZoomFactor = _zooming.Factor / (1f - speed);
            }

            // limit zoom factor
            newZoomFactor = Math.Min(Math.Max(MinZoom, newZoomFactor), MaxZoom);
        }


        if (newZoomFactor == _zooming.Factor) return false;

        var location = point ?? new Point(-1, -1);

        // use the center point if the point is outside
        if (!Bounds.Contains(location))
        {
            location = DrawingArea.Center;
        }


        _zooming.OldFactor = _zooming.Factor;
        _zooming.Factor = newZoomFactor;
        _zooming.IsManual = true;
        _zooming.ZoomedPoint = new(location.X, location.Y);


        // update drawing regions
        CalculateDrawingRegion();


        if (requestRerender) InvalidateVisual();


        // emit ZoomChanged event
        ZoomChanged?.Invoke(this, new ViewerZoomEventArgs()
        {
            ZoomFactor = _zooming.Factor,
            IsManualZoom = _zooming.IsManual,
            IsZoomModeChange = false,
            IsPreviewingImage = _isPreviewing.Value,
            ChangeSource = ZoomChangeSource.Unknown,
        });

        return true;
    }


    /// <summary>
    /// Pan the current viewport to a distance.
    /// </summary>
    /// <param name="hDistance">Horizontal distance.</param>
    /// <param name="vDistance">Vertical distance.</param>
    /// <param name="requestRerender"><c>true</c> to request the control invalidates.</param>
    /// 
    /// <returns>
    /// <list type="table">
    ///   <item><c>true</c> if the viewport is changed.</item>
    ///   <item><c>false</c> if the viewport is unchanged.</item>
    /// </list>
    /// </returns>
    public bool PanTo(double hDistance, double vDistance, Point? pointerPosition, bool requestRerender = true)
    {
        if (hDistance == 0 && vDistance == 0) return false;

        hDistance *= Dpi;
        vDistance *= Dpi;
        var oldSrcRect = SrcRect;


        // horizontal
        if (hDistance != 0)
        {
            var newX = _logicalSrcPoint.X + hDistance / _zooming.Factor;
            _logicalSrcPoint = _logicalSrcPoint.WithX(newX);
        }

        // vertical 
        if (vDistance != 0)
        {
            var newY = _logicalSrcPoint.Y + vDistance / _zooming.Factor;
            _logicalSrcPoint = _logicalSrcPoint.WithY(newY);
        }

        if (pointerPosition is not null)
        {
            _zooming.ZoomedPoint = pointerPosition.Value;
        }


        // emit panning event
        Panning?.Invoke(this, new ViewerPanEventArgs(oldSrcRect, SrcRect));

        // update drawing regions
        CalculateDrawingRegion();


        if (requestRerender) InvalidateVisual();

        return true;
    }


    /// <summary>
    /// Moves the view to the left by a specified distance.
    /// </summary>
    /// <param name="distance">Distance to move</param>
    /// <param name="requestRerender"><c>true</c> to request the control invalidates.</param>
    public void PanLeft(double? distance = null, bool requestRerender = true)
    {
        distance ??= PanSpeed;
        distance = Math.Max(distance.Value, 0); // min 0

        _ = PanTo(-distance.Value, 0, null, requestRerender);
    }


    /// <summary>
    /// Moves the view to the right by a specified distance.
    /// </summary>
    /// <param name="distance">Distance to move</param>
    /// <param name="requestRerender"><c>true</c> to request the control invalidates.</param>
    public void PanRight(double? distance = null, bool requestRerender = true)
    {
        distance ??= PanSpeed;
        distance = Math.Max(distance.Value, 0); // min 0

        _ = PanTo(distance.Value, 0, null, requestRerender);
    }


    /// <summary>
    /// Moves the view to the top by a specified distance.
    /// </summary>
    /// <param name="distance">Distance to move</param>
    /// <param name="requestRerender"><c>true</c> to request the control invalidates.</param>
    public void PanUp(double? distance = null, bool requestRerender = true)
    {
        distance ??= PanSpeed;
        distance = Math.Max(distance.Value, 0); // min 0

        _ = PanTo(0, -distance.Value, null, requestRerender);
    }


    /// <summary>
    /// Moves the view to the bottom by a specified distance.
    /// </summary>
    /// <param name="distance">Distance to move</param>
    /// <param name="requestRerender"><c>true</c> to request the control invalidates.</param>
    public void PanDown(double? distance = null, bool requestRerender = true)
    {
        distance ??= PanSpeed;
        distance = Math.Max(distance.Value, 0); // min 0

        _ = PanTo(0, distance.Value, null, requestRerender);
    }




    #endregion // Public Methods

}
