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
using Avalonia.Media;
using ImageGlass.Common.Types;

namespace ImageGlass.Common.AppThemes;


public partial class IgThemeComputedColors : PhReactive
{
    // Viewer
    public Color TextColor { get; private set; } = new();
    public Color BgColor { get; private set; } = new();
    public Color ToolbarBgColor { get; private set; } = new();
    public Color GalleryBgColor { get; private set; } = new();
    public Color MenuBgColor { get; private set; } = new();



    /// <summary>
    /// Initializes new instance of <see cref="IgThemeComputedColors"/>.
    /// </summary>
    public IgThemeComputedColors(IgThemeColors? themeColors = null)
    {
        Load(themeColors);
    }


    /// <summary>
    /// Computes colors from the color strings.
    /// </summary>
    public void Load(IgThemeColors? colors = null, Color? accent = null)
    {
        if (colors is null) return;


        // Viewer
        var color = BHelper.ColorFromHex(colors.TextColor, accent);
        if (TextColor != color)
        {
            TextColor = color;
            OnPropertyChanged(nameof(TextColor));
        }
        color = BHelper.ColorFromHex(colors.BgColor, accent);
        if (BgColor != color)
        {
            BgColor = color;
            OnPropertyChanged(nameof(BgColor));
        }


        // Toolbar
        color = BHelper.ColorFromHex(colors.ToolbarBgColor, accent);
        if (ToolbarBgColor != color)
        {
            ToolbarBgColor = color;
            OnPropertyChanged(nameof(ToolbarBgColor));
        }


        // Gallery
        color = BHelper.ColorFromHex(colors.GalleryBgColor, accent);
        if (GalleryBgColor != color)
        {
            GalleryBgColor = color;
            OnPropertyChanged(nameof(GalleryBgColor));
        }


        // Menu
        color = BHelper.ColorFromHex(colors.MenuBgColor, accent);
        if (MenuBgColor != color)
        {
            MenuBgColor = color;
            OnPropertyChanged(nameof(MenuBgColor));
        }

    }


}
