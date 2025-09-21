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
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Windows.System;

namespace ImageGlass.Common;


[JsonConverter(typeof(JsonStringToHotkeyConverter))]
public class Hotkey
{
    /// <summary>
    /// Gets, sets the keyboard accelerator.
    /// </summary>
    public KeyboardAccelerator Data { get; private set; } = new();


    /// <summary>
    /// Gets, sets the command of the hotkey.
    /// </summary>
    public IIgCommand? Command { get; set; } = null;


    public bool Control => Data.Modifiers.HasFlag(VirtualKeyModifiers.Control);
    public bool Shift => Data.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
    public bool Alt => Data.Modifiers.HasFlag(VirtualKeyModifiers.Menu);
    public VirtualKey Key => Data.Key;
    public string KeyString => ToString(this);



    public Hotkey() { }

    public Hotkey(KeyboardAccelerator accel)
    {
        Data = new KeyboardAccelerator()
        {
            Modifiers = accel.Modifiers,
            Key = accel.Key,
        };
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override string ToString() => KeyString;


    /// <summary>
    /// Parses string to <see cref="Hotkey"/> instance.
    /// </summary>
    public static Hotkey? ParseFrom(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        var hotkey = new Hotkey();

        try
        {
            var keySpan = s.ToLowerInvariant()
                .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .AsSpan();

            foreach (var str in keySpan)
            {
                if (str.Equals("ctrl"))
                {
                    hotkey.Data.Modifiers |= VirtualKeyModifiers.Control;
                }
                else if (str.Equals("shift"))
                {
                    hotkey.Data.Modifiers |= VirtualKeyModifiers.Shift;
                }
                else if (str.Equals("alt"))
                {
                    hotkey.Data.Modifiers |= VirtualKeyModifiers.Menu;
                }
                else if (Enum.TryParse<VirtualKey>(str, true, out var vKey))
                {
                    hotkey.Data.Key = vKey;
                }
            }

            return hotkey;
        }
        catch { }

        return null;
    }


    /// <summary>
    /// Parse <see cref="Hotkey"/> to string.
    /// </summary>
    public static string ToString(Hotkey hotkey)
    {
        var modifiers = new List<string>(4);
        if (hotkey.Data.Modifiers.HasFlag(VirtualKeyModifiers.Control)) modifiers.Add("Ctrl");
        if (hotkey.Data.Modifiers.HasFlag(VirtualKeyModifiers.Shift)) modifiers.Add("Shift");
        if (hotkey.Data.Modifiers.HasFlag(VirtualKeyModifiers.Menu)) modifiers.Add("Alt");

        modifiers.Add(hotkey.Data.Key.ToString());

        return string.Join('+', modifiers);
    }


}
