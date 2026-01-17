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
using ImageGlass.Common;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace ImageGlass.Win32.Common;

public partial class WHelper
{

    /// <summary>
    /// Get distinct directories list from paths list.
    /// </summary>
    public static (List<string> DirPaths, List<string> FilePaths) GetDistinctDirsFromPaths(IEnumerable<string> pathList)
    {
        return BHelper.GetDistinctDirsFromPaths(pathList,
            lnkPath => FileShortcutApi.GetTargetPathFromShortcut(lnkPath));
    }


    /// <summary>
    /// Resolves a relative/protocol/link path to absolute path.
    /// </summary>
    public static string ResolvePath(string? inputPath)
    {
        var path = BHelper.ResolvePath(inputPath);

        if (string.Equals(Path.GetExtension(inputPath), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            path = FileShortcutApi.GetTargetPathFromShortcut(path);
        }

        return path;
    }


    /// <summary>
    /// Opens file path in Explorer and selects it.
    /// </summary>
    public static void OpenFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            ExplorerApi.SelectFileFromExplorer(filePath);
        }
        catch
        {
            using var proc = Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }


    /// <summary>
    /// Opens the folder path in Explorer, creates the folder path if not existed.
    /// </summary>
    public static void OpenFolderPath(string? dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath)) return;

        try
        {
            Directory.CreateDirectory(dirPath);
        }
        catch { }

        try
        {
            using var proc = Process.Start("explorer.exe", $"\"{dirPath}\"");
        }
        catch { }
    }


    /// <summary>
    /// Delete a file.
    /// </summary>
    /// <param name="filePath">Full file path to delete</param>
    /// <param name="moveToRecycleBin"><c>true</c>: Move to Recycle bin; <c>false</c>: Delete permanently</param>
    public static void DeleteFile(string filePath, bool moveToRecycleBin = true)
    {
        var option = moveToRecycleBin ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;

        try
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, option);
        }
        catch (OperationCanceledException) { }
    }



}
