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
using ImageGlass.Common.Extensions;
using SkiaSharp;
using System;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Tone-maps HDR images to SDR sRGB for display on standard monitors.
/// Matches Chrome/BT.2408 behavior: SDR reference white (203 nits) maps to
/// full SDR white; only super-white highlights are compressed.
/// Tone mapping is applied on luminance to preserve hue and saturation.
/// </summary>
public static class HdrToneMapper
{
    /// <summary>PQ EOTF peak luminance in nits (SMPTE ST 2084).</summary>
    private const float PqPeakNits = 10_000f;

    /// <summary>SDR reference white in nits (ITU-R BT.2408).</summary>
    private const float SdrWhiteNits = 203f;

    /// <summary>Normalization multiplier: after PQ EOTF (1.0 = 10 000 nits),
    /// scale so that 203 nits -> 1.0.</summary>
    private const float PqNormScale = PqPeakNits / SdrWhiteNits; // ≈ 49.26

    // Rec.2020 luminance coefficients (ITU-R BT.2020)
    private const float LumR = 0.2627f;
    private const float LumG = 0.6780f;
    private const float LumB = 0.0593f;

    // Rec.2020 -> sRGB  3×3 gamut mapping matrix (linear)
    // M = sRGB_from_XYZ × XYZ_from_Rec2020
    private const float M00 = 1.6605f, M01 = -0.5876f, M02 = -0.0729f;
    private const float M10 = -0.1245f, M11 = 1.1329f, M12 = -0.0083f;
    private const float M20 = -0.0182f, M21 = -0.1006f, M22 = 1.1188f;



    #region Public Methods

    /// <summary>
    /// Tone-maps an HDR image to SDR sRGB for display on standard monitors.
    /// Returns <c>null</c> if the source is invalid, when <paramref name="mode"/>
    /// is <see cref="HdrToneMappingMode.None"/> (pass-through), or when the
    /// decoded image is not actually HDR-encoded.
    /// </summary>
    public static SKImage? ToneMapToSdr(SKImage? source, HdrTransferFunction transferFn,
        HdrToneMappingMode mode, SKColorSpace? destColorSpace = null)
    {
        if (source.IsDisposed()) return null;
        if (mode == HdrToneMappingMode.None) return null;

        // Gain-map images: the decoded base layer is already SDR.
        if (transferFn == HdrTransferFunction.GainMap) return null;

        SKImage? retaggedSource = null;
        try
        {
            var effectiveSource = source;
            if (!IsHdrColorSpace(source.ColorSpace))
            {
                var hdrCs = BuildHdrColorSpace(transferFn, source.ColorSpace);
                if (hdrCs is null) return null;

                retaggedSource = ReinterpretColorSpace(source, hdrCs);
                if (retaggedSource is null) return null;

                effectiveSource = retaggedSource;
            }

            // Tone-mapping operators
            // (input / output in normalized linear, 1.0 = SDR reference white = 203 nits)
            return mode switch
            {
                HdrToneMappingMode.Auto => ToneMapManual(effectiveSource, transferFn, destColorSpace, Bt2408KneeToneMap),
                HdrToneMappingMode.Reinhard => ToneMapManual(effectiveSource, transferFn, destColorSpace, ExtendedReinhardToneMap),
                HdrToneMappingMode.ACES => ToneMapManual(effectiveSource, transferFn, destColorSpace, AcesToneMap),
                _ => null,
            };
        }
        finally
        {
            retaggedSource?.Dispose();
        }
    }


    /// <summary>
    /// Returns <c>true</c> when the color space uses a PQ or HLG transfer function.
    /// </summary>
    public static bool IsHdrColorSpace(SKColorSpace? cs)
    {
        if (cs is null || cs.IsSrgb || cs.GammaIsLinear) return false;
        if (cs.GammaIsCloseToSrgb) return false;
        if (cs.GetNumericalTransferFunction(out _)) return false;
        return true;
    }

    #endregion // Public Methods



    #region Private Methods

