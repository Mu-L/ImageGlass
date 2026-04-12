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
/// <para>
/// Two tone-mapping strategies are used depending on content type:
/// <list type="bullet">
/// <item><b>Per-channel</b> for linear scene-referred content (EXR, Radiance HDR, JXR)
/// — avoids overflow artifacts (skyblue tint, blue->white wash-out) in sRGB space.</item>
/// <item><b>Luminance-based</b> for wide-gamut PQ/HLG content (JXL, AVIF, HEIF)
/// — preserves channel ratios needed by the Rec.2020->sRGB gamut matrix.</item>
/// </list>
/// </para>
/// Monitor color profile is applied by the caller after tone mapping.
/// </summary>
public static class HdrToneMapper
{
    /// <summary>
    /// PQ EOTF peak luminance in nits (SMPTE ST 2084).
    /// </summary>
    private const float PqPeakNits = 10_000f;

    /// <summary>
    /// SDR reference white in nits (ITU-R BT.2408).
    /// </summary>
    private const float SdrWhiteNits = 203f;

    /// <summary>
    /// Normalization multiplier: after PQ EOTF (1.0 = 10 000 nits),
    /// scale so that 203 nits -> 1.0.
    /// </summary>
    private const float PqNormScale = PqPeakNits / SdrWhiteNits; // ~ 49.26

    // Rec.2020 luminance coefficients (ITU-R BT.2020) — used by luminance-based path
    private const float Lum2020R = 0.2627f;
    private const float Lum2020G = 0.6780f;
    private const float Lum2020B = 0.0593f;

