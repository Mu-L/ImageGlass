/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
using System.Runtime.InteropServices;

namespace ImageGlass.SDK.Native;

/// <summary>
/// Versioning constants and the well-known native entry-point name for the
/// in-process native plugin ABI.
/// </summary>
public static class IGNativeAbi
{
    /// <summary>
    /// The current native plugin ABI version. Encoded as MAJOR * 1_000_000 + MINOR * 1_000 + PATCH.
    /// Major-version mismatch causes the host to reject the plugin outright.
    /// </summary>
    public const int IG_PLUGIN_ABI_VERSION = 1_000_000;

    /// <summary>
    /// Major component of <see cref="IG_PLUGIN_ABI_VERSION"/>. Plugins and host must agree on this.
    /// </summary>
    public const int IG_PLUGIN_ABI_MAJOR = 1;

    /// <summary>
    /// The well-known name of the single C entry point every native plugin must export.
    /// Signature (C):
    /// <code>
    /// const IGPluginApi* ig_plugin_get_api(int hostAbiVersion, const IGHostApi* hostApi);
    /// </code>
    /// Returns null on rejection (e.g., incompatible ABI).
    /// </summary>
    public const string ENTRY_POINT_NAME = "ig_plugin_get_api";
}


/// <summary>
/// Status codes returned across the native ABI. Values are stable; never reorder or repurpose.
/// </summary>
public enum IGStatus : int
{
    OK = 0,
    Unsupported = 1,
    Canceled = 2,
    InvalidArg = 3,
    DecodeFailed = 4,
    OutOfMemory = 5,
    Internal = 6,
    NotImplemented = 7,
    IoError = 8,
}


/// <summary>
/// Pixel format identifiers used by <see cref="IGPixelBuffer"/> and <see cref="IGImageInfo"/>.
/// Values are stable; never reorder or repurpose.
/// </summary>
public enum IGPixelFormat : int
{
    Unknown = 0,
    Bgra8Unorm = 1,
    Rgba8Unorm = 2,
    Rgba16Unorm = 3,
    RgbaFloat16 = 4,
}


/// <summary>
/// Identifies which plugin kind a manifest declares.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<IGPluginKind>))]
public enum IGPluginKind
{
    /// <summary>Out-of-process plugin (separate executable, named-pipe IPC).</summary>
    OOP = 0,

    /// <summary>In-process native plugin (shared library, C ABI).</summary>
    Native = 1,
}


/// <summary>
/// UTF-16 string slice. The pointer is non-owning; the producing side controls the lifetime.
/// Strings passed from host -> plugin live until the call returns.
/// Strings passed from plugin -> host live until the next plugin call on that codec.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGStringRef
{
    /// <summary>Pointer to UTF-16 code units. May be null when <see cref="Length"/> is 0.</summary>
    public char* Data;

    /// <summary>Number of UTF-16 code units (not bytes). Must be non-negative.</summary>
    public int Length;
}


/// <summary>
/// Identity record for a plugin instance.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct IGPluginInfo
{
    /// <summary>Stable plugin id (must match manifest <c>Id</c>).</summary>
    public IGStringRef PluginId;

    /// <summary>Plugin display name.</summary>
    public IGStringRef Name;

    /// <summary>Plugin version string.</summary>
    public IGStringRef Version;

    /// <summary>ABI version reported by the plugin. Must equal <see cref="IGNativeAbi.IG_PLUGIN_ABI_VERSION"/>'s major.</summary>
    public int AbiVersion;

    /// <summary>Number of codec entries advertised by this plugin.</summary>
    public int CodecCount;
}


/// <summary>
/// Capability descriptor advertised by one codec. Used both at probe time and at codec selection time.
/// All fields use <see cref="int"/> for booleans to stay strictly POD.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGCodecCapability
{
    /// <summary>Stable codec id (e.g. <c>"plugin.wic.jxr"</c>).</summary>
    public IGStringRef CodecId;

    /// <summary>Friendly codec name (for diagnostics/UI).</summary>
    public IGStringRef CodecName;

    /// <summary>Higher = chosen first when multiple codecs report metadata support.</summary>
    public int MetadataPriority;

    /// <summary>Higher = chosen first when multiple codecs report decode support.</summary>
    public int DecodePriority;

    /// <summary>1 if the codec implements <c>LoadMetadata</c>.</summary>
    public int SupportsMetadata;

    /// <summary>1 if the codec implements <c>DecodeStaticRaster</c>.</summary>
    public int SupportsStaticRaster;

    /// <summary>Reserved; must be 0 in the first phase.</summary>
    public int SupportsAnimation;

    /// <summary>Reserved; must be 0 in the first phase.</summary>
    public int SupportsVector;

    /// <summary>Reserved; must be 0 in the first phase unless trivially safe.</summary>
    public int SupportsHdr;

    /// <summary>1 if the codec returns embedded color profile information.</summary>
    public int SupportsColorProfiles;

    /// <summary>Number of file extensions in <see cref="Extensions"/>.</summary>
    public int ExtensionCount;

    /// <summary>
    /// Pointer to an array of <see cref="IGStringRef"/> file extensions (lowercase, with leading dot).
    /// The array and its string data must remain valid for the lifetime of the plugin.
    /// </summary>
    public IGStringRef* Extensions;
}


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

    /// <summary>EXIF orientation (1..8); 0 means unknown.</summary>
    public int Orientation;

    /// <summary>Frame count for animated formats. Should be 1 for the first phase.</summary>
    public int FrameCount;

    /// <summary>Source file size in bytes. -1 if unknown.</summary>
    public long FileSizeBytes;

    /// <summary>Reserved for future ABI growth. Must be zeroed by the plugin.</summary>
    public long Reserved0;

    /// <summary>Reserved for future ABI growth. Must be zeroed by the plugin.</summary>
    public long Reserved1;
}


/// <summary>
/// Pixel buffer descriptor returned from a successful decode. Always paired with a free callback.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IGPixelBuffer
{
    /// <summary>Pointer to the raw pixels. Owned by whoever set <see cref="ReleaseContext"/>.</summary>
    public byte* Data;

    /// <summary>Pixel width.</summary>
    public int Width;

    /// <summary>Pixel height.</summary>
    public int Height;

    /// <summary>Row stride in bytes. Must be at least <c>Width * bytesPerPixel</c>.</summary>
    public int Stride;

    /// <summary>One of <see cref="IGPixelFormat"/>.</summary>
    public int PixelFormat;

    /// <summary>Reserved color-space hint; 0 means unspecified / sRGB.</summary>
    public int ColorSpaceHint;

    /// <summary>Opaque value the plugin's free callback uses to identify the buffer.</summary>
    public void* ReleaseContext;
}
