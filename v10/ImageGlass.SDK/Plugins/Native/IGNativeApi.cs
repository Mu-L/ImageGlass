/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
using System.Runtime.CompilerServices;
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


/// <summary>
/// Plugin-side API table. Returned from the well-known entry point
/// <see cref="IGNativeAbi.ENTRY_POINT_NAME"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGPluginApi
{
    /// <summary>
    /// Size of this struct in bytes. Plugins set this to <c>sizeof(IGPluginApi)</c> at the time
    /// they were compiled. The host validates this matches its expected size for the negotiated ABI.
    /// </summary>
    public int StructSize;

    /// <summary>ABI version the plugin was built against. Must match the host's major version.</summary>
    public int AbiVersion;

    /// <summary>Identity of the plugin.</summary>
    public IGPluginInfo Info;

    /// <summary>
    /// Returns the codec API table for the given codec index in [0, <see cref="IGPluginInfo.CodecCount"/>).
    /// Signature: <c>IGStatus GetCodec(int index, IGCodecApi** outCodecApi)</c>.
    /// The returned pointer must remain valid for the plugin's lifetime.
    /// </summary>
    public delegate* unmanaged[Cdecl]<int, IGCodecApi**, IGStatus> GetCodec;

    /// <summary>
    /// Optional one-time initialization callback. May be null.
    /// Signature: <c>IGStatus Initialize()</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<IGStatus> Initialize;

    /// <summary>
    /// Optional shutdown callback. May be null. Called once at host shutdown.
    /// Signature: <c>void Shutdown()</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<void> Shutdown;

    /// <summary>
    /// Optional self-test callback. May be null. Returns <see cref="IGStatus.OK"/> on success.
    /// Signature: <c>IGStatus SelfTest()</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<IGStatus> SelfTest;
}


/// <summary>
/// Host services exposed to plugins for non-codec concerns (logging, allocation, cancellation).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGHostCoreApi
{
    /// <summary>
    /// Logs a UTF-16 message to the host's plugin log channel.
    /// Signature: <c>void Log(int level, IGStringRef message)</c>.
    /// Levels: 0=trace, 1=debug, 2=info, 3=warn, 4=error.
    /// </summary>
    public delegate* unmanaged[Cdecl]<int, IGStringRef, void> Log;

    /// <summary>
    /// Allocates raw memory the plugin can use; freed via <see cref="Free"/>.
    /// Signature: <c>void* Alloc(nuint sizeInBytes)</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<nuint, void*> Alloc;

    /// <summary>
    /// Frees memory previously returned by <see cref="Alloc"/>.
    /// Signature: <c>void Free(void* ptr)</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<void*, void> Free;

    /// <summary>
    /// Returns 1 if the host has requested cancellation for the given opaque cancellation token.
    /// Signature: <c>int IsCancellationRequested(void* cancellation)</c>.
    /// </summary>
    public delegate* unmanaged[Cdecl]<void*, int> IsCancellationRequested;

    /// <summary>
    /// Returns the absolute UTF-16 path of the host's config directory into the buffer.
    /// Signature: <c>int GetConfigDirectory(char* buffer, int bufferLength)</c>.
    /// Returns the number of code units written, or the required size if the buffer is too small.
    /// </summary>
    public delegate* unmanaged[Cdecl]<char*, int, int> GetConfigDirectory;
}


/// <summary>
/// Codec-only host services. Reserved for future host helpers (color profile lookup, format hints, etc.).
/// All fields may be null in this phase. Plugins must check before calling.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGHostCodecApi
{
    /// <summary>
    /// Reserved for future host-allocated pixel buffer support. Null in this phase.
    /// </summary>
    public void* Reserved0;

    /// <summary>
    /// Reserved for future color-profile callback. Null in this phase.
    /// </summary>
    public void* Reserved1;

    /// <summary>
    /// Reserved for future metadata helper. Null in this phase.
    /// </summary>
    public void* Reserved2;
}


/// <summary>
/// Top-level host API table passed to the plugin via the entry point.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGHostApi
{
    /// <summary>Size of this struct in bytes; set by the host.</summary>
    public int StructSize;

    /// <summary>Host ABI version. Must match what the plugin was built against (major).</summary>
    public int AbiVersion;

    /// <summary>Pointer to the core API table. Never null.</summary>
    public IGHostCoreApi* Core;

    /// <summary>Pointer to the codec API table. Never null but its members may be null in this phase.</summary>
    public IGHostCodecApi* Codec;

    /// <summary>
    /// Reserved slot for a future viewer API table. MUST be null in this ABI version;
    /// exposed solely to keep the layout forward-compatible.
    /// </summary>
    public void* Viewer;
}


/// <summary>
/// Convenience helpers for working with <see cref="IGStringRef"/> on the SDK side.
/// </summary>
public static unsafe class IGStringRefExtensions
{
    /// <summary>
    /// Materializes the slice as a managed string. Returns <see cref="string.Empty"/> if the slice is empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToManaged(this IGStringRef s)
    {
        if (s.Data == null || s.Length <= 0) return string.Empty;
        return new string(s.Data, 0, s.Length);
    }
}
