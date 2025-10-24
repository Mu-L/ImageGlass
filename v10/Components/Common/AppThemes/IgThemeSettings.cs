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
/// Theme other settings
/// </summary>
public record IgThemeSettings
{
    /// <summary>
    /// Default value is <c>true</c>.
    /// </summary>
    public bool IsDarkMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the thickness of the window frame
    /// </summary>
    public double FrameThickness { get; set; } = 0;

    /// <summary>
    /// Gets, sets the navigation left arrow
    /// </summary>
    public string NavButtonLeft { get; set; } = string.Empty;

    /// <summary>
    /// Gets, sets the navigation right arrow
    /// </summary>
    public string NavButtonRight { get; set; } = string.Empty;

    /// <summary>
    /// Sets, sets app logo
    /// </summary>
    public string AppLogo { get; set; } = string.Empty;

    /// <summary>
    /// The preview image of the theme
    /// </summary>
    public string PreviewImage { get; set; } = string.Empty;
}

