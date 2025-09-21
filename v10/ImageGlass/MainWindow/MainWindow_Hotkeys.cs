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
using ImageGlass.Common;
using ImageGlass.UI;
using System.Collections.Generic;
using Windows.System;

namespace ImageGlass;

public partial class MainWindow
{
    private Dictionary<Hotkey, string> _hotkeys = new()
    {
        { new(VirtualKeyModifiers.Control, VirtualKey.O, new SingleAction(nameof(API.IG_OpenFile))), "FrmMain.MnuOpenFile" },

        { new(VirtualKey.Right, new SingleAction(nameof(API.IG_ViewByStep), "1")), "FrmMain.MnuViewNext" },
        { new(VirtualKey.Left, new SingleAction(nameof(API.IG_ViewByStep), "-1")), "FrmMain.MnuViewPrevious" },

        { new(VirtualKey.B, new SingleAction(nameof(API.IG_ToggleCheckerboard))), "FrmMain.MnuToggleCheckerboard" },


        { new(VirtualKey.Escape, new SingleAction(nameof(API.IG_Exit))), "FrmMain.MnuExit" },
    };


    private async void Hotkey_Invoked(object? sender, HotkeyInvokedEventArgs e)
    {
        var error = await RunActionAsync(e.Hotkey.Action);
        if (error is null) return;

        // get the language string for error title
        string? errorTitle = null;
        if (_hotkeys.TryGetValue(e.Hotkey, out var langKey))
        {
            errorTitle = AP.Config.Lang[langKey];
        }

        // show error message
        _ = await ModalWindow.ShowErrorAsync(this, errorTitle, error.Message);
    }


    public void RegisterDefaultHotkeys()
    {
        foreach (var item in _hotkeys)
        {
            Content.KeyboardAccelerators.Add(item.Key.Data);
        }
    }


    public void RegisterHotkey(Hotkey hk, string? langKey)
    {
        // delete the hotkey if value is null
        if (langKey is null)
        {
            _ = _hotkeys.Remove(hk);
            return;
        }

        // add or update the command
        if (_hotkeys.ContainsKey(hk))
        {
            _hotkeys[hk] = langKey;
        }
        else
        {
            _hotkeys.TryAdd(hk, langKey);
        }
    }

}