    /// <summary>
    /// Tone mapping pipeline:
    /// 1. Linearize PQ/HLG via Skia color-space conversion to linear Rec.2020.
    /// 2. Normalize so 203 nits = 1.0 (PQ) or keep 1.0 (HLG).
    /// 3. Compute Rec.2020 luminance; apply tone curve on luminance only.
    /// 4. Scale RGB by (mapped luminance / original luminance) to preserve hue.
    /// 5. Gamut-map Rec.2020 -> sRGB via 3×3 matrix.
    /// 6. Encode to sRGB gamma.
    /// </summary>
    private static unsafe SKImage? ToneMapManual(SKImage source,
        HdrTransferFunction transferFn, SKColorSpace? destColorSpace, Func<float, float> toneCurve)
    {
        // Step 1: Decode into linear-light Rec.2020 float buffer.
        var linearRec2020 = SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Linear, SKColorSpaceXyz.Rec2020);
        var linearInfo = new SKImageInfo(source.Width, source.Height,
            SKColorType.RgbaF32, SKAlphaType.Unpremul, linearRec2020);

        using var linearSurface = SKSurface.Create(linearInfo);
        if (linearSurface is null) return null;

        linearSurface.Canvas.DrawImage(source, 0, 0);

        // Step 2: Read back linear-light pixels.
        using var linearSnapshot = linearSurface.Snapshot();
        using var pixmap = linearSnapshot.PeekPixels();
        if (pixmap is null) return null;

        var width = pixmap.Width;
        var height = pixmap.Height;
        var rowBytes = pixmap.RowBytes;

        // Allocate output buffer (linear sRGB, RgbaF32)
        var outputInfo = new SKImageInfo(width, height, SKColorType.RgbaF32, SKAlphaType.Unpremul);
        using var outputBmp = new SKBitmap(outputInfo);
        var srcPtr = (byte*)pixmap.GetPixels();
        var dstPtr = (byte*)outputBmp.GetPixels();

        // PQ: after EOTF 1.0 = 10 000 nits.  Scale so 203 nits -> 1.0.
        // HLG: after EOTF 1.0 ≈ reference white already.
        var normScale = transferFn == HdrTransferFunction.PQ ? PqNormScale : 1f;

        for (var y = 0; y < height; y++)
        {
            var srcRow = (float*)(srcPtr + (long)y * rowBytes);
            var dstRow = (float*)(dstPtr + (long)y * outputBmp.RowBytes);

            for (var x = 0; x < width; x++)
            {
                var i = x * 4;
                var r = srcRow[i] * normScale;
                var g = srcRow[i + 1] * normScale;
                var b = srcRow[i + 2] * normScale;
                var a = srcRow[i + 3];

                // ── Luminance-based tone mapping ──
                // Compute Rec.2020 luminance
                var lum = LumR * r + LumG * g + LumB * b;

                if (lum > 0f)
                {
                    var mappedLum = toneCurve(lum);
                    var scale = mappedLum / lum;

                    // Scale RGB proportionally — preserves hue and saturation
                    r *= scale;
                    g *= scale;
                    b *= scale;
                }
                else
                {
                    r = 0f;
                    g = 0f;
                    b = 0f;
                }

                // ── Gamut map: Rec.2020 linear -> sRGB linear ──
                var sr = M00 * r + M01 * g + M02 * b;
                var sg = M10 * r + M11 * g + M12 * b;
                var sb = M20 * r + M21 * g + M22 * b;

                // Clamp to [0, 1] (out-of-gamut values after matrix)
                dstRow[i] = Math.Clamp(sr, 0f, 1f);
                dstRow[i + 1] = Math.Clamp(sg, 0f, 1f);
                dstRow[i + 2] = Math.Clamp(sb, 0f, 1f);
                dstRow[i + 3] = Math.Clamp(a, 0f, 1f);
            }
        }

        // Step 3: Tag output as sRGB-linear, then convert to final sRGB gamma.
        var srgbLinear = SKColorSpace.CreateSrgbLinear();
        using var toneMappedBmpImg = SKImage.FromBitmap(outputBmp);
        if (toneMappedBmpImg is null) return null;

        using var toneMappedLinear = ReinterpretColorSpace(toneMappedBmpImg, srgbLinear);
        if (toneMappedLinear is null) return null;

