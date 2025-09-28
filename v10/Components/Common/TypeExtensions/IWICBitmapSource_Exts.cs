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

using System;
using System.IO;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.WIC;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace ImageGlass.Common;


public static class IWICBitmapSource_Exts
{

    /// <summary>
    /// Gets pixel info of the current bitmap.
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


    /// <summary>
    /// Gets pixels buffer of the current bitmap.
    /// </summary>
    public static async Task<byte[]?> GetPixelsAsync(this IWICBitmapSource? srcBmp)
    {
        using var info = srcBmp?.GetPixelInfo();
        if (info is null) return null;

        // decode into raw pixels (off-thread)
        var pixels = await Task.Run(() =>
        {
            var buffer = new byte[info.BufferSize];
            srcBmp?.CopyPixels(info.Stride, buffer);

            return buffer;
        });

        return pixels;
    }


    /// <summary>
    /// Saves the input bitmap to a file in the given format.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static void SaveAs(this IWICBitmapSource? srcBmp, string destFilePath, Size? size = null,
        ContainerFormat format = ContainerFormat.Png)
    {
        if (srcBmp.IsDisposed()) return;

        using var fs = new FileStream(destFilePath,
            FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

        srcBmp.SaveAs(fs, size, format);
    }


    /// <summary>
    /// Saves the input bitmap to a file in the given format.
    /// </summary>
    /// <exception cref="SharpGen.Runtime.SharpGenException"></exception>
    public static void SaveAs(this IWICBitmapSource? srcBmp, Stream destStream, Size? size = null,
        ContainerFormat format = ContainerFormat.Png)
    {
        if (srcBmp.IsDisposed()) return;

        using var fac = new IWICImagingFactory2();
        using var stream = fac.CreateStream(destStream);
        using var encoder = fac.CreateEncoder(format);
        encoder.Initialize(stream, BitmapEncoderCacheOption.NoCache);

        size ??= new Size(srcBmp.Size.Width, srcBmp.Size.Height);

        // writing a frame
        using (var frameEncode = encoder.CreateNewFrame(out _))
        {
            frameEncode.Initialize();

            frameEncode.SetSize((uint)size.Value.Width, (uint)size.Value.Height);
            frameEncode.SetPixelFormat(Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA);

            frameEncode.WriteSource(srcBmp);
            frameEncode.Commit();
        }

        encoder.Commit();
        destStream.Position = 0;
    }


    /// <summary>
    /// Converts the current bitmap to GUID_WICPixelFormat32bppPBGRA format.
    /// </summary>
    public static void To32bppPBGRA(this IWICBitmapSource? srcBmp)
    {
        if (srcBmp.IsDisposed()) return;

        var pxFormat = Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA;
        if (srcBmp.PixelFormat == pxFormat) return;

        try
        {
            var newBmp = WIC.WICConvertBitmapSource(pxFormat, srcBmp);

            // dispose the old bitmap
            srcBmp.Dispose();

            // save the new bitmap to the old one
            srcBmp.NativePointer = newBmp.NativePointer;
        }
        catch { }

        return;
    }


    /// <summary>
    /// Converts the current bitmap to Software Bitmap.
    /// </summary>
    public static async Task<SoftwareBitmap?> ToSoftwareBitmapAsync(this IWICBitmapSource? srcBmp, BitmapTransform? transform = null)
    {
        // make sure correct pixel format
        srcBmp?.To32bppPBGRA();

        using var ms = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        using var stream = ms.AsStream();

        // convert to stream
        srcBmp.SaveAs(stream);

        // create SoftwareBitmap from stream
        var bmpDecoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ms)
            .AsTask().ConfigureAwait(false);

        var softwareBmp = await bmpDecoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform ?? new(),
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.ColorManageToSRgb)
            .AsTask().ConfigureAwait(false);

        return softwareBmp;
    }


    /// <summary>
    /// Converts WIC bitmap to Direct2D bitmap off-thread.
    /// </summary>
    public static async Task<ID2D1Bitmap1?> ToD2BitmapAsync(this IWICBitmapSource? srcBmp,
        ID3D11Device d3Device, ID2D1DeviceContext d2Context)
    {
        if (srcBmp is null) return null;

        // make sure the bitmap is in correct format
        srcBmp.To32bppPBGRA();

        using var info = srcBmp.GetPixelInfo();
        if (info is null) return null;

        // get raw pixels
        var buffer = await srcBmp.GetPixelsAsync();
        if (buffer is null) return null;


        var texDesc = new Texture2DDescription
        {
            Width = info.Width,
            Height = info.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
            SampleDescription = Vortice.DXGI.SampleDescription.Default,
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None,
        };

        // upload pixel buffer to GPU
        using var texture = d3Device.CreateTexture2D(texDesc);
        d3Device.ImmediateContext.UpdateSubresource(buffer, texture, 0, info.Stride, 0);

        // get bitmap from GPU
        using var dxgiSurface = texture.QueryInterface<Vortice.DXGI.IDXGISurface>();
        var wicBmp = d2Context.CreateBitmapFromDxgiSurface(dxgiSurface);

        return wicBmp;
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
