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
using Avalonia;
using ImageGlass.Common.Types;
using ImageMagick;
using System;
using System.Text.RegularExpressions;

namespace ImageGlass.Common.Photoing;

public static partial class MagickCodec
{
    [GeneratedRegex(@"(^data\:(?<type>image\/[a-z\+\-]*);base64,)?(?<data>[a-zA-Z0-9\+\/\=]+)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled, "en-US")]
    private static partial Regex CreateBase64DataUriRegex__();



    /// <summary>
    /// Processes single-frame Magick image.
    /// Returns thumbnail image if requested.
    /// </summary>
    /// <param name="refImgM">Input Magick image to process</param>
    private static MagickImage? ProcessMagickImage__(MagickImage refImgM,
        PhotoReadOptions options, PhotoMetadata meta, bool requestThumbnail)
    {
        IMagickImage? thumbM = null;


        // Use embedded thumbnails if specified
        if (requestThumbnail && meta.ExifProfile != null && options.UseEmbeddedThumbnailOtherFormats)
        {
            // Fetch the embedded thumbnail
            thumbM = meta.ExifProfile.CreateThumbnail();
            if (thumbM != null
                && thumbM.Width > options.EmbeddedThumbnailMinWidth
                && thumbM.Height > options.EmbeddedThumbnailMinHeight)
            {
                if (options.CorrectRotation) thumbM.AutoOrient();

                ApplySizeSettings__(thumbM, options);
            }
            else
            {
                thumbM?.Dispose();
                thumbM = null;
            }
        }

        // Revert to source image if an embedded thumbnail with required size was not found.
        if (!requestThumbnail || thumbM == null)
        {
            // resize the image
            ApplySizeSettings__(refImgM, options);

            // for HEIC/HEIF, PreserveOrientation must be false
            // see https://github.com/d2phap/ImageGlass/issues/1928
            if (options.CorrectRotation) refImgM.AutoOrient();


            // make sure the output color space is not CMYK
            if (meta.ColorSpace == ColorSpace.CMYK && meta.ColorProfileData is not null)
            {
                var colorProfile = new ColorProfile(meta.ColorProfileData);
                refImgM.TransformColorSpace(colorProfile, ColorProfiles.SRGB);
            }
        }


        return (MagickImage?)thumbM;
    }


    /// <summary>
    /// Applies the size settings
    /// </summary>
    private static void ApplySizeSettings__(IMagickImage imgM, PhotoReadOptions options)
    {
        if (options.Width > 0 && options.Height > 0)
        {
            if (imgM.BaseWidth > options.Width || imgM.BaseHeight > options.Height)
            {
                imgM.Thumbnail(options.Width, options.Height);
            }
        }
    }


    /// <summary>
    /// Applies changes from <paramref name="transform"/>.
    /// </summary>
    private static void TransformImage__(IMagickImage imgM, ImgTransform? transform = null)
    {
        if (transform == null) return;

        // rotate
        if (transform.Rotation != 0)
        {
            imgM.Rotate(transform.Rotation);
        }

        // flip
        if (transform.Flips.HasFlag(FlipOptions.Horizontal))
        {
            imgM.Flop();
        }
        if (transform.Flips.HasFlag(FlipOptions.Vertical))
        {
            imgM.Flip();
        }

        // invert color
        if (transform.IsColorInverted)
        {
            imgM.Negate(Channels.RGB);
        }
    }


    /// <summary>
    /// Gets maximum image dimention.
    /// </summary>
    private static Size GetMaxImageRenderSize__(uint srcWidth, uint srcHeight, uint maxSize = Const.MAX_IMAGE_DIMENSION)
    {
        var widthScale = 1f;
        var heightScale = 1f;

        if (srcWidth > maxSize)
        {
            widthScale = 1f * maxSize / srcWidth;
        }

        if (srcHeight > maxSize)
        {
            heightScale = 1f * maxSize / srcHeight;
        }

        var scale = Math.Min(widthScale, heightScale);
        var newW = srcWidth * scale;
        var newH = srcHeight * scale;

        return new Size(newW, newH);
    }


    /// <summary>
    /// Gets <see cref="MagickFormat"/> from mime type.
    /// </summary>
    private static MagickFormat ConvertMimeTypeToMagickFormat__(string? mimeType)
    {
        return mimeType switch
        {
            "image/avif" => MagickFormat.Avif,
            "image/bmp" => MagickFormat.Bmp,
            "image/gif" => MagickFormat.Gif,
            "image/tiff" => MagickFormat.Tiff,
            "image/jpeg" => MagickFormat.Jpeg,
            "image/svg+xml" => MagickFormat.Rsvg,
            "image/x-icon" => MagickFormat.Ico,
            "image/x-portable-anymap" => MagickFormat.Pnm,
            "image/x-portable-bitmap" => MagickFormat.Pbm,
            "image/x-portable-graymap" => MagickFormat.Pgm,
            "image/x-portable-pixmap" => MagickFormat.Ppm,
            "image/x-xbitmap" => MagickFormat.Xbm,
            "image/x-xpixmap" => MagickFormat.Xpm,
            "image/x-cmu-raster" => MagickFormat.Ras,
            _ => MagickFormat.Png,
        };
    }


    /// <summary>
    /// Get EXIF value.
    /// </summary>
    private static T? GetExifValue__<T>(IExifProfile? profile, ExifTag<T> tag, T? defaultValue = default)
    {
        if (profile == null) return default;

        var exifValue = profile.GetValue(tag);
        if (exifValue == null) return defaultValue;

        return exifValue.Value;
    }


}
