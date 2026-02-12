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
using Avalonia.Threading;
using ImageGlass.Common;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Types;
using SkiaSharp;
using System;
using System.Threading;

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


    private readonly Rect _bounds;
    private readonly Action<SKImage?> _onDrawFirstTime;
    private readonly Lock _lock;

    private readonly SKImageRef? _imgSource;
    private readonly SKImageRef? _imgRender;
    private readonly SKRect _srcRect;
    private readonly SKRect _destRect;
    private readonly bool _hasSrcColorProfile;
    private readonly ImageInterpolation _interpolation;
    private readonly bool _isFirstDraw;


    #region Public Properties

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Rect Bounds => _bounds;


    #endregion // Public Properties


    public PhotoRenderer(ViewerControl viewer, Action<SKImage?> processFirstDrawFn)
    {
        _lock = viewer._lock;

        lock (_lock)
        {
            _bounds = viewer.Bounds;
            _onDrawFirstTime = processFirstDrawFn;

            _imgSource = viewer._imgSource;
            _imgRender = viewer._imgRender;

            // keep images alive until this renderer is disposed
            _imgSource?.KeepAlive();
            _imgRender?.KeepAlive();

            _srcRect = viewer.SrcRect.ToSKRect();
            _destRect = viewer.DestRect.ToSKRect();
            _interpolation = viewer.CurrentInterpolation;
            _hasSrcColorProfile = viewer.Photo?.Metadata?.ColorProfileData is not null;
            _isFirstDraw = viewer._isFirstDraw.Value;
        }
    }



    #region Interface Methods


    protected virtual void OnDisposing()
    {
        _imgSource?.RequestDispose();
        _imgRender?.RequestDispose();
    }
    public bool Equals(ICustomDrawOperation? other) => false;
    public bool HitTest(Point p) => true;



    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Render(ImmediateDrawingContext c)
    {
        var leaseFeature = c.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null) return;

        using var lease = leaseFeature.Lease();
        if (lease is null) return;


        lock (_lock)
        {
            SKImageRef.ImageLease? imageLease = null;
            SKImageRef.ImageLease? srcLease = null;

            try
            {
                SKImage? imageRender;

                if (_isFirstDraw)
                {
                    srcLease = _imgSource?.Acquire();
                    var srcImage = srcLease?.Image;
                    if (srcImage is null || srcImage.IsDisposed()) return;

                    // process the original image
                    imageRender = ProcessImageForFirstDrawing(lease.GrContext, srcImage);


                    // set the image to draw
                    imageRender ??= srcImage;

                    // update the processed image
                    Dispatcher.UIThread.Post(() => _onDrawFirstTime(imageRender), DispatcherPriority.Render);
                }
                else
                {
                    imageLease = _imgRender?.Acquire() ?? _imgSource?.Acquire();
                    imageRender = imageLease?.Image;
                }

                if (imageRender is null || imageRender.IsDisposed()) return;


                // start drawing image
                var canvas = lease.SkCanvas;
                canvas.Save();

                using var paintOptions = new SKPaint
                {
                    FilterQuality = (SKFilterQuality)_interpolation,
                };

                canvas.DrawImage(imageRender, _srcRect, _destRect, paintOptions);
                canvas.Restore();


                if (_isFirstDraw)
                {
                    // clear old cache
                    lease.GrContext?.PurgeResources();
                }
            }
            finally
            {
                imageLease?.Dispose();
                srcLease?.Dispose();
            }
        }
    }


    #endregion // Interface Methods



    #region Private Methods

    /// <summary>
    /// Processes image for the first drawing.
    /// </summary>
    private SKImage? ProcessImageForFirstDrawing(GRContext? dc, SKImage? srcImage)
    {
        if (dc.IsDisposed() || srcImage.IsDisposed()) return null;

        SKImage? outputImage = null;
        lock (_lock)
        {
            // double check the source image
            if (srcImage.IsDisposed()) return outputImage;

            var useColorManagement = !Core.DestColorProfile.IsDisposed()
                    && Core.IsDestColorProfileSupported
                    && (Core.Config.ShouldUseColorProfileForAll || _hasSrcColorProfile);

            // apply color management
            if (useColorManagement)
            {
                outputImage = ApplyColorManagement(dc, srcImage, Core.DestColorProfile);
            }
        }

        return outputImage;
    }


    /// <summary>
    /// Returns a new image after applying color management effect on the original image.
    /// </summary>
    private SKImage? ApplyColorManagement(GRContext dc, SKImage oriImg, SKColorSpace? destColor)
    {
        if (destColor.IsDisposed()) return null;

        // 1. convert the original image to the destination color space
        using var convertedImg = ConvertToColorSpace(dc, oriImg, destColor);

        // 2. convert again to sRGB color space
        var newImg = ConvertToSrgbSpace(dc, convertedImg);

        return newImg;
    }


    /// <summary>
    /// Returns a new image after converting the given image to the destination color space.
    /// </summary>
    private SKImage? ConvertToColorSpace(GRContext dc, SKImage? srcImg, SKColorSpace? destColor)
    {
        if (srcImg.IsDisposed() || destColor.IsDisposed()) return null;

        lock (_lock)
        {
            if (srcImg.IsDisposed() || destColor.IsDisposed()) return null;

            var dstInfo = srcImg.Info.WithColorSpace(destColor);
            using var surface = SKSurface.Create(dc, false, dstInfo);
            if (surface.IsDisposed()) return null;

            // convert ICC color profile with GPU
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.DrawImage(srcImg, 0, 0);

            // return the converted image
            return surface.Snapshot();
        }
    }


    /// <summary>
    /// Returns a new image after converting the given image to the sRGB color space.
    /// </summary>
    private SKImage? ConvertToSrgbSpace(GRContext gr, SKImage? srcImg)
    {
        if (srcImg.IsDisposed()) return null;

        lock (_lock)
        {
            if (srcImg.IsDisposed()) return null;

            var sRGB = SKColorSpace.CreateSrgb();
            var dstInfo = srcImg.Info.WithColorSpace(sRGB);
            using var surface = SKSurface.Create(gr, false, dstInfo);
            if (surface.IsDisposed()) return null;

            // convert sRGB color profile with GPU
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.DrawImage(srcImg, 0, 0);

            // return the converted image
            return surface.Snapshot();
        }
    }


    #endregion // Private Methods


}
