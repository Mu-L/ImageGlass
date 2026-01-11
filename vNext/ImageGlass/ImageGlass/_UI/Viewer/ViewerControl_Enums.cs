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
namespace ImageGlass._UI.Viewer;


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
/// These values are based on <see cref="Avalonia.Media.Imaging.BitmapInterpolationMode"/>.
/// </summary>
public enum ImageInterpolation : int
{
    //
    // Summary:
    //     Disable interpolation.
    None = 1,
    //
    // Summary:
    //     The best performance but worst image quality.
    LowQuality = 2,
    //
    // Summary:
    //     Good performance and decent image quality.
    MediumQuality = 3,
    //
    // Summary:
    //     Highest quality but worst performance.
    HighQuality = 4,
}

