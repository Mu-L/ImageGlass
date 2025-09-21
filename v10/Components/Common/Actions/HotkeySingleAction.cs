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


[JsonSerializable(typeof(HotkeySingleAction))]
public partial class HotkeySingleActionJsonContext : JsonSerializerContext { }



public partial class HotkeySingleAction : SingleAction, IJsonOnDeserialized
{
    /// <summary>
    /// Gets, sets hotkeys.
    /// </summary>
    public Hotkey[] Hotkeys { get; set; } = [];


    public void OnDeserialized()
    {
        // bind the action for the hotkey
        foreach (var hk in Hotkeys)
        {
            hk.SetAction(this);
        }
    }
}

