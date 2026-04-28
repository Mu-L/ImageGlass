/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
namespace ImageGlass.SDK.Plugins;

/// <summary>
/// Cached capability descriptor for one codec advertised by a plugin.
/// JSON-serialized; mirrors the runtime <see cref="IGCodecCapability"/> POD.
/// </summary>
public sealed class CodecPluginCapability
{
    public required string CodecId { get; init; }
    public string CodecName { get; init; } = string.Empty;
    public required int MetadataPriority { get; init; }
    public required int DecodePriority { get; init; }

    /// <summary>
    /// Extensions the codec handles. May be the plugin-reported list or, when
    /// the manifest specifies <see cref="PluginManifest.SupportedExtensions"/>,
    /// the user-overridden list. Always normalized (lowercase, leading dot).
    /// </summary>
    public required string[] SupportedExtensions { get; set; }

    public bool SupportsMetadata { get; init; }
    public bool SupportsStaticRaster { get; init; }
    public bool SupportsColorProfiles { get; init; }
}


