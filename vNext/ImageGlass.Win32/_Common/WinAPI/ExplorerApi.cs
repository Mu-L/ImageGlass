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
using D2Phap;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace ImageGlass.Win32.Common;


public static class ExplorerApi
{

    /// <summary>
    /// Finds the opening windows and select the file item.
    /// </summary>
    public static unsafe void SelectFileFromExplorer(string filePath)
    {
        var isOpened = false;
        var folderPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        using var shell = new EggShell();

        shell.WithOpeningWindows(ev =>
        {
            if (ev.GetTabFolderView() is not ExplorerFolderView fv) return false;

            nint pfPtr = IntPtr.Zero;
            ITEMIDLIST* ppidl = null;
            ITEMIDLIST* pidlFile = null;


            try
            {
                var fv2 = (IFolderView2)fv.FolderView;
                var pf2Guid = typeof(IPersistFolder2).GUID;

                fv2.GetFolder(pf2Guid, out var pfPtrObj);
                pfPtr = (nint)pfPtrObj;
                var pf = Marshal.GetObjectForIUnknown((nint)pfPtr);

                if (pf is IPersistFolder2 persistFolder)
                {
                    // get current folder pidl
                    persistFolder.GetCurFolder(&ppidl);

                    // get path of the folder
                    Span<char> buffer = stackalloc char[260];
                    fixed (char* pPath = buffer)
                    {
                        if (!PInvoke.SHGetPathFromIDList(ppidl, pPath)) return false;

                        int len = buffer.IndexOf('\0');
                        var dirPath = new string(buffer.Slice(0, len));

                        if (folderPath.Equals(dirPath, StringComparison.OrdinalIgnoreCase))
                        {
                            var hr = PInvoke.SHParseDisplayName(filePath, null, out pidlFile, 0, out var _);
                            if (hr.Succeeded)
                            {
                                hr = PInvoke.SHOpenFolderAndSelectItems(in *pidlFile, 0, null, 0);
                                isOpened = hr.Succeeded;
                            }

                            return true;
                        }
                    }
                }

                return false;
            }
            finally
            {
                if (pidlFile != null)
                    PInvoke.CoTaskMemFree(pidlFile);

                if (ppidl != null)
                    PInvoke.CoTaskMemFree(ppidl);

                if (pfPtr != IntPtr.Zero)
                    Marshal.Release(pfPtr);
            }
        });



        // no opened window found, we open a new window to select the file item
        if (!isOpened)
        {
            using var proc = Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }


    /// <summary>
    /// Show file property dialog.
    /// </summary>
    /// <param name="filePath">Full file path</param>
    /// <param name="windowHandle">Window handle</param>
    public static unsafe void DisplayFileProperties(string filePath, nint windowHandle)
    {
        const int SEE_MASK_INVOKEIDLIST = 0xc;
        const int SW_SHOW = 5;
        var shInfo = new SHELLEXECUTEINFOW();

        fixed (char* pFilePath = filePath)
        {
            fixed (char* pVerb = "properties")
            {
                fixed (char* pParams = "Details")
                {
                    shInfo.cbSize = (uint)sizeof(SHELLEXECUTEINFOW);
                    shInfo.lpFile = pFilePath;
                    shInfo.nShow = SW_SHOW;
                    shInfo.fMask = SEE_MASK_INVOKEIDLIST;
                    shInfo.lpVerb = pVerb;
                    shInfo.lpParameters = pParams;
                    shInfo.hwnd = new HWND(windowHandle);
                }
            }
        }

        _ = PInvoke.ShellExecuteEx(ref shInfo);
    }


}
