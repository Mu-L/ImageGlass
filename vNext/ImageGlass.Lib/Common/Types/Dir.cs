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
namespace ImageGlass.Common.Types;

/// <summary>
/// Directory name constants
/// </summary>
public static class Dir
{
    /// <summary>
    /// Gets the Themes folder name
    /// </summary>
    public static string Themes => "Themes";

    /// <summary>
    /// Gets the Icons folder name
    /// </summary>
    public static string Icons => "Icons";

    /// <summary>
    /// Gets the Ext-Icons folder name
    /// </summary>
    public static string ExtIcons => "Ext-Icons";

    /// <summary>
    /// Gets the Languages folder name
    /// </summary>
    public static string Language => "Language";

    /// <summary>
    /// Gets the WebUI folder name
    /// </summary>
    public static string WebUI => "WebUI";

    /// <summary>
    /// Gets the WebView2_Runtime folder.
    /// </summary>
    public static string WebView2Runtime => "WebView2_Runtime";

    /// <summary>
    /// Gets the cached thumbnails folder name
    /// </summary>
    public static string ThumbnailsCache => "ThumbnailsCache";

    /// <summary>
    /// Gets the License folder name
    /// </summary>
    public static string License => "License";

    /// <summary>
    /// Gets the temporary folder name
    /// </summary>
    public static string Temporary => "Temp";

#if DEBUG
    /// <summary>
    /// Logging should not be to the temporary folder, as it is deleted on shutdown
    /// </summary>
    public static string Log => "Log";
#endif

}
