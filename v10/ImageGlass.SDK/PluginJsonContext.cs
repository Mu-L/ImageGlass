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
