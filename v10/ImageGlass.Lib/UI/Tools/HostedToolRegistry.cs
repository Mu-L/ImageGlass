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

namespace ImageGlass.UI;


/// <summary>
/// Factory delegate that creates an <see cref="IToolControl"/> instance
/// bound to the given <see cref="ViewerControl"/>.
/// </summary>
public delegate IToolControl ToolFactory(ViewerControl viewer);


/// <summary>
/// Central registry for hosted tools.
/// Built-in tools register during <see cref="ImageGlass.Common.ServiceProviders.AppAPIProvider"/> construction.
/// Future plugins will register via the same <see cref="Register"/> method.
/// </summary>
public sealed class HostedToolRegistry
{
    // Map of tool factories: <tool_id, tool_factory>
    private readonly Dictionary<string, ToolFactory> _factories = new();


    /// <summary>
    /// Registers a tool factory for the given tool ID.
    /// </summary>
    public void Register(string toolId, ToolFactory factory)
    {
        _factories[toolId] = factory;
    }


    /// <summary>
    /// Creates a new tool instance if <paramref name="toolId"/> is registered.
    /// </summary>
    public IToolControl? CreateTool(string toolId, ViewerControl viewer)
    {
        if (_factories.TryGetValue(toolId, out var factory))
        {
            return factory(viewer);
        }

        return null;
    }


    /// <summary>
    /// Checks whether a tool ID is registered.
    /// </summary>
    public bool Contains(string toolId) => _factories.ContainsKey(toolId);

}
