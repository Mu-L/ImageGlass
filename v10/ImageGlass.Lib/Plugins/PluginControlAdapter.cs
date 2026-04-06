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

namespace ImageGlass.Plugins;


/// <summary>
/// Wraps a <see cref="PluginControlFactory"/> as an <see cref="IPlugin"/> for registry compatibility.
/// The factory is invoked each time a hosted plugin needs to be opened.
/// </summary>
public sealed class PluginControlAdapter : IPlugin
{
    private readonly PluginControlFactory _factory;

    public string PluginId { get; }
    public bool IsHosted => true;

    // Settings and Viewer are not used on the adapter itself;
    // they are set on the IPluginControl instance created by the factory.
    public object? Settings => null;
    public ViewerControl Viewer { get; set; } = null!;

    public PluginControlAdapter(string pluginId, PluginControlFactory factory)
    {
        PluginId = pluginId;
        _factory = factory;
    }

    /// <summary>
    /// Creates a new hosted plugin instance.
    /// </summary>
    public IPluginControl CreatePluginControl(ViewerControl viewer) => _factory(viewer);
}