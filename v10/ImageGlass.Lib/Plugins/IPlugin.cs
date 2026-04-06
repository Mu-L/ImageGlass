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
using ImageGlass.UI.Viewer;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageGlass.Plugins;

/// <summary>
/// Base interface for all plugins in the ImageGlass plugin registry.
/// Contains common members for identity, settings, and viewer access.
/// Hosted plugins (with UI in PluginHostControl) extend via <see cref="IPluginControl"/>.
/// Non-hosted plugins (modal windows, actions) implement this directly.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the unique plugin identifier.
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// Gets whether this plugin is hosted in <see cref="PluginHostControl"/>.
    /// Hosted plugins use <see cref="PluginHostControl"/> for UI.
    /// Non-hosted plugins use <see cref="ExecuteAsync"/> instead.
    /// </summary>
    bool IsHosted { get; }

    /// <summary>
    /// Gets, sets settings for this plugin, written in app's config file.
    /// </summary>
    object? Settings { get; }

    /// <summary>
    /// Gets, sets the instance of Viewer control.
    /// Set by the plugin manager before use.
    /// </summary>
    ViewerControl Viewer { get; set; }

    /// <summary>
    /// Loads and parses plugin settings from JSON element.
    /// Called by the plugin manager before the plugin is opened/executed.
    /// </summary>
    void LoadSettings(JsonElement? jsonEl) { }

    /// <summary>
    /// Saves the plugin settings as JSON element.
    /// Called by the plugin manager when the plugin is closed/completed.
    /// </summary>
    JsonElement? SaveSettings() => null;

    /// <summary>
    /// Executes the non-hosted plugin. Called by <c>IG_OpenPlugin</c> for non-hosted plugins.
    /// Hosted plugins leave the default no-op
    /// (they are opened via <see cref="PluginHostControl"/> instead).
    /// </summary>
    Task ExecuteAsync(PluginExecutionContext context) => Task.CompletedTask;
}
