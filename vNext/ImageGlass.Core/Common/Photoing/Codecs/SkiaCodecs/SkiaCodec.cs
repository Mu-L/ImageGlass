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
using ImageGlass.Common.Photoing;
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Core.Common.Photoing;

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


            // get WIC color profile
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


    public static SkiaDecoderOutput Load(PhotoMetadata meta)
    {
        var result = new SkiaDecoderOutput();

        // create Skia codec
        var decoder = SKCodec.Create(meta.FilePath);
        result.Size = new Size(decoder.Info.Width, decoder.Info.Height);

        // 1. read animated formats
        if (decoder.FrameCount > 0)
        {
            // TODO:
            result.Animator = decoder;

            return result;
        }


        // 2. read single-frame formats
        var bitmap = new SKBitmap(decoder.Info);
        if (decoder.GetPixels(decoder.Info, bitmap.GetPixels(), new SKCodecOptions((int)meta.FrameIndex)) == SKCodecResult.Success)
        {
            result.SingleFrame = bitmap;
        }

        decoder.Dispose();
        decoder = null;


        return result;
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


}
