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

using ImageGlass.Common;
using System;
using System.Linq;
using Windows.Foundation;

namespace ImageGlass.UI;


public partial class VirtualViewerControl
{

    private ZoomInfo _zooming = new();
    private double _panSpeed = 20f;
    private bool _enablePanningVelocity = true;


    // Public Events
    #region Public Events

    /// <summary>
    /// Occurs when <see cref="ZoomFactor"/> value changes.
    /// </summary>
    public event TypedEventHandler<VirtualViewerControl, ZoomEventArgs>? ZoomChanged;


    /// <summary>
    /// Occurs when the image is being panned.
    /// </summary>
    public event TypedEventHandler<VirtualViewerControl, PanningEventArgs>? Panning;

    #endregion // Public Events



    // Public Properies
    #region Public Properies

    /// <summary>
    /// Gets, sets zoom mode.
    /// </summary>
    public ZoomMode ZoomMode
    {
        get => _zooming.Mode;
        set
        {
            _zooming.Mode = value;
            Refresh();
        }
    }


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
            _zooming.Speed = Math.Min(value, 500f); // max 500f
            _zooming.Speed = Math.Max(value, -500f); // min -500f
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
    /// Gets the center point of the image viewport.
    /// </summary>
    public Point ViewportCenterPoint => new(
        _destRect.X + _destRect.Width / 2.0,
        _destRect.Y + _destRect.Height / 2.0);


    #endregion // Public Properies




    // Public Methods
    #region Public Methods

    /// <summary>
    /// Calculates the drawing region for an image based on zoom level and control dimensions.
    /// Adjusts source and destination rectangles accordingly.
    /// </summary>
    public virtual void CalculateDrawingRegion()
    {
        if (DrawingArea.IsEmpty() || BitmapSize.IsEmpty()) return;

        // 1. scale the values according to DPI
        // 1.1 zoom point
        var zoomX = _zooming.ZoomedPoint.X * CompositionScaleX - Padding.Left;
        var zoomY = _zooming.ZoomedPoint.Y * CompositionScaleY - Padding.Top;


        // 1.2 source and viewport size
        var controlW = DrawingArea.Width;
        var controlH = DrawingArea.Height;

        var scaledImgWidth = BitmapSize.Width * _zooming.Factor;
        var scaledImgHeight = BitmapSize.Height * _zooming.Factor;


        // 1.3 initialize new values for source and destination rectangles
        var srcX = _srcRect.X;
        var srcY = _srcRect.Y;
        var srcWidth = _srcRect.Width;
        var srcHeight = _srcRect.Height;

        var destX = _destRect.X;
        var destY = _destRect.Y;
        var destWidth = _destRect.Width;
        var destHeight = _destRect.Height;


        // 2. calculate x-axis and width
        if (scaledImgWidth <= controlW)
        {
            srcX = 0;
            srcWidth = BitmapSize.Width;

            destX = (controlW - scaledImgWidth) / 2.0f + DrawingArea.Left;
            destWidth = scaledImgWidth;
        }
        else
        {
            var oldControlW = controlW / _zooming.OldFactor;
            var newControlW = controlW / _zooming.Factor;

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

            destY = (controlH - scaledImgHeight) / 2f + DrawingArea.Top;
            destHeight = scaledImgHeight;
        }
        else
        {
            var oldControlH = controlH / _zooming.OldFactor;
            var newControlH = controlH / _zooming.Factor;

            srcY += (oldControlH - newControlH) / ((controlH + float.Epsilon) / zoomY);
            srcHeight = newControlH;

            destY = DrawingArea.Top;
            destHeight = controlH;
        }

        _zooming.OldFactor = _zooming.Factor;


        // 4. Panning to the edge:
        // Make sure the source coordinates are within the image bounds
        if (srcX < 0)
        {
            srcX = 0;
        }
        else if (srcX + srcWidth > BitmapSize.Width)
        {
            srcX = BitmapSize.Width - srcWidth;
        }

        if (srcY + srcHeight > BitmapSize.Height)
        {
            srcY = BitmapSize.Height - srcHeight;
        }

        if (srcY < 0)
        {
            srcY = 0;
        }


        // 5. get the final rectangles
        _srcRect = new(srcX, srcY, srcWidth, srcHeight);
        _destRect = new(destX, destY, destWidth, destHeight);
    }


