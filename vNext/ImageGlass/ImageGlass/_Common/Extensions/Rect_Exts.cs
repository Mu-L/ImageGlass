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
using Avalonia;

namespace ImageGlass.Common.Extensions;


public static class Rect_Exts
{

    extension(Rect rect)
    {
        /// <summary>
        /// Checks if the specified rectangle has no area.
        /// </summary>
        public bool IsEmpty => rect.Size.IsEmpty;


        /// <summary>
        /// Ensures the rectangle location and size values are finite numbers.
        /// </summary>
        public Rect Normalize()
        {
            var x = double.IsFinite(rect.X) ? rect.X : 0;
            var y = double.IsFinite(rect.Y) ? rect.Y : 0;
            var w = double.IsFinite(rect.Width) ? rect.Width : 0;
            var h = double.IsFinite(rect.Height) ? rect.Height : 0;

            return new Rect(x, y, w, h);
        }


        /// <summary>
        /// Converts the given rectangle to double array.
        /// </summary>
        public double[] ToArray()
        {
            return [rect.X, rect.Y, rect.Width, rect.Height];
        }


    }


}
