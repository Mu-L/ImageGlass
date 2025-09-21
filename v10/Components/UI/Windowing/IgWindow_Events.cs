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
using Microsoft.UI.Windowing;
using System;
using Windows.Foundation;

namespace ImageGlass.UI;


public class WindowStateChangedEventArgs() : EventArgs
{
    public Rect Bounds { get; internal set; } = new();
    public Rect OldBounds { get; internal set; } = new();
    public OverlappedPresenterState State { get; internal set; }
    public OverlappedPresenterState OldState { get; internal set; }
}


public class WindowIconChangedEventArgs(byte[]? iconData, int iconSize) : EventArgs
{
    public byte[]? IconData { get; set; } = iconData;
    public int Size { get; set; } = iconSize;
}

