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

using SharpGen.Runtime;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ImageGlass.Common;


public static class Vortice_Exts
{

    /// <summary>
    /// Checks if the object is <c>null</c> or disposed.
    /// </summary>
    public static bool IsDisposed([NotNullWhen(false)] this CppObject? self)
    {
        return self == null || self?.NativePointer == IntPtr.Zero;
    }

}
