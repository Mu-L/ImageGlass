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
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace ImageGlass.Win32.Common;

public static class FileShortcutApi
{
    private static Guid CLSID_ShellLink = new Guid("00021401-0000-0000-C000-000000000046");


    /// <summary>
    /// Get the target path from shortcut (*.lnk)
    /// </summary>
    /// <param name="shortcutPath">Path of shortcut (*.lnk)</param>
    public static unsafe string? GetTargetPathFromShortcut(string shortcutPath)
    {
        try
        {
            var CLSID_IShellLinkW = typeof(IShellLinkW).GUID;

            PInvoke.CoCreateInstance(CLSID_ShellLink, null,
                CLSCTX.CLSCTX_INPROC_SERVER, in CLSID_IShellLinkW, out object obj)
                .ThrowOnFailure();

            if (obj is not IShellLinkW shellLink) return null;
            if (obj is not IPersistFile persistFile) return null;

            // open the shortcut path
            persistFile.Load(shortcutPath, STGM.STGM_READ);

            // get full path
            char* buffer = stackalloc char[260];
            WIN32_FIND_DATAW findData = default;
            shellLink.GetPath(new PWSTR(buffer), 260, &findData, 0);

            var path = new string(buffer);
            return path;
        }
        catch { }

        return null;
    }


}

