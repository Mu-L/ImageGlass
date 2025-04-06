using SharpGen.Runtime;
using System;
using Vortice.Direct2D1;
using Vortice.WIC;



namespace ImageGlass.WinNT;


public static class Wic
{

    /// <summary>
    /// Loads a bitmap image and convert
    /// to <c><see cref="Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA"/></c> format.
    /// </summary>
    public static IWICBitmapSource? Load(string filePath, uint frameIndex = 0)
    {
        IWICBitmapSource? bmp = null;

        try
        {
            using var wicFactory = new IWICImagingFactory2();
            using var decoder = wicFactory.CreateDecoderFromFileName(filePath);
            using var frameBmp = decoder.GetFrame(frameIndex);

            bmp = WIC.WICConvertBitmapSource(Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA, frameBmp);
        }
        catch (SharpGenException ex)
        {
            // TODO
        }

        return bmp;
    }


    /// <summary>
    /// Creates a bitmap image with specified dimensions and pixel format.
    /// </summary>
    /// <param name="width">Bitmap width</param>
    /// <param name="height">Bitmap height</param>
    /// <param name="pixelFormat">By default, use <c><see cref="Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA"/></c></param>
    public static IWICBitmapSource? CreateBitmap(double width, double height, Guid? pixelFormat = null)
    {
        IWICBitmapSource? bmp = null;
        pixelFormat ??= Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA;

        try
        {
            using var wicFactory = new IWICImagingFactory2();

            bmp = wicFactory.CreateBitmap((uint)width, (uint)height,
                pixelFormat.Value, BitmapCreateCacheOption.CacheOnLoad);
        }
        catch (SharpGenException ex)
        {
            // TODO
        }

        return bmp;
    }


    /// <summary>
    /// Creates a render target from a bitmap source for drawing operations.
    /// </summary>
    public static ID2D1RenderTarget? CreateRenderTarget(IWICBitmapSource bmpWic)
    {
        ID2D1RenderTarget? target = null;

        try
        {
            using var factory = D2D1.D2D1CreateFactory<ID2D1Factory8>(FactoryType.MultiThreaded);

            target = factory.CreateWicBitmapRenderTarget(bmpWic.As<IWICBitmap>(),
                new(Vortice.DCommon.PixelFormat.Premultiplied));
        }
        catch (SharpGenException ex)
        {
            // TODO
        }

        return target;
    }


}



