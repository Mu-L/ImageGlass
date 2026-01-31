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
using ImageMagick;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.Photoing;

public static partial class SkiaCodec
{
    /// <summary>
    /// Loads photo metadata from file path.
    /// </summary>
    public static async Task<PhotoMetadata> LoadMetadataAsync(string? filePath,
        PhotoReadOptions? options = null, CancellationToken token = default)
    {
        filePath ??= string.Empty;
        var meta = new PhotoMetadata(filePath);

        // 0. get file info
        if (string.IsNullOrWhiteSpace(filePath)) return meta;

        // create Skia codec
        using var decoder = SKCodec.Create(filePath);
        if (decoder is null) return meta;


        // 1. calculate the specific frame index
        var frameIndex = options?.FrameIndex ?? 0;

        // make sure frame index is within range
        if (frameIndex >= decoder.FrameCount) frameIndex = 0;
        else if (frameIndex < 0) frameIndex = (int)decoder.FrameCount - 1;

        meta.FrameIndex = (uint)frameIndex;
        meta.FrameCount = (uint)decoder.FrameCount;
        if (!decoder.GetFrameInfo(0, out var frame)) return meta;
        if (token.IsCancellationRequested) return meta;


        // 2. read metadata of first frame only
        var readingTask = Task.Run(() =>
        {
            try
            {
                // image size
                meta.OriginalWidth = meta.Width = (uint)decoder.Info.Width;
                meta.OriginalHeight = meta.Height = (uint)decoder.Info.Height;

                meta.HasAlpha = !decoder.Info.IsOpaque;
            }
            catch { }
            if (token.IsCancellationRequested) return;


            // get color profile
            try
            {
                // TODO: check null
                // get embedded color profile
                using var colorProfile = decoder.Info.ColorSpace.ToProfile();
                var bytes = new byte[colorProfile.Size];
                Marshal.Copy(colorProfile.Buffer, bytes, 0, (int)colorProfile.Size);

                // create photo profile
                var photoProfile = new PhotoColorProfile(bytes);

                // save to meta
                meta.ColorSpace = decoder.Info.ColorSpace.IsSrgb
                    ? ImageMagick.ColorSpace.sRGB
                    : ImageMagick.ColorSpace.Undefined;
                meta.ColorProfileName = photoProfile.GetIccDescription();
                meta.ColorProfileData = photoProfile.ProfileData;
            }
            catch { }
        }, token).ConfigureAwait(false);


        await readingTask;

        return meta;
    }


    /// <summary>
    /// Loads photo.
    /// </summary>
    public static async Task<SkiaDecoderOutput> LoadAsync(PhotoMetadata meta, CancellationToken token)
    {
        return await Task.Run(() => Load(meta), token).ConfigureAwait(false);
    }


    /// <summary>
    /// Loads photo.
    /// </summary>
    public static SkiaDecoderOutput Load(PhotoMetadata meta)
    {
        // 0. create Skia codec
        var codec = SKCodec.Create(meta.FilePath);
        var result = new SkiaDecoderOutput();
        result.Size = new Size(codec.Info.Width, codec.Info.Height);


        // 1. read animated formats
        if (codec.FrameCount > 0)
        {
            result.Animator = new SkiaAnimator(codec, meta);
            return result;
        }


        // 2. read single-frame formats
        var bitmap = new SKBitmap(codec.Info);
        var codecOption = new SKCodecOptions((int)meta.FrameIndex);
        if (codec.GetPixels(codec.Info, bitmap.GetPixels(), codecOption) == SKCodecResult.Success)
        {
            result.SingleFrame = bitmap;
        }

        codec.Dispose();
        codec = null;

        return result;
    }


