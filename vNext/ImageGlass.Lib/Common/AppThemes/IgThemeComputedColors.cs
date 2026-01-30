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
using Avalonia.Media;
using ImageGlass.Common.Types;

namespace ImageGlass.Common.AppThemes;


public partial class IgThemeComputedColors : PhReactive
{
    // Viewer
    public Color TextColor { get; private set; } = new();
    public Color BgColor { get; private set; } = new();
    public Color NavigationButtonColor { get; private set; } = new();


    // Toolbar
    public Color ToolbarBgColor { get; private set; } = new();
    public Color ToolbarTextColor { get; private set; } = new();
    public Color ToolbarItemHoverColor { get; private set; } = new();
    public Color ToolbarItemActiveColor { get; private set; } = new();
    public Color ToolbarItemSelectedColor { get; private set; } = new();


    // Gallery
    public Color GalleryBgColor { get; private set; } = new();
    public Color GalleryTextColor { get; private set; } = new();
    public Color GalleryItemHoverColor { get; private set; } = new();
    public Color GalleryItemActiveColor { get; private set; } = new();
    public Color GalleryItemSelectedColor { get; private set; } = new();


    // Menu
    public Color MenuBgColor { get; private set; } = new();
    public Color MenuBgHoverColor { get; private set; } = new();
    public Color MenuBgActiveColor { get; private set; } = new();
    public Color MenuTextColor { get; private set; } = new();
    public Color MenuTextHoverColor { get; private set; } = new();




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
        color = BHelper.ColorFromHex(colors.NavigationButtonColor, accent);
        if (NavigationButtonColor != color)
        {
            NavigationButtonColor = color;
            OnPropertyChanged(nameof(NavigationButtonColor));
        }


        // Toolbar
        color = BHelper.ColorFromHex(colors.ToolbarBgColor, accent);
        if (ToolbarBgColor != color)
        {
            ToolbarBgColor = color;
            OnPropertyChanged(nameof(ToolbarBgColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarTextColor, accent);
        if (ToolbarTextColor != color)
        {
            ToolbarTextColor = color;
            OnPropertyChanged(nameof(ToolbarTextColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarItemHoverColor, accent);
        if (ToolbarItemHoverColor != color)
        {
            ToolbarItemHoverColor = color;
            OnPropertyChanged(nameof(ToolbarItemHoverColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarItemActiveColor, accent);
        if (ToolbarItemActiveColor != color)
        {
            ToolbarItemActiveColor = color;
            OnPropertyChanged(nameof(ToolbarItemActiveColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarItemSelectedColor, accent);
        if (ToolbarItemSelectedColor != color)
        {
            ToolbarItemSelectedColor = color;
            OnPropertyChanged(nameof(ToolbarItemSelectedColor));
        }


        // Gallery
        color = BHelper.ColorFromHex(colors.GalleryBgColor, accent);
        if (GalleryBgColor != color)
        {
            GalleryBgColor = color;
            OnPropertyChanged(nameof(GalleryBgColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryTextColor, accent);
        if (GalleryTextColor != color)
        {
            GalleryTextColor = color;
            OnPropertyChanged(nameof(GalleryTextColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryItemHoverColor, accent);
        if (GalleryItemHoverColor != color)
        {
            GalleryItemHoverColor = color;
            OnPropertyChanged(nameof(GalleryItemHoverColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryItemActiveColor, accent);
        if (GalleryItemActiveColor != color)
        {
            GalleryItemActiveColor = color;
            OnPropertyChanged(nameof(GalleryItemActiveColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryItemSelectedColor, accent);
        if (GalleryItemSelectedColor != color)
        {
            GalleryItemSelectedColor = color;
            OnPropertyChanged(nameof(GalleryItemSelectedColor));
        }


        // Menu
        color = BHelper.ColorFromHex(colors.MenuBgColor, accent);
        if (MenuBgColor != color)
        {
            MenuBgColor = color;
            OnPropertyChanged(nameof(MenuBgColor));
        }
        color = BHelper.ColorFromHex(colors.MenuBgHoverColor, accent);
        if (MenuBgHoverColor != color)
        {
            MenuBgHoverColor = color;
            OnPropertyChanged(nameof(MenuBgHoverColor));
        }
        color = BHelper.ColorFromHex(colors.MenuBgActiveColor, accent);
        if (MenuBgActiveColor != color)
        {
            MenuBgActiveColor = color;
            OnPropertyChanged(nameof(MenuBgActiveColor));
        }
        color = BHelper.ColorFromHex(colors.MenuTextColor, accent);
        if (MenuTextColor != color)
        {
            MenuTextColor = color;
            OnPropertyChanged(nameof(MenuTextColor));
        }
        color = BHelper.ColorFromHex(colors.MenuTextHoverColor, accent);
        if (MenuTextHoverColor != color)
        {
            MenuTextHoverColor = color;
            OnPropertyChanged(nameof(MenuTextHoverColor));
        }

    }


}
