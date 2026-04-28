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
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.SDK.Plugins;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Plugins;


/// <summary>
/// Bridges a single native codec (one entry of an <see cref="IGPluginApi"/>) into the
/// host's <see cref="ICodec"/> contract so the rest of the codec system can treat it
/// identically to built-in codecs.
/// </summary>
internal sealed unsafe class NativeCodecProxy : PhDisposable, ICodec
{
    private readonly NativePlugin _plugin;
    private readonly IGCodecApi* _codecApi;
    private readonly PluginFailureManager _quarantine;
    private readonly HashSet<string> _supportedExtensionsSet;


    public string CodecId { get; }
    public string CodecName { get; }
    public int MetadataPriority { get; }
    public int DecodePriority { get; }
    public IReadOnlyList<string> SupportedExtensions { get; }
    public bool SupportsMetadata { get; }
    public bool SupportsStaticRaster { get; }
    public bool SupportsColorProfiles { get; }


    public NativeCodecProxy(
        NativePlugin plugin,
        IGCodecApi* codecApi,
        CodecPluginCapability capability,
        PluginFailureManager quarantine)
    {
        _plugin = plugin;
        _codecApi = codecApi;
        _quarantine = quarantine;

        CodecId = capability.CodecId;
        CodecName = string.IsNullOrEmpty(capability.CodecName)
            ? $"{plugin.PluginId}/{capability.CodecId}" : capability.CodecName;
        MetadataPriority = capability.MetadataPriority;
        DecodePriority = capability.DecodePriority;
        SupportsMetadata = capability.SupportsMetadata;
        SupportsStaticRaster = capability.SupportsStaticRaster;
        SupportsColorProfiles = capability.SupportsColorProfiles;

        var exts = new List<string>(capability.SupportedExtensions.Length);
        _supportedExtensionsSet = new(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in capability.SupportedExtensions)
        {
            if (string.IsNullOrEmpty(ext)) continue;
            var normalized = ext.StartsWith('.') ? ext : "." + ext;
            exts.Add(normalized);
            _supportedExtensionsSet.Add(normalized);
        }
        SupportedExtensions = exts;
    }


    public bool CanLoadMetadata(string filePath)
    {
        if (!SupportsMetadata) return false;
        if (_quarantine.IsQuarantined(_plugin.PluginId)) return false;
        if (string.IsNullOrEmpty(filePath)) return false;
        var ext = Path.GetExtension(filePath);
        return _supportedExtensionsSet.Contains(ext);
    }


    public bool CanDecode(PhotoMetadata metadata, CodecSelectionContext context)
    {
        if (!SupportsStaticRaster) return false;
        if (_quarantine.IsQuarantined(_plugin.PluginId)) return false;
        if (metadata is null || string.IsNullOrEmpty(metadata.FilePath)) return false;
        return _supportedExtensionsSet.Contains(metadata.FileExtension);
    }


