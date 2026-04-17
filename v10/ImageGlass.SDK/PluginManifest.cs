/*
ImageGlass.SDK – ImageGlass 10 Plugins Development Kit
Copyright (C) 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org
MIT License
*/
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
    /// Executable filename (e.g. "MyPlugin.exe" on Windows, "MyPlugin" on Linux/macOS).
    /// </summary>
    public required string Executable { get; init; }



    /// <summary>
    /// Gets the plugin manifest filename.
    /// </summary>
    public static string FILE_NAME { get; } = "imageglass.plugin.json";

}
