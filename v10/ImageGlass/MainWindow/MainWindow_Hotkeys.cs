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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using VKey = Windows.System.VirtualKey;

namespace ImageGlass;

public partial class MainWindow
{
    private Hotkey? _lastHotkeyPressed = null;

    // default hotkeys
    private static IReadOnlyCollection<KeyValuePair<Hotkey, SingleAction>> _defaultHotkeys = [
        new(new(MKeys.Ctrl, VKey.O),          new(API.IG_OpenFile, null,              "FrmMain.MnuOpenFile")),
        new(new(VKey.Right),                  new(API.IG_ViewByStep, "1",             "FrmMain.MnuViewNext")),
        new(new(VKey.Left),                   new(API.IG_ViewByStep, "-1",            "FrmMain.MnuViewPrevious")),
        new(new(VKey.B),                      new(API.IG_ToggleCheckerboard, null,    "FrmMain.MnuToggleCheckerboard")),
        new(new(VKey.Escape),                 new(API.IG_Exit, null,                  "FrmMain.MnuExit")),
    ];

    // all runtime hotkeys
    private static Dictionary<Hotkey, SingleAction> _hotkeys { get; set; } = new(_defaultHotkeys);






    private void Content_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        // the quick browsing ends, now start loading full resolution
        Viewer.ShouldLoadFullResolution = true;

        _lastHotkeyPressed = null;
    }


    private async void Hotkey_Invoked(object? sender, HotkeyInvokedEventArgs e)
    {
        // 0. get hotkey action
        var action = _hotkeys.GetValueOrDefault(e.Hotkey);
        if (action is null) return;

        var isPressedMultipleTimes = e.Hotkey == _lastHotkeyPressed;
        var executable = action.Executable ?? string.Empty;


        // 1. handle special hotkeys
        // save the last hotkey pressed
        _lastHotkeyPressed = e.Hotkey;

        // for quick browsing, only load photo preview
        if (executable.Equals(nameof(API.IG_ViewByStep), StringComparison.Ordinal))
        {
            if (isPressedMultipleTimes) Viewer.ShouldLoadFullResolution = false;
        }


        // 2. backup the current focused element
        var focusedEl = (UIElement?)FocusManager.GetFocusedElement(Content.XamlRoot);

        // 3. run the action
        await RunActionAsync(action);


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
                // register custom hotkey
                _ = Content.KeyboardAccelerators.Remove(hk.Data);
                Content.KeyboardAccelerators.Add(hk.Data);

                // save custom hotkey to the list
                item.OnClick.LangKey = item.Text;
                _ = _hotkeys.Remove(hk);
                _ = _hotkeys.TryAdd(hk, item.OnClick);
            }
        }
    }


}
