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
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.WIC;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using WinRT;


namespace ImageGlass.Common.Photoing;


public static partial class PhotoWIC
{

    /// <summary>
    /// Converts the given WIC bitmap to a 32bpp PBGRA format.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static IWICBitmapSource? ConvertToWic32bppPBGRA(IWICBitmapSource? wicBmp)
    {
        if (wicBmp.IsDisposed()) return null;

        return WIC.WICConvertBitmapSource(
            Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA,
            wicBmp);
    }


    /// <summary>
    /// Converts the given WIC bitmap to Software Bitmap.
    /// </summary>
    public static async Task<SoftwareBitmap?> ConvertToSoftwareBitmapAsync(IWICBitmapSource? wicBmp, BitmapTransform? transform = null)
    {
        var newBmp = ConvertToWic32bppPBGRA(wicBmp);
        if (newBmp is null) return null;

        using var ms = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        using var stream = ms.AsStream();

        // convert to stream
        SaveAs(wicBmp, stream);

        // create SoftwareBitmap from stream
        var bmpDecoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ms)
            .AsTask().ConfigureAwait(false);

        var softwareBmp = await bmpDecoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform ?? new(),
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.ColorManageToSRgb)
            .AsTask().ConfigureAwait(false);

        return softwareBmp;
    }


    /// <summary>
    /// Creates a Direct2D bitmap from the given WIC bitmap.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static ID2D1Bitmap1? CreateD2dBitmap(IWICBitmapSource? wicBmp, ID2D1DeviceContext dc)
    {
        if (dc.IsDisposed()) return null;


        using var newBmp = ConvertToWic32bppPBGRA(wicBmp);
        if (newBmp == null) return null;

        var bmpProps = new BitmapProperties1(new Vortice.DCommon.PixelFormat()
        {
            Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
            AlphaMode = Vortice.DCommon.AlphaMode.Premultiplied,
        });

        return dc.CreateBitmapFromWicBitmap(newBmp, bmpProps);
    }


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


    /// <summary>
    /// Saves the input bitmap to a file in the given format.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static void SaveAs(IWICBitmapSource? srcBmp, string destFilePath, Size? size = null,
        ContainerFormat format = ContainerFormat.Png)
    {
        if (srcBmp.IsDisposed()) return;

        using var fs = new FileStream(destFilePath,
            FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

        SaveAs(srcBmp, fs, size, format);
    }


    /// <summary>
    /// Saves the input bitmap to a file in the given format.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static void SaveAs(IWICBitmapSource? srcBmp, Stream destStream, Size? size = null,
        ContainerFormat format = ContainerFormat.Png)
    {
        if (srcBmp.IsDisposed()) return;


        using var fac = new IWICImagingFactory2();
        using var stream = fac.CreateStream(destStream);
        using var encoder = fac.CreateEncoder(format);
        encoder.Initialize(stream, BitmapEncoderCacheOption.NoCache);

        size ??= new Size(srcBmp.Size.Width, srcBmp.Size.Height);

        // writing a frame
        using (var frameEncode = encoder.CreateNewFrame(out _))
        {
            frameEncode.Initialize();

            frameEncode.SetSize((uint)size.Value.Width, (uint)size.Value.Height);
            frameEncode.SetPixelFormat(Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA);

            frameEncode.WriteSource(srcBmp);
            frameEncode.Commit();
        }

        encoder.Commit();
        destStream.Position = 0;
    }


}