        var dest = destColorSpace ?? SKColorSpace.CreateSrgb();
        var finalInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul, dest);
        using var finalSurface = SKSurface.Create(finalInfo);
        if (finalSurface is null) return null;

        finalSurface.Canvas.DrawImage(toneMappedLinear, 0, 0);
        return finalSurface.Snapshot();
    }


    /// <summary>
    /// Re-wraps an existing image with a different color space without modifying pixel data.
    /// </summary>
    private static SKImage? ReinterpretColorSpace(SKImage source, SKColorSpace newColorSpace)
    {
        using var pixmap = source.PeekPixels();
        if (pixmap is null) return null;

        var newInfo = pixmap.Info.WithColorSpace(newColorSpace);
        using var reinterpreted = new SKPixmap(newInfo, pixmap.GetPixels(), pixmap.RowBytes);

        return SKImage.FromPixelCopy(reinterpreted);
    }


    /// <summary>
    /// Builds an HDR color space from the transfer function and the source gamut.
    /// </summary>
    private static SKColorSpace? BuildHdrColorSpace(HdrTransferFunction transferFn, SKColorSpace? sourceCs)
    {
        var gamut = SKColorSpaceXyz.Rec2020;
        if (sourceCs?.ToColorSpaceXyz(out var sourceXyz) == true)
        {
            gamut = sourceXyz;
        }

        return transferFn switch
        {
            HdrTransferFunction.PQ => SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Pq, gamut),
            HdrTransferFunction.HLG => SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Hlg, gamut),
            _ => null,
        };
    }


    /// <summary>
    /// BT.2408-style knee curve (used by Auto mode, closest to Chrome).
    /// Linear below the knee, exponential soft roll-off above.
    /// SDR content (≤ 1.0) passes through unchanged.
    /// </summary>
    private static float Bt2408KneeToneMap(float v)
    {
        if (v <= 0f) return 0f;

        // Knee starts at 0.9 to give a smooth transition before 1.0
        const float kneeStart = 0.9f;
        const float maxOut = 1.0f;
        const float range = maxOut - kneeStart; // 0.1

        if (v <= kneeStart) return v;

        // Soft exponential shoulder: asymptotically approaches maxOut
        float excess = v - kneeStart;
        return kneeStart + range * (1f - MathF.Exp(-excess / range));
    }


    /// <summary>
    /// Extended Reinhard with knee-based shoulder (no discontinuity).
    /// Linear below the knee, Reinhard-shaped soft roll-off above.
    /// Preserves more highlight differentiation than exponential decay.
    /// Derivative at knee = 1 (matches linear passthrough).
    /// </summary>
    private static float ExtendedReinhardToneMap(float v)
    {
        if (v <= 0f) return 0f;

        const float kneeStart = 0.9f;
        const float maxOut = 1.0f;
        const float range = maxOut - kneeStart; // 0.1

        if (v <= kneeStart) return v;

        // Reinhard shoulder: range * x / (x + range)
        // f'(knee) = range / (0 + range)² = 1/range · range²/range² ... = 1  ✓
        // Approaches maxOut more slowly than exponential → more highlight detail.
        float excess = v - kneeStart;
        return kneeStart + range * excess / (excess + range);
    }


    /// <summary>
    /// ACES-style filmic curve with knee-based shoulder (no discontinuity).
    /// Linear below the knee, tanh-shaped roll-off above for cinematic feel.
    /// Converges to 1.0 faster than Reinhard → punchier highlight rolloff.
    /// Derivative at knee = 1 (matches linear passthrough).
    /// </summary>
    private static float AcesToneMap(float v)
    {
        if (v <= 0f) return 0f;

        const float kneeStart = 0.9f;
        const float maxOut = 1.0f;
        const float range = maxOut - kneeStart; // 0.1

        if (v <= kneeStart) return v;

        // Tanh shoulder: range * tanh(x / range)
        // f'(knee) = tanh'(0) = 1  ✓  (sech²(0) = 1)
        // Converges to maxOut faster than Reinhard, giving a punchier rolloff.
        float excess = v - kneeStart;
        return kneeStart + range * MathF.Tanh(excess / range);
    }


    #endregion // Private Methods

}

