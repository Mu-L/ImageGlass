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
using ImageGlass.Win64.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Windows.System;

namespace ImageGlass.Common;

public partial class BHelper
{
    public static string AppName => "ImageGlass_10";


    /// <summary>
    /// Gets the base dir path.
    /// </summary>
    public static string BasePath => AppDomain.CurrentDomain.BaseDirectory;


    /// <summary>
    /// Gets the config dir path.
    /// </summary>
    public static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);


    /// <summary>
    /// Computes the full path based on the installed folder.
    /// </summary>
    public static string BaseDir(params string[] paths)
    {
        var newPaths = paths.ToList();
        newPaths.Insert(0, BasePath);
        var path = Path.Combine([.. newPaths]);

        return path;
    }


    /// <summary>
    /// Computes the full path based on the config folder.
    /// </summary>
    public static string ConfigDir(params string[] paths)
    {
        // create the directory if not exists
        Directory.CreateDirectory(ConfigPath);

        var newPaths = paths.ToList();
        newPaths.Insert(0, ConfigPath);
        var path = Path.Combine([.. newPaths]);

        return path;
    }



    /// <summary>
    /// Check if the given path (file or directory) is writable. 
    /// </summary>
    /// <param name="type">Indicates if the given path is either file or directory</param>
    /// <param name="path">Full path of file or directory</param>
    public static bool CheckPathWritable(PathType type, string path)
    {
        try
        {
            // If path is file
            if (type == PathType.File)
            {
                using (File.OpenWrite(path)) { }
            }

            // if path is directory
            else
            {
                var isDirExist = Directory.Exists(path);

                if (!isDirExist)
                {
                    Directory.CreateDirectory(path);
                }

                var sampleFile = Path.Combine(path, "test_write_file.temp");

                using (File.Create(sampleFile)) { }
                File.Delete(sampleFile);

                if (!isDirExist)
                {
                    Directory.Delete(path, true);
                }
            }


            return true;
        }
        catch
        {
            return false;
        }
    }



    /// <summary>
    /// Checks type of the path.
    /// </summary>
    public static PathType CheckPath(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);

            if (attrs.HasFlag(FileAttributes.Directory))
            {
                return PathType.Dir;
            }

            return PathType.File;
        }
        catch { }

        return PathType.Unknown;
    }



    /// <summary>
    /// Get distinct directories list from paths list.
    /// </summary>
    public static (List<string> DirPaths, List<string> FilePaths) GetDistinctDirsFromPaths(IEnumerable<string> pathList)
    {
        if (!pathList.Any()) return ([], []);

        var hashedDirsList = new HashSet<string>();
        var hashedFilesList = new HashSet<string>();

        foreach (var path in pathList)
        {
            var pathType = BHelper.CheckPath(path);
            if (pathType == PathType.Unknown) continue;

            if (pathType == PathType.Dir)
            {
                hashedDirsList.Add(path);
            }
            else
            {
                string dir;
                if (string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    var shortcutPath = FileShortcutApi.GetTargetPathFromShortcut(path);
                    var shortcutPathType = BHelper.CheckPath(shortcutPath);
                    if (shortcutPathType == PathType.Unknown) continue;

                    // get the DIR path of shortcut target
                    if (shortcutPathType == PathType.Dir)
                    {
                        dir = shortcutPath;
                    }
                    else
                    {
                        hashedFilesList.Add(shortcutPath);
                        dir = Path.GetDirectoryName(shortcutPath) ?? "";
                    }
                }
                else
                {
                    hashedFilesList.Add(path);
                    dir = Path.GetDirectoryName(path) ?? "";
                }


                if (string.IsNullOrEmpty(dir)) continue;
                hashedDirsList.Add(dir);
            }
        }

        return ([.. hashedDirsList], [.. hashedFilesList]);
    }


    /// <summary>
    /// Resolves a relative/protocol/link path to absolute path
    /// </summary>
    /// <param name="inputPath">A path</param>
    /// <returns></returns>
    public static string ResolvePath(string? inputPath)
    {
        if (string.IsNullOrEmpty(inputPath))
            return inputPath ?? "";

        var path = inputPath;
        const string protocol = Const.APP_PROTOCOL + ":";

        // If inputPath is URI Scheme
        if (path.StartsWith(protocol))
        {
            // Retrieve the real path
            path = Uri.UnescapeDataString(path).Remove(0, protocol.Length);
        }

        // Parse environment vars to absolute path
        path = Environment.ExpandEnvironmentVariables(path);

        if (string.Equals(Path.GetExtension(inputPath), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            path = FileShortcutApi.GetTargetPathFromShortcut(path);
        }

        return path;
    }


    /// <summary>
    /// Open URL in the default browser.
    /// </summary>
    public static async Task OpenUrlAsync(string? url, string campaign = "from_unknown")
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            var ub = new UriBuilder(url);
            var queries = HttpUtility.ParseQueryString(ub.Query);
            queries["utm_source"] = "app_TODO"; // TODO: App.Version;
            queries["utm_medium"] = "app_click";
            queries["utm_campaign"] = campaign;

            ub.Query = queries.ToString();

            _ = await Launcher.LaunchUriAsync(ub.Uri);
        }
        catch { }
    }


    /// <summary>
    /// Opens file path in Explorer and selects it.
    /// </summary>
    public static void OpenFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            // TODO:
            //ExplorerApi.OpenFolderAndSelectItem(filePath);
        }
        catch
        {
            using var proc = Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }


    /// <summary>
    /// Opens the folder path in Explorer, creates the fodler path if not existed.
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


}


/// <summary>
/// Types of path
/// </summary>
public enum PathType
{
    File,
    Dir,
    Unknown,
}

