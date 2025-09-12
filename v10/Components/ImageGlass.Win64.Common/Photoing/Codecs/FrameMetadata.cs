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

namespace ImageGlass.Common.Photoing;


public class FrameMetadata
{
    public MagickColor BackgroundColor { get; set; } = MagickColors.Transparent;
    public uint Width { get; set; } = 0;
    public uint Height { get; set; } = 0;
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;

    public uint AnimationDelay { get; set; } = 0;
    public uint AnimationTicksPerSecond { get; set; } = 0;
    public GifDisposeMethod GifDisposeMethod { get; set; } = GifDisposeMethod.Undefined;

}

