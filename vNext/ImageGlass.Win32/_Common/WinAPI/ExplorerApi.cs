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
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace ImageGlass.Win32.Common;


public static class ExplorerApi
{

    #region Win32 APIs for SelectFileFromTheOpeningWindows

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern void SHParseDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext,
        [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder,
        uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

    #endregion // SelectFileFromTheOpeningWindows



    /// <summary>
    /// Finds the opening windows and select the file item.
    /// </summary>
    public static unsafe void SelectFileFromExplorer(string filePath)
    {
        var isOpened = false;
        var folderPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        var nativeFile = IntPtr.Zero;
        var pidlDir = IntPtr.Zero;

        using var shell = new EggShell();
        shell.WithOpeningWindows(ev =>
        {
            if (ev.GetTabFolderView() is ExplorerFolderView fv)
            {
                try
                {
                    var fv2 = (IFolderView2)fv.FolderView;
                    var pf2Guid = typeof(IPersistFolder2).GUID;

                    fv2.GetFolder(pf2Guid, out var pfPtr);
                    var pf = Marshal.GetObjectForIUnknown((nint)pfPtr);

                    if (pf is IPersistFolder2 persistFolder)
                    {
                        var pidl = new ITEMIDLIST();
                        ITEMIDLIST* ppidl = &pidl;

                        // get current folder pidl
                        persistFolder.GetCurFolder(&ppidl);
                        var pidlDir = (nint)ppidl;

                        // get path of the folder
                        var path = new StringBuilder(1024);
                        _ = SHGetPathFromIDList(pidlDir, path);
                        var dirPath = path.ToString();

                        if (folderPath.Equals(dirPath, StringComparison.OrdinalIgnoreCase))
                        {
                            var displayName = Path.Combine(folderPath, filePath);
                            var nativeFile = IntPtr.Zero;
                            SHParseDisplayName(displayName, IntPtr.Zero, out nativeFile, 0, out var _);

                            nint[] fileArray;
                            if (nativeFile == IntPtr.Zero)
                            {
                                // Open the folder without the file selected
                                // if we can't find the file
                                fileArray = [];
                            }
                            else
                            {
                                fileArray = [nativeFile];
                            }

                            _ = SHOpenFolderAndSelectItems(pidlDir, (uint)fileArray.Length, fileArray, 0u);
                            isOpened = true;

                            Marshal.Release((nint)pfPtr);
                            return true;
                        }
                    }

                    Marshal.Release((nint)pfPtr);
                }
                finally
                {
                    if (nativeFile != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(nativeFile);
                    }

                    if (pidlDir != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(pidlDir);
                    }
                }
            }

            return false;
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
                    shInfo.cbSize = (uint)Marshal.SizeOf(shInfo);
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
