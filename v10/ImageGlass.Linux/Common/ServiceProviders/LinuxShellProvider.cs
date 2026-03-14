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
using ImageGlass.Common;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ImageGlass.Linux.Common.ServiceProviders;

internal class LinuxShellProvider : PhDisposable, IShellProvider
{
    private static readonly string _desktopFileId = $"imageglass.desktop";


    public object? ForegroundShell { get; set; }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();
        ForegroundShell = null;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool CanUseForegroundShell()
    {
        // Linux does not support foreground shell integration
        return false;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void DeleteFile(string filePath, bool moveToRecycleBin = true)
    {
        if (moveToRecycleBin)
        {
            // use 'gio trash' to move file to the FreeDesktop trash
            BHelper.RunProcess("gio", $"trash \"{filePath}\"");
        }
        else
        {
            try
            {
                File.Delete(filePath);
            }
            catch { }
        }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public object? GetForegroundWindowView()
    {
        // Linux does not have a foreground shell object
        return null;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string? GetTargetPathFromShortcut(string? lnkFilePath)
    {
        if (string.IsNullOrWhiteSpace(lnkFilePath)) return null;

        // resolve symlinks
        try
        {
            var fileInfo = new FileInfo(lnkFilePath);
            if (fileInfo.LinkTarget is string target)
            {
                // resolve relative symlink targets to absolute paths
                var resolvedPath = Path.IsPathRooted(target)
                    ? target
                    : Path.GetFullPath(target, Path.GetDirectoryName(lnkFilePath)!);

                return resolvedPath;
            }
        }
        catch { }

        // parse .desktop files to extract the Exec= target
        if (lnkFilePath.EndsWith(".desktop", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                foreach (var line in File.ReadLines(lnkFilePath))
                {
                    if (line.StartsWith("Exec=", StringComparison.Ordinal))
                    {
                        // strip field codes like %f, %F, %u, %U
                        var exec = line["Exec=".Length..].Trim();
                        var spaceIndex = exec.IndexOf(' ');
                        return spaceIndex > 0 ? exec[..spaceIndex] : exec;
                    }
                }
            }
            catch { }
        }

        return null;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task OpenDefaultEditingAppAsync(string filePath, Action? callbackFn = null)
    {
        // xdg-open launches the file in its associated application
        BHelper.RunProcess("xdg-open", $"\"{filePath}\"");
        callbackFn?.Invoke();

        return Task.CompletedTask;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void OpenFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        // try DBus call to select the file in the default file manager
        try
        {
            BHelper.RunProcess("dbus-send",
                "--session --type=method_call --dest=org.freedesktop.FileManager1 " +
                "/org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems " +
                $"array:string:\"file://{filePath}\" string:\"\"");
            return;
        }
        catch { }

        // fallback: open the parent directory
        var dirPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dirPath))
        {
            BHelper.RunProcess("xdg-open", $"\"{dirPath}\"");
        }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void OpenFolderPath(string? dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath)) return;

        try
        {
            Directory.CreateDirectory(dirPath);
        }
        catch { }

        BHelper.RunProcess("xdg-open", $"\"{dirPath}\"");
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task SetDefaultPhotoViewerAsync(string[] extensions, bool enable)
    {
        foreach (var ext in extensions)
        {
            // query the MIME type for this extension
            var mimeType = BHelper.RunProcessAndReadOutput("xdg-mime", $"query filetype dummy{ext}");
            if (string.IsNullOrWhiteSpace(mimeType)) continue;

            mimeType = mimeType.Trim();

            if (enable)
            {
                BHelper.RunProcess("xdg-mime", $"default {_desktopFileId} {mimeType}");
            }
            else
            {
                // reset to the system default by removing the user override
                var mimeappsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "mimeapps.list");

                RemoveMimeAssociation(mimeappsPath, mimeType);
            }
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task SetLockScreenAsync(string filePath)
    {
        // GNOME lock screen background
        BHelper.RunProcess("gsettings",
            $"set org.gnome.desktop.screensaver picture-uri \"file://{filePath}\"");

        return Task.CompletedTask;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void SetWallpaper(string filePath)
    {
        // GNOME desktop wallpaper (light and dark)
        BHelper.RunProcess("gsettings",
            $"set org.gnome.desktop.background picture-uri \"file://{filePath}\"");
        BHelper.RunProcess("gsettings",
            $"set org.gnome.desktop.background picture-uri-dark \"file://{filePath}\"");
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void ShowOpenWith(string filePath)
    {
        // -n forces a menu choice if multiple apps exist
        _ = Process.Start(new ProcessStartInfo
        {
            FileName = "mimeopen",
            Arguments = $"-n \"{filePath}\"",
            UseShellExecute = false,
            CreateNoWindow = false, // Often needs a terminal context for choice
        });
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void ShowFileProperties(string filePath, nint windowHandle)
    {
        // try DBus call to show the properties dialog in the file manager
        try
        {
            BHelper.RunProcess("dbus-send",
                "--session --type=method_call --dest=org.freedesktop.FileManager1 " +
                "/org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItemProperties " +
                $"array:string:\"file://{filePath}\" string:\"\"");
        }
        catch { }
    }



    #region Private helpers

    /// <summary>
    /// Removes a MIME type association from the mimeapps.list file.
    /// </summary>
    private static void RemoveMimeAssociation(string mimeappsPath, string mimeType)
    {
        if (!File.Exists(mimeappsPath)) return;

        try
        {
            var lines = File.ReadAllLines(mimeappsPath);
            var prefix = $"{mimeType}=";

            using var writer = new StreamWriter(mimeappsPath);
            foreach (var line in lines)
            {
                // skip lines that assign this MIME type to our app
                if (line.StartsWith(prefix, StringComparison.Ordinal)
                    && line.Contains(_desktopFileId, StringComparison.Ordinal))
                {
                    continue;
                }

                writer.WriteLine(line);
            }
        }
        catch { }
    }

    #endregion // Private helpers

}
