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

using D2Phap.Canvas2D;
using ImageGlass.WinNT.Common;
using Vortice.Direct2D1;
using Windows.Foundation;

namespace ImageGlass.WinNT;


public partial class VirtualViewerControl
{

    private CheckerboardInfo _checkerboard = new();
    private ID2D1BitmapBrush1? _checkerboardBrush;



    // Public Properties
    #region Public Properties

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
                _checkerboard.Size = value;

                // reset checkerboard brush
                DisposeCheckerboardBrushes();

                Invalidate();
            }
        }
    }


    /// <summary>
    /// Returns the DPI scale of the checkerboard size.
    /// </summary>
    public Size CheckerboardSize_Dpi => DpiScale(_checkerboard.Size);


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
                _checkerboard.Mode = value;

                // reset checkerboard brush
                DisposeCheckerboardBrushes();

                Invalidate();
            }
        }
    }

    #endregion // Public Properties



    // Private Functions
    #region Private Functions

    /// <summary>
    /// Draw checkerboard layer
    /// </summary>
    private void DrawCheckerboardLayer(SwapChainCanvasRenderEventArgs e)
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

            region = _destRect;
        }
        else
        {
            region = DrawingArea;
        }


        // create bitmap brush
        _checkerboardBrush ??= CreateCheckerboardTileBrush();

        // draw checkerboard
        e.D2DContext.FillRectangle(region.ToRawRectF(), _checkerboardBrush);
    }


    /// <summary>
    /// Creates checkerboard tile brush
    /// </summary>
    private ID2D1BitmapBrush1? CreateCheckerboardTileBrush()
    {
        // create tile: [X,O]
        //              [O,X]
        var size = CheckerboardSize_Dpi;
        var width = (int)size.Width * 2;
        var height = (int)size.Height * 2;

        // 1. create empty WIC bitmap
        var tileBmp = PhotoWIC.CreateWicBitmapSource(width, height);
        if (tileBmp == null) return null;


        // 2. create render target from WIC bitmap
        using var tileBmpRt = PhotoWIC.CreateD2dRenderTarget(tileBmp);
        if (tileBmpRt == null) return null;


        // 3. start drawing
        tileBmpRt.AntialiasMode = AntialiasMode.Aliased;
        tileBmpRt.BeginDraw();

        // 3.1 draw X cells -------------------------------
        using var brush1 = tileBmpRt.CreateSolidColorBrush(_checkerboard.Color1.ToVorticeColor());

        // draw cell: [X, ]
        //            [ ,X]
        tileBmpRt.FillRectangle(new Rect(0, 0, size.Width, size.Height).ToRawRectF(), brush1);
        tileBmpRt.FillRectangle(new Rect(size.Width, size.Height, size.Width, size.Height).ToRawRectF(), brush1);

        // 3.2 draw O cells -------------------------------
        using var brush2 = tileBmpRt.CreateSolidColorBrush(_checkerboard.Color2.ToVorticeColor());

        // draw cell: [X,O]
        //            [O,X]
        tileBmpRt.FillRectangle(new Rect(size.Width, 0, size.Width, size.Height).ToRawRectF(), brush2);
        tileBmpRt.FillRectangle(new Rect(0, size.Height, size.Width, size.Height).ToRawRectF(), brush2);

        tileBmpRt.EndDraw();


        // 4. create D2DBitmap from WICBitmapSource
        using var tileD2 = PhotoWIC.CreateD2dBitmap(tileBmp, D2dContext);
        var bmpProps = new BitmapBrushProperties1()
        {
            ExtendModeX = ExtendMode.Wrap,
            ExtendModeY = ExtendMode.Wrap,
        };


        // 5. create bitmap brush
        var tileBrush = D2dContext.CreateBitmapBrush(tileD2, bmpProps);

        return tileBrush;
    }


    /// <summary>
    /// Disposes and set all checkerboard brushes to <c>null</c>.
    /// </summary>
    private void DisposeCheckerboardBrushes()
    {
        _checkerboardBrush?.Dispose();
        _checkerboardBrush = null;
    }

    #endregion // Private Functions


}


