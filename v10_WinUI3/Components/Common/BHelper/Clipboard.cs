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
using Clowd.Clipboard;
using ImageGlass.Common.Photoing;
using System.Threading.Tasks;
using Vortice.WIC;
using Windows.ApplicationModel.DataTransfer;

namespace ImageGlass.Common;

public partial class BHelper
{
    /// <summary>
    /// Gets the image from clipboard.
    /// </summary>
    public static async Task<IWICBitmapSource?> GetClipboardImageAsync()
    {
        try
        {
            var img = await ClipboardGdi.GetImageAsync();
            var wicBmp = PhotoWIC.ConvertFromGdiBitmap(img);

            return wicBmp;
        }
        catch { }

        return null;
    }


    /// <summary>
    /// Copies the given image to clipboard.
    /// </summary>
    public static async Task<bool> SetClipboardImageAsync(IWICBitmapSource? wicBmp)
    {
        try
        {
            var gdiBmp = wicBmp?.ToGdiBitmap();
            if (gdiBmp is null) return false;

            await ClipboardGdi.SetImageAsync(gdiBmp);

            return true;
        }
        catch { }

        return false;
    }


    /// <summary>
    /// Copies files to clipboard.
    /// </summary>
    public static async Task<bool> SetClipboardFilesAsync(string[] filePaths, bool forCutting)
    {
        try
        {
            using var cb = await ClipboardGdi.OpenAsync();

            if (forCutting)
            {
                cb.SetFormat(ClipboardFormat.DropEffect, (byte)DataPackageOperation.Move);
            }

            cb.SetFileDropList(filePaths);

            return true;
        }
        catch { }

        return false;
    }


    /// <summary>
    /// Clears clipboard.
    /// </summary>
    public static async Task ClearClipboardAsync()
    {
        try
        {
            await ClipboardGdi.EmptyAsync();
        }
        catch { }
    }


}
