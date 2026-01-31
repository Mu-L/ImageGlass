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
using ImageGlass.Common.Photoing;
using SkiaSharp;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.ServiceProviders;

public interface IPhotoPreviewProvider
{
    /// <summary>
    /// Retrieves an embedded thumbnail from either a RAW format or an EXIF profile if exists.
    /// Tries to use native platform API to get the thumbnail if it not exist or size is too small.
    /// </summary>
    Task<SKBitmap?> GetPreviewAsync(PhotoMetadata meta, double? minHeight, CancellationToken token = default);


    /// <summary>
    /// Gets thumbnail of the photo.
    /// </summary>
    Task<SKBitmap?> GetThumbnailAsync(PhotoMetadata meta, double minHeight, CancellationToken token = default);

}
