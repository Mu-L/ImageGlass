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
namespace ImageGlass.Common.Types;

/// <summary>
/// Directory name constants
/// </summary>
public static class Dir
{
    /// <summary>
    /// Gets the theme pack folder name.
    /// </summary>
    public static string Themes { get; } = "_themes";

    /// <summary>
    /// Gets the extension icon folder name.
    /// </summary>
    public static string ExtIcons { get; } = "_ext_icons";

    /// <summary>
    /// Gets the language folder name.
    /// </summary>
    public static string Language { get; } = "_lang";

    /// <summary>
    /// Gets the external plugins folder name.
    /// </summary>
    public static string Plugins { get; } = "_plugins";

    /// <summary>
    /// Gets the credit folder name.
    /// </summary>
    public static string Credits { get; } = "_credits";

    /// <summary>
    /// Gets the temporary folder name.
    /// </summary>
    public static string Temporary { get; } = "_temp";


#if DEBUG
    /// <summary>
    /// Logging should not be to the temporary folder, as it is deleted on shutdown
    /// </summary>
    public static string Log { get; } = "_log";
#endif

}
