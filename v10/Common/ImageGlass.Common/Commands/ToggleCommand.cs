/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
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
using System.Text.Json.Serialization;

namespace ImageGlass.Common;


[JsonSerializable(typeof(ToggleCommand))]
public partial class ToggleCommandJsonContext : JsonSerializerContext { }


public partial class ToggleCommand : IgReactive
{
    /// <summary>
    /// Gets the manager to check whether the <see cref="ToggleCommand"/>
    /// value is on (<c>true</c>) or off (<c>false</c>).
    /// </summary>
    private static readonly Dictionary<Guid, bool> _manager = [];


    /// <summary>
    /// Gets the id of the action for toggling.
    /// </summary>
    [JsonIgnore]
    public Guid Id { get; init; } = Guid.NewGuid();


    /// <summary>
    /// Action to run when toggling on.
    /// </summary>
    public SingleCommand? ToggleOn { get; set; } = null;


    /// <summary>
    /// Action to run when toggling off.
    /// </summary>
    public SingleCommand? ToggleOff { get; set; } = null;


    public ToggleCommand(SingleCommand? toggleOn = null, SingleCommand? toggleOff = null)
    {
        ToggleOn = toggleOn;
        ToggleOff = toggleOff;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override string ToString()
    {
        return $"{ToggleOn?.ToString() ?? "<empty>"} | {ToggleOff?.ToString() ?? "<empty>"}";
    }


    #region Static Methods

    /// <summary>
    /// Checks if the given command is off.
    /// </summary>
    public static bool IsToggleOff(Guid cmdId)
    {
        if (_manager.TryGetValue(cmdId, out var isToggled))
        {
            return isToggled;
        }

        return false;
    }


    /// <summary>
    /// Sets the toggling value of the given command.
    /// </summary>
    public static void SetToggleValue(Guid cmdId, bool isToggled)
    {
        if (!_manager.TryAdd(cmdId, isToggled))
        {
            _manager[cmdId] = isToggled;
        }
    }

    #endregion // Static Methods

}



