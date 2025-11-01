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
using ImageMagick;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct2D1;
using Vortice.WIC;
using WinRT;


namespace ImageGlass.Common.Photoing;


public static partial class PhotoWIC
{

    /// <summary>
    /// Creates a new <see cref="ID2D1Bitmap1"/> instance from <see cref="ID2D1Bitmap"/>.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static ID2D1Bitmap1? CreateD2dBitmap1(ID2D1Bitmap srcBmp, ID2D1DeviceContext dc)
    {
        if (dc.IsDisposed()) return null;

        // Get size and pixel format of the source bitmap
        var size = srcBmp.Size.ToSizeI();

        // Create a compatible ID2D1Bitmap1 with same size and pixel format
        var bmpProps = new BitmapProperties1(srcBmp.PixelFormat);
        var bmp1 = dc.CreateBitmap(size, IntPtr.Zero, 0, bmpProps);

        // Copy from source bitmap
        bmp1.CopyFromBitmap(srcBmp);

        return bmp1;
    }



    /// <summary>
    /// Creates a Direct2D render target from WIC Bitmap for drawing operation.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static ID2D1RenderTarget? CreateWicRenderTarget(IWICBitmapSource? wicBmp, RenderTargetProperties? rtProps = null)
    {
        if (wicBmp.IsDisposed()) return null;

        using var fac = D2D1.D2D1CreateFactory<ID2D1Factory8>(FactoryType.MultiThreaded);
        rtProps ??= new RenderTargetProperties(Vortice.DCommon.PixelFormat.Premultiplied);

        var rt = fac.CreateWicBitmapRenderTarget(wicBmp.As<IWICBitmap>(), rtProps.Value);

        return rt;
    }



    /// <summary>
    /// Creates a Direct2D device context from WIC Bitmap for drawing operation.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static ID2D1DeviceContext7? CreateWicDeviceContext(IWICBitmapSource? wicBmp, RenderTargetProperties? rtProps = null)
    {
        var rt = CreateWicDeviceContext(wicBmp);
        if (rt.IsDisposed()) return null;

        return rt.As<ID2D1DeviceContext7>();
    }



    /// <summary>
    /// Draws a WIC bitmap using the specified Direct2D device context action.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static void DrawWicBitmap(IWICBitmapSource? wicBmp, Action<ID2D1DeviceContext7> fn)
    {
        // create device context
        using var dc = CreateWicDeviceContext(wicBmp);
        if (dc.IsDisposed()) return;

        // start drawing
        dc.BeginDraw();
        fn(dc);
        dc.EndDraw();
    }



    /// <summary>
    /// Gets color profile of the photo.
    /// </summary>
    private static PhotoColorProfile GetWicColorProfile(IWICBitmapSource? wicBmp)
    {
        if (wicBmp is null) return new();

        using var wicFactory = new IWICImagingFactory2();
        var frame = wicBmp.As<IWICBitmapFrameDecode>();

        try
        {
            var contexts = frame.TryGetColorContexts(wicFactory) ?? [];
            var bestProfile = FindBestWicColorProfile(contexts);
            byte[]? profileBytes = null;


            // get color profile
            if (bestProfile?.Profile is not null)
            {
                using var ms = new MemoryStream();
                bestProfile.Profile.CopyTo(ms);
                profileBytes = ms.ToArray();
            }


            // get color space
            var exitColorSpace = bestProfile?.ExifColorSpace;
            var colorSpace = PhotoColorSpace.None;

            if (exitColorSpace == 1)
            {
                colorSpace = PhotoColorSpace.sRGB;
            }
            else if (exitColorSpace == 2
                || (profileBytes != null && Encoding.ASCII.GetString(profileBytes).Contains("Adobe RGB")))
            {
                colorSpace = PhotoColorSpace.AdobeRGB;
            }
            else if (exitColorSpace == 0xFFFF)
            {
                colorSpace = PhotoColorSpace.Uncalibrated;
            }
            else if (exitColorSpace != null)
            {
                colorSpace = PhotoColorSpace.Unknown;
            }


            var colorContext = new PhotoColorProfile(colorSpace, profileBytes, bestProfile);


            // dispose native color contexts
            foreach (var ctx in contexts)
            {
                if (ctx.NativePointer != bestProfile?.NativePointer)
                {
                    ctx.Dispose();
                }
            }

            return colorContext;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌❌❌ {nameof(GetWicColorProfile)}: {ex.Message}");
        }

        return new PhotoColorProfile();
    }



