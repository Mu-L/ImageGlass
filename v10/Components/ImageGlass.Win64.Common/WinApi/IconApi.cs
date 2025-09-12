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
using ImageGlass.Win64.Common.Photoing;
using System;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
namespace ImageGlass.Win64.Common;

public static class IconApi
{
    private const uint WM_SETICON = 0x80u;
    private const int ICON_BIG = 1;


    /// <summary>
    /// Sets taskbar icon.
    /// </summary>
    public static void SetTaskbarIcon(IntPtr windowHandle, IntPtr hIcon)
    {
        _ = PInvoke.SendMessage(new HWND(windowHandle), WM_SETICON, ICON_BIG, hIcon);
    }


    /// <summary>
    /// Creates HICON from pixel byte array.
    /// </summary>
    public static unsafe IntPtr CreateHIcon(byte[] colorBytes, int width, int height)
    {
        var hbmColor = new HBITMAP(IntPtr.Zero);
        var hbmMask = new HBITMAP(IntPtr.Zero);
        var hIcon = IntPtr.Zero;

        try
        {
            // create color and mask bitmaps
            fixed (byte* pBytes = colorBytes)
            {
                hbmColor = PInvoke.CreateBitmap(width, height, 1, 32, pBytes); // Assuming 32-bit color
                hbmMask = PInvoke.CreateBitmap(width, height, 1, 1, pBytes);   // Monochrome mask
            }

            if (hbmColor == IntPtr.Zero || hbmMask == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var iconInfo = new ICONINFO()
            {
                fIcon = true, // This is an icon
                xHotspot = 0,
                yHotspot = 0,
                hbmColor = hbmColor,
                hbmMask = hbmMask,
            };

            // create icon
            var iconObj = PInvoke.CreateIconIndirect(in iconInfo);

            // get icon handle
            hIcon = iconObj.DangerousGetHandle();

            return hIcon;
        }
        finally
        {
            // clean up GDI objects
            if (hbmColor != IntPtr.Zero) PInvoke.DeleteObject(hbmColor);
            if (hbmMask != IntPtr.Zero) PInvoke.DeleteObject(hbmMask);
        }
    }


    /// <summary>
    /// Destroys HICON.
    /// </summary>
    public static void DestroyHIcon(nint hIcon)
    {
        if (hIcon != IntPtr.Zero)
        {
            _ = PInvoke.DeleteObject(new HGDIOBJ(hIcon));
            hIcon = IntPtr.Zero;
        }
    }


    /// <summary>
    /// Gets system icon.
    /// </summary>
    public static async Task<SoftwareBitmap?> GetSystemIconAsync(StockIconId? iconType, int size = 256)
    {
        if (iconType == null) return null;

        try
        {
            using var icon = SystemIcons.GetStockIcon(iconType.Value, size);
            using var gdiBmp = icon.ToBitmap();

            using var wicBmp = PhotoWIC.ConvertFromGdiBitmap(gdiBmp);
            var sb = await PhotoWIC.ConvertToSoftwareBitmapAsync(wicBmp);

            return sb;
        }
        catch { }

        return null;
    }

}

