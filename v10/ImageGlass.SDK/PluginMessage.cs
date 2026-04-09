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

namespace ImageGlass.SDK;

/// <summary>
/// Wire-format envelope for all host↔plugin messages.
/// Each message is serialized as a single JSON line terminated by <c>\n</c>.
/// </summary>
public sealed class PluginMessage
{
    /// <summary>
    /// Message type identifier (ALL_CAP_SNAKE_CASE).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Correlates a plugin request with the host's response.
    /// Null for one-way events.
    /// </summary>
    public int? RequestId { get; init; }

    /// <summary>
    /// Type-specific payload data.
    /// </summary>
    public JsonElement? Payload { get; init; }
}