    // Rec.2020 -> sRGB  3x3 gamut mapping matrix (linear)
    // M = sRGB_from_XYZ x XYZ_from_Rec2020
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
    /// <param name="brightness">Brightness adjustment in EV stops.
    /// <c>0</c> = no change, <c>+1</c> = 2x brighter, <c>-1</c> = 0.5x.</param>
    public static SKImage? ToneMapToSdr(SKImage? source, HdrTransferFunction transferFn,
        HdrToneMappingMode mode, double brightness = 0d)
    {
        if (source.IsDisposed()) return null;
        if (mode == HdrToneMappingMode.None) return null;

        // Gain-map images: the decoded base layer is already SDR.
        if (transferFn == HdrTransferFunction.GainMap) return null;

        SKImage? retaggedSource = null;
        try
        {
            var effectiveSource = source;
            var brightnessVal = (float)brightness;

            if (!IsHdrColorSpace(source.ColorSpace))
            {
                if (transferFn is HdrTransferFunction.None)
                {
                    // Linear scene-referred HDR (EXR, Radiance HDR, JXR):
                    // pixels are already linear but the source may have no color
                    // space tag (or sRGB). Tag as linear-sRGB so that Skia's
                    // DrawImage in ToneMapManual doesn't apply an unwanted sRGB
                    // gamma -> linear conversion (which would darken the image).
                    // Using sRGB primaries (not Rec.2020) because most EXR/HDR
                    // files use Rec.709/sRGB primaries; Skia will then correctly
                    // gamut-map from sRGB to Rec.2020 in ToneMapManual.
                    var linearCs = SKColorSpace.CreateSrgbLinear();

                    retaggedSource = ReinterpretColorSpace(source, linearCs);
                    if (retaggedSource is null) return null;

                    effectiveSource = retaggedSource;
                }
                else
                {
                    // PQ/HLG: metadata says PQ or HLG but the decoded color space
                    // doesn't reflect it — re-tag with the correct transfer function.
                    var hdrCs = BuildHdrColorSpace(transferFn, source.ColorSpace);
                    if (hdrCs is null) return null;

                    retaggedSource = ReinterpretColorSpace(source, hdrCs);
                    if (retaggedSource is null) return null;

                    effectiveSource = retaggedSource;
                }
            }

            // Tone-mapping operators
            // (input / output in normalized linear, 1.0 = SDR reference white = 203 nits)
            return mode switch
            {
                HdrToneMappingMode.BT2408 => ToneMapManual(effectiveSource, transferFn,
                    brightnessVal, Bt2408KneeToneMap),
                HdrToneMappingMode.Reinhard => ToneMapManual(effectiveSource, transferFn,
                    brightnessVal, ExtendedReinhardToneMap),
                HdrToneMappingMode.ACES => ToneMapManual(effectiveSource, transferFn,
                    brightnessVal, AcesToneMap),
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
    /// Tone mapping pipeline with two strategies:
    /// <para><b>PQ/HLG path</b> (wide-gamut Rec.2020):</para>
    /// 1. Linearize via Skia color-space conversion to linear Rec.2020.
    /// 2. Normalize so 203 nits = 1.0 (PQ) or keep 1.0 (HLG).
    /// 3. Apply tone curve on <b>luminance</b>, scale RGB proportionally.
    /// 4. Gamut-map Rec.2020 -> sRGB via 3×3 matrix.
    /// 5. Encode to sRGB gamma.
    /// <para><b>Linear scene-referred path</b> (EXR, Radiance HDR, JXR):</para>
    /// 1. Read float pixels directly (already linear sRGB).
    /// 2. Apply tone curve <b>per-channel</b> independently.
    /// 3. Encode to sRGB gamma.
    /// <para>
    /// The two strategies exist because:
    /// <list type="bullet">
    /// <item>Per-channel avoids overflow artifacts in sRGB space (no skyblue
    /// tint on near-white, no blue->white wash-out).</item>
    /// <item>Luminance-based preserves channel ratios that the Rec.2020->sRGB
    /// gamut matrix needs; per-channel would compress all channels toward 1.0,
    /// making the matrix output near-white.</item>
    /// </list>
    /// </para>
    /// </summary>
    private static unsafe SKImage? ToneMapManual(SKImage source,
        HdrTransferFunction transferFn,
        float brightness, Func<float, float> toneCurve)
    {
        var isLinearSceneReferred = transferFn == HdrTransferFunction.None;

        // ── Step 1: linearize source into float pixels ──
        using var linearBmp = LinearizeToFloat(source, isLinearSceneReferred);
        if (linearBmp is null) return null;

        var width = linearBmp.Width;
        var height = linearBmp.Height;

        // ── Step 2: allocate output (linear sRGB, RgbaF32) ──
        var outputInfo = new SKImageInfo(width, height,
            SKColorType.RgbaF32, SKAlphaType.Unpremul, SKColorSpace.CreateSrgbLinear());
        using var outputBmp = new SKBitmap(outputInfo);

        var srcPtr = (byte*)linearBmp.GetPixels();
        var dstPtr = (byte*)outputBmp.GetPixels();
        var srcRowBytes = linearBmp.RowBytes;
        var dstRowBytes = outputBmp.RowBytes;

        // ── Step 3: normalization and brightness ──
        var normScale = ComputeNormScale(transferFn, brightness);

        // ── Step 4: per-pixel tone mapping ──
        if (isLinearSceneReferred)
        {
            ToneMapPerChannel(srcPtr, srcRowBytes, dstPtr, dstRowBytes,
                width, height, normScale, toneCurve);
        }
        else
        {
            ToneMapLuminanceBased(srcPtr, srcRowBytes, dstPtr, dstRowBytes,
                width, height, normScale, toneCurve);
        }

        // ── Step 5: convert linear sRGB float -> final sRGB Rgba8888 ──
        return ConvertToFinalSrgb(outputBmp);
    }


    /// <summary>
    /// Linearizes the source image into an <see cref="SKColorType.RgbaF32"/> bitmap.
    /// For scene-referred content (EXR/HDR/JXR), the target is linear sRGB.
    /// For PQ/HLG content, the target is linear Rec.2020.
    /// </summary>
    /// <returns>An <see cref="SKBitmap"/> owning the linearized pixels,
    /// or <c>null</c> on failure. Caller owns disposal.</returns>
    private static SKBitmap? LinearizeToFloat(SKImage source, bool isLinearSceneReferred)
    {
        var targetCs = isLinearSceneReferred
            ? SKColorSpace.CreateSrgbLinear()
            : SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Linear, SKColorSpaceXyz.Rec2020);

        var linearInfo = new SKImageInfo(source.Width, source.Height,
            SKColorType.RgbaF32, SKAlphaType.Unpremul, targetCs);

        using var linearSurface = SKSurface.Create(linearInfo);
        if (linearSurface is null) return null;

        linearSurface.Canvas.DrawImage(source, 0, 0);

        // Copy pixels into an owned bitmap so the surface can be disposed safely.
        using var linearSnapshot = linearSurface.Snapshot();
        var bitmap = new SKBitmap(linearInfo);
        if (!linearSnapshot.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
        {
            bitmap.Dispose();
            return null;
        }

        return bitmap;
    }


    /// <summary>
    /// Computes the combined normalization scale: PQ EOTF rescaling + brightness EV.
    /// </summary>
    private static float ComputeNormScale(HdrTransferFunction transferFn, float brightness)
    {
        // PQ: after EOTF 1.0 = 10 000 nits. Scale so 203 nits -> 1.0.
        // HLG/Linear: 1.0 ≈ reference white already.
        var normScale = transferFn == HdrTransferFunction.PQ ? PqNormScale : 1f;

        // Brightness in EV stops: 0 = no change, +1 = 2×, -1 = 0.5×.
        if (brightness != 0f)
        {
            normScale *= MathF.Pow(2f, brightness);
        }

        return normScale;
    }


    /// <summary>
    /// Per-channel tone mapping for linear scene-referred sRGB content.
    /// Each channel is independently compressed — avoids overflow artifacts.
    /// </summary>
    private static unsafe void ToneMapPerChannel(
        byte* srcPtr, int srcRowBytes, byte* dstPtr, int dstRowBytes,
        int width, int height, float normScale, Func<float, float> toneCurve)
    {
        for (var y = 0; y < height; y++)
        {
            var srcRow = (float*)(srcPtr + (long)y * srcRowBytes);
            var dstRow = (float*)(dstPtr + (long)y * dstRowBytes);

            for (var x = 0; x < width; x++)
            {
                var i = x * 4;
                var r = srcRow[i] * normScale;
                var g = srcRow[i + 1] * normScale;
                var b = srcRow[i + 2] * normScale;
                var a = srcRow[i + 3];

                if (r > 0f || g > 0f || b > 0f)
                {
                    r = toneCurve(r);
                    g = toneCurve(g);
                    b = toneCurve(b);
                }

                dstRow[i] = Math.Clamp(r, 0f, 1f);
                dstRow[i + 1] = Math.Clamp(g, 0f, 1f);
                dstRow[i + 2] = Math.Clamp(b, 0f, 1f);
                dstRow[i + 3] = Math.Clamp(a, 0f, 1f);
            }
        }
    }


    /// <summary>
    /// Luminance-based tone mapping for wide-gamut Rec.2020 content (PQ/HLG).
    /// Preserves channel ratios for correct Rec.2020 -> sRGB gamut mapping.
    /// </summary>
    private static unsafe void ToneMapLuminanceBased(
        byte* srcPtr, int srcRowBytes, byte* dstPtr, int dstRowBytes,
        int width, int height, float normScale, Func<float, float> toneCurve)
    {
        for (var y = 0; y < height; y++)
        {
            var srcRow = (float*)(srcPtr + (long)y * srcRowBytes);
            var dstRow = (float*)(dstPtr + (long)y * dstRowBytes);

            for (var x = 0; x < width; x++)
            {
                var i = x * 4;
                var r = srcRow[i] * normScale;
                var g = srcRow[i + 1] * normScale;
                var b = srcRow[i + 2] * normScale;
                var a = srcRow[i + 3];

                var lum = Lum2020R * r + Lum2020G * g + Lum2020B * b;

                if (lum > 0f)
                {
                    var scale = toneCurve(lum) / lum;
                    r *= scale;
                    g *= scale;
                    b *= scale;
                }
                else
                {
                    r = 0f; g = 0f; b = 0f;
                }

                // Gamut map: Rec.2020 linear -> sRGB linear
                var sr = M00 * r + M01 * g + M02 * b;
                var sg = M10 * r + M11 * g + M12 * b;
                var sb = M20 * r + M21 * g + M22 * b;

                dstRow[i] = Math.Clamp(sr, 0f, 1f);
                dstRow[i + 1] = Math.Clamp(sg, 0f, 1f);
                dstRow[i + 2] = Math.Clamp(sb, 0f, 1f);
                dstRow[i + 3] = Math.Clamp(a, 0f, 1f);
            }
        }
    }


    /// <summary>
    /// Converts a linear sRGB <see cref="SKColorType.RgbaF32"/> bitmap to a final
    /// sRGB <see cref="SKColorType.Rgba8888"/> image. Monitor profile is applied
    /// by the caller.
    /// </summary>
    private static SKImage? ConvertToFinalSrgb(SKBitmap linearBmp)
    {
        using var linearImg = SKImage.FromBitmap(linearBmp);
        if (linearImg is null) return null;

        var finalInfo = new SKImageInfo(linearBmp.Width, linearBmp.Height,
            SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
        using var finalSurface = SKSurface.Create(finalInfo);
        if (finalSurface is null) return null;

        finalSurface.Canvas.DrawImage(linearImg, 0, 0);
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
    /// Extended Reinhard with wide shoulder (no discontinuity).
    /// Knee at 0.7 with Reinhard-shaped rolloff over a 0.3 range.
    /// Trades ~15% SDR brightness for significantly more highlight detail:
    /// 2x SDR white = 94%, 5x SDR white = 98% (vs BT.2408 which clips both near 100%).
    /// </summary>
    private static float ExtendedReinhardToneMap(float v)
    {
        if (v <= 0f) return 0f;

        const float kneeStart = 0.7f;
        const float maxOut = 1.0f;
        const float range = maxOut - kneeStart; // 0.3

        if (v <= kneeStart) return v;

        // Reinhard shoulder: range * x / (x + range)
        // Derivative at knee = 1 (matches linear passthrough).
        // Approaches maxOut more slowly than exp/tanh -> most highlight detail.
        float excess = v - kneeStart;
        return kneeStart + range * excess / (excess + range);
    }


    /// <summary>
    /// ACES-style filmic curve with wide shoulder (no discontinuity).
    /// Knee at 0.5 with tanh-shaped rolloff over a 0.5 range.
    /// Starts compressing earlier than Reinhard (0.5 vs 0.7), but tanh
    /// converges faster — punchier look with highlights clipped sooner.
    /// At SDR white: BT.2408 96% -> ACES 88% -> Reinhard 85%.
    /// </summary>
    private static float AcesToneMap(float v)
    {
        if (v <= 0f) return 0f;

        const float kneeStart = 0.5f;
        const float maxOut = 1.0f;
        const float range = maxOut - kneeStart; // 0.5

        if (v <= kneeStart) return v;

        // Tanh shoulder: range * tanh(x / range)
        // Derivative at knee = 1 (sech²(0) = 1).
        // Converges to maxOut faster than Reinhard -> punchier highlight rolloff.
        float excess = v - kneeStart;
        return kneeStart + range * MathF.Tanh(excess / range);
    }


    #endregion // Private Methods

}

