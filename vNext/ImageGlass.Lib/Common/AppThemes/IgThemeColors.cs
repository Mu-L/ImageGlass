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
namespace ImageGlass.Common.AppThemes;


/// <summary>
/// Theme colors
/// </summary>
public class IgThemeColors
{
    public string AccentColor { get; set; } = string.Empty; // use system accent color
    public string TextColor { get; set; } = "#d3d3d3";
    public string BgColor { get; set; } = "#151b1f00";


    public string ToolbarBgColor { get; set; } = "#1E242900";
    public string GalleryBgColor { get; set; } = "#1E242900";
    public string MenuBgColor { get; set; } = "#0000";




    // Legacy theme specs

    public string NavigationButtonColor { get; set; } = "ff000015";


    // Toolbar

    public string ToolbarTextColor { get; set; } = "#dedede";
    public string ToolbarItemHoverColor { get; set; } = "#ffffff33";
    public string ToolbarItemActiveColor { get; set; } = "#ffffff22";
    public string ToolbarItemSelectedColor { get; set; } = "#ffffff44";


    // Gallery

    public string GalleryTextColor { get; set; } = "#dedede";
    public string GalleryItemHoverColor { get; set; } = "#ffffff33";
    public string GalleryItemActiveColor { get; set; } = "#ffffff22";
    public string GalleryItemSelectedColor { get; set; } = "#ffffff44";


    // Menu
    public string MenuBgHoverColor { get; set; } = "#ffffff10";
    public string MenuBgActiveColor { get; set; } = "#ffffff08";
    public string MenuTextColor { get; set; } = "#d3d3d3";
    public string MenuTextHoverColor { get; set; } = "#d3d3d3";

}

