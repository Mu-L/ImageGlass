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
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using ImageGlass.Common.Photoing;
using ImageGlass.UI.Viewer.Checkerboard;
using SkiaSharp;
using System;
using System.Globalization;
using System.Threading;

namespace ImageGlass.UI.Viewer;


public partial class ViewerControl
{
    private PhotoRenderer? _photoRenderer;

    // FPS
    private double _fps;
    private long _lastFpsTime = Environment.TickCount64;


    // drawing image
    private SKImage? _imgSource;
    private SKImage? _imgPreview;
    private SkiaAnimator? _animator;

    private RenderTargetBitmap? _bmpCheckerboard;
    private readonly CheckerboardInfo _checkerboard = new();

    private readonly Lock _lockSource = new();
    private readonly Lock _lockPreview = new();
    private ImageInterpolation _interpolationScaleDown = ImageInterpolation.High;
    private ImageInterpolation _interpolationScaleUp = ImageInterpolation.None;



    #region Public Properties

    /// <summary>
    /// Gets, sets the debug mode.
    /// </summary>
    public bool EnableDebug { get; set; } = true;


    /// <summary>
    /// Gets, sets value indicates whether the previewing is enabled or not.
    /// </summary>
    public bool EnableImagePreview { get; set; } = true;


    /// <summary>
    /// Gets the rectangle of the source image region being drawn.
    /// </summary>
    public Rect SrcRect { get; internal set; } = new();


    /// <summary>
    /// Gets rectangle of the viewport.
    /// </summary>
    public Rect DestRect { get; internal set; } = new();


    /// <summary>
    /// Gets or sets the size of the checkerboard.
    /// </summary>
    public Size CheckerboardSize
    {
        get => _checkerboard.Size;
        set
        {
            if (_checkerboard.Size.Width != value.Width
                || _checkerboard.Size.Height != value.Height)
            {
                DisposeCheckerboard();

                _checkerboard.Size = value;
                InvalidateVisual();
            }
        }
    }


    /// <summary>
    /// Gets or sets the mode of the checkerboard.
    /// </summary>
    public CheckerboardMode CheckerboardMode
    {
        get => _checkerboard.Mode;
        set
        {
            if (_checkerboard.Mode != value)
            {
                DisposeCheckerboard();

                _checkerboard.Mode = value;
                InvalidateVisual();
            }
        }
    }


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
                InvalidateVisual();
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
                InvalidateVisual();
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

