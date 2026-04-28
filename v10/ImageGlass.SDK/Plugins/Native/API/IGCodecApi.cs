/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
using System.Runtime.InteropServices;

namespace ImageGlass.SDK.Plugins;

/// <summary>
/// Per-codec function pointer table. The plugin allocates one of these for each codec
/// it advertises and keeps the table alive for the lifetime of the plugin.
/// All callbacks must be exported with <c>[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGCodecApi
{
    /// <summary>
    /// Returns the capability descriptor for this codec.
    /// Signature: <c>IGStatus GetCapability(IGCodecCapability* outCapability)</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<IGCodecCapability*, IGStatus> GetCapability;

    /// <summary>
    /// Returns 1 if the codec can handle a file with the given extension (lowercase, with leading dot).
    /// Signature: <c>int CanHandleExtension(IGStringRef extension)</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<IGStringRef, int> CanHandleExtension;

    /// <summary>
    /// Optional content-sniffing probe. Returns 1 on match, 0 on no-match.
    /// Signature: <c>int CanHandleSignature(byte* signature, int length)</c>.
    /// May be null; if null the host falls back to <see cref="CanHandleExtension"/>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<byte*, int, int> CanHandleSignature;

    /// <summary>
    /// Loads metadata for the given file path into <paramref name="outImageInfo"/>.
    /// Signature: <c>IGStatus LoadMetadata(IGStringRef filePath, IGImageInfo* outImageInfo, void* cancellation)</c>.
    /// <para>
    /// <c>cancellation</c> is an opaque token supplied by the host; the plugin should call
    /// <see cref="IGHostCoreApi.IsCancellationRequested"/> periodically and return
    /// <see cref="IGStatus.Canceled"/> if it returns 1.
    /// </para>
    /// </summary>
    public delegate* unmanaged[Cdecl]<IGStringRef, IGImageInfo*, void*, IGStatus> LoadMetadata;

    /// <summary>
    /// Decodes a single static raster from the file path. The plugin allocates the buffer and
    /// fills <paramref name="outBuffer"/>; the host releases it via <see cref="FreePixelBuffer"/>.
    /// Signature: <c>IGStatus DecodeStaticRaster(IGStringRef filePath, IGPixelBuffer* outBuffer, void* cancellation)</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<IGStringRef, IGPixelBuffer*, void*, IGStatus> DecodeStaticRaster;

    /// <summary>
    /// Releases a buffer previously returned by <see cref="DecodeStaticRaster"/>.
    /// Signature: <c>void FreePixelBuffer(IGPixelBuffer* buffer)</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<IGPixelBuffer*, void> FreePixelBuffer;
}
