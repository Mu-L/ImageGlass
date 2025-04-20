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

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace ImageGlass.WinNT.Common;


public static class Windows_Foundation_Size_Exts
{

    /// <summary>
    /// Checks if the given size has non-positive dimensions or if the dimensions are not finite.
    /// </summary>
    public static bool IsEmpty(this Size size)
    {
        return size.Width <= 0
            || size.Height <= 0
            || !double.IsFinite(size.Width)
            || !double.IsFinite(size.Height);
    }


    /// <summary>
    /// Inflates a Size object by a specified amount, increasing its dimensions.
    /// </summary>
    public static Size Inflate(this Size size, double thickness)
    {
        return size.Inflate(new Thickness(thickness));
    }


    /// <summary>
    /// Inflates a Size object by adding specified thickness values to its width and height.
    /// This results in a new Size with increased dimensions.
    /// </summary>
    public static Size Inflate(this Size size, Thickness thickness)
    {
        return new Size(
            size.Width + thickness.Left + thickness.Right,
            size.Height + thickness.Top + thickness.Bottom);
    }


}
