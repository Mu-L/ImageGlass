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

namespace ImageGlass.Common;

public static partial class NativeValues
{

    internal struct POINT
    {
        /// <summary>
        ///  The x-coordinate of the point.
        /// </summary>
        public int x;

        /// <summary>
        /// The x-coordinate of the point.
        /// </summary>
        public int y;

        public static implicit operator System.Drawing.Point(POINT point)
        {
            return new(point.x, point.y);
        }

        public static implicit operator POINT(System.Drawing.Point point)
        {
            return new() { x = point.X, y = point.Y };
        }
    }

    [Flags]
    internal enum DWM_BLURBEHIND_Mask
    {
        Enable = 0x00000001,
        BlurRegion = 0x00000002,
        TransitionMaximized = 0x00000004,
    }

    public enum WindowMessage
    {
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_SYSCOMMAND = 0x0112,
        WM_SYSMENU = 0x0313,
        WM_GETMINMAXINFO = 0x0024,
        WM_PAINT = 0x000F,
        WM_ERASEBKGND = 0x0014,
        WM_MOVE = 3,
        WM_CLOSE = 0x0010,
        WM_SETCURSOR = 0x20,
        WM_NCMOUSEMOVE = 0x00a0,
        WM_ACTIVATE = 0x0006,
        WM_ACTIVATEAPP = 0x001c,
        WM_SHOWWINDOW = 0x018,
        WM_WINDOWPOSCHANGING = 0x0046,
        WM_WINDOWPOSCHANGED = 0x0047,
        WM_SETTEXT = 0x000c,
        WM_GETTEXT = 0x000d,
        WM_GETTEXTLENGTH = 0x000e,
        WM_NCACTIVATE = 0x0086,
        WM_CAPTURECHANGED = 0x0215,
        WM_NCMOUSELEAVE = 0x02a2,
        WM_MOVING = 0x0216,
        WM_POINTERLEAVE = 0x024A,
        WM_POINTERUPDATE = 0x0245,
        WM_NCPOINTERUPDATE = 0x0241,
        WM_SIZE = 0x0005,
        WM_NCUAHDRAWCAPTION = 0x00AE,
        WM_NCHITTEST = 0x0084,
        WM_SIZING = 0x0214,
        WM_ENABLE = 0x000A,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_EXITSIZEMOVE = 0x0232,
        WM_CONTEXTMENU = 0x007b,
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,
        WM_USER = 0x0400,
        WM_GETICON = 0x007f,
        WM_SETICON = 0x0080,
        WM_DPICHANGED = 0x02E0,
        WM_DISPLAYCHANGE = 0x007E,
        WM_SETTINGCHANGE = 0x001A,
        WM_THEMECHANGE = 0x031A,
        WM_NCCALCSIZE = 0x0083,
        WM_NCPAINT = 0x0085,
        WM_NCPOINTERDOWN = 0x0242,
        WM_NCPOINTERUP = 0x0243,
        NIN_SELECT = WM_USER,
        NIN_KEYSELECT = WM_USER + 1,
        NIN_BALLOONSHOW = WM_USER + 2,
        NIN_BALLOONHIDE = WM_USER + 3,
        NIN_BALLOONTIMEOUT = WM_USER + 4,
        NIN_BALLOONUSERCLICK = WM_USER + 5,
        NIN_POPUPOPEN = WM_USER + 6,
        NIN_POPUPCLOSE = WM_USER + 7,
        WA_ACTIVE = 0x01,
        WA_INACTIVE = 0x00,
        WM_INITMENUPOPUP = 0x0117,

        WM_DWMCOMPOSITIONCHANGED = 0x0000031E,
    }
}
