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
using System;

namespace ImageGlass.Common.ServiceProviders;

public interface IShellProvider : IDisposable
{
    /// <summary>
    /// Gets, sets the Shell object of foreground window.
    /// </summary>
    object? ForegroundShell { get; set; }


    /// <summary>
    /// Check if we can use the foreground shell folder for loading images.
    /// </summary>
    bool CanUseForegroundShell();


    /// <summary>
    /// Gets the foreground shell object.
    /// </summary>
    object? GetForegroundWindowView();


    /// <summary>
    /// Gets the target path from shortcute file path
    /// </summary>
    string? GetTargetPathFromShortcut(string? lnkFilePath);


    /// <summary>
    /// Opens file explorer and selects the file.
    /// </summary>
    void OpenFilePath(string? filePath);


    /// <summary>
    /// Opens file explorer and selects the folder.
    /// </summary>
    void OpenFolderPath(string? dirPath);


    /// <summary>
    /// Deletes a file with option to move to recycle bin.
    /// </summary>
    void DeleteFile(string filePath, bool moveToRecycleBin = true);

}
