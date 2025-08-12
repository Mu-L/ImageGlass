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

using ImageGlass.Common.Photoing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Vortice.WIC;

namespace ImageGlass.Win64.Common.Photoing;


public static class PhotoMetadata_Extensions
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
        IWICBitmapSource? wicThumb = null;

        // try to get embedded preview
        using var thumbM = meta.GetEmbeddedPreview(token);


        // cancel if requested
        token.ThrowIfCancellationRequested();
        var previewHeight = minHeight ?? double.MinValue;


        // get thumbnail using Magick decoder
        if (thumbM is not null && thumbM.Height >= previewHeight)
        {
            wicThumb = PhotoWIC.ConvertFromMagick(thumbM);
        }
        // get the thumbnail using WinRT
        // if no embedded thumbnail found or the size is too small
        else
        {
            wicThumb = await Task.Run(() => ShellThumbnailApi.GetThumbnail(meta.FilePath,
                (int)previewHeight, (int)previewHeight, thumbnailOptions), token);
        }


        return wicThumb;
    }

}

