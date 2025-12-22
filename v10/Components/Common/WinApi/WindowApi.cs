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
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ImageGlass.Common;

public static class WindowApi
{
    /// <summary>
    /// Sets window owner.
    /// </summary>
    public static void SetOwner(nint thisWindowHandle, nint ownerWindowHandle)
    {
        _ = PInvoke.SetWindowLongPtr(new HWND(thisWindowHandle),
            WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, ownerWindowHandle);
    }


    /// <summary>
    /// Return DPI scale of the given window.
    /// </summary>
    public static double GetDpiScale(nint windowHandle)
    {
        var dpi = PInvoke.GetDpiForWindow(new HWND(windowHandle));
        var dpiScale = dpi / 96d;

        return dpiScale;
    }


    /// <summary>
    /// Loads window in the background but don't visually show it.
    /// </summary>
    public static void ShowHidden(nint windowHandle)
    {
        _ = PInvoke.SetWindowPos(new HWND(windowHandle), new HWND(),
            0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE
            | SET_WINDOW_POS_FLAGS.SWP_NOSIZE
            | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
            | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
            | SET_WINDOW_POS_FLAGS.SWP_NOSENDCHANGING
            | SET_WINDOW_POS_FLAGS.SWP_HIDEWINDOW);
    }


    /// <summary>
    /// Sets window border.
    /// </summary>
    public static void SetBorder(nint windowHandle, bool enabled)
    {
        var hwnd = new HWND(windowHandle);
        var style = (WINDOW_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);

        if (enabled)
        {
            style |= WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_BORDER;
        }
        else
        {
            style &= ~(WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_BORDER);
        }

        _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)style);
        _ = PInvoke.SetWindowPos(
            hwnd,
            HWND.Null,
            0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED
        );

    }


    /// <summary>
    /// Sets window opacity.
    /// </summary>
    public static void SetOpacity(nint windowHandle, double opacity)
    {
        var hwnd = new HWND(windowHandle);
        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        exStyle |= WINDOW_EX_STYLE.WS_EX_LAYERED;

        // enable opacity
        _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle);

        var opacityByte = (byte)(opacity * 255);

        // update opacity
        _ = PInvoke.SetLayeredWindowAttributes(
            hwnd, new COLORREF(0), opacityByte,
            LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA
        );
    }

}
