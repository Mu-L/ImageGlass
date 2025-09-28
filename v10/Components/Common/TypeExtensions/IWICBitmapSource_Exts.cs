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

using Vortice.WIC;

namespace ImageGlass.Common;


public static class IWICBitmapSource_Exts
{

    /// <summary>
    /// Gets pixel info.
    /// </summary>
    public static PhotoPixelInfo? GetPixelInfo(this IWICBitmapSource? wicBmp)
    {
        if (wicBmp is not IWICBitmapSource bmpSrc) return null;

        // get pixel format info
        using var wicFactory = new IWICImagingFactory2();
        using var compInfo = wicFactory.CreateComponentInfo(bmpSrc.PixelFormat);
        using var info = compInfo.QueryInterfaceOrNull<IWICPixelFormatInfo2>();
        if (info is null) return null;

        // calculate stride
        bmpSrc.GetSize(out var width, out var height);
        var bitsPerPixel = info.BitsPerPixel;
        var stride = (uint)(((width * bitsPerPixel + 7) / 8 + 3) & ~3);

        return new PhotoPixelInfo()
        {
            Width = width,
            Height = height,
            BitsPerPixel = info.BitsPerPixel,
            ChannelCount = info.ChannelCount,
            Stride = stride,
            NumericRepresentation = info.NumericRepresentation,
            ColorContext = info.ColorContext,
        };
    }

}


public partial class PhotoPixelInfo : DisposableImpl
{
    public uint Width { get; internal set; }
    public uint Height { get; internal set; }
    public uint BitsPerPixel { get; internal set; }
    public uint Stride { get; internal set; }
    public uint BufferSize => Stride * Height;
    public uint ChannelCount { get; internal set; }
    public PixelFormatNumericRepresentation NumericRepresentation { get; internal set; }
    public IWICColorContext? ColorContext { get; internal set; }


    protected override void OnDisposing()
    {
        base.OnDisposing();

        ColorContext?.Dispose();
        ColorContext = null;
    }
}
