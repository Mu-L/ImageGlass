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
using System.Collections.Generic;

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

}