    /// <summary>
    /// Finds the best color profile.
    /// </summary>
    private static IWICColorContext? FindBestWicColorProfile(IWICColorContext[]? contexts)
    {
        IWICColorContext? bestProfile = null;
        if (contexts == null) return bestProfile;

        // get the last non-uncalibrated color context
        // https://stackoverflow.com/a/70215280/403671
        for (var i = contexts.Length - 1; i >= 0; i--)
        {
            var ctx = contexts[i];

            // Uncalibrated
            if (ctx.ExifColorSpace == 0xFFFF)
                continue;

            if (ctx.Type == Vortice.WIC.ColorContextType.Profile)
            {
                bestProfile = ctx;
            }
        }


        return bestProfile;
    }



    /// <summary>
    /// Loads pixel format information for a bitmap.
    /// </summary>
    private static IWICPixelFormatInfo2 LoadWicPixelInfo(IWICBitmapSource? wicBmp)
    {
        if (wicBmp is null) return new IWICPixelFormatInfo2(IntPtr.Zero);

        using var wicFactory = new IWICImagingFactory2();

        var comInfo = wicFactory.CreateComponentInfo(wicBmp.PixelFormat);
        return comInfo.As<IWICPixelFormatInfo2>();
    }






    /// <summary>
    /// Creates a WIC bitmap image with specified dimensions and pixel format.
    /// </summary>
    /// <param name="width">Bitmap width</param>
    /// <param name="height">Bitmap height</param>
    /// <param name="pixelFormat">By default, use <c><see cref="Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA"/></c></param>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static IWICBitmapSource? CreateWicBitmapSource(double width, double height, Guid? pixelFormat = null)
    {
        pixelFormat ??= Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA;

        using var wicFactory = new IWICImagingFactory2();
        var wicBmp = wicFactory.CreateBitmap((uint)width, (uint)height,
            pixelFormat.Value, BitmapCreateCacheOption.CacheOnLoad);

        return wicBmp;
    }



    /// <summary>
    /// Converts <see cref="MagickImage"/>
    /// to <see cref="IWICBitmapSource"/> object.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static IWICBitmapSource? ConvertFromMagick(MagickImage? imgM)
    {
        if (imgM is null) return null;

        // get raw pixel data
        using var pixels = imgM.GetPixelsUnsafe();
        var buffer = pixels?.ToByteArray(PixelMapping.BGRA);
        if (buffer is null) return null;

        // create empty WIC bitmap
        using var fac = new IWICImagingFactory2();
        var wicBitmap = fac.CreateBitmap(imgM.Width, imgM.Height, Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppBGRA);

        // copy Magick's raw pixels directly into WIC buffer
        using (var bmpLock = wicBitmap.Lock(BitmapLockFlags.Write))
        {
            Marshal.Copy(buffer, 0, bmpLock.Data.DataPointer, buffer.Length);
            Array.Clear(buffer);
        }

        return wicBitmap;
    }



    /// <summary>
    /// Converts GDI Bitmap to <see cref="IWICBitmapSource"/> object.
    /// </summary>
    public static IWICBitmapSource? ConvertFromGdiBitmap(System.Drawing.Bitmap? gdiBmp)
    {
        if (gdiBmp is null) return null;

        var hBitmap = gdiBmp.GetHbitmap();
        using var wicFactory = new IWICImagingFactory2();

        var wicBmp = wicFactory.CreateBitmapFromHBITMAP(hBitmap, IntPtr.Zero, BitmapAlphaChannelOption.UseAlpha);

        return wicBmp;
    }


    /// <summary>
    /// Convert WPF bitmap source to IWICBitmapSource object.
    /// </summary>
    public static IWICBitmapSource? ConvertFromWpfBitmap(object? bmp)
    {
        if (bmp == null) return null;

        var prop = bmp.GetType().GetProperty("WicSourceHandle",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var srcHandle = (SafeHandleZeroOrMinusOneIsInvalid?)prop?.GetValue(bmp);
        if (srcHandle == null) return null;

        var handle = srcHandle.DangerousGetHandle();
        var wicSrc = new IWICBitmapSource(handle);
        wicSrc?.To32bppPBGRA();

        return wicSrc;
    }


    /// <summary>
    /// Creates WIC Decoder from file path.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static IWICBitmapDecoder? CreateDecoder(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // copy to memory stream to release file access
        var ms = new MemoryStream();
        fs.CopyTo(ms);
        ms.Position = 0;

        var decoder = CreateDecoder(ms);

        return decoder;
    }


    /// <summary>
    /// Creates WIC Decoder from byte array.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static IWICBitmapDecoder? CreateDecoder(byte[] bytes)
    {
        var ms = new MemoryStream(bytes) { Position = 0 };

        return CreateDecoder(ms);
    }


    /// <summary>
    /// Creates WIC Decoder from stream.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static IWICBitmapDecoder? CreateDecoder(Stream stream)
    {
        using var wicFactory = new IWICImagingFactory2();
        var decoder = wicFactory.CreateDecoderFromStream(stream);

        return decoder;
    }

}


