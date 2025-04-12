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
using SharpGen.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Vortice.Direct2D1;
using Vortice.WIC;


namespace ImageGlass.WinNT.Common;


public partial class Photo : IPhoto<IWICBitmapSource>
{

    #region IDisposable Disposing

    public bool IsDisposed { get; private set; } = false;


    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            DisposeNativeResources();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Photo()
    {
        Dispose(false);
    }

    #endregion



    private IWICBitmapSource? _bitmap;
    private PhotoColorContext? _colorContext;
    private IWICPixelFormatInfo2? _pixelFormatInfo;


    public IWICBitmapSource? Bitmap => _bitmap;

    public int Width => _bitmap == null ? 0 : _bitmap.Size.Width;

    public int Height => _bitmap == null ? 0 : _bitmap.Size.Height;


    /// <summary>
    /// Gets the color contexts of the photo.
    /// </summary>
    public PhotoColorContext ColorContext => LazyInitializer.EnsureInitialized(
        ref _colorContext, GetColorContext);


    /// <summary>
    /// Gets the pixel information of the photo.
    /// </summary>
    public IWICPixelFormatInfo2? PixelFormatInfo => LazyInitializer.EnsureInitialized(
        ref _pixelFormatInfo, LoadPixelInfo);


    public Photo() { }


    public Photo(string filePath)
    {
        Load(filePath);
    }


    public Photo(IWICBitmapSource wicSrc)
    {
        DisposeNativeResources();

        _bitmap = wicSrc;
    }


    // Public Functions
    #region Public Functions

    /// <summary>
    /// Loads an image from file.
    /// </summary>
    public void Load(string filePath, uint frameIndex = 0)
    {
        DisposeNativeResources();

        try
        {
            using var wicFactory = new IWICImagingFactory2();
            using var decoder = wicFactory.CreateDecoderFromFileName(filePath);
            var frameBmp = decoder.GetFrame(frameIndex);

            _bitmap = frameBmp;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }


    /// <summary>
    /// Converts the current bitmap to a 32bpp PBGRA format.
    /// </summary>
    public Photo? ConvertTo32bppPBGRA()
    {
        if (_bitmap == null) return null;

        try
        {
            var newBmp = WIC.WICConvertBitmapSource(
                Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA,
                _bitmap);

            return new Photo(newBmp);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }


    /// <summary>
    /// Creates a render target from a bitmap source for drawing operations.
    /// </summary>
    public ID2D1RenderTarget? CreateDirect2dRenderTarget()
    {
        ID2D1RenderTarget? target = null;

        try
        {
            using var factory = D2D1.D2D1CreateFactory<ID2D1Factory8>(FactoryType.MultiThreaded);

            target = factory.CreateWicBitmapRenderTarget(_bitmap.As<IWICBitmap>(),
                new(Vortice.DCommon.PixelFormat.Premultiplied));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return target;
    }


    /// <summary>
    /// Creates a Direct2D bitmap from an existing bitmap if available.
    /// </summary>
    public ID2D1Bitmap1? CreateDirect2dBitmap(ID2D1DeviceContext dc, BitmapProperties1? bmpProps = null)
    {
        if (_bitmap == null) return null;

        try
        {
            var newPhoto = ConvertTo32bppPBGRA();
            if (newPhoto == null) return null;

            return dc.CreateBitmapFromWicBitmap(newPhoto.Bitmap, bmpProps);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }


    #endregion // Public Functions


    // Private Functions
    #region Private Functions

    /// <summary>
    /// Releases unmanaged resources.
    /// </summary>
    private void DisposeNativeResources()
    {
        // dispose color contexts
        _colorContext?.Dispose();
        _colorContext = null;


        // dispose pixel format info
        _pixelFormatInfo?.Dispose();
        _pixelFormatInfo = null;


        //  dispose bitmap
        _bitmap?.Dispose();
        _bitmap = null;
    }


    /// <summary>
    /// Retrieves an array of color contexts from a bitmap if available.
    /// </summary>
    private unsafe PhotoColorContext GetColorContext()
    {
        if (_bitmap == null) return new();

        using var wicFactory = new IWICImagingFactory2();
        var frame = _bitmap.As<IWICBitmapFrameDecode>();

        try
        {
            IWICColorContext? bestContext = null;
            var contexts = frame.TryGetColorContexts(wicFactory);

            if (contexts.Length == 1)
            {
                bestContext = contexts[0];
            }

            // try to get the best color context
            else if (contexts.Length > 1)
            {
                // https://stackoverflow.com/a/70215280/403671
                // get last not uncalibrated color context
                foreach (var ctx in contexts.Reverse())
                {
                    // Uncalibrated
                    if (ctx.ExifColorSpace == 0xFFFF)
                        continue;

                    bestContext = ctx;
                }

                // last resort
                bestContext ??= contexts[contexts.Length - 1];
            }

            if (bestContext == null) return new PhotoColorContext();


            // get color space
            var exitColorSpace = bestContext.ExifColorSpace;
            var colorSpace = PhotoColorSpace.Unknown;

            if (exitColorSpace == 1)
            {
                colorSpace = PhotoColorSpace.sRGB;
            }
            else if (exitColorSpace == 2)
            {
                colorSpace = PhotoColorSpace.AdobeRGB;
            }
            else if (exitColorSpace == 0xFFFF)
            {
                colorSpace = PhotoColorSpace.Uncalibrated;
            }


            // get color profile
            using var ms = new MemoryStream();
            bestContext.Profile?.CopyTo(ms);
            var profileBytes = ms.ToArray();


            // dispose native color contexts
            foreach (var ctx in contexts)
            {
                ctx.Dispose();
            }


            var colorContext = new PhotoColorContext(colorSpace, profileBytes);
            return colorContext;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return new PhotoColorContext();
    }


    /// <summary>
    /// Loads pixel format information for a bitmap.
    /// </summary>
    private IWICPixelFormatInfo2 LoadPixelInfo()
    {
        if (_bitmap == null) return new IWICPixelFormatInfo2(IntPtr.Zero);

        using var wicFactory = new IWICImagingFactory2();

        var comInfo = wicFactory.CreateComponentInfo(_bitmap.PixelFormat);
        return comInfo.As<IWICPixelFormatInfo2>();
    }

    #endregion // Private Functions


}


