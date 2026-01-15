/*
ImageGlass Project - Image viewer for Windows
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
using Avalonia.Input;
using ImageGlass.Common.Types.JsonTypeConverters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ImageGlass.Common.Types;


[JsonConverter(typeof(JsonStringToHotkeyConverter))]
public class Hotkey
{
    private const string KEY_STR_CTRL = "Ctrl";
    private const string KEY_STR_SHIFT = "Shift";
    private const string KEY_STR_ALT = "Alt";


    /// <summary>
    /// Gets, sets the virtual key.
    /// </summary>
    public Key Key { get; set; } = Key.None;

    /// <summary>
    /// Gets, sets the key modifiers.
    /// </summary>
    public KeyModifiers Modifiers { get; set; } = KeyModifiers.None;
    public bool Control => Modifiers.HasFlag(KeyModifiers.Control);
    public bool Shift => Modifiers.HasFlag(KeyModifiers.Shift);
    public bool Alt => Modifiers.HasFlag(KeyModifiers.Alt);

    public string KeyString => ToString(this);



    public Hotkey() { }

    public Hotkey(Key key)
    {
        Key = key;
    }

    public Hotkey(KeyModifiers modifiers, Key key)
    {
        Modifiers = modifiers;
        Key = key;
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
            var keySpan = s
                .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .AsSpan();

            foreach (var str in keySpan)
            {
                if (str.Equals(KEY_STR_CTRL, StringComparison.InvariantCultureIgnoreCase))
                {
                    hotkey.Modifiers |= KeyModifiers.Control;
                }
                else if (str.Equals(KEY_STR_SHIFT, StringComparison.InvariantCultureIgnoreCase))
                {
                    hotkey.Modifiers |= KeyModifiers.Shift;
                }
                else if (str.Equals(KEY_STR_ALT, StringComparison.InvariantCultureIgnoreCase))
                {
                    hotkey.Modifiers |= KeyModifiers.Alt;
                }
                else if (Enum.TryParse<Key>(str, true, out var vKey))
                {
                    hotkey.Key = vKey;
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
        if (hotkey.Modifiers.HasFlag(KeyModifiers.Control)) modifiers.Add(KEY_STR_CTRL);
        if (hotkey.Modifiers.HasFlag(KeyModifiers.Shift)) modifiers.Add(KEY_STR_SHIFT);
        if (hotkey.Modifiers.HasFlag(KeyModifiers.Alt)) modifiers.Add(KEY_STR_ALT);

        modifiers.Add(hotkey.Key.ToString());

        return string.Join('+', modifiers);
    }

}
