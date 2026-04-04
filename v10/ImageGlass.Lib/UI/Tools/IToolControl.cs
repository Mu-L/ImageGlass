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

namespace ImageGlass.UI;

public interface IToolControl
{
    /// <summary>
    /// Gets the ID of the tool.
    /// </summary>
    string ToolId { get; }


    /// <summary>
    /// Gets, sets settings for this tool, written in app's config file.
    /// </summary>
    object? Settings { get; }


    /// <summary>
    /// Gets the value indicates that the tool contains settings UI,
    /// that can be open with <see cref="ShowSettingsWindowAsync"/>.
    /// </summary>
    bool HasSettingsUI { get; }


    /// <summary>
    /// Gets the instance of Viewer control.
    /// </summary>
    ViewerControl Viewer { get; init; }


    /// <summary>
    /// Shows the tool settings window.
    /// </summary>
    Task ShowSettingsWindowAsync() => Task.CompletedTask;


    /// <summary>
    /// Loads and parses tool settings from JSON element.
    /// </summary>
    void LoadSettings(JsonElement? jsonEl) { }


    /// <summary>
    /// Saves the tool settings as JSON element.
    /// </summary>
    JsonElement? SaveSettings() => null;

}