            return ImageInterpolation.None;
        }
    }

    #endregion // Public Properties



    protected void DisposeCheckerboard()
    {
        _bmpCheckerboard?.Dispose();
        _bmpCheckerboard = null;
    }


    public override void Render(DrawingContext c)
    {
        CalculateFps();

        OnDrawCheckerboard(c);      // draw checkerboard
        OnDrawImage(c);             // draw image
        OnDrawSelection(c);         // draw selection
        base.Render(c);
        OnDrawAnimationSource();    // draw animation source
        OnDrawDebugInfo(c);         // draw debug info


        // loop for drawing animation source
        if (EnableDrawingAnimation)
        {
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        }
    }


    /// <summary>
    /// Calculates FPS in debug mode.
    /// </summary>
    private void CalculateFps()
    {
        if (!EnableDebug) return;

        var now = Environment.TickCount64;
        var delta = now - _lastFpsTime;

        if (delta > 0)
        {
            _fps = _fps * 0.9 + (1000.0 / delta) * 0.1;
            _fps = Math.Round(_fps, 2);
        }

        _lastFpsTime = now;
    }


    /// <summary>
    /// Draw debug information.
    /// </summary>
    protected virtual void OnDrawDebugInfo(DrawingContext c)
    {
        if (!EnableDebug) return;

        var ft = new FormattedText(
            $"""
            DPI = {Dpi}, FPS = {_fps}
            Control Bounds = {Bounds}
            DrawingArea = {DrawingArea}
            Image size = {BitmapSize}
            _srcRect = {SrcRect}
            _destRect = {DestRect}
            _zoomFactor = {_zooming.Factor}
            _zoomedPoint = {_zooming.ZoomedPoint}
            """,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas"),
            13, Brushes.HotPink);

        // draw text info
        c.DrawText(ft, new Point(DrawingArea.X + 20, DrawingArea.Y + 20));

        // draw image bounds
        c.DrawRectangle(new Pen(Brushes.LightGreen, 2, DashStyle.Dash, PenLineCap.Round, PenLineJoin.Round), DestRect);

        // draw zoomed point
        c.DrawEllipse(Brushes.Red, new Pen(Brushes.White, 2), _zooming.ZoomedPoint, 5, 5);
    }


    /// <summary>
    /// Draws checkerboard.
    /// </summary>
    protected virtual void OnDrawCheckerboard(DrawingContext c)
    {
        if (CheckerboardMode == CheckerboardMode.None) return;

        // region to draw
        Rect region;
        if (CheckerboardMode == CheckerboardMode.Image)
        {
            //if (UseWebview2)
            //{
            //    region = _web2DestRect;
            //}
            //else
            //{
            //    // no need to draw checkerboard if image does not has alpha pixels
            //    if (!HasAlphaPixels) return;

            //    region = _destRect;
            //}

            region = DestRect;
        }
        else
        {
            region = DrawingArea;
        }


        // create empty render bitmap: [X, O]
        //                             [O, X]
        var cellW = (int)(_checkerboard.Size.Width * Dpi);
        var cellH = (int)(_checkerboard.Size.Height * Dpi);
        var pxSize = new PixelSize(cellW * 2, cellH * 2);
        _bmpCheckerboard ??= new RenderTargetBitmap(pxSize);

        // fill the bitmap with a tile
        using (var ctx = _bmpCheckerboard.CreateDrawingContext())
        {
            var brushX = new ImmutableSolidColorBrush(Colors.Black);
            var brushO = new ImmutableSolidColorBrush(Colors.White);

            // draw cells: [X, ]
            //             [ ,X]
            ctx.FillRectangle(brushX, new Rect(0, 0, cellW, cellH));
            ctx.FillRectangle(brushX, new Rect(cellW, cellH, cellW, cellH));

            // draw cells: [ ,O]
            //             [O, ]
            ctx.FillRectangle(brushO, new Rect(cellW, 0, cellW, cellH));
            ctx.FillRectangle(brushO, new Rect(0, cellH, cellW, cellH));
        }


        // create brush for checkerboard
        var imgBrush = new ImageBrush
        {
            Source = _bmpCheckerboard,
            Stretch = Stretch.Fill,
            TileMode = TileMode.Tile,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            DestinationRect = new(0, 0, cellW, cellH, RelativeUnit.Absolute),
            Opacity = 0.1,
        };


        // draw the checkerboard
        using (c.PushRenderOptions(new() { BitmapInterpolationMode = BitmapInterpolationMode.None }))
        {
            c.FillRectangle(imgBrush, region);
        }
    }


    /// <summary>
    /// Draws image.
    /// </summary>
    protected virtual void OnDrawImage(DrawingContext c)
    {
        _photoRenderer ??= new PhotoRenderer(this);
        c.Custom(_photoRenderer);


        // draw bitmap preview
        if (_imgPreview is not null)
        {
            lock (_lockPreview)
            {
                if (_imgPreview is not null)
                {
                    _photoRenderer.Image = _imgPreview;
                    c.Custom(_photoRenderer);
                }
            }
        }


        // draw bitmap in full resolution
        if (_imgSource is not null)
        {
            lock (_lockSource)
            {
                if (_imgSource is not null)
                {
                    _photoRenderer.Image = _imgSource;
                    c.Custom(_photoRenderer);
                }
            }
        }

    }


    /// <summary>
    /// Draws animation source.
    /// </summary>
    protected virtual void OnDrawAnimationSource()
    {
        //if (UseWebview2) return;

        // Panning
        if (AnimationSource.HasFlag(AnimationSources.PanLeft))
        {
            PanLeft(requestRerender: false);
        }
        else if (AnimationSource.HasFlag(AnimationSources.PanRight))
        {
            PanRight(requestRerender: false);
        }

        if (AnimationSource.HasFlag(AnimationSources.PanUp))
        {
            PanUp(requestRerender: false);
        }
        else if (AnimationSource.HasFlag(AnimationSources.PanDown))
        {
            PanDown(requestRerender: false);
        }


        // Zooming
        if (AnimationSource.HasFlag(AnimationSources.ZoomIn))
        {
            _ = ZoomByDeltaToPoint(20, null, requestRerender: false);
        }
        else if (AnimationSource.HasFlag(AnimationSources.ZoomOut))
        {
            _ = ZoomByDeltaToPoint(-20, null, requestRerender: false);
        }
    }



}
