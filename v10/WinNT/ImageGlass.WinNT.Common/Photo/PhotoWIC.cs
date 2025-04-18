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
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Vortice.Direct2D1;
using Vortice.WIC;
using WinRT;


namespace ImageGlass.WinNT.Common;


public static partial class PhotoWIC
{

    /// <summary>
    /// Converts the given WIC bitmap to a 32bpp PBGRA format.
    /// </summary>
    public static IWICBitmapSource? ConvertToWic32bppPBGRA(IWICBitmapSource? wicBmp)
    {
        if (wicBmp is null) return null;

        try
        {
            var newBmp = WIC.WICConvertBitmapSource(
                Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA,
                wicBmp);

            return newBmp;
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
    public static ID2D1RenderTarget? CreateD2dRenderTarget(IWICBitmapSource? wicBmp)
    {
        if (wicBmp is null) return null;
        ID2D1RenderTarget? target = null;

        try
        {
            using var factory = D2D1.D2D1CreateFactory<ID2D1Factory8>(FactoryType.MultiThreaded);

            target = factory.CreateWicBitmapRenderTarget(wicBmp.As<IWICBitmap>(),
                new(Vortice.DCommon.PixelFormat.Premultiplied));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return target;
    }


    /// <summary>
    /// Creates a Direct2D bitmap from the given WIC bitmap.
    /// </summary>
    public static ID2D1Bitmap1? CreateD2dBitmap(IWICBitmapSource? wicBmp, ID2D1DeviceContext dc, BitmapProperties1? bmpProps = null)
    {
        try
        {
            using var newBmp = ConvertToWic32bppPBGRA(wicBmp);
            if (newBmp == null) return null;

            return dc.CreateBitmapFromWicBitmap(newBmp, bmpProps);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
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
            Log.Error(ex);
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
        foreach (var ctx in contexts.Reverse())
        {
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
    public static IWICBitmapSource? CreateWicBitmapSource(double width, double height, Guid? pixelFormat = null)
    {
        pixelFormat ??= Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA;

        try
        {
            using var wicFactory = new IWICImagingFactory2();
            var wicBmp = wicFactory.CreateBitmap((uint)width, (uint)height,
                pixelFormat.Value, BitmapCreateCacheOption.CacheOnLoad);

            return wicBmp;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }



    /// <summary>
    /// Converts <see cref="System.Windows.Media.Imaging.BitmapSource"/>
    /// to <see cref="IWICBitmapSource"/> object.
    /// </summary>
    public static IWICBitmapSource? ToWicBitmapSource(System.Windows.Media.Imaging.BitmapSource? bmp, bool hasAlpha = true)
    {
        if (bmp == null) return null;

        try
        {
            var prop = bmp.GetType().GetProperty("WicSourceHandle",
            BindingFlags.NonPublic | BindingFlags.Instance);

            var srcHandle = (SafeHandleZeroOrMinusOneIsInvalid?)prop?.GetValue(bmp);
            if (srcHandle == null) return null;

            // TODO: Memory leak!!
            var bmpHandle = srcHandle.DangerousGetHandle();
            var wicSrc = new IWICBitmapSource(bmpHandle);

            return wicSrc;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }



    /// <summary>
    /// Converts a byte array to <see cref="IWICBitmapSource"/> object.
    /// </summary>
    public static IWICBitmapSource? ToWicBitmapSource(byte[] bytes)
    {
        try
        {
            var ms = new MemoryStream(bytes) { Position = 0 };

            using var wicFactory = new IWICImagingFactory2();
            var decoder = wicFactory.CreateDecoderFromStream(ms);

            var wicBmp = ConvertToWic32bppPBGRA(decoder.GetFrame(0));

            return wicBmp;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }




}


