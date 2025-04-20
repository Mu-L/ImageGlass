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
using ImageMagick;
using System;
using System.IO;
using System.Linq;
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
    /// Converts <see cref="MagickImage"/>
    /// to <see cref="IWICBitmapSource"/> object.
    /// </summary>
    public static IWICBitmapSource? ConvertFromMagick(MagickImage? imgM)
    {
        if (imgM == null) return null;

        try
        {
            using var pixels = imgM.GetPixelsUnsafe();
            if (pixels is null) return null;


            // RGBA (with alpha)
            Guid? format = null;
            var pxMap = PixelMapping.RGB;

            // Grayscale
            if (imgM.ChannelCount == 1)
            {
                format = Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat8bppGray;
            }
            // RGB (no alpha)
            else if (imgM.ChannelCount == 3)
            {
                format = Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat24bppRGB;
            }
            // RGBA
            else if (imgM.ChannelCount == 4)
            {
                format = Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppBGRA;
                pxMap = PixelMapping.BGRA;
            }


            using var fac = new IWICImagingFactory2();
            byte[]? bytes = null;

            if (format != null)
            {
                //bytes = pixels.ToByteArray(pxMap);
                bytes = pixels.ToArray();
            }
            else
            {
                format = Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat24bppRGB;
                if (imgM.HasAlpha)
                {
                    pxMap = PixelMapping.BGRA;
                    format = Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppBGRA;
                }

                bytes = pixels.ToByteArray(pxMap);
            }

            if (bytes is null) return null;
            var wicSrc = fac.CreateBitmapFromMemory<byte>(imgM.Width, imgM.Height, format.Value, bytes);

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
    public static IWICBitmapSource? ConvertFromBytes(byte[] bytes)
    {
        try
        {
            var decoder = ConvertFromBytesToDecoder(bytes);
            var wicBmp = ConvertToWic32bppPBGRA(decoder?.GetFrame(0));

            return wicBmp;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }


    /// <summary>
    /// Converts a byte array to <see cref="IWICBitmapDecoder"/> object.
    /// </summary>
    public static IWICBitmapDecoder? ConvertFromBytesToDecoder(byte[] bytes)
    {
        try
        {
            var ms = new MemoryStream(bytes) { Position = 0 };

            using var wicFactory = new IWICImagingFactory2();
            var decoder = wicFactory.CreateDecoderFromStream(ms);

            return decoder;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }




}


