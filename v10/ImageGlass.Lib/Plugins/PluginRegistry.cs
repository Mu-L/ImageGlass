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
using ImageGlass.Common;
using ImageGlass.Plugins.External;
using ImageGlass.SDK;
using ImageGlass.UI.Viewer;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageGlass.Plugins;


/// <summary>
/// Factory delegate that creates an <see cref="IPluginControl"/> hosted plugin instance.
/// </summary>
public delegate IPluginControl PluginControlFactory(ViewerControl viewer);


/// <summary>
/// Central registry for all plugins (hosted and non-hosted).
/// Built-in plugins register during <see cref="ImageGlass.Common.ServiceProviders.AppAPIProvider"/> construction.
/// </summary>
public sealed class PluginRegistry
{
    private readonly Dictionary<string, IPlugin> _plugins = new();


    /// <summary>
    /// Registers a plugin.
    /// </summary>
    public void Register(string pluginId, IPlugin plugin)
    {
        _plugins[pluginId] = plugin;
    }


    /// <summary>
    /// Gets the plugin for a plugin ID, or null if not found.
    /// </summary>
    public IPlugin? Get(string pluginId)
    {
        _plugins.TryGetValue(pluginId, out var plugin);
        return plugin;
    }


    /// <summary>
    /// Checks whether a plugin ID is registered.
    /// </summary>
    public bool Contains(string pluginId) => _plugins.ContainsKey(pluginId);


    /// <summary>
    /// Gets all registered external plugin proxies for menu building.
    /// </summary>
    public IEnumerable<PluginManifest> GetAllExternalPluginManifests()
    {
        foreach (var plugin in _plugins.Values)
        {
            if (plugin is ExternalPluginProxy proxy)
                yield return proxy.Manifest;
        }
    }


    /// <summary>
    /// Gets all registered plugin IDs.
    /// </summary>
    public IEnumerable<string> GetAllPluginIds() => _plugins.Keys;


    /// <summary>
    /// Loads settings for a plugin from the app config.
    /// </summary>
    public static void LoadPluginSettings(IPlugin plugin)
    {
        JsonElement? jsonEl = Core.Config.PluginSettings.TryGetValue(plugin.PluginId, out var el)
            ? el
            : null;

        plugin.LoadSettings(jsonEl);
    }


    /// <summary>
    /// Saves settings for a plugin to the app config.
    /// </summary>
    public static void SavePluginSettings(IPlugin plugin)
    {
        var jsonEl = plugin.SaveSettings();

        if (jsonEl is null)
        {
            Core.Config.PluginSettings.Remove(plugin.PluginId);
        }
        else
        {
            Core.Config.PluginSettings[plugin.PluginId] = jsonEl.Value;
        }
    }


    /// <summary>
    /// Executes a non-hosted plugin and saves its settings on completion.
    /// </summary>
    public static async Task ExecuteNonHostedPluginAsync(IPlugin plugin, PluginExecutionContext context)
    {
        try
        {
            await plugin.ExecuteAsync(context);
        }
        finally
        {
            SavePluginSettings(plugin);
        }
    }


}
