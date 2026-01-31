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
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.UI.Viewer.Checkerboard;
using SkiaSharp;
using System.Globalization;
using System.Threading;

namespace ImageGlass.UI.Viewer;


public partial class ViewerControl
{
    private PhotoRenderer? _photoRenderer;

    internal Photo? _photo;
    internal CancellationTokenSource? _cancelPreview;
    internal InterlockedBool _isPreviewing = new();

    // drawing image
    internal SKImage? _bmpSource;
    internal SKImage? _bmpPreview;

    internal RenderTargetBitmap? _bmpCheckerboard;
    internal readonly CheckerboardInfo _checkerboard = new();

    internal readonly Lock _lockSource = new();
    internal readonly Lock _lockPreview = new();
    internal ImageInterpolation _interpolationScaleDown = ImageInterpolation.High;
    internal ImageInterpolation _interpolationScaleUp = ImageInterpolation.None;


    // Public Properties
    #region Public Properties

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
        OnDrawCheckerboard(c);      // draw checkerboard
        OnDrawImage(c);             // draw image
        OnDrawSelection(c);         // draw selection
        OnDrawDebugInfo(c);         // draw debug info

        base.Render(c);
    }


    protected virtual void OnDrawDebugInfo(DrawingContext c)
    {
        var ft = new FormattedText(
            $"""
            DPI = {Dpi}
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


    protected virtual void OnDrawImage(DrawingContext c)
    {
        _photoRenderer ??= new PhotoRenderer(this);
        c.Custom(_photoRenderer);


        // draw bitmap preview
        if (_bmpPreview is not null)
        {
            lock (_lockPreview)
            {
                if (_bmpPreview is not null)
                {
                    _photoRenderer.Image = _bmpPreview;
                    c.Custom(_photoRenderer);
                }
            }
        }


        // draw bitmap in full resolution
        if (_bmpSource is not null)
        {
            lock (_lockSource)
            {
                if (_bmpSource is not null)
                {
                    _photoRenderer.Image = _bmpSource;
                    c.Custom(_photoRenderer);
                }
            }
        }

    }



}
