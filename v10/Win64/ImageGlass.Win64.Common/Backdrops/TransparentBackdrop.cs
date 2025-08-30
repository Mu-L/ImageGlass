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
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using System;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Controls;

namespace ImageGlass.Win64.Common;

public partial class TransparentBackdrop : CompositionBrushBackdrop
{
    private HBRUSH _bgHBrush = HBRUSH.Null;
    private WindowMessageMonitor _msgMonitor;

    private Windows.UI.Composition.CompositionColorBrush? _brush;
    private Color _color;


    public Color TintColor
    {
        get => _color;
        set
        {
            _color = value;
            if (_brush != null)
            {
                _brush.Color = value;
            }
        }
    }


    public TransparentBackdrop(WindowMessageMonitor wndProc) : this(wndProc, Colors.Transparent) { }

    public TransparentBackdrop(WindowMessageMonitor wndProc, Color tintColor)
    {
        _msgMonitor = wndProc;
        _color = tintColor;
    }



    protected override Windows.UI.Composition.CompositionBrush CreateBrush(Windows.UI.Composition.Compositor compositor)
    {
        return Compositor.CreateColorBrush(TintColor);
    }


    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        _msgMonitor.MessageReceived += Monitor_WindowMessageReceived;

        var hWnd = xamlRoot.ContentIslandEnvironment.AppWindowId.Value;
        ConfigureDwm(hWnd);

        base.OnTargetConnected(connectedTarget, xamlRoot);

        var hdc = PInvoke.GetDC(new HWND((nint)hWnd));
        ClearBackground((nint)hWnd, hdc);
    }


    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        _msgMonitor.MessageReceived -= Monitor_WindowMessageReceived;

        var backdrop = disconnectedTarget.SystemBackdrop;
        disconnectedTarget.SystemBackdrop = null;
        backdrop?.Dispose();
        _brush?.Dispose();
        _brush = null;

        if (!_bgHBrush.IsNull)
            _ = PInvoke.DeleteObject(_bgHBrush);

        _bgHBrush = HBRUSH.Null;
        base.OnTargetDisconnected(disconnectedTarget);
    }


    private static void ConfigureDwm(ulong hWnd)
    {
        var handle = new IntPtr((nint)hWnd);
        var margins = new MARGINS(); // You may need to set appropriate values for margins

        _ = PInvoke.DwmExtendFrameIntoClientArea(new HWND(handle), in margins);

        var dwm = new DWM_BLURBEHIND()
        {
            dwFlags = (uint)(NativeValues.DWM_BLURBEHIND_Mask.Enable | NativeValues.DWM_BLURBEHIND_Mask.BlurRegion),
            fEnable = true,
            hRgnBlur = PInvoke.CreateRectRgn(-2, -2, -1, -1),
        };

        _ = PInvoke.DwmEnableBlurBehindWindow(new HWND(handle), in dwm);
    }


    private unsafe bool ClearBackground(nint hwnd, nint hdc)
    {
        if (PInvoke.GetClientRect(new HWND(hwnd), out var rect))
        {
            if (_bgHBrush.IsNull)
                _bgHBrush = PInvoke.CreateSolidBrush(new COLORREF(0));

            _ = NativeMethods.FillRect(hdc, ref rect, _bgHBrush);
            return true;
        }

        return false;
    }


    private void Monitor_WindowMessageReceived(object? sender, WindowMessageReceivedEventArgs e)
    {
        if (e.MessageType == (uint)NativeValues.WindowMessage.WM_ERASEBKGND)
        {
            if (ClearBackground(e.Message.Hwnd, (nint)e.Message.WParam))
            {
                e.Result = 1;
                e.Handled = true;
            }
        }
        else if (e.MessageType == (uint)NativeValues.WindowMessage.WM_DWMCOMPOSITIONCHANGED)
        {
            ConfigureDwm((ulong)e.Message.Hwnd);
            e.Handled = true;
            e.Result = 0;
        }
    }

}
