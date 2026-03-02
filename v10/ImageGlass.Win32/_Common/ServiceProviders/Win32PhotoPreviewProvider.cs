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
        var size = (int)(minHeight ?? double.MinValue);

        // 1. fast path: try Shell cache only (instant, no decoding)
        var imgPreview = await Task.Run(() => Win32ShellThumbnailApi.GetThumbnail(meta.FilePath, size, size, true))
            .ConfigureAwait(false);
        if (imgPreview is not null) return imgPreview;


        // 2. fast path: native scaled decode via SkiaSharp
        imgPreview = await Task.Run(() => SkiaCodec.LoadThumbnail(meta.FilePath, size), token)
            .ConfigureAwait(false);
        if (imgPreview is not null) return imgPreview;


        // 3. try getting thumbnail from Shell
        imgPreview = await Task.Run(() => Win32ShellThumbnailApi.GetThumbnail(meta.FilePath, size, size, false))
            .ConfigureAwait(false);
        if (imgPreview is not null) return imgPreview;


        // 4. try embedded EXIF preview
        using var thumbM = meta.GetEmbeddedPreview();
        if (thumbM is not null && thumbM.Height >= minHeight)
        {
            imgPreview = SkiaCodec.FromMagick(thumbM, meta.SkiaColorSpace);
            if (imgPreview is not null) return imgPreview;
        }


        return null;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<SKImage?> GetThumbnailAsync(PhotoMetadata meta, double minHeight, CancellationToken token = default)
    {
        var size = (int)minHeight;

        // 1. fast path: try to get the quick preview
        var imgPreview = await GetPreviewAsync(meta, size, token);
        if (imgPreview is not null) return imgPreview;


        // 2. slow path: use ImageMagick for unsupported formats, skip for those larger than 3000px
        using var imgM = await MagickCodec.QuickDecodeAsync(meta.FilePath, 0, 0, 0, 3000, token);
        imgPreview = SkiaCodec.FromMagick(imgM, meta.SkiaColorSpace);

        return imgPreview;
    }


}
