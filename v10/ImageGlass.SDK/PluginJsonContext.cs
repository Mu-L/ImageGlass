/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageGlass.SDK;

/// <summary>
/// Source-generated JSON serialization context for all IPC message types.
/// AOT-safe — no runtime reflection.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PluginMessage))]
[JsonSerializable(typeof(PluginManifest))]
[JsonSerializable(typeof(PluginInitPayload))]
[JsonSerializable(typeof(ReadPixelRequest))]
[JsonSerializable(typeof(ReadPixelResponse))]
[JsonSerializable(typeof(GetPixelBufferRequest))]
[JsonSerializable(typeof(GetPixelBufferResponse))]
[JsonSerializable(typeof(ReleasePixelBufferRequest))]
[JsonSerializable(typeof(RunApiRequest))]
[JsonSerializable(typeof(RunApiResponse))]
[JsonSerializable(typeof(SourceSizeResponse))]
[JsonSerializable(typeof(SetSelectionRequest))]
[JsonSerializable(typeof(EnableSelectionRequest))]
[JsonSerializable(typeof(FrameChangedPayload))]
[JsonSerializable(typeof(PhotoChangedEventArgs))]
[JsonSerializable(typeof(LanguageChangedEventArgs))]
[JsonSerializable(typeof(PointerEventArgs))]
[JsonSerializable(typeof(SelectionEventArgs))]
[JsonSerializable(typeof(ThemeInfo))]
[JsonSerializable(typeof(PluginEventSubscriptions))]
[JsonSerializable(typeof(PluginPhotoMetadata))]
[JsonSerializable(typeof(PluginPhotoList))]
[JsonSerializable(typeof(PluginPhotoListItem))]
[JsonSerializable(typeof(PluginColor))]
[JsonSerializable(typeof(PluginRect))]
[JsonSerializable(typeof(JsonElement))]
public partial class PluginJsonContext : JsonSerializerContext;