    /// <summary>
    /// Saves photo to file using Magick codec.
    /// </summary>
    /// <returns><c>true</c> if file is saved.</returns>
    /// <exception cref="Exception"></exception>
    public static async Task SaveAsync(SKBitmap? srcBmp, string destFilePath,
        ImgTransform? transform = null, uint quality = 100, CancellationToken token = default)
    {
        if (srcBmp is null) return;

        try
        {
            // 1. apply transforms
            using var dstBmp = TransformImage(srcBmp, transform);
            if (dstBmp is null || token.IsCancellationRequested) return;


            // 2. use Magick to save
            if (MagickCodec.CanWrite(destFilePath))
            {
                // convert to pixels
                var pixels = dstBmp.GetPixelSpan();
                if (token.IsCancellationRequested) return;

                // convert to MagickImage
                using var imgM = new MagickImage();
                var settings = new MagickReadSettings()
                {
                    Format = MagickFormat.Bgra,
                    Width = (uint)dstBmp.Width,
                    Height = (uint)dstBmp.Height,
                };
                imgM.Read(pixels, settings);
                imgM.Quality = quality;

                // write image data to file
                await imgM.WriteAsync(destFilePath, token);
            }
            else
            {
                throw new FormatException("IGE: Unsupported image format.");
            }

        }
        catch (OperationCanceledException) { }
    }


    /// <summary>
    /// Checks if the photo can be pinged to get metadata.
    /// </summary>
    public static bool CanPing(string srcFilePath)
    {
        using var codec = SKCodec.Create(srcFilePath);
        if (codec is null) return false;

        return true;
    }


    /// <summary>
    /// Checks if the photo can be decoded.
    /// </summary>
    public static bool CanRead(string srcFilePath)
    {
        using var meta = new PhotoMetadata()
        {
            FilePath = srcFilePath,
        };

        return CanRead(meta);
    }


    /// <summary>
    /// Checks if the photo can be decoded.
    /// </summary>
    public static bool CanRead(PhotoMetadata meta)
    {
        using var codec = SKCodec.Create(meta.FilePath);
        if (codec is null) return false;

        // multiple frames
        var isMultiFrames = meta.FrameCount > 1;
        if (isMultiFrames) return codec.FrameCount > 1;

        return true;
    }


    /// <summary>
    /// Returns the new transformed bitmap.
    /// </summary>
    public static SKBitmap? TransformImage(SKBitmap? bmpSrc, ImgTransform? transform)
    {
        if (bmpSrc is null || transform is null || !transform.HasChanges) return null;


        var dstBmp = new SKBitmap(bmpSrc.Info);
        using var canvas = new SKCanvas(dstBmp);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(dstBmp.Width * 0.5f, dstBmp.Height * 0.5f);


        // flip
        var isFlipX = transform.Flips.HasFlag(FlipOptions.Horizontal);
        var isFlipY = transform.Flips.HasFlag(FlipOptions.Vertical);
        if (transform.Flips.HasFlag(FlipOptions.Horizontal) || transform.Flips.HasFlag(FlipOptions.Vertical))
        {
            canvas.Scale(isFlipX ? -1 : 1, isFlipY ? -1 : 1);
        }


        // rotation
        if (transform.Rotation != 0)
        {
            canvas.RotateDegrees(transform.Rotation);
        }


        canvas.Translate(-dstBmp.Width * 0.5f, -dstBmp.Height * 0.5f);
        canvas.DrawBitmap(bmpSrc, 0, 0);


        // invert color
        if (transform.IsColorInverted)
        {
            var colorMatrix = new float[]
            {
                -1,  0,  0,  0, 255,
                 0, -1,  0,  0, 255,
                 0,  0, -1,  0, 255,
                 0,  0,  0,  1,   0
            };

            using var paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix),
            };

            canvas.DrawBitmap(bmpSrc, 0, 0, paint);
        }


        return dstBmp;
    }


    /// <summary>
    /// Converts Magick image to SKBitmap.
    /// </summary>
    public static SKBitmap? ConvertFromMagick(MagickImage? imgM)
    {
        if (imgM is null) return null;

        var info = new SKImageInfo((int)imgM.Width, (int)imgM.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bmp = new SKBitmap(info);

        // use direct pixels from magick image
        var imgMPtr = MagickCodec.GetPixelsPointer(imgM);
        bmp.SetPixels(imgMPtr);

        return bmp;
    }


    /// <summary>
    /// Converts bitmap to image with optional source color space.
    /// </summary>
    public static SKImage? ConvertToSKImage(SKBitmap? bmp, SKColorSpace? srcColorSpace = null)
    {
        if (bmp is null) return null;

        // convert color space
        if (srcColorSpace is not null)
        {
            var info = bmp.Info.WithColorSpace(srcColorSpace);
            return SKImage.FromPixels(info, bmp.GetPixels());
        }

        var pixels = bmp.GetPixels();
        var img = SKImage.FromPixels(bmp.Info, pixels);

        return img;
    }


}
