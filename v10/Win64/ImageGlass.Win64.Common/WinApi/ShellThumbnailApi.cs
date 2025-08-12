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
using System.IO;
using System.Runtime.InteropServices;
using Vortice.WIC;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

namespace ImageGlass.Win64.Common;

public static class ShellThumbnailApi
{

    private static Guid IID_IShellItem2 = new Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93");


    /// <summary>
    /// Gets Shell thumbnail from file.
    /// </summary>
    public static IWICBitmapSource? GetThumbnail(string filePath,
        int width, int height, ShellThumbnailOptions options)
    {
        if (!File.Exists(filePath)) return null;

        IWICBitmapSource? thumbnail = null;
        HGDIOBJ? bmpObj = null;

        try
        {
            // get thumbnail HBitmap
            using var hBitmap = GetThumbnailHBitmap(filePath, width, height, options);
            if (hBitmap is null) return null;

            // get pointer
            var handle = hBitmap.DangerousGetHandle();
            bmpObj = new HGDIOBJ(handle);

            // create IWICBitmap from HBitmap
            using var fac = new IWICImagingFactory2();
            var bitmap = fac.CreateBitmapFromHBITMAP(handle, IntPtr.Zero, BitmapAlphaChannelOption.UseAlpha);

            // clone the bitmap and dispose the original handle
            thumbnail = fac.CreateBitmapFromSource(bitmap, BitmapCreateCacheOption.CacheOnDemand);
            bitmap?.Dispose();
        }
        catch (Exception ex)
        {
            if (ex is not COMException)
            {
                Log.Warn(ex.Message, nameof(GetThumbnail), nameof(ShellThumbnailApi));
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
        int width, int height, ShellThumbnailOptions options)
    {
        // create shell item
        PInvoke.SHCreateItemFromParsingName(filePath, null, IID_IShellItem2, out var shItemObj)
            .ThrowOnFailure();

        if (shItemObj is not IShellItemImageFactory shItemImageFac) return null;

        // get thumbnail
        shItemImageFac.GetImage(new SIZE(width, height), (SIIGBF)options, out var hBitmap);
        Marshal.ReleaseComObject(shItemImageFac);

        return hBitmap;
    }

}




/// <summary>
/// <c>SIIGBF</c> Flags:
/// <see href="https://learn.microsoft.com/en-gb/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishellitemimagefactory-getimage" />
/// </summary>
[Flags]
public enum ShellThumbnailOptions
{
    /// <summary>
    /// <c>SIIGBF_RESIZETOFIT</c>:
    /// Shrink the bitmap as necessary to fit, preserving its aspect ratio.
    /// Returns thumbnail if available, else icon.
    /// </summary>
    None = 0x00,


    /// <summary>
    /// <c>SIIGBF_BIGGERSIZEOK</c>:
    /// Passed by callers if they want to stretch the returned image themselves.
    /// For example, if the caller passes an icon size of 80x80, a 96x96 thumbnail could be returned.
    /// </summary>
    /// <remarks>
    /// This action can be used as a performance optimization if the caller expects that they will need to stretch the image. Note that the Shell implementation of IShellItemImageFactory performs a GDI stretch blit. If the caller wants a higher quality image stretch than provided through that mechanism, they should pass this flag and perform the stretch themselves.
    /// </remarks>
    BiggerSizeOk = 0x01,


    /// <summary>
    /// <c>SIIGBF_MEMORYONLY</c>:
    /// Return the item only if it is already in memory.
    /// Do not access the disk even if the item is cached.
    /// </summary>
    /// <remarks>
    /// Note that this only returns an already-cached icon and can fall back to a per-class icon if an item has a per-instance icon that has not been cached. Retrieving a thumbnail, even if it is cached, always requires the disk to be accessed, so <see cref="ShellThumbnailApi.GetThumbnail"/> should not be called from the UI thread without passing <c>SIIGBF_MEMORYONLY</c>.
    /// </remarks>
    InMemoryOnly = 0x02,


    /// <summary>
    /// <c>SIIGBF_ICONONLY</c>: Return only the icon, never the thumbnail.
    /// </summary>
    IconOnly = 0x04,


    /// <summary>
    /// <c>SIIGBF_THUMBNAILONLY</c>: Return only the thumbnail, never the icon.
    /// </summary>
    /// <remarks>
    /// Note that not all items have thumbnails,
    /// so <c>SIIGBF_THUMBNAILONLY</c> will cause the method to fail in these cases.
    /// </remarks>
    ThumbnailOnly = 0x08,


    /// <summary>
    /// <c>SIIGBF_INCACHEONLY</c>: Allows access to the disk, but only to retrieve a cached item.
    /// </summary>
    /// <remarks>
    /// This returns a cached thumbnail if it is available. If no cached thumbnail is available, it returns a cached per-instance icon but does not extract a thumbnail or icon.
    /// </remarks>
    InCacheOnly = 0x10,


    /// <summary>
    /// <c>SIIGBF_CROPTOSQUARE</c>:
    /// If necessary, crop the bitmap to a square.
    /// </summary>
    Win8CropToSquare = 0x20,


    /// <summary>
    /// <c>SIIGBF_WIDETHUMBNAILS</c>:
    /// Stretch and crop the bitmap to a 0.7 aspect ratio.
    /// </summary>
    Win8WideThumbnails = 0x40,


    /// <summary>
    /// <c>SIIGBF_ICONBACKGROUND</c>:
    /// If returning an icon, paint a background using the associated app's registered background color.
    /// </summary>
    Win8IconBackground = 0x80,


    /// <summary>
    /// <c>SIIGBF_SCALEUP</c>:
    /// If necessary, stretch the bitmap so that the height and width fit the given size.
    /// </summary>
    Win8ScaleUp = 0x100,
}
