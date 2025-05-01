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
using ImageGlass.Common.Photoing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Vortice.WIC;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace ImageGlass.WinNT.Common.Photoing;


public static class PhotoMetadata_Extensions
{
    /// <summary>
    /// Retrieves an embedded thumbnail from either a RAW format or an EXIF profile if exists.
    /// Tries to use WinRT to get the thumbnail if it not exist or size is too small.
    /// </summary>
    public static async Task<IWICBitmapSource?> GetPreviewAsync(this PhotoMetadata meta, double? minHeight, CancellationToken token)
    {
        // try to get photo preview
        using var thumbM = meta.GetPreview(token);
        IWICBitmapSource? wicThumb = null;

        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();
            minHeight ??= double.MinValue;

            // get the thumbnail using WinRT
            // if no embedded thumbnail found or the size is too small
            if (thumbM is null || thumbM.Height < minHeight)
            {
                var fi = await StorageFile.GetFileFromPathAsync(meta.FilePath);
                using var fiThumb = await fi.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);

                // cancel if requested
                token.ThrowIfCancellationRequested();

                var thumbBytes = await fiThumb.ReadBytesAsync();
                if (thumbBytes is null) return null;

                wicThumb = PhotoWIC.ConvertFromBytes(thumbBytes);
            }
            // get thumbnail using Magick decoder
            else
            {
                wicThumb = PhotoWIC.ConvertFromMagick(thumbM);
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Info($"{nameof(GetPreviewAsync)}: Cancelled retrieving preview");
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }


        return wicThumb;
    }

}

