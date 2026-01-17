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

using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace ImageGlass.Common;


/// <summary>
/// Manages the color profile for a window.
/// </summary>
public interface IWindowColorProfileProvider : IDisposable
{
    /// <summary>
    /// Occurs when a physical display's color profile is changed.
    /// </summary>
    event EventHandler? Changed;


    /// <summary>
    /// Represents color profile data in byte array format.
    /// </summary>
    byte[]? Data { get; }


    /// <summary>
    /// Indicates whether the instance has been initialized.
    /// </summary>
    bool IsInitialized { get; }


    /// <summary>
    /// Initializes the color profile setting instance for the given window.
    /// </summary>
    Task InitializeAsync(Window window);

}

