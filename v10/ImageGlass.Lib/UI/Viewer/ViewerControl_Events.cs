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
using Avalonia;
using Avalonia.Input;
using System;

namespace ImageGlass.UI.Viewer;

public class ViewerPointerEventArgs(PointerEventArgs e, PointerPoint p, PixelPoint srcPoint) : EventArgs
{
    /// <summary>
    /// Gets the pointer event data associated with the current input event.
    /// </summary>
    public PointerEventArgs Event => e;

    /// <summary>
    /// Gets pointer point.
    /// </summary>
    public PointerPoint Point => p;

    /// <summary>
    /// Gets the corresponding source image coordinates
    /// for the current position within the viewer control.
    /// It can be out of the image bounds.
    /// </summary>
    public PixelPoint SourcePoint => srcPoint;

}
