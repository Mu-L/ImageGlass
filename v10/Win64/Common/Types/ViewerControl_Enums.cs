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

namespace ImageGlass.Common;


/// <summary>
/// Specifies the display styles for the background texture grid
/// </summary>
public enum CheckerboardMode
{
    /// <summary>
    /// No background.
    /// </summary>
    None = 0,

    /// <summary>
    /// Background is displayed in the control's client area.
    /// </summary>
    Client = 1,

    /// <summary>
    /// Background is displayed only in the image region.
    /// </summary>
    Image = 2,
}



public enum ZoomMode
{
    AutoZoom,
    LockZoom,
    ScaleToWidth,
    ScaleToHeight,
    ScaleToFit,
    ScaleToFill,
}


public enum ZoomChangeSource
{
    Unknown,
    ZoomMode,
    SizeChanged,
}


/// <summary>
/// Interpolation modes.
/// These values are based on <see cref="Vortice.Direct2D1.InterpolationMode"/>.
/// </summary>
public enum ImageInterpolation : int
{
    /// <summary>
    /// Pixelated scaling down (poor quality) and up.
    /// </summary>
    NearestNeighbor = 0,

    /// <summary>
    /// Pixelated scaling down (poor quality), smooth scaling up (normal quality).
    /// </summary>
    Linear = 1,

    /// <summary>
    /// Pixelated scaling down (poor quality), smooth scaling up (better quality).
    /// </summary>
    Cubic = 2,

    /// <summary>
    /// Smooth scaling down (the best), smooth scaling up (normal quality).
    /// </summary>
    MultiSampleLinear = 3,

    /// <summary>
    /// Smooth scaling down (normal quality) and up (normal quality).
    /// </summary>
    Antisotropic = 4,

    /// <summary>
    /// Smooth scaling down (normal quality) and up (better quality).
    /// </summary>
    HighQualityBicubic = 5,
}



