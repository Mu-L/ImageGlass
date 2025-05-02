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

using ImageMagick;
using System.Numerics;

namespace ImageGlass.Common;

public static class Magick_Exts
{

    /// <summary>
    /// Converts the current <see cref="MagickColor"/>
    /// to a <see cref="Vector4"/> <c>(R = X, G = Y, B = Z, A = W)</c>.
    /// </summary>
    public static Vector4 ToVector4(this MagickColor self)
    {
        return new Vector4(self.R, self.G, self.B, self.A);
    }

}
