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

using System;
using System.Threading;
using System.Threading.Tasks;
using Vortice.WIC;

namespace ImageGlass.Common.Photoing;


public static class PhotoMetadata_Exts
{
    /// <summary>
    /// Retrieves an embedded thumbnail from either a RAW format or an EXIF profile if exists.
    /// Tries to use WinRT to get the thumbnail if it not exist or size is too small.
    /// </summary>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    public static async Task<IWICBitmapSource?> GetPreviewAsync(
        this PhotoMetadata meta, double? minHeight, CancellationToken token,
        ShellThumbnailOptions thumbnailOptions = ShellThumbnailOptions.ThumbnailOnly | ShellThumbnailOptions.BiggerSizeOk)
    {
        var previewHeight = minHeight ?? double.MinValue;

        // 1. get thumbnail from Shell first
        var wicThumb = await Task.Run(() => ShellThumbnailApi.GetThumbnail(meta.FilePath,
            (int)previewHeight, (int)previewHeight, thumbnailOptions), token);


        // 2. try to get embedded preview
        if (wicThumb is null)
        {
            using var thumbM = meta.GetEmbeddedPreview();

            if (thumbM is not null && thumbM.Height >= previewHeight)
            {
                wicThumb = PhotoWIC.ConvertFromMagick(thumbM);
            }
        }

        return wicThumb;
    }


    /// <summary>
    /// Gets thumbnail photo.
    /// </summary>
    public static async Task<IWICBitmapSource?> GetThumbnailAsync(
        this PhotoMetadata meta, double minHeight, CancellationToken token = default)
    {
        // 1. try to get thumbnail from the Shell & embedded thumbnail
        var wicBmp = await meta.GetPreviewAsync(minHeight, default, ShellThumbnailOptions.ThumbnailOnly);
        if (wicBmp is not null) return wicBmp;


        // 2. use ImageMagick to decode the unsupported formats, skip for those larger than 3000px
        using var imgM = await MagickDecoder.QuickDecodeAsync(meta.FilePath, 0, 0, 0, 3000, token);
        wicBmp = PhotoWIC.ConvertFromMagick(imgM);

        return wicBmp;
    }

}

