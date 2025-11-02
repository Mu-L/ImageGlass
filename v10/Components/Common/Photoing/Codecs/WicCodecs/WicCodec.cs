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
using SharpGen.Runtime;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.DXGI;
using Vortice.WIC;
using Windows.Foundation;

namespace ImageGlass.Common.Photoing;

public static partial class WicCodec
{
    /// <summary>
    /// Gets all WIC encoders and decoders.
    /// </summary>
    public static ImmutableArray<WicCodecInfo> AllCodecs => _allCodecs.Value.Values;


    /// <summary>
    /// Gets the supported extensions for decoding.
    /// </summary>
    public static FrozenSet<string> DecoderExtensions => _decoderExtensions.Value;


    /// <summary>
    /// Gets the supported extensions for encoding.
    /// </summary>
    public static FrozenSet<string> EncoderExtensions => _encoderExtensions.Value;


    /// <summary>
    /// Disposes all static native resources.
    /// </summary>
    public static void DisposeResources()
    {
        if (_allCodecs.IsValueCreated)
        {
            foreach (var item in _allCodecs.Value.Values)
            {
                item.Dispose();
            }
        }

    }


    /// <summary>
    /// Loads photo.
    /// </summary>
    public static async Task<WicDecoderOutput> LoadAsync(PhotoMetadata meta, CancellationToken token)
    {
        return await Task.Run(() => Load(meta), token).ConfigureAwait(false);
    }


    /// <summary>
    /// Loads photo.
    /// </summary>
    public static WicDecoderOutput Load(PhotoMetadata meta)
    {
        var result = new WicDecoderOutput();

        var decoder = PhotoWIC.CreateDecoder(meta.FilePath);
        if (decoder.IsDisposed()) return result;


        // 1. read animated formats
        if (meta.CanAnimate)
        {
            result.Size = new Size((int)meta.Width, meta.Height);

            // .GIF
            if (meta.IsOneOfExtensions(".GIF", ".GIFV"))
            {
                result.Animator = new GifAnimator(decoder, meta);
            }
            // .WEBP
            else if (meta.IsOneOfExtensions(".WEBP"))
            {
                result.Animator = new WebpAnimator(decoder, meta);
            }
            // use default WIC animator
            else
            {
                result.Animator = new WicAnimator(decoder, meta);
            }

            return result;
        }

        // 2. read non-animated multi-frame formats
        if (meta.FrameCount > 1)
        {
            result.Size = new Size((int)meta.Width, meta.Height);
            result.MultiFrames = decoder;

            return result;
        }


        // 3. read single-frame formats
        using var frameBmp = decoder.GetFrame(meta.FrameIndex);
        decoder.Dispose();
        decoder = null;

        using var fac = new IWICImagingFactory2();
        var wicBmp = fac.CreateBitmapFromSource(frameBmp, BitmapCreateCacheOption.CacheOnLoad);

        result.Size = new Size((int)meta.Width, meta.Height);
        result.SingleFrame = wicBmp;

        return result;
    }


    /// <summary>
    /// Applies changes from <see cref="ImgTransform"/>.
    /// </summary>
    public static IWICBitmapSource? TransformImage(IWICBitmapSource? bmpSrc, ImgTransform? transform)
    {
        if (bmpSrc.IsDisposed() || transform is null) return bmpSrc;

        // list of flips
        var flips = new List<BitmapTransformOptions>();
        if (transform.Flips.HasFlag(FlipOptions.Horizontal))
        {
            flips.Add(BitmapTransformOptions.FlipHorizontal);
        }
        if (transform.Flips.HasFlag(FlipOptions.Vertical))
        {
            flips.Add(BitmapTransformOptions.FlipVertical);
        }


        // apply flips
        foreach (var flip in flips)
        {
            bmpSrc.ApplyTransform(flip);
        }


        // rotate
        var rotate = transform.Rotation switch
        {
            90 => BitmapTransformOptions.Rotate90,
            -270 => BitmapTransformOptions.Rotate90,

            -90 => BitmapTransformOptions.Rotate270,
            270 => BitmapTransformOptions.Rotate270,

            180 => BitmapTransformOptions.Rotate180,
            -180 => BitmapTransformOptions.Rotate180,

            _ => BitmapTransformOptions.Rotate0,
        };

        if (rotate != BitmapTransformOptions.Rotate0)
        {
            bmpSrc.ApplyTransform(rotate);
        }


        // invert color
        if (transform.IsColorInverted)
        {
            using var fac = new IWICImagingFactory2();
            var newBmp = fac.CreateBitmap((uint)bmpSrc.Size.Width, (uint)bmpSrc.Size.Height, PixelFormat.Format32bppPRGBA);

            using var d2Fact = D2D1.D2D1CreateFactory<ID2D1Factory>();
            using var d2Rt = d2Fact.CreateWicBitmapRenderTarget(newBmp, new RenderTargetProperties()
            {
                PixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
                Type = RenderTargetType.Default,
                MinLevel = FeatureLevel.Default,
                Usage = RenderTargetUsage.None,
            });
            using var dc = d2Rt.As<ID2D1DeviceContext>();
            using var effect = new Invert(dc);

            using (var cb = dc.CreateBitmapFromWicBitmap(bmpSrc))
            {
                effect.SetInput(0, cb, false);
                dc.BeginDraw();
                dc.DrawImage(effect);
                dc.EndDraw();
            }

            bmpSrc.Dispose();
            bmpSrc = newBmp;
        }

        return bmpSrc;
    }


    /// <summary>
    /// Gets WIC Encoder from file extension.
    /// E.g. <c>".png"</c>, <c>"C:\photo.png"</c>
    /// </summary>
    public static WicCodecInfo? GetCodecFromExtension(string extOrPath)
    {
        if (string.IsNullOrWhiteSpace(extOrPath)) return null;

        var ext = extOrPath;
        if (!extOrPath.StartsWith('.'))
        {
            ext = Path.GetExtension(extOrPath);
        }

        var codec = AllCodecs.FirstOrDefault(i => i.ComponentType == ComponentType.Encoder
            && i.Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase));

        return codec;
    }




}

