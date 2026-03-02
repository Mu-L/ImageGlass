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
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace ImageGlass.Common;

public static class InstanceApi
{
    private static IntPtr _redirectEventPtr = IntPtr.Zero;


    /// <summary>
    /// Redirects the activation arguments to an app instance.
    /// </summary>
    public static void RedirectActivationTo(AppInstance instance, AppActivationArguments args)
    {
        var eventHandle = PInvoke.CreateEvent(null, true, false, null);
        _redirectEventPtr = eventHandle.DangerousGetHandle();
        var redirectEventHandle = new HANDLE(_redirectEventPtr);

        _ = Task.Run(() =>
        {
            instance.RedirectActivationToAsync(args).AsTask().Wait();
            _ = PInvoke.SetEvent(redirectEventHandle);
        });

        var CWMO_DEFAULT = 0u;
        var INFINITE = 0xFFFFFFFF;
        _ = PInvoke.CoWaitForMultipleObjects(CWMO_DEFAULT, INFINITE, [redirectEventHandle], out var handleIndex);

        // Bring the window to the foreground
        var process = Process.GetProcessById((int)instance.ProcessId);
        _ = PInvoke.SetForegroundWindow(new HWND(process.MainWindowHandle));
    }


    /// <summary>
    /// Parses a Unicode command line string and returns an array of pointers to the command line arguments.
    /// <para>
    ///   Example:
    ///   <c>ImageGlass.exe "C:\My Photos\pic1.jpg"</c> => <c>["ImageGlass.exe", "C:\My Photos\pic1.jpg"]</c>
    /// </para>
    /// </summary>
    public static unsafe string[] ParseCommandLineArguments(string argStr)
    {
        var argsPtr = PInvoke.CommandLineToArgv(argStr, out var argsCount);
        if (argsPtr is null) return [];

        try
        {
            var result = new string[argsCount];
            for (int i = 0; i < argsCount; i++)
            {
                result[i] = argsPtr[i].ToString();
            }

            return result;
        }
        finally
        {
            PInvoke.LocalFree(new HLOCAL(argsPtr));
        }
    }

}
