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

