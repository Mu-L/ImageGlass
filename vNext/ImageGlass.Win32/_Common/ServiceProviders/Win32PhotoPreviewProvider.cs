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
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.ServiceProviders;
using SkiaSharp;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Win32.Common.ServiceProviders;

public class Win32PhotoPreviewProvider : IPhotoPreviewProvider
{

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<SKImage?> GetPreviewAsync(PhotoMetadata meta, double? minHeight, CancellationToken token = default)
    {
        var previewHeight = minHeight ?? double.MinValue;

        // 1. get thumbnail from Shell first
        var imgThumbnail = await Task.Run(() => ShellThumbnailApi.GetThumbnail(meta.FilePath,
            (int)previewHeight, (int)previewHeight), token);


        // 2. try to get embedded preview
        if (imgThumbnail is null)
        {
            using var thumbM = meta.GetEmbeddedPreview();

            if (thumbM is not null && thumbM.Height >= previewHeight)
            {
                imgThumbnail = SkiaCodec.ConvertFromMagick(thumbM);
            }
        }

        return imgThumbnail;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<SKImage?> GetThumbnailAsync(PhotoMetadata meta, double minHeight, CancellationToken token = default)
    {
        // 1. try to get thumbnail from the Shell & embedded thumbnail
        var imgPreview = await GetPreviewAsync(meta, minHeight, default);
        if (imgPreview is not null) return imgPreview;


        // 2. use ImageMagick to decode the unsupported formats, skip for those larger than 3000px
        using var imgM = await MagickCodec.QuickDecodeAsync(meta.FilePath, 0, 0, 0, 3000, token);
        imgPreview = SkiaCodec.ConvertFromMagick(imgM);

        return imgPreview;
    }


}