    public Task<PhotoMetadata> LoadMetadataAsync(string filePath,
        PhotoReadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Factory.StartNew(() => LoadMetadataCore(filePath, cancellationToken),
            cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }


    public Task<CodecDecodeResult> DecodeAsync(PhotoMetadata metadata,
        PhotoReadOptions options,
        CodecSelectionContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.Factory.StartNew(() => DecodeCore(metadata, cancellationToken),
            cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }


    private PhotoMetadata LoadMetadataCore(string filePath, CancellationToken token)
    {
        var meta = new PhotoMetadata(filePath);
        if (_codecApi->LoadMetadata == null) return meta;

        var cancelHandle = PluginHostApiTable.RegisterCancellation(token);
        try
        {
            IGImageInfo info = default;
            IGStatus status;
            try
            {
                fixed (char* pPath = filePath)
                {
                    var pathRef = new IGStringRef { Data = pPath, Length = filePath.Length };
                    status = _codecApi->LoadMetadata(pathRef, &info, (void*)cancelHandle);
                }
            }
            catch (Exception ex)
            {
                _quarantine.RecordSoftFailure(_plugin.PluginId,
                    $"managed exception during LoadMetadata: {ex.Message}");
                return meta;
            }

            if (status == IGStatus.Canceled)
            {
                token.ThrowIfCancellationRequested();
            }
            if (status == IGStatus.OK)
            {
                if (info.Width > 0) meta.Width = (uint)info.Width;
                if (info.Height > 0) meta.Height = (uint)info.Height;
                if (info.Width > 0) meta.OriginalWidth = (uint)info.Width;
                if (info.Height > 0) meta.OriginalHeight = (uint)info.Height;
                meta.HasAlpha = info.HasAlpha != 0;
                meta.FrameCount = (uint)Math.Max(1, info.FrameCount);
                meta.IsHdr = info.IsHdr != 0;
            }
        }
        finally
        {
            PluginHostApiTable.ReleaseCancellation(cancelHandle);
        }
        return meta;
    }


    private CodecDecodeResult DecodeCore(PhotoMetadata metadata, CancellationToken token)
    {
        if (_codecApi->DecodeStaticRaster == null || _codecApi->FreePixelBuffer == null)
        {
            throw new NotSupportedException($"Native codec '{CodecId}' does not support static-raster decode.");
        }

        var cancelHandle = PluginHostApiTable.RegisterCancellation(token);
        var filePath = metadata.FilePath;
        IGPixelBuffer buffer = default;
        var bufferOwned = false;

        try
        {
            IGStatus status;
            try
            {
                fixed (char* pPath = filePath)
                {
                    var pathRef = new IGStringRef { Data = pPath, Length = filePath.Length };
                    status = _codecApi->DecodeStaticRaster(pathRef, &buffer, (void*)cancelHandle);
                }
            }
            catch (Exception ex)
            {
                _quarantine.RecordSoftFailure(_plugin.PluginId,
                    $"managed exception during DecodeStaticRaster: {ex.Message}");
                throw new InvalidDataException(
                    $"Native codec '{CodecId}' threw during decode of '{filePath}'.", ex);
            }

            if (status == IGStatus.Canceled)
            {
                token.ThrowIfCancellationRequested();
            }
            if (status != IGStatus.OK)
            {
                throw new InvalidDataException(
                    $"Native codec '{CodecId}' returned status {status} for '{filePath}'.");
            }
            bufferOwned = true;

            if (buffer.Data == null || buffer.Width <= 0 || buffer.Height <= 0)
            {
                throw new InvalidDataException(
                    $"Native codec '{CodecId}' returned an invalid pixel buffer.");
            }

            var image = ConvertToSKImage(in buffer);
            if (image is null)
            {
                throw new InvalidDataException(
                    $"Native codec '{CodecId}' returned an unsupported pixel format ({buffer.PixelFormat}).");
            }

            // success: synchronize metadata size and return result
            if (metadata.Width == 0) metadata.Width = (uint)buffer.Width;
            if (metadata.Height == 0) metadata.Height = (uint)buffer.Height;
            if (metadata.OriginalWidth == 0) metadata.OriginalWidth = (uint)buffer.Width;
            if (metadata.OriginalHeight == 0) metadata.OriginalHeight = (uint)buffer.Height;

            return new CodecDecodeResult
            {
                CodecId = $"native:{_plugin.PluginId}:{CodecId}",
                ContentKind = CodecContentKind.StaticRaster,
                Size = new Size(buffer.Width, buffer.Height),
                SingleFrame = image,
                HasEmbeddedColorProfile = SupportsColorProfiles,
            };
        }
        finally
        {
            if (bufferOwned)
            {
                try
                {
                    IGPixelBuffer localBuf = buffer;
                    _codecApi->FreePixelBuffer(&localBuf);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NativeCodecProxy] FreePixelBuffer threw: {ex.Message}");
                }
            }
            PluginHostApiTable.ReleaseCancellation(cancelHandle);
        }
    }


    private SKImage? ConvertToSKImage(in IGPixelBuffer buffer)
    {
        var format = (IGPixelFormat)buffer.PixelFormat;
        var (colorType, alphaType) = format switch
        {
            IGPixelFormat.Bgra8Unorm => (SKColorType.Bgra8888, SKAlphaType.Unpremul),
            IGPixelFormat.Rgba8Unorm => (SKColorType.Rgba8888, SKAlphaType.Unpremul),
            IGPixelFormat.Rgba16Unorm => (SKColorType.Rgba16161616, SKAlphaType.Unpremul),
            IGPixelFormat.RgbaFloat16 => (SKColorType.RgbaF16, SKAlphaType.Unpremul),
            _ => (SKColorType.Unknown, SKAlphaType.Unknown),
        };
        if (colorType == SKColorType.Unknown) return null;

        var info = new SKImageInfo(buffer.Width, buffer.Height, colorType, alphaType);

        // Copy pixels into a host-owned SKBitmap so the plugin can free its buffer
        // immediately after the decode call returns. This is one full-frame copy;
        // a future ABI revision may add zero-copy via a host-allocated buffer.
        var bitmap = new SKBitmap();
        if (!bitmap.InstallPixels(info, (nint)buffer.Data, buffer.Stride))
        {
            bitmap.Dispose();
            return null;
        }
        var copy = bitmap.Copy();
        bitmap.Dispose();
        if (copy is null) return null;

        var image = SKImage.FromBitmap(copy);
        copy.Dispose();
        return image;
    }


    protected override void OnDisposing()
    {
        base.OnDisposing();
    }
}
