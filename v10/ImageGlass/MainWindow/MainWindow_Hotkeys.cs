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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Frozen;
using System.Collections.Generic;
using Windows.System;

namespace ImageGlass;

public partial class MainWindow
{
    private static FrozenDictionary<Hotkey, string> _hotkeys => new Dictionary<Hotkey, string>()
    {
        { new(VirtualKeyModifiers.Control, VirtualKey.O, new SingleAction(nameof(API.IG_OpenFile))), "FrmMain.MnuOpenFile" },

        { new(VirtualKey.Right, new SingleAction(nameof(API.IG_ViewByStep), "1")), "FrmMain.MnuViewNext" },
        { new(VirtualKey.Left, new SingleAction(nameof(API.IG_ViewByStep), "-1")), "FrmMain.MnuViewPrevious" },

        { new(VirtualKey.B, new SingleAction(nameof(API.IG_ToggleCheckerboard))), "FrmMain.MnuToggleCheckerboard" },


        { new(VirtualKey.Escape, new SingleAction(nameof(API.IG_Exit))), "FrmMain.MnuExit" },
    }.ToFrozenDictionary();


    private async void Hotkey_Invoked(object? sender, HotkeyInvokedEventArgs e)
    {
        // 1. backup the current focused element
        var focusedEl = (UIElement?)FocusManager.GetFocusedElement(Content.XamlRoot);

        // 2. run the action
        var error = await RunActionAsync(e.Hotkey.Action);

        // 3. show error message if any
        if (error is not null)
        {
            // get the language string for error title
            string? errorTitle = null;
            if (_hotkeys.TryGetValue(e.Hotkey, out var langKey))
            {
                errorTitle = AP.Config.Lang[langKey];
            }

            _ = await ModalWindow.ShowErrorAsync(this, errorTitle, error.Message);
        }

        // 4. restore the focus
        focusedEl ??= Content;
        focusedEl.Focus(FocusState.Keyboard);
    }


    public void RegisterHotkeys()
    {
        // 1. register the default hotkeys
        foreach (var item in _hotkeys)
        {
            Content.KeyboardAccelerators.Add(item.Key.Data);
        }


        // 2. register hotkeys from toolbar buttons
        foreach (var item in AP.Config.ToolbarButtons)
        {
            if (item.IsSeparator || item.OnClick is null) continue;

            foreach (var hk in item.OnClick.Hotkeys)
            {
                if (Content.KeyboardAccelerators.Contains(hk.Data))
                {
                    Content.KeyboardAccelerators.Remove(hk.Data);
                }

                Content.KeyboardAccelerators.Add(hk.Data);
            }
        }
    }

}
