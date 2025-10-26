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
using Microsoft.UI;
using System;
using System.Runtime.InteropServices;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.WIC;
using Windows.UI;

namespace ImageGlass.Common;


public ref struct Direct2DBitmapData
{
    public Span<byte> Pixels = [];
    public Vortice.Mathematics.SizeI Size = new();
    public uint Stripe = 0;

    public Direct2DBitmapData() { }
}


public static class ID2D1Bitmap_Exts
{

    /// <summary>
    /// Converts <see cref="ID2D1Bitmap1"/> to <see cref="IWICBitmapSource"/>
    /// </summary>
    public static IWICBitmapSource? ToWICBitmapSource(this ID2D1Bitmap1? srcBmp1, ID2D1DeviceContext6? dc)
    {
        var data = srcBmp1.GetBitmapData(dc);
        if (data.Pixels.Length == 0) return null;

        using var wicFactory = new IWICImagingFactory2();
        var bmp = wicFactory.CreateBitmapFromMemory((uint)data.Size.Width, (uint)data.Size.Height,
            Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA,
            data.Pixels, data.Stripe);

        return bmp;
    }


    /// <summary>
    /// Create bitmap for CPU Read only.
    /// </summary>
    public static ID2D1Bitmap1? CreateCpuReadBitmap(this ID2D1Bitmap1? srcBmp1, ID2D1DeviceContext6? dc)
    {
        if (srcBmp1.IsDisposed() || dc.IsDisposed()) return null;

        var bmpProps = new BitmapProperties1()
        {
            BitmapOptions = BitmapOptions.CannotDraw | BitmapOptions.CpuRead,
            PixelFormat = new Vortice.DCommon.PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
            ColorContext = srcBmp1.ColorContext,
        };


        // create CPU-read bitmap
        var cpuBmp = dc.CreateBitmap(srcBmp1.Size.ToSizeI(), bmpProps);
        cpuBmp.CopyFromBitmap(srcBmp1);

        return cpuBmp;
    }


    /// <summary>
    /// Gets pixel data of <see cref="ID2D1Bitmap1"/> bitmap.
    /// </summary>
    public static Direct2DBitmapData GetBitmapData(this ID2D1Bitmap1? srcBmp1, ID2D1DeviceContext6? dc)
    {
        var bmpData = new Direct2DBitmapData();
        if (srcBmp1.IsDisposed() || dc.IsDisposed()) return bmpData;

        // create CPU-read bitmap
        bmpData.Size = srcBmp1.Size.ToSizeI();
        using var bitmapCpu = srcBmp1.CreateCpuReadBitmap(dc);
        if (bitmapCpu.IsDisposed()) return bmpData;


        // 2. copy all raw pixel data
        var map = bitmapCpu.Map(MapOptions.Read);
        bmpData.Stripe = map.Pitch;
        var totalDataSize = bmpData.Size.Height * (int)bmpData.Stripe;

        var bytes = new byte[totalDataSize];
        Marshal.Copy(map.Bits, bytes, 0, totalDataSize);
        bitmapCpu.Unmap();


        // 3. process raw pixel data
        // since pixel data is D2D1_ALPHA_MODE_PREMULTIPLIED,
        // we need to re-calculate the color values
        bmpData.Pixels = bytes.AsSpan();
        for (int i = 0; i < bmpData.Pixels.Length; i += 4)
        {
            var a = bmpData.Pixels[i + 3];
            var alphaPremultiplied = a / 255f;

            bmpData.Pixels[i + 2] = (byte)(bmpData.Pixels[i + 2] / alphaPremultiplied); // r
            bmpData.Pixels[i + 1] = (byte)(bmpData.Pixels[i + 1] / alphaPremultiplied); // g
            bmpData.Pixels[i] = (byte)(bmpData.Pixels[i] / alphaPremultiplied); // b
        }

        return bmpData;
    }


    /// <summary>
    /// Gets pixel color at the given point.
    /// </summary>
    /// <returns>
    ///   <see cref="Colors.Transparent"/> if <paramref name="srcBmp1"/>
    ///   or <paramref name="dc"/> is <c>null</c>.
    /// </returns>
    public static Color GetPixelColor(this ID2D1Bitmap1? srcBmp1, ID2D1DeviceContext6? dc, int x, int y)
    {
        if (srcBmp1.IsDisposed() || dc.IsDisposed()) return Colors.Transparent;

        // 1. create CPU-read bitmap
        using var bitmapCpu = srcBmp1.CreateCpuReadBitmap(dc);
        if (bitmapCpu.IsDisposed()) return Colors.Transparent;


        // 2. get start index of the pixel to read
        var map = bitmapCpu.Map(MapOptions.Read);
        var startIndex = (y * map.Pitch) + (x * 4);

        // 3. get the pixel data
        var pixel = new byte[4];
        Marshal.Copy((nint)(map.Bits + startIndex), pixel, 0, pixel.Length);
        bitmapCpu.Unmap();


        // 4. process raw pixel data
        // since pixel data is D2D1_ALPHA_MODE_PREMULTIPLIED,
        // we need to re-calculate the color values
        var a = pixel[3];
        var alphaPremultiplied = a / 255f;

        var r = (byte)(pixel[2] / alphaPremultiplied);
        var g = (byte)(pixel[1] / alphaPremultiplied);
        var b = (byte)(pixel[0] / alphaPremultiplied);

        var color = Color.FromArgb(a, r, g, b);

        return color;
    }


}
