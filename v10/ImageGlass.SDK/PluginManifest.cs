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
