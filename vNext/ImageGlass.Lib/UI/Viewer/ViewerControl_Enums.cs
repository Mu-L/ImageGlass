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
using System;

namespace ImageGlass.UI.Viewer;


public enum PhotoSource
{
    None,
    Native,
    Webview2,
}


/// <summary>
/// Specifies the display styles for the background texture grid
/// </summary>
public enum CheckerboardType
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


[Flags]
public enum AnimationSources
{
    None = 0,

    PanLeft = 1 << 1,
    PanRight = 1 << 2,
    PanUp = 1 << 3,
    PanDown = 1 << 4,

    /// <summary>
    /// Zoom in animation. It does nothing if <see cref="ViewerCanvas.ZoomLevels"/> is set.
    /// </summary>
    ZoomIn = 1 << 5,
    /// <summary>
    /// Zoom out animation. It does nothing if <see cref="ViewerCanvas.ZoomLevels"/> is set.
    /// </summary>
    ZoomOut = 1 << 6,
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
    None = 0,
    //
    // Summary:
    //     The best performance but worst image quality. 
    Low = 2,
    //
    // Summary:
    //     Good performance and decent image quality.
    Medium = 3,
    //
    // Summary:
    //     Highest quality but worst performance.
    High = 4,
}


public class AnimationSourceArgs(AnimationSources source, Action? callbackFn)
{
    public AnimationSources Source => source;
    public Action? CallbackFn => callbackFn;
}

