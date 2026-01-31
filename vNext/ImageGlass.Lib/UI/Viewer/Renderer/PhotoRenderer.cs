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
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace ImageGlass.UI.Viewer;

public partial class PhotoRenderer : ICustomDrawOperation
{

    #region IDisposable Disposing

    /// <summary>
    /// Gets a value indicating whether the object has been disposed.
    /// </summary>
    public bool IsDisposed { get; protected set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            OnDisposing();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PhotoRenderer()
    {
        Dispose(false);
    }

    #endregion


    private ViewerControl _viewer;


    #region Public Properties

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Rect Bounds => _viewer.DrawingArea;


    /// <summary>
    /// Gets, sets the image to render.
    /// </summary>
    public SKImage? Image { get; set; }

    #endregion // Public Properties


    public PhotoRenderer(ViewerControl viewer)
    {
        _viewer = viewer;
    }



    #region Interface Methods

    /// <summary>
    /// Releases the managed objects.
    /// </summary>
    protected virtual void OnDisposing()
    {
        //
    }


    public bool Equals(ICustomDrawOperation? other) => false;


    public bool HitTest(Point p) => true;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Render(ImmediateDrawingContext c)
    {
        if (Image is null) return;

        var leaseFeature = c.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null) return;

        using var lease = leaseFeature.Lease();
        if (lease is null) return;


        var canvas = lease.SkCanvas;
        canvas.Save();

        // paint the image
        using var paintOptions = new SKPaint
        {
            FilterQuality = (SKFilterQuality)_viewer.CurrentInterpolation,
        };
        canvas.DrawImage(Image, _viewer.SrcRect.ToSKRect(), _viewer.DestRect.ToSKRect(), paintOptions);

        canvas.Restore();
    }

    #endregion // Interface Methods



    #region Private Methods

    /// <summary>
    /// Returns a new image after applying color management effect on the original image.
    /// </summary>
    private static SKImage? ApplyColorManagement(GRContext gr, SKImage oriImg, SKColorSpace destIccColor)
    {
        // 1. convert the original image to the destination color space
        using var convertedImg = ConvertToColorSpace(gr, oriImg, destIccColor);

        // 2. convert again to sRGB color space
        var newImg = ConvertToSrgbSpace(gr, convertedImg);

        return newImg;
    }


    /// <summary>
    /// Returns a new image after converting the given image to the destination color space.
    /// </summary>
    private static SKImage? ConvertToColorSpace(GRContext gr, SKImage? srcImg, SKColorSpace destColorSpace)
    {
        if (srcImg is null) return null;

        var dstInfo = srcImg.Info.WithColorSpace(destColorSpace);
        using var surface = SKSurface.Create(gr, false, dstInfo);
        if (surface is null) return null;

        // convert ICC color profile with GPU
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawImage(srcImg, 0, 0);

        // return the converted image
        return surface.Snapshot();
    }


    /// <summary>
    /// Returns a new image after converting the given image to the sRGB color space.
    /// </summary>
    private static SKImage? ConvertToSrgbSpace(GRContext gr, SKImage? srcImg)
    {
        if (srcImg is null) return null;

        var srgb = SKColorSpace.CreateSrgb();
        var dstInfo = srcImg.Info.WithColorSpace(srgb);
        using var surface = SKSurface.Create(gr, false, dstInfo);
        if (surface is null) return null;

        // convert sRGB color profile with GPU
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawImage(srcImg, 0, 0);

        // return the converted image
        return surface.Snapshot();
    }


    #endregion // Private Methods


}
