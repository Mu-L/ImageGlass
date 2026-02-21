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
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageGlass.Common.Extensions;
using ImageMagick;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        if (decoder.IsDisposed()) return meta;


        // 1. calculate the specific frame index
        var frameIndex = options?.FrameIndex ?? 0;

        // make sure frame index is within range
        if (frameIndex >= decoder.FrameCount) frameIndex = 0;
        else if (frameIndex < 0) frameIndex = (int)decoder.FrameCount - 1;

        meta.FrameIndex = (uint)frameIndex;
        meta.FrameCount = (uint)decoder.FrameCount;
        if (token.IsCancellationRequested) return meta;


        // 2. read metadata of first frame only
        var readingTask = Task.Run(() =>
        {
            try
            {
                // get animation frames
                var skiaFramesInfo = SkiaCodec.GetFramesMetadata(meta.FilePath);
                meta.CanAnimate = skiaFramesInfo != null;

                // get frame metadata
                meta.Frames = skiaFramesInfo?.Select(aniInfo => new FrameMetadata
                {
                    Animation = aniInfo,
                }).ToImmutableList() ?? [];

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
                // get embedded color profile
                using var colorProfile = decoder.Info.ColorSpace.ToProfile();
                if (colorProfile.Size > 0)
                {
                    var bytes = new byte[colorProfile.Size];
                    Marshal.Copy(colorProfile.Buffer, bytes, 0, (int)colorProfile.Size);

                    // create photo profile
                    var photoProfile = new PhotoColorProfile(bytes);

                    meta.ColorProfileName = photoProfile.GetIccDescription();
                    meta.ColorProfileData = photoProfile.ProfileData;
                }

                // save to meta
                meta.ColorSpace = decoder.Info.ColorSpace.IsSrgb
                    ? ImageMagick.ColorSpace.sRGB
                    : ImageMagick.ColorSpace.Undefined;
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
            var frames = meta.Frames.Select(f => (SKCodecFrameInfo)f.Animation!).ToArray();
            result.Animator = new SkiaAnimator(codec, frames);
            return result;
        }


        // 2. read single-frame formats
        using var bmpFrame = new SKBitmap(codec.Info);
        var codecOption = new SKCodecOptions((int)meta.FrameIndex);
        if (codec.GetPixels(codec.Info, bmpFrame.GetPixels(), codecOption) == SKCodecResult.Success)
        {
            if (TryApplyOrientation(bmpFrame, codec.EncodedOrigin, out var fixedBmp))
            {
                if (fixedBmp is not null)
                {
                    result.Size = new Size(fixedBmp.Width, fixedBmp.Height);
                    result.SingleFrame = ToSKImage(fixedBmp);
                    bmpFrame.Dispose();
                }
            }

            if (fixedBmp is null)
            {
                result.SingleFrame = ToSKImage(bmpFrame);
            }
        }

        codec.Dispose();
        codec = null;

        return result;
    }


    /// <summary>
    /// Loads a thumbnail using native scaled decoding.
    /// For JPEG, this uses libjpeg-turbo's IDCT scaling to decode at
    /// a reduced resolution (1/2, 1/4, 1/8) without processing the full image data.
    /// </summary>
    public static SKImage? LoadThumbnail(string filePath, int maxDimension)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

        try
        {
            using var codec = SKCodec.Create(filePath);
            if (codec.IsDisposed()) return null;

            var origW = codec.Info.Width;
            var origH = codec.Info.Height;
            if (origW <= 0 || origH <= 0) return null;

            // calculate the scale needed to fit within maxDimension
            var scale = Math.Min((float)maxDimension / origW, (float)maxDimension / origH);
            scale = Math.Min(scale, 1f); // don't upscale

            // get the native scaled dimensions (JPEG supports 1/2, 1/4, 1/8 natively)
            var scaledSize = codec.GetScaledDimensions(scale);

            var info = new SKImageInfo(scaledSize.Width, scaledSize.Height,
                SKColorType.Bgra8888, SKAlphaType.Premul);
            using var bmp = new SKBitmap(info);

            var result = codec.GetPixels(info, bmp.GetPixels());
            if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
            {
                return null;
            }

            if (TryApplyOrientation(bmp, codec.EncodedOrigin, out var fixedBmp))
            {
                var output = ToSKImage(fixedBmp);
                fixedBmp?.Dispose();

                return output;
            }

            return ToSKImage(bmp);
        }
        catch { return null; }
    }


    /// <summary>
    /// Saves photo to file using Magick codec.
    /// </summary>
    /// <returns><c>true</c> if file is saved.</returns>
    /// <exception cref="Exception"></exception>
    public static async Task SaveAsync(SKImage? srcImg, string destFilePath,
        ImgTransform? transform = null, uint quality = 100, CancellationToken token = default)
    {
        if (srcImg.IsDisposed()) return;

        try
        {
            // 1. apply transforms
            using var dstImg = TransformImage(srcImg, transform) ?? srcImg;
            if (dstImg.IsDisposed() || token.IsCancellationRequested) return;


            // 2. use Magick to save
            if (MagickCodec.CanWrite(destFilePath))
            {
                using var imgM = ToMagick(dstImg);
                if (imgM is null) return;

                // write image data to file
                imgM.Quality = quality;
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
        if (codec.IsDisposed()) return false;

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
        if (codec.IsDisposed()) return false;

        // multiple frames
        var isMultiFrames = meta.FrameCount > 1;
        if (isMultiFrames) return codec.FrameCount > 1;

        return true;
    }


    /// <summary>
    /// Extracts metadata for all frames.
    /// </summary>
    public static List<SKCodecFrameInfo>? GetFramesMetadata(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;


        using var data = SKData.Create(filePath);
        using var codec = SKCodec.Create(data);
        if (codec == null) return null;

        int frameCount = codec.FrameCount;
        var metadataList = new List<SKCodecFrameInfo>(frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            // GetFrameInfo provides duration and alpha info
            if (codec.GetFrameInfo(i, out var info))
            {
                metadataList.Add(info);
            }
        }

        return metadataList;
    }


    /// <summary>
    /// Returns the new transformed image by applying flip, rotation, and color inversion.
    /// </summary>
    public static SKImage? TransformImage(SKImage? imgSrc, ImgTransform? transform)
    {
        if (imgSrc.IsDisposed() || transform is null || !transform.HasChanges) return null;

        SKImage? result = null;

        // flip
        if (transform.Flips != FlipOptions.None)
        {
            var flipped = FlipImage(result ?? imgSrc, transform.Flips);
            result?.Dispose();
            result = flipped;
        }

        // rotation
        if (transform.Rotation != 0)
        {
            var rotated = RotateImage(result ?? imgSrc, transform.Rotation);
            result?.Dispose();
            result = rotated;
        }

        // invert color
        if (transform.IsColorInverted)
        {
            var inverted = InvertImageColors(result ?? imgSrc);
            result?.Dispose();
            result = inverted;
        }

        return result;
    }


    /// <summary>
    /// Returns a new image with inverted RGB colors, preserving alpha.
    /// </summary>
    public static SKImage? InvertImageColors(SKImage? imgSrc)
    {
        if (imgSrc.IsDisposed()) return null;

        float[] invertMatrix =
        [
            -1,  0,  0,  0,  1,
             0, -1,  0,  0,  1,
             0,  0, -1,  0,  1,
             0,  0,  0,  1,  0,
        ];

        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(invertMatrix),
        };

        var info = new SKImageInfo(imgSrc.Width, imgSrc.Height, imgSrc.ColorType, imgSrc.AlphaType, imgSrc.ColorSpace);
        using var surface = SKSurface.Create(info);
        if (surface.IsDisposed()) return null;

        surface.Canvas.DrawImage(imgSrc, 0, 0, paint);
        return surface.Snapshot();
    }


    /// <summary>
    /// Returns a new image with the specified color channels filtered.
    /// Single-channel views (R, G, or B only) are shown as grayscale.
    /// Alpha-only shows the alpha channel as grayscale with opaque output.
    /// </summary>
    public static SKImage? FilterImageColorChannels(SKImage? imgSrc, ColorChannels colors)
    {
        if (imgSrc.IsDisposed()) return null;

        var redOnly = colors.HasFlag(ColorChannels.R)
            && !colors.HasFlag(ColorChannels.G)
            && !colors.HasFlag(ColorChannels.B);
        var greenOnly = colors.HasFlag(ColorChannels.G)
            && !colors.HasFlag(ColorChannels.R)
            && !colors.HasFlag(ColorChannels.B);
        var blueOnly = colors.HasFlag(ColorChannels.B)
            && !colors.HasFlag(ColorChannels.G)
            && !colors.HasFlag(ColorChannels.R);
        var alphaOnly = colors == ColorChannels.A;
        var keepAlpha = colors.HasFlag(ColorChannels.A) && !alphaOnly;

        var red = !alphaOnly && colors.HasFlag(ColorChannels.R) ? 1f : 0f;
        var green = !alphaOnly && colors.HasFlag(ColorChannels.G) ? 1f : 0f;
        var blue = !alphaOnly && colors.HasFlag(ColorChannels.B) ? 1f : 0f;

        // for single-channel views, spread the channel value to all RGB outputs
        var mRed = redOnly ? 1f : 0f;
        var mGreen = greenOnly ? 1f : 0f;
        var mBlue = blueOnly ? 1f : 0f;

        // for alpha-only, map alpha to RGB for grayscale visualization
        var fromAlpha = alphaOnly ? 1f : 0f;
        var alphaScale = alphaOnly ? 0f : 1f;
        var alphaOffset = alphaOnly ? 1f : 0f;

        // when alpha channel is not selected and not alpha-only,
        // blend with black background instead of forcing opaque
        var blendWithBlack = !keepAlpha && !alphaOnly;

        #pragma warning disable format
        float[] matrix =
        [
            red,    mGreen, mBlue,  fromAlpha,      0,
            mRed,   green,  mBlue,  fromAlpha,      0,
            mRed,   mGreen, blue,   fromAlpha,      0,
            0,      0,      0,      alphaScale,     alphaOffset,
        ];
        #pragma warning restore format

        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(matrix),
        };

        var info = new SKImageInfo(imgSrc.Width, imgSrc.Height, imgSrc.ColorType, imgSrc.AlphaType, imgSrc.ColorSpace);
        using var surface = SKSurface.Create(info);
        if (surface.IsDisposed()) return null;

        // blend transparent pixels with black background
        if (blendWithBlack)
        {
            surface.Canvas.Clear(SKColors.Black);
        }

        surface.Canvas.DrawImage(imgSrc, 0, 0, paint);
        return surface.Snapshot();
    }


    /// <summary>
    /// Returns a new image rotated by the specified degree.
    /// Handles arbitrary angles by computing the bounding box and centering.
    /// </summary>
    public static SKImage? RotateImage(SKImage? imgSrc, double degree)
    {
        if (imgSrc.IsDisposed()) return null;

        // normalize to [0, 360)
        degree = ((degree % 360) + 360) % 360;
        if (degree == 0) return null;

        var w = imgSrc.Width;
        var h = imgSrc.Height;

        // compute the bounding box of the rotated image
        var rad = degree * Math.PI / 180.0;
        var cos = Math.Abs(Math.Cos(rad));
        var sin = Math.Abs(Math.Sin(rad));
        var outW = (int)Math.Ceiling(w * cos + h * sin);
        var outH = (int)Math.Ceiling(w * sin + h * cos);

        var info = new SKImageInfo(outW, outH, imgSrc.ColorType, imgSrc.AlphaType, imgSrc.ColorSpace);
        using var surface = SKSurface.Create(info);
        if (surface.IsDisposed()) return null;

        var canvas = surface.Canvas;

        // move origin to the center of the output, rotate, then draw centered
        canvas.Translate(outW / 2f, outH / 2f);
        canvas.RotateDegrees((float)degree);
        canvas.Translate(-w / 2f, -h / 2f);
        canvas.DrawImage(imgSrc, 0, 0);

        return surface.Snapshot();
    }


    /// <summary>
    /// Returns a new image flipped according to the specified options.
    /// </summary>
    public static SKImage? FlipImage(SKImage? imgSrc, FlipOptions options)
    {
        if (imgSrc.IsDisposed() || options == FlipOptions.None) return null;

        var w = imgSrc.Width;
        var h = imgSrc.Height;
        var info = new SKImageInfo(w, h, imgSrc.ColorType, imgSrc.AlphaType, imgSrc.ColorSpace);
        using var surface = SKSurface.Create(info);
        if (surface.IsDisposed()) return null;

        var canvas = surface.Canvas;

        if (options.HasFlag(FlipOptions.Horizontal))
        {
            canvas.Scale(-1, 1, w / 2f, 0);
        }
        if (options.HasFlag(FlipOptions.Vertical))
        {
            canvas.Scale(1, -1, 0, h / 2f);
        }

        canvas.DrawImage(imgSrc, 0, 0);
        return surface.Snapshot();
    }


    /// <summary>
    /// Attempts to apply the specified orientation to the source bitmap and outputs the transformed bitmap if
    /// successful.
    /// </summary>
    public static bool TryApplyOrientation(SKBitmap? bmpSrc, SKEncodedOrigin? orientation, out SKBitmap? output)
    {
        output = null;
        if (bmpSrc.IsDisposed()) return false;

        var origin = orientation ?? SKEncodedOrigin.TopLeft;
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default) return false;


        // 1. determine if the result dimensions are swapped (90°/270° rotations)
        var swapDims = origin is SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.LeftBottom;

        var w = swapDims ? bmpSrc.Height : bmpSrc.Width;
        var h = swapDims ? bmpSrc.Width : bmpSrc.Height;

        var info = bmpSrc.Info.WithSize(w, h);
        var bmpOutput = new SKBitmap(info);

        try
        {
            // 2. fix the orientation
            using (var surface = SKSurface.Create(info, bmpOutput.GetPixels(), info.RowBytes))
            {
                var canvas = surface.Canvas;
                switch (origin)
                {
                    case SKEncodedOrigin.TopRight:
                        canvas.Translate(w, 0);
                        canvas.Scale(-1, 1);
                        break;

                    case SKEncodedOrigin.BottomRight:
                        canvas.RotateDegrees(180, w * 0.5f, h * 0.5f);
                        break;

                    case SKEncodedOrigin.BottomLeft:
                        canvas.Translate(0, h);
                        canvas.Scale(1, -1);
                        break;

                    case SKEncodedOrigin.LeftTop:
                        canvas.Translate(w, 0);
                        canvas.RotateDegrees(90);
                        canvas.Scale(1, -1);
                        break;

                    case SKEncodedOrigin.RightTop:
                        canvas.Translate(w, 0);
                        canvas.RotateDegrees(90);
                        break;

                    case SKEncodedOrigin.RightBottom:
                        canvas.Translate(0, h);
                        canvas.RotateDegrees(-90);
                        canvas.Scale(1, -1);
                        break;

                    case SKEncodedOrigin.LeftBottom:
                        canvas.Translate(0, h);
                        canvas.RotateDegrees(-90);
                        break;
                }

                canvas.DrawBitmap(bmpSrc, 0, 0);
                canvas.Flush();
            }


            output = bmpOutput;
            return true;
        }
        catch
        {
            bmpOutput.Dispose();
        }

        return false;
    }


    /// <summary>
    /// Converts the Skia's <see cref="SKEncodedOrigin"/> value to Magick's <see cref="OrientationType"/> value.
    /// </summary>
    public static OrientationType ToMagickOrientation(SKEncodedOrigin orientation)
    {
        return orientation switch
        {
            SKEncodedOrigin.TopLeft => OrientationType.TopLeft,
            SKEncodedOrigin.TopRight => OrientationType.TopRight,
            SKEncodedOrigin.BottomRight => OrientationType.BottomRight,
            SKEncodedOrigin.BottomLeft => OrientationType.BottomLeft,
            SKEncodedOrigin.LeftTop => OrientationType.LeftTop,
            SKEncodedOrigin.RightTop => OrientationType.RightTop,
            SKEncodedOrigin.RightBottom => OrientationType.RightBottom,
            SKEncodedOrigin.LeftBottom => OrientationType.LeftBottom,
            _ => OrientationType.Undefined,
        };
    }


    /// <summary>
    /// Converts Magick image to SKBitmap.
    /// </summary>
    public static unsafe SKImage? FromMagick(MagickImage? imgM)
    {
        if (imgM is null) return null;

        // prepare image info
        var alphaType = imgM.HasAlpha ? SKAlphaType.Unpremul : SKAlphaType.Opaque;
        var info = new SKImageInfo((int)imgM.Width, (int)imgM.Height, SKColorType.Rgba8888, alphaType);

        // get pixels array
        using var pixels = imgM.GetPixelsUnsafe();
        var bytes = pixels.ToByteArray(PixelMapping.RGBA);
        if (bytes is null) return null;

        // create data from pinned pointer
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var data = SKData.Create(
            handle.AddrOfPinnedObject(),
            bytes.Length,
            (addr, ctx) => ((GCHandle)ctx).Free(),
            handle
        );

        return SKImage.FromPixels(info, data);
    }


    /// <summary>
    /// Converts <see cref="Bitmap"/> to <see cref="SKBitmap"/>.
    /// </summary>
    public static SKBitmap? FromBitmap(Bitmap? abmp)
    {
        if (abmp is null) return null;

        // 1. Prepare Skia Info matching Avalonia's default layout (Bgra8888)
        var info = new SKImageInfo(
            abmp.PixelSize.Width,
            abmp.PixelSize.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);

        // 2. Allocate Skia Pixel Buffer
        var skBmp = new SKBitmap(info);

        // 3. Direct Memory Copy
        // CopyPixels transfers data from Avalonia's internal buffer 
        // directly to the address of the Skia bitmap.
        try
        {
            abmp.CopyPixels(
                new PixelRect(0, 0, info.Width, info.Height),
                skBmp.GetPixels(),
                info.RowBytes * info.Height, // Buffer size
                info.RowBytes);              // Stride
        }
        catch
        {
            skBmp.Dispose();
            return null;
        }

        return skBmp;
    }


    /// <summary>
    /// Converts bitmap to image with optional source color space.
    /// </summary>
    public static SKImage? ToSKImage(SKBitmap? bmp, SKColorSpace? srcColorSpace = null)
    {
        if (bmp.IsDisposed()) return null;

        // convert color space
        if (srcColorSpace is not null)
        {
            var info = bmp.Info.WithColorSpace(srcColorSpace);
            return SKImage.FromPixels(info, bmp.GetPixels());
        }

        var img = SKImage.FromBitmap(bmp);

        return img;
    }



    /// <summary>
    /// Converts <see cref="SKBitmap"/> to <see cref="MagickImage"/>.
    /// </summary>
    public static MagickImage? ToMagick(SKBitmap? skBmp)
    {
        using var skImg = ToSKImage(skBmp);
        return ToMagick(skImg);
    }


    /// <summary>
    /// Converts <see cref="SKImage"/> to <see cref="MagickImage"/>.
    /// </summary>
    public static MagickImage? ToMagick(SKImage? skImg)
    {
        if (skImg.IsDisposed()) return null;

        // convert to pixels
        using var pixmap = skImg.PeekPixels();
        var pixSpan = pixmap.GetPixelSpan();

        // convert to MagickImage
        var imgM = new MagickImage();
        var settings = new MagickReadSettings()
        {
            Format = MagickFormat.Bgra,
            Width = (uint)skImg.Width,
            Height = (uint)skImg.Height,
        };
        imgM.Read(pixSpan, settings);

        return imgM;
    }


    /// <summary>
    /// Converts <see cref="SKImage"/> to <see cref="WriteableBitmap"/>.
    /// </summary>
    public static WriteableBitmap? ToWritableBitmap(SKImage? img)
    {
        if (img.IsDisposed()) return null;

        // PeekPixels returns null if the image is on the GPU (Texture-backed)
        using var pixmap = img.PeekPixels();
        return ToWritableBitmap(pixmap);
    }


    /// <summary>
    /// Converts <see cref="SKBitmap"/> to <see cref="WriteableBitmap"/>.
    /// </summary>
    public static WriteableBitmap? ToWritableBitmap(SKBitmap? bmp)
    {
        if (bmp.IsDisposed()) return null;

        using var pixmap = bmp.PeekPixels();
        return ToWritableBitmap(pixmap);
    }


    /// <summary>
    /// Converts <see cref="SKPixmap"/> to <see cref="WriteableBitmap"/>.
    /// </summary>
    public static unsafe WriteableBitmap? ToWritableBitmap(SKPixmap? pixmap)
    {
        if (pixmap.IsDisposed()) return null;

        var info = pixmap.Info;
        var wbmp = new WriteableBitmap(
            new PixelSize(info.Width, info.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (ILockedFramebuffer fb = wbmp.Lock())
        {
            // If the source is already Bgra8888, we can do a super-fast memcpy.
            // We also check matching AlphaType to ensure we don't copy Premul into Unpremul blindly.
            bool isFastPath = info.ColorType == SKColorType.Bgra8888 &&
                              (info.AlphaType == SKAlphaType.Premul || info.AlphaType == SKAlphaType.Opaque);

            if (isFastPath)
            {
                byte* srcPtr = (byte*)pixmap.GetPixels();
                byte* dstPtr = (byte*)fb.Address;
                long srcRowBytes = pixmap.RowBytes;
                long dstRowBytes = fb.RowBytes;
                long bytesToCopy = info.Width * 4;

                if (srcRowBytes == dstRowBytes)
                {
                    // Single block copy if strides match (most common)
                    Unsafe.CopyBlock(dstPtr, srcPtr, (uint)(srcRowBytes * info.Height));
                }
                else
                {
                    // Row-by-row copy if strides differ
                    for (int y = 0; y < info.Height; y++)
                    {
                        Unsafe.CopyBlock(dstPtr, srcPtr, (uint)bytesToCopy);
                        srcPtr += srcRowBytes;
                        dstPtr += dstRowBytes;
                    }
                }
            }
            else
            {
                // Slow path: Source is not Bgra8888 (e.g., Rgba8888, Gray8).
                // We let Skia convert the pixels into the destination buffer.
                var dstInfo = new SKImageInfo(info.Width, info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                pixmap.ReadPixels(dstInfo, fb.Address, fb.RowBytes);
            }
        }

        return wbmp;
    }





}
