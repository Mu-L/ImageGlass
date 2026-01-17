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
using Avalonia.Controls;
using ImageGlass.Common;
using ImageGlass.Common.Types;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ImageGlass.Win32.Common.Types;


public partial class Win32ColorProfileProvider : DisposableImpl, IWindowColorProfileProvider
{
    private Window? _window;
    private nint _windowHandle;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler? Changed;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public byte[]? Data { get; private set; }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool IsInitialized { get; private set; }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        if (_window != null)
        {
            _window.PositionChanged -= OnWindowMoved;
        }

        Data = null;
        IsInitialized = false;
    }



    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    public async Task InitializeAsync(Window window)
    {
        if (IsInitialized) return;

        _window = window;
        _windowHandle = _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

        if (_windowHandle != IntPtr.Zero)
        {
            var profilePath = GetColorProfilePath(_windowHandle);

            if (!string.IsNullOrWhiteSpace(profilePath))
            {
                Data = await File.ReadAllBytesAsync(profilePath);
            }

            _window.PositionChanged += OnWindowMoved;
        }

        IsInitialized = true;
        Changed?.Invoke(this, EventArgs.Empty);
        return;
    }


    private async void OnWindowMoved(object? sender, PixelPointEventArgs e)
    {
        if (_windowHandle == 0) return;

        // get the color profile data of the current monitor
        var newData = await GetColorProfileDataAsync(_windowHandle);

        // update color profile
        if (newData is not null && !AreEqual(Data, newData))
        {
            Data = newData;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }


    private static bool AreEqual(byte[]? a, byte[]? b)
    {
        if (a == null || b == null) return a == b;
        if (a.Length != b.Length) return false;
        return a.AsSpan().SequenceEqual(b);
    }



    /// <summary>
    /// Gets color profile data of the current monitor where the window is at.
    /// </summary>
    public static async Task<byte[]?> GetColorProfileDataAsync(nint windowHandle)
    {
        var profilePath = GetColorProfilePath(windowHandle);
        if (string.IsNullOrEmpty(profilePath)) return null;

        var data = await File.ReadAllBytesAsync(profilePath);
        return data;
    }


    /// <summary>
    /// Gets color profile path of current monitor where the window is at.
    /// </summary>
    public static string? GetColorProfilePath(nint windowHandle)
    {
        try
        {
            // get monitor from window
            var hMonitor = PInvoke.MonitorFromWindow(new HWND(windowHandle), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

            // get the monitor info
            var mi = new MONITORINFO();
            mi.cbSize = (uint)Marshal.SizeOf(mi);
            if (!PInvoke.GetMonitorInfo(hMonitor, ref mi)) return null;

            var dc = PInvoke.CreateDCW("DISPLAY");

            try
            {
                // get the length of profile path
                uint size = 0;
                if (!PInvoke.GetICMProfile(dc, ref size, null)) return null;

                // get the profile buffer
                var buffer = new char[size];
                if (!PInvoke.GetICMProfile(dc, ref size, buffer)) return null;

                // get the profile path
                var profilePath = new string(buffer, 0, (int)size - 1);
                if (!File.Exists(profilePath)) return null;

                return profilePath;
            }
            finally
            {
                PInvoke.DeleteDC(dc);
            }
        }
        catch { }

        return null;
    }

}
