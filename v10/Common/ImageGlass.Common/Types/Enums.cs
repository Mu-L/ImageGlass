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
/// Window backdrop effect.
/// </summary>
public enum BackdropStyle
{
    /// <summary>
    /// Use default setting of Windows.
    /// </summary>
    None = 0,

    /// <summary>
    /// Mica effect.
    /// </summary>
    Mica = 2,

    /// <summary>
    /// Acrylic effect.
    /// </summary>
    Acrylic = 3,

    /// <summary>
    /// Draw the backdrop material effect corresponding to a window with a tabbed title bar.
    /// </summary>
    MicaAlt = 4,
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
