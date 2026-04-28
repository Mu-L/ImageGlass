/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
using System.Runtime.InteropServices;

namespace ImageGlass.SDK.Plugins;

/// <summary>
/// Metadata populated by the plugin during a metadata-load call.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct IGImageInfo
{
    public int Width;
    public int Height;

    /// <summary>One of <see cref="IGPixelFormat"/>.</summary>
    public int PixelFormat;

    /// <summary>1 if the source carries an alpha channel.</summary>
    public int HasAlpha;

    /// <summary>
    /// 1 if the source image is HDR (wide-gamut, high bit-depth, or PQ/HLG-encoded).
    /// The plugin owns this decision; the host trusts the value verbatim.
    /// </summary>
    public int IsHdr;

    /// <summary>EXIF orientation (1..8); 0 means unknown.</summary>
    public int Orientation;

    /// <summary>Frame count for animated formats. Should be 1 for the first phase.</summary>
    public int FrameCount;

    /// <summary>Source file size in bytes. -1 if unknown.</summary>
    public long FileSizeBytes;
}
