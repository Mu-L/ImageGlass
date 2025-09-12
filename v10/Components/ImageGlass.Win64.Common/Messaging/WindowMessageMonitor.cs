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
using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace ImageGlass.Common;


public partial class WindowMessageMonitor : DisposableImpl
{
    private HWND _windowHandle = new HWND();
    private SUBCLASSPROC _subClassDelegate;
    private static nuint _classIdCounter = 101;
    private readonly nuint _classId;

    // to manage MessageReceived events
    private List<EventHandler<WindowMessageReceivedEventArgs>> _messageReceivedEvents = [];
    private event EventHandler<WindowMessageReceivedEventArgs>? _messageReceived;


    /// <summary>
    /// Event raised when a window message is received.
    /// </summary>
    public event EventHandler<WindowMessageReceivedEventArgs>? MessageReceived
    {
        add
        {
            if (value is not null)
            {
                if (_messageReceived is null)
                {
                    SetWindowSubclass();
                }

                _messageReceived += value;
                _messageReceivedEvents.Add(value);
            }
        }
        remove
        {
            if (value is not null)
            {
                _messageReceived -= value;
                _messageReceivedEvents.Remove(value);
            }

            if (_messageReceivedEvents.Count == 0)
            {
                RemoveWindowSubclass();
            }
        }
    }


    public WindowMessageMonitor(nint windowHandle)
    {
        _windowHandle = new HWND(windowHandle);
        _subClassDelegate = new SUBCLASSPROC(Window_MessageReceived);
        _classId = _classIdCounter++;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        // remove MessageReceived events
        foreach (var eventHandler in _messageReceivedEvents)
        {
            _messageReceived -= eventHandler;
        }
        _messageReceivedEvents.Clear();

        // remove window subclass
        RemoveWindowSubclass();
    }


    /// <summary>
    /// Raises event <see cref="MessageReceived"/>.
    /// </summary>
    protected void OnMessageReceived(WindowMessageReceivedEventArgs e)
    {
        _messageReceived?.Invoke(this, e);
    }


    /// <summary>
    /// Handles window subclass <see cref="MessageReceived"/> event.
    /// </summary>
    private unsafe LRESULT Window_MessageReceived(HWND hWnd,
        uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (_messageReceived != null)
        {
            var args = new WindowMessageReceivedEventArgs(hWnd, uMsg, wParam, lParam);
            OnMessageReceived(args);

            if (args.Handled) return new LRESULT(args.Result);
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }


    /// <summary>
    /// Registers a subclass callback from the window <see cref="_windowHandle"/>.
    /// </summary>
    private unsafe void SetWindowSubclass()
    {
        _ = PInvoke.SetWindowSubclass(_windowHandle, _subClassDelegate, _classId, 0);
    }


    /// <summary>
    /// Removes a subclass callback from the window <see cref="_windowHandle"/>.
    /// </summary>
    private void RemoveWindowSubclass()
    {
        _ = PInvoke.RemoveWindowSubclass(_windowHandle, _subClassDelegate, _classId);
    }

}

