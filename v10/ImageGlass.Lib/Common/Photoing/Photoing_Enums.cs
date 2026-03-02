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
using System;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Color profile options.
/// </summary>
public enum ColorProfileOption
{
    None,
    Custom,
    CurrentMonitorProfile,

    // ImageMagick's profiles
    AdobeRGB1998,
    AppleRGB,
    CoatedFOGRA39,
    ColorMatchRGB,
    sRGB,
    USWebCoatedSWOP,
}


/// <summary>
/// Flip options.
/// </summary>
[Flags]
public enum FlipOptions
{
    None = 0,
    Horizontal = 1 << 1,
    Vertical = 1 << 2,
}


/// <summary>
/// Rotate option.
/// </summary>
public enum RotateOption
{
    Left = 0,
    Right = 1,
}


/// <summary>
/// Color channels
/// </summary>
[Flags]
public enum ColorChannels
{
    R = 1 << 1,
    G = 1 << 2,
    B = 1 << 3,
    A = 1 << 4,

    RGB = R | G | B,
    RGBA = RGB | A,
    RA = R | A,
    GA = G | A,
    BA = B | A,
}
