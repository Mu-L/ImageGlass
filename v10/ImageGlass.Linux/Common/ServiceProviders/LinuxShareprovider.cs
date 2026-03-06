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
using ImageGlass.Common.ServiceProviders;
using System;
using System.Diagnostics;
using System.Linq;

namespace ImageGlass.Linux.Common.ServiceProviders;

internal class LinuxShareProvider : IShareProvider
{

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void ShowShare(nint windowHandle, string[] filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        if (filePaths.Length == 0) return;

        // try xdg-desktop-portal Email compose (modern desktop portal)
        if (TryShareViaPortal(filePaths)) return;

        // fallback: xdg-email with file attachments
        TryShareViaXdgEmail(filePaths);
    }


    /// <summary>
    /// Invokes the xdg-desktop-portal Email compose dialog via D-Bus.
    /// </summary>
    private static bool TryShareViaPortal(string[] filePaths)
    {
        try
        {
            // build GVariant array of file path strings for the 'attachments' option
            var fileList = string.Join(", ", filePaths.Select(f => $"'{f}'"));
            var options = $"{{'attachments': <[{fileList}]>}}";

            using var proc = new Process();
            proc.StartInfo.FileName = "gdbus";
            proc.StartInfo.Arguments =
                "call --session " +
                "--dest org.freedesktop.portal.Desktop " +
                "--object-path /org/freedesktop/portal/desktop " +
                "--method org.freedesktop.portal.Email.ComposeMessage " +
                $"\"\" \"{options}\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();
            proc.WaitForExit(5000);

            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }


    /// <summary>
    /// Opens the default email client with file attachments using xdg-email.
    /// </summary>
    private static void TryShareViaXdgEmail(string[] filePaths)
    {
        var attachArgs = string.Join(" ", filePaths.Select(f => $"--attach \"{f}\""));

        using var proc = new Process();
        proc.StartInfo.FileName = "xdg-email";
        proc.StartInfo.Arguments = attachArgs;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.CreateNoWindow = true;
        proc.Start();
    }

}
