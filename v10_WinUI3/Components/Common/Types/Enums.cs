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
namespace ImageGlass.Common;

/// <summary>
/// Window backdrop effect.
/// </summary>
public enum BackdropStyle
{
    Mica,
    MicaAlt,
    Acrylic,
    AcrylicThin,
    Transparent,

    /// <summary>
    /// No backdrop.
    /// </summary>
    None,
}


/// <summary>
/// Exit codes of ImageGlass ultilities
/// </summary>
public enum IgExitCode
{
    Done = 0,
    AdminRequired = 1,
    Error = 2,
    Error_FileNotFound = 3,
}


/// <summary>
/// Determines Windows OS requirement
/// </summary>
public enum WindowsOS
{
    /// <summary>
    /// Build 22621
    /// </summary>
    Win11_22H2_OrLater,

    /// <summary>
    /// Build 22000
    /// </summary>
    Win11OrLater,
    Win10,
    Win10OrLater,
}


/// <summary>
/// Options indicate what source of image is saved.
/// </summary>
public enum ImageSaveSource
{
    Undefined,
    SelectedArea,
    Clipboard,
    CurrentFile,
}


public enum PhotoCodec
{
    None,
    Magick,
    WIC,
}

