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

using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace ImageGlass.Common;

public partial class BHelper
{
    /// <summary>
    /// Generates a list of unique indexes within a specified range,
    /// wrapping around the center index, in the Center-Right-Left order.
    /// Example:
    /// <list type="bullet">
    ///   <item><c>GenerateWrappedIndexes(0, 2, 10, true) => [0, 1, 9, 2, 8]</c></item>
    ///   <item><c>GenerateWrappedIndexes(0, 2, 10, false) => [1, 9, 2, 8]</c></item>
    ///   <item><c>GenerateWrappedIndexes(0, 2, 1, true) => [0]</c></item>
    ///   <item><c>GenerateWrappedIndexes(-1, 2, 10, true) => []</c></item>
    /// </list>
    /// </summary>
    public static List<int> GenerateWrappedIndexes(int centerIndex, int range, int count, bool includeCenterIndex)
    {
        if (centerIndex < 0 || centerIndex >= count - 1) return [];

        var unloadSet = new HashSet<int>();

        // include the center index
        if (includeCenterIndex)
        {
            unloadSet.Add(centerIndex);
        }

        // generate range [-range, centerIndex, +range]
        for (var i = 1; i <= range; i++)
        {
            var rightIndex = ComputeIndexInRange(centerIndex + i, count);
            var leftIndex = ComputeIndexInRange(centerIndex - i, count);

            unloadSet.Add(rightIndex);
            unloadSet.Add(leftIndex);
        }

        return unloadSet.ToList();
    }

    /// <summary>
    /// Calculates a valid index within a circular range. Example:
    /// <list type="bullet">
    ///   <item><c>CalculateIndexInRange(1, 10) => 1</c></item>
    ///   <item><c>CalculateIndexInRange(0, 10) => 0</c></item>
    ///   <item><c>CalculateIndexInRange(-1, 10) => 9</c></item>
    ///   <item><c>CalculateIndexInRange(-2, 10) => 8</c></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Use modulo expression <c>((i % count) + count) % count</c>
    /// to ensure the index stays within <c>[0, count-1]</c>, even for negative values.
    /// </remarks>
    public static int ComputeIndexInRange(int index, int count)
    {
        var newIndex = (((index) % count) + count) % count;

        return newIndex;
    }


    /// <summary>
    /// Create an unique key for the input file.
    /// </summary>
    public static string CreateUniqueFileKey(string filePath, Vector2? size = null)
    {
        var fi = new FileInfo(filePath);
        var sb = new StringBuilder();

        sb.Append(filePath);
        sb.Append(':');
        sb.Append(fi.LastWriteTimeUtc.ToBinary());

        // Thumbnail size
        if (size is Vector2 s)
        {
            sb.Append(':');
            sb.Append(s.X);
            sb.Append(',');
            sb.Append(s.Y);
        }


        var hash = MD5.HashData(Encoding.ASCII.GetBytes(sb.ToString()));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

}