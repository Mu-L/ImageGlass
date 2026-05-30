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
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.IO;

namespace ImageGlass.Common.Types;


public static class StockIcon
{

    /// <summary>
    /// Gets stock icon.
    /// </summary>
    public static Bitmap? Get(StockIconId? id)
    {
        if (id is null) return null;

        try
        {
            using var stream = AssetLoader.Open(new Uri($"avares://ImageGlass.Lib/Assets/{id}.png"));
            return Bitmap.DecodeToHeight(stream, 256);
        }
        catch { }

        return null;
    }


    /// <summary>
    /// Gets default app icon.
    /// </summary>
    public static WindowIcon? GetDefaultWindowIcon()
    {
        try
        {
            using var stream = GetDefaultWindowIconAsStream();
            if (stream is null) return null;

            return new WindowIcon(stream);
        }
        catch { }

        return null;
    }


    /// <summary>
    /// Gets default app icon.
    /// </summary>
    public static Stream? GetDefaultWindowIconAsStream()
    {
        try
        {
            var stream = AssetLoader.Open(new Uri($"avares://ImageGlass.Lib/Assets/icon256.ico"));
            return stream;
        }
        catch { }

        return null;
    }
}


public enum StockIconId
{
    Delete,
    Error,
    Find,
    Info,
    Lock,
    RecycleBin,
    Rename,
    Shield,
    Warning,
}

