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
using System.IO;
using System.Threading.Tasks;

namespace ImageGlass.Mac.Common.ServiceProviders;

internal class MacShellProvider : PhDisposable, IShellProvider
{
    private static readonly string _bundleId = $"com.duongdieuphap.imageglass";


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
        // macOS does not support foreground shell integration
        return false;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void DeleteFile(string filePath, bool moveToRecycleBin = true)
    {
        if (moveToRecycleBin)
        {
            // use AppleScript to move the file to Trash via Finder
            RunAppleScript(
                $"tell application \"Finder\" to delete POSIX file \"{filePath}\"");
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
        // macOS does not have a foreground shell object
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
                var resolvedPath = Path.IsPathRooted(target)
                    ? target
                    : Path.GetFullPath(target, Path.GetDirectoryName(lnkFilePath)!);

                return resolvedPath;
            }
        }
        catch { }

        // resolve macOS Finder aliases via AppleScript
        try
        {
            var script = $"tell application \"Finder\" to get POSIX path of " +
                         $"(original item of alias (POSIX file \"{lnkFilePath}\" as text))";
            var result = RunAppleScriptAndReadOutput(script);

            if (!string.IsNullOrWhiteSpace(result))
            {
                return result.Trim();
            }
        }
        catch { }

        return null;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task OpenDefaultEditingAppAsync(string filePath, Action? callbackFn = null)
    {
        // 'open' launches the file in its default associated application
        BHelper.RunProcess("open", $"\"{filePath}\"");
        callbackFn?.Invoke();

        return Task.CompletedTask;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void OpenFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        // 'open -R' reveals and selects the file in Finder
        BHelper.RunProcess("open", $"-R \"{filePath}\"");
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

        BHelper.RunProcess("open", $"\"{dirPath}\"");
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task SetDefaultPhotoViewerAsync(string[] extensions, bool enable)
    {
        // 'duti' manages default application associations on macOS;
        // it must be installed separately (e.g., via Homebrew).
        foreach (var ext in extensions)
        {
            // map file extension to a macOS UTI
            var uti = BHelper.RunProcessAndReadOutput("mdls",
                $"-name kMDItemContentType -raw /dev/null{ext}").Trim();

            // fallback to a generic dynamic UTI
            if (string.IsNullOrWhiteSpace(uti) || uti.Contains("(null)"))
            {
                uti = $"public.{ext.TrimStart('.')}";
            }

            if (enable)
            {
                BHelper.RunProcess("duti", $"-s {_bundleId} {uti} all");
            }
            else
            {
                // reset by removing the override; macOS will fall back to its default
                BHelper.RunProcess("duti", $"-s com.apple.Preview {uti} all");
            }
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task SetLockScreenAsync(string filePath)
    {
        // macOS lock screen image is stored at a fixed system path;
        // writing requires elevated privileges on recent macOS versions.
        var lockScreenPath = "/Library/Caches/Desktop Pictures/lockscreen.png";

        try
        {
            File.Copy(filePath, lockScreenPath, overwrite: true);
        }
        catch { }

        return Task.CompletedTask;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void SetWallpaper(string filePath)
    {
        // use AppleScript to set the desktop wallpaper via Finder
        RunAppleScript(
            "tell application \"Finder\" to set desktop picture to POSIX file " +
            $"\"{filePath}\"");
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void ShowFileProperties(string filePath, nint windowHandle)
    {
        // use AppleScript to open the Finder "Get Info" window for the file
        RunAppleScript(
            "tell application \"Finder\" to open information window of " +
            $"(POSIX file \"{filePath}\" as alias)");
    }



    #region Private helpers

    /// <summary>
    /// Executes an AppleScript expression via <c>osascript</c>.
    /// </summary>
    private static void RunAppleScript(string script)
    {
        BHelper.RunProcess("osascript", $"-e '{script}'");
    }


    /// <summary>
    /// Executes an AppleScript expression and returns its output.
    /// </summary>
    private static string RunAppleScriptAndReadOutput(string script)
    {
        return BHelper.RunProcessAndReadOutput("osascript", $"-e '{script}'");
    }

    #endregion // Private helpers

}
