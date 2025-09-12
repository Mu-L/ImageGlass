/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
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
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace ImageGlass.Common;

public struct Message
{
    internal Message(IntPtr hwnd, uint messageId, nuint wParam, IntPtr lParam)
    {
        Hwnd = hwnd;
        MessageId = messageId;
        WParam = wParam;
        LParam = lParam;
    }

    public IntPtr Hwnd { get; private set; }

    public uint MessageId { get; private set; }

    public nuint WParam { get; private set; }

    public nint LParam { get; private set; }

    internal int LowOrder => unchecked((short)LParam);

    internal int HighOrder => unchecked((short)((long)LParam >> 16));


    /// <inheritdoc />
    public override string ToString()
    {
        switch ((NativeValues.WindowMessage)MessageId)
        {
            case NativeValues.WindowMessage.WM_SIZING:
                string side = WParam switch
                {
                    1 => "Left",
                    2 => "Right",
                    3 => "Top",
                    4 => "Top-Left",
                    5 => "Top-Right",
                    6 => "Bottom",
                    7 => "Bottom-Left",
                    8 => "Bottom-Right",
                    _ => WParam.ToString(),
                };
                var rect = Marshal.PtrToStructure<RECT>((IntPtr)LParam);

                return $"WM_SIZING: Side: {side} Rect: {rect.left},{rect.top},{rect.right},{rect.bottom}";
            default:
                break;
        }

        return $"{(NativeValues.WindowMessage)MessageId}: LParam={LParam} WParam={WParam}";
    }
}
