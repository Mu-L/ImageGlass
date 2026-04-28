/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
using ImageGlass.SDK.Native;

namespace ImageGlass.SDK;

/// <summary>
/// JSON metadata for an external plugin, read from <c>imageglass.plugin.json</c>.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>
    /// Unique plugin ID (e.g. "Plugin_MyTool"). Must match <see cref="PluginBase.PluginId"/>.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name shown in menus.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Short description of the plugin.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Plugin version (e.g. "1.0.0").
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Author name or organization.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Plugin website URL.
    /// </summary>
    public string? Website { get; init; }

    /// <summary>
    /// Plugin kind. Defaults to <see cref="IGPluginKind.OOP"/> for backwards compatibility
    /// with existing manifests that omit this field.
    /// </summary>
    public IGPluginKind Kind { get; init; } = IGPluginKind.OOP;

    /// <summary>
    /// Filename of the plugin binary, relative to the plugin folder.
    /// <list type="bullet">
    /// <item>For <see cref="IGPluginKind.OOP"/>: the executable (e.g. <c>MyPlugin.exe</c> on Windows, <c>MyPlugin</c> on Linux/macOS).</item>
    /// <item>For <see cref="IGPluginKind.Native"/>: the shared library (e.g. <c>MyPlugin.dll</c>, <c>libMyPlugin.so</c>, <c>MyPlugin.dylib</c>).</item>
    /// </list>
    /// Required for both kinds. <see cref="Kind"/> determines how the file is launched/loaded.
    /// </summary>
    public required string Executable { get; init; }


    /// <summary>
    /// Gets the plugin manifest filename.
    /// </summary>
    public static string FILE_NAME { get; } = "imageglass.plugin.json";

}
