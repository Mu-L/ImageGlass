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
using Windows.UI;

namespace ImageGlass.Common;


public partial class IgThemeComputedColors : IgReactive
{
    // Viewer
    public Color TextColor { get; set; } = new();
    public Color BgColor { get; set; } = new();
    public Color NavigationButtonColor { get; set; } = new();

    // Toolbar
    public Color ToolbarBgColor { get; set; } = new();
    public Color ToolbarTextColor { get; set; } = new();
    public Color ToolbarItemHoverColor { get; set; } = new();
    public Color ToolbarItemActiveColor { get; set; } = new();
    public Color ToolbarItemSelectedColor { get; set; } = new();

    // Gallery
    public Color GalleryBgColor { get; set; } = new();
    public Color GalleryTextColor { get; set; } = new();
    public Color GalleryItemHoverColor { get; set; } = new();
    public Color GalleryItemActiveColor { get; set; } = new();
    public Color GalleryItemSelectedColor { get; set; } = new();

    // Menu
    public Color MenuBgColor { get; set; } = new();
    public Color MenuBgHoverColor { get; set; } = new();
    public Color MenuBgActiveColor { get; set; } = new();
    public Color MenuTextColor { get; set; } = new();
    public Color MenuTextHoverColor { get; set; } = new();




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
    public void Load(IgThemeColors? colors = null, Color? accentColor = null)
    {
        if (colors is null) return;


        // Viewer
        var color = BHelper.ColorFromHex(colors.TextColor, accentColor);
        if (TextColor != color)
        {
            TextColor = color;
            OnPropertyChanged(nameof(TextColor));
        }
        color = BHelper.ColorFromHex(colors.BgColor, accentColor);
        if (BgColor != color)
        {
            BgColor = color;
            OnPropertyChanged(nameof(BgColor));
        }
        color = BHelper.ColorFromHex(colors.NavigationButtonColor, accentColor);
        if (NavigationButtonColor != color)
        {
            NavigationButtonColor = color;
            OnPropertyChanged(nameof(NavigationButtonColor));
        }


        // Toolbar
        color = BHelper.ColorFromHex(colors.ToolbarBgColor, accentColor);
        if (ToolbarBgColor != color)
        {
            ToolbarBgColor = color;
            OnPropertyChanged(nameof(ToolbarBgColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarTextColor, accentColor);
        if (ToolbarTextColor != color)
        {
            ToolbarTextColor = color;
            OnPropertyChanged(nameof(ToolbarTextColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarItemHoverColor, accentColor);
        if (ToolbarItemHoverColor != color)
        {
            ToolbarItemHoverColor = color;
            OnPropertyChanged(nameof(ToolbarItemHoverColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarItemActiveColor, accentColor);
        if (ToolbarItemActiveColor != color)
        {
            ToolbarItemActiveColor = color;
            OnPropertyChanged(nameof(ToolbarItemActiveColor));
        }
        color = BHelper.ColorFromHex(colors.ToolbarItemSelectedColor, accentColor);
        if (ToolbarItemSelectedColor != color)
        {
            ToolbarItemSelectedColor = color;
            OnPropertyChanged(nameof(ToolbarItemSelectedColor));
        }


        // Gallery
        color = BHelper.ColorFromHex(colors.GalleryBgColor, accentColor);
        if (GalleryBgColor != color)
        {
            GalleryBgColor = color;
            OnPropertyChanged(nameof(GalleryBgColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryTextColor, accentColor);
        if (GalleryTextColor != color)
        {
            GalleryTextColor = color;
            OnPropertyChanged(nameof(GalleryTextColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryItemHoverColor, accentColor);
        if (GalleryItemHoverColor != color)
        {
            GalleryItemHoverColor = color;
            OnPropertyChanged(nameof(GalleryItemHoverColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryItemActiveColor, accentColor);
        if (GalleryItemActiveColor != color)
        {
            GalleryItemActiveColor = color;
            OnPropertyChanged(nameof(GalleryItemActiveColor));
        }
        color = BHelper.ColorFromHex(colors.GalleryItemSelectedColor, accentColor);
        if (GalleryItemSelectedColor != color)
        {
            GalleryItemSelectedColor = color;
            OnPropertyChanged(nameof(GalleryItemSelectedColor));
        }


        // Menu
        color = BHelper.ColorFromHex(colors.MenuBgColor, accentColor);
        if (MenuBgColor != color)
        {
            MenuBgColor = color;
            OnPropertyChanged(nameof(MenuBgColor));
        }
        color = BHelper.ColorFromHex(colors.MenuBgHoverColor, accentColor);
        if (MenuBgHoverColor != color)
        {
            MenuBgHoverColor = color;
            OnPropertyChanged(nameof(MenuBgHoverColor));
        }
        color = BHelper.ColorFromHex(colors.MenuBgActiveColor, accentColor);
        if (MenuBgActiveColor != color)
        {
            MenuBgActiveColor = color;
            OnPropertyChanged(nameof(MenuBgActiveColor));
        }
        color = BHelper.ColorFromHex(colors.MenuTextColor, accentColor);
        if (MenuTextColor != color)
        {
            MenuTextColor = color;
            OnPropertyChanged(nameof(MenuTextColor));
        }
        color = BHelper.ColorFromHex(colors.MenuTextHoverColor, accentColor);
        if (MenuTextHoverColor != color)
        {
            MenuTextHoverColor = color;
            OnPropertyChanged(nameof(MenuTextHoverColor));
        }

    }


}
