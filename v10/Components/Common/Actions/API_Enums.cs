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
namespace ImageGlass;


public enum API
{
    IG_OpenMainMenu,
    IG_ViewByStep,
    IG_ViewByIndex,


    // Menu > File
    IG_OpenFile,
    IG_OpenFolder,
    IG_OpenPath,
    IG_NewWindow,
    IG_Save,
    IG_SaveAs,

    // Menu > Navigation
    IG_ViewNext,
    IG_ViewPrevious,
    IG_Goto,
    IG_GotoFirst,
    IG_GotoLast,

    // Menu > Zoom
    IG_CustomZoom,
    IG_SetZoom,
    IG_ZoomIn,
    IG_ZoomOut,

    // Menu > Panning
    IG_PanLeft,
    IG_PanRight,
    IG_PanUp,
    IG_PanDown,

    // Menu > Image

    // Menu > Clipboard

    // Menu > Window Fit
    // Menu > Frameless
    // Menu > Fullscreen
    // Menu > Slideshow

    // Menu > Layout
    IG_ToggleCheckerboard,

    // Menu > Tools

    // Menu > Settings

    // Menu > Help

    // Menu > Exit
    IG_Exit,
}

