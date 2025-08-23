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
using System;
using System.Globalization;
using Windows.UI;

namespace ImageGlass.Win64.Common;

public partial class WHelper
{

    /// <summary>
    /// Creates a new <see cref="Color"/> from the given hex color string (with alpha).
    /// Returns <c>Color(0,0,0,0)</c> if the <paramref name="hex"/> is invalid.
    /// </summary>
    public static Color ColorFromHex(string hex, bool skipAlpha = false)
    {
        var color = new Color();
        if (string.IsNullOrWhiteSpace(hex)) return color;

        // remove # if present
        hex = hex.TrimStart('#');

        try
        {
            if (hex.Length == 8)
            {
                // #RRGGBBAA
                color.R = byte.Parse(hex.AsSpan(0, 2), NumberStyles.AllowHexSpecifier);
                color.G = byte.Parse(hex.AsSpan(2, 2), NumberStyles.AllowHexSpecifier);
                color.B = byte.Parse(hex.AsSpan(4, 2), NumberStyles.AllowHexSpecifier);
                color.A = byte.Parse(hex.AsSpan(6, 2), NumberStyles.AllowHexSpecifier);
            }
            else if (hex.Length == 6)
            {
                // #RRGGBB
                color.R = byte.Parse(hex.AsSpan(0, 2), NumberStyles.AllowHexSpecifier);
                color.G = byte.Parse(hex.AsSpan(2, 2), NumberStyles.AllowHexSpecifier);
                color.B = byte.Parse(hex.AsSpan(4, 2), NumberStyles.AllowHexSpecifier);
                color.A = 255;
            }
            else if (hex.Length == 4)
            {
                // #RGBA
                color.R = byte.Parse($"{hex[0]}{hex[0]}", NumberStyles.AllowHexSpecifier);
                color.G = byte.Parse($"{hex[1]}{hex[1]}", NumberStyles.AllowHexSpecifier);
                color.B = byte.Parse($"{hex[2]}{hex[2]}", NumberStyles.AllowHexSpecifier);
                color.A = byte.Parse($"{hex[3]}{hex[3]}", NumberStyles.AllowHexSpecifier);
            }
            else if (hex.Length == 3)
            {
                // #RGB
                color.R = byte.Parse($"{hex[0]}{hex[0]}", NumberStyles.AllowHexSpecifier);
                color.G = byte.Parse($"{hex[1]}{hex[1]}", NumberStyles.AllowHexSpecifier);
                color.B = byte.Parse($"{hex[2]}{hex[2]}", NumberStyles.AllowHexSpecifier);
                color.A = 255;
            }
            else
            {
                return color;
            }

            if (skipAlpha) color.A = 255;
        }
        catch { }

        return color;
    }

}
