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
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
    public static SKBitmap? GetThumbnail(string filePath, int width, int height)
    {
        if (!File.Exists(filePath)) return null;

        SKBitmap? thumbnail = null;
        HGDIOBJ? bmpObj = null;

        try
        {
            // get thumbnail HBitmap
            using var hBitmap = GetThumbnailHBitmap(filePath, width, height,
                SIIGBF.SIIGBF_THUMBNAILONLY | SIIGBF.SIIGBF_BIGGERSIZEOK);
            if (hBitmap is null) return null;

            // convert to SKBitmap
            thumbnail = ConvertHBitmapToSKBitmap(hBitmap);
        }
        catch (Exception ex)
        {
            if (ex is not COMException)
            {
                Debug.WriteLine($"⚠️⚠️⚠️ {nameof(GetThumbnail)}: {ex.Message}");
            }
        }
        finally
        {
            // delete HBitmap to avoid memory leaks
            if (bmpObj is not null)
            {
                PInvoke.DeleteObject(bmpObj.Value);
            }
        }

        return thumbnail;
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
    private static unsafe SKBitmap? ConvertHBitmapToSKBitmap(DeleteObjectSafeHandle hBitmap)
    {
        HDC hdc = default;
        HGDIOBJ oldBitmap = default;
        SKBitmap? bitmap = null;

        try
        {
            hdc = PInvoke.CreateCompatibleDC(default);
            oldBitmap = PInvoke.SelectObject(hdc, (HGDIOBJ)hBitmap.DangerousGetHandle());

            BITMAP bm;
            if (PInvoke.GetObject((HGDIOBJ)hBitmap.DangerousGetHandle(), sizeof(BITMAP), &bm) == 0)
                return null;

            var info = new SKImageInfo(bm.bmWidth, bm.bmHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            bitmap = new SKBitmap(info);

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
                0, (uint)bm.bmHeight, (void*)bitmap.GetPixels(), &bmi, DIB_USAGE.DIB_RGB_COLORS) == 0)
            {
                bitmap.Dispose();
                bitmap = null;
                return null;
            }

            var result = bitmap;
            bitmap = null; // prevent disposal in finally
            return result;
        }
        catch
        {
            bitmap?.Dispose();
            return null;
        }
        finally
        {
            bitmap?.Dispose(); // cleanup if still referenced
            if (oldBitmap != default)
                PInvoke.SelectObject(hdc, oldBitmap);
            if (hdc != default)
                PInvoke.DeleteDC(hdc);
        }
    }

}

