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
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Gdi;

namespace ImageGlass.Win32.Common.Types;


public partial class Win32ColorProfileProvider : DisposableImpl, IWindowColorProfileProvider
{
    private Window? _window;
    private nint _windowHandle;
    private nint _currentMonitor;

    const int WM_COLORSPACECHANGED = 0x0320;
    const int WM_DISPLAYCHANGE = 0x007E;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event TEventHandler<IWindowColorProfileProvider, ColorProfileChangedEventArgs>? Changed;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string ProfilePath { get; private set; } = string.Empty;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool IsHdr { get; private set; } = false;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool IsInitialized { get; private set; } = false;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();


        if (TopLevel.GetTopLevel(_window) is TopLevel top)
        {
            Win32Properties.RemoveWndProcHookCallback(top, WndProcHook);
        }

        if (_window != null)
        {
            _window.PositionChanged -= OnWindowMoved;
        }


        ProfilePath = string.Empty;
        IsHdr = false;
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

        IsInitialized = true;
        _window = window;
        _windowHandle = _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

        if (_windowHandle == IntPtr.Zero) return;


        // get current monitor
        _currentMonitor = GetMonitorFromWindow(_windowHandle);

        // load profile of the monitor
        UpdateColorProfile();

        // listen to events
        _window.PositionChanged += OnWindowMoved;
        if (TopLevel.GetTopLevel(window) is TopLevel top)
        {
            Win32Properties.AddWndProcHookCallback(top, WndProcHook);
        }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<byte[]?> ReadColorProfileDataAsync()
    {
        if (string.IsNullOrEmpty(ProfilePath)) return null;

        var data = await File.ReadAllBytesAsync(ProfilePath);
        return data;
    }



    private IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_COLORSPACECHANGED || msg == WM_DISPLAYCHANGE)
        {
            // update profile of the monitor
            UpdateColorProfile();

            handled = true;
        }

        return IntPtr.Zero;
    }



    private void UpdateColorProfile()
    {
        // get profile of the monitor
        ProfilePath = GetColorProfilePath(_currentMonitor);
        IsHdr = IsHdrEnabled(_currentMonitor);

        Changed?.Invoke(this, new ColorProfileChangedEventArgs(ProfilePath, IsHdr));
    }



    private async void OnWindowMoved(object? sender, PixelPointEventArgs e)
    {
        if (_windowHandle == IntPtr.Zero) return;

        // check the current monitor of window
        var monitor = GetMonitorFromWindow(_windowHandle);
        if (monitor == _currentMonitor) return;
        _currentMonitor = monitor;


        // get the color profile of the current monitor
        UpdateColorProfile();
    }



    /// <summary>
    /// Gets the monitor from window.
    /// </summary>
    public static nint GetMonitorFromWindow(nint windowHandle)
    {
        var monitor = PInvoke.MonitorFromWindow(new HWND(windowHandle),
            MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

        return monitor;
    }



    /// <summary>
    /// Gets color profile path of current monitor where the window is at.
    /// </summary>
    public static unsafe string GetColorProfilePath(nint hMonitor)
    {
        try
        {
            // get the monitor info
            var mi = new MONITORINFOEXW();
            mi.monitorInfo.cbSize = (uint)sizeof(MONITORINFOEXW);
            if (!PInvoke.GetMonitorInfo((HMONITOR)hMonitor, (MONITORINFO*)(void*)&mi)) return string.Empty;

            // get monitor name
            var deviceName = new string(mi.szDevice.AsSpan());
            var dc = PInvoke.CreateDCW(string.Empty, deviceName);
            if (dc.IsNull) return string.Empty;


            try
            {
                // get the length of profile path
                uint size = 0;
                _ = PInvoke.GetICMProfile(dc, ref size, null);

                // get the profile buffer
                var buffer = new char[size];
                _ = PInvoke.GetICMProfile(dc, ref size, buffer);

                // get the profile path
                var profilePath = new string(buffer, 0, (int)size - 1);
                if (!File.Exists(profilePath)) return string.Empty;

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



    /// <summary>
    /// Checks if HDR is enabled.
    /// </summary>
    public static bool IsHdrEnabled(nint hMonitor)
    {
        var iidFactory6 = typeof(IDXGIFactory6).GUID;

        PInvoke.CreateDXGIFactory1(iidFactory6, out var factoryObj).ThrowOnFailure();
        if (factoryObj is not IDXGIFactory6 factory) return false;

        for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1 adapter).Succeeded; i++)
        {
            for (uint j = 0; adapter.EnumOutputs(j, out IDXGIOutput output).Succeeded; j++)
            {
                if (output is not IDXGIOutput6 output6) continue;

                // find the monitor
                var desc = output6.GetDesc1();
                if (desc.Monitor != hMonitor) continue;

                // check HDR
                var isHdr = desc.ColorSpace == DXGI_COLOR_SPACE_TYPE.DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020;
                return isHdr;
            }
        }

        return false;
    }

}




