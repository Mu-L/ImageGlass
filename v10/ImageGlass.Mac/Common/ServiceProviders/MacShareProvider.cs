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
using System;
using System.Linq;

namespace ImageGlass.Mac.Common.ServiceProviders;

internal class MacShareProvider : IShareProvider
{

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void ShowShare(nint windowHandle, string[] filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        if (filePaths.Length == 0) return;

        // build a comma-separated list of POSIX file references for AppleScript
        var fileList = string.Join(", ",
            filePaths.Select(f => $"POSIX file \"{f}\""));

        // use NSSharingServicePicker via AppleScript to open the macOS Share sheet
        var script =
            "use framework \"AppKit\" " +
            "use scripting additions " +
            $"set shareItems to {{{fileList}}} " +
            "set picker to current application's NSSharingServicePicker's alloc()'s initWithItems:shareItems " +
            "tell application \"System Events\" " +
            "set frontApp to first application process whose frontmost is true " +
            "end tell " +
            "picker's showRelativeToRect:(current application's NSZeroRect) ofView:(missing value) preferredEdge:0";

        BHelper.RunProcess("osascript", $"-e '{script}'");
    }

}
