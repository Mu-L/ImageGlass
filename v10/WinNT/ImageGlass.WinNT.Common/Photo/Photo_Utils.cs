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
using System;
using Vortice.Direct2D1;
using Vortice.WIC;
using WinRT;


namespace ImageGlass.WinNT.Common;


public partial class Photo : IPhoto<IWICBitmapSource>
{


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




    /// <summary>
    /// Creates a bitmap image with specified dimensions and pixel format.
    /// </summary>
    /// <param name="width">Bitmap width</param>
    /// <param name="height">Bitmap height</param>
    /// <param name="pixelFormat">By default, use <c><see cref="Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA"/></c></param>
    public static Photo? Create(double width, double height, Guid? pixelFormat = null)
    {
        pixelFormat ??= Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA;

        try
        {
            using var wicFactory = new IWICImagingFactory2();
            var bmp = wicFactory.CreateBitmap((uint)width, (uint)height,
                pixelFormat.Value, BitmapCreateCacheOption.CacheOnLoad);

            return new Photo(bmp);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }


}