    /// <summary>
    /// Sets the zoom factor for a view while ensuring it stays within defined limits.
    /// </summary>
    public void SetZoomFactor(double zoomValue, bool isManualZoom)
    {
        if (_zooming.Factor == zoomValue) return;

        _zooming.Factor = Math.Min(MaxZoom, Math.Max(zoomValue, MinZoom));
        _zooming.IsManual = isManualZoom;


        // update drawing regions
        CalculateDrawingRegion();

        Invalidate();


        ZoomChanged?.Invoke(this, new ZoomEventArgs()
        {
            ZoomFactor = _zooming.Factor,
            IsManualZoom = _zooming.IsManual,
            IsZoomModeChange = false,
            IsPreviewingImage = _isPreviewing,
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
        _zooming.Mode = mode ?? _zooming.Mode;
        _zooming.Factor = CalculateZoomFactor(_zooming.Mode, BitmapSize.Width, BitmapSize.Height);
        _zooming.IsManual = isManualZoom;

        // update drawing regions
        CalculateDrawingRegion();


        ZoomChanged?.Invoke(this, new ZoomEventArgs()
        {
            ZoomFactor = _zooming.Factor,
            IsManualZoom = _zooming.IsManual,
            IsZoomModeChange = mode != _zooming.Mode,
            IsPreviewingImage = _isPreviewing,
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
    /// Scales the image using factor value.
    /// </summary>
    /// <param name="factor">Zoom factor (<c>1.0f = 100%</c>).</param>
    /// <param name="point">
    /// Client's cursor location to zoom out.
    /// If its value is <c>null</c> or outside of the <see cref="VirtualViewerControl"/> control,
    /// <c><see cref="ViewportCenterPoint"/></c> is used.
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
        if (!Bounds_Dpi.Contains(location))
        {
            location = ViewportCenterPoint;
        }

        // get the gap when the viewport is smaller than the control size
        var gapX = Math.Max(0, _destRect.X);
        var gapY = Math.Max(0, _destRect.Y);

        // the location after zoomed
        var zoomedLocation = new Point()
        {
            X = (location.X - gapX) * newZoomFactor / ZoomFactor,
            Y = (location.Y - gapY) * newZoomFactor / ZoomFactor,
        };

        // the distance of 2 points after zoomed
        var zoomedDistance = new Size()
        {
            Width = Math.Max(0, zoomedLocation.X - location.X),
            Height = Math.Max(0, zoomedLocation.Y - location.Y),
        };

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

            PanTo(zoomedDistance.Width, zoomedDistance.Height, location, requestRerender);

            // emit ZoomChanged event
            ZoomChanged?.Invoke(this, new ZoomEventArgs()
            {
                ZoomFactor = _zooming.Factor,
                IsManualZoom = _zooming.IsManual,
                IsZoomModeChange = false,
                IsPreviewingImage = _isPreviewing,
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
    /// <c><see cref="ViewportCenterPoint"/></c> is the default value.
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
        var isZoomingByMouseWheel = true; // TODO: Math.Abs(delta) == SystemInformation.MouseWheelScrollDelta;

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
            var speed = delta / (501f - ZoomSpeed);

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
        if (!Bounds_Dpi.Contains(location))
        {
            location = ViewportCenterPoint;
        }


        _zooming.OldFactor = _zooming.Factor;
        _zooming.Factor = newZoomFactor;
        _zooming.IsManual = true;
        _zooming.ZoomedPoint = new(location.X, location.Y);


        // update drawing regions
        CalculateDrawingRegion();


        if (requestRerender) Invalidate();


        // emit ZoomChanged event
        ZoomChanged?.Invoke(this, new ZoomEventArgs()
        {
            ZoomFactor = _zooming.Factor,
            IsManualZoom = _zooming.IsManual,
            IsZoomModeChange = false,
            IsPreviewingImage = _isPreviewing,
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

        hDistance *= DpiX;
        vDistance *= DpiY;
        var oldSrcRect = _srcRect;


        // horizontal
        if (hDistance != 0)
        {
            _srcRect.X += hDistance / _zooming.Factor;
        }

        // vertical 
        if (vDistance != 0)
        {
            _srcRect.Y += vDistance / _zooming.Factor;
        }

        _zooming.ZoomedPoint = pointerPosition ?? new();


        // emit panning event
        Panning?.Invoke(this, new PanningEventArgs(oldSrcRect, _srcRect));

        // update drawing regions
        CalculateDrawingRegion();


        if (requestRerender) Invalidate();

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


