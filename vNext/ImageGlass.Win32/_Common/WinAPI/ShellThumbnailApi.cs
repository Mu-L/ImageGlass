/*
ImageGlass - A lightweight, versatile image viewer
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
using ImageGlass.Common.Photoing;
using SkiaSharp;
using System;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

namespace ImageGlass.Common;

public static class ShellThumbnailApi
{
    private static Guid IID_IShellItem2 = new Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93");


    /// <summary>
    /// Gets Shell thumbnail from file.
    /// </summary>
    public static SKImage? GetThumbnail(string filePath, int width, int height, bool useCacheOnly)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            var flags = SIIGBF.SIIGBF_THUMBNAILONLY | SIIGBF.SIIGBF_BIGGERSIZEOK;

            // 1. try to get cached thumbnail first
            if (useCacheOnly)
            {
                using var hBitmapCached = GetThumbnailHBitmap(filePath, width, height,
                    flags | SIIGBF.SIIGBF_INCACHEONLY);

                if (hBitmapCached is not null)
                {
                    return ConvertHBitmapToSKBitmap(hBitmapCached);
                }
            }

            // 2. get the uncached thumbnail
            else
            {
                using var hBitmap = GetThumbnailHBitmap(filePath, width, height, flags);

                if (hBitmap is not null)
                {
                    return ConvertHBitmapToSKBitmap(hBitmap);
                }
            }

        }
        catch { }

        return null;
    }


    /// <summary>
    /// Gets thumbnail HBitmap from file path.
    /// </summary>
    private static DeleteObjectSafeHandle? GetThumbnailHBitmap(string filePath,
        int width, int height, SIIGBF options)
    {
        // create shell item
        PInvoke.SHCreateItemFromParsingName(filePath, null, IID_IShellItem2, out var shItemObj)
            .ThrowOnFailure();

        if (shItemObj is not IShellItemImageFactory shItemImageFac) return null;

        // get thumbnail
        shItemImageFac.GetImage(new SIZE(width, height), options, out var hBitmap);

        return hBitmap;
    }


    /// <summary>
    /// Converts HBitmap to SKBitmap
    /// </summary>
    private static unsafe SKImage? ConvertHBitmapToSKBitmap(DeleteObjectSafeHandle hBitmap)
    {
        HDC hdc = default;
        HGDIOBJ oldBitmap = default;
        SKImage? imgOutput = null;

        try
        {
            hdc = PInvoke.CreateCompatibleDC(default);
            oldBitmap = PInvoke.SelectObject(hdc, (HGDIOBJ)hBitmap.DangerousGetHandle());

            BITMAP bm;
            if (PInvoke.GetObject((HGDIOBJ)hBitmap.DangerousGetHandle(), sizeof(BITMAP), &bm) == 0)
                return null;

            var info = new SKImageInfo(bm.bmWidth, bm.bmHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            var bmpOutput = new SKBitmap(info);

            var bmi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = (uint)sizeof(BITMAPINFOHEADER),
                    biWidth = bm.bmWidth,
                    biHeight = -bm.bmHeight,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0
                }
            };

            if (PInvoke.GetDIBits(hdc, (HBITMAP)hBitmap.DangerousGetHandle(),
                0, (uint)bm.bmHeight, (void*)bmpOutput.GetPixels(), &bmi, DIB_USAGE.DIB_RGB_COLORS) == 0)
            {
                bmpOutput.Dispose();
                bmpOutput = null;
                return null;
            }

            imgOutput = SkiaCodec.ToSKImage(bmpOutput);
            return imgOutput;
        }
        catch
        {
            imgOutput?.Dispose();
            return null;
        }
        finally
        {
            if (oldBitmap != default)
                PInvoke.SelectObject(hdc, oldBitmap);

            if (hdc != default)
                PInvoke.DeleteDC(hdc);
        }
    }

}

