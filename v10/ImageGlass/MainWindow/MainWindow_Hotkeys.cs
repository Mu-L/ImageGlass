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
using System.Linq;
using VKey = Windows.System.VirtualKey;

namespace ImageGlass;

public partial class MainWindow
{
    private Hotkey? _lastHotkeyPressed = null;


    // list of all menu items & default action, hotkeys
    private static IReadOnlyCollection<HotkeySingleAction> _defaultMenuList => [
        new(LangId.FrmMain_MnuOpenFile,                 API.IG_OpenFile,            MKeys.Ctrl, VKey.O),
        new(LangId.FrmMain_MnuViewNext,                 API.IG_ViewNext,            VKey.Right),
        new(LangId.FrmMain_MnuViewPrevious,             API.IG_ViewPrevious,        VKey.Left),
        new(LangId.FrmMain_MnuToggleCheckerboard,       API.IG_ToggleCheckerboard,  VKey.B),
        new(LangId.FrmMain_MnuExit,                     API.IG_Exit,                [new(VKey.Escape), new(MKeys.Ctrl, VKey.W)]),
    ];


    // a map of menu and its action
    private static Dictionary<LangId, HotkeySingleAction> _menuMap { get; set; }
        = new(_defaultMenuList.Select(ac => new KeyValuePair<LangId, HotkeySingleAction>(
            IgLang.GetKey(ac.LangKey)!.Value, ac)));

    // a map of hotkeys and actions
    private static Dictionary<Hotkey, SingleAction> _hotkeyMap { get; set; } = new();



    private void Content_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        // the quick browsing ends, now start loading full resolution
        Viewer.ShouldLoadFullResolution = true;

        _lastHotkeyPressed = null;
    }


    private async void Hotkey_Invoked(object? sender, HotkeyInvokedEventArgs e)
    {
        // 0. get hotkey action
        var action = _hotkeyMap.GetValueOrDefault(e.Hotkey);
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


    private void RegisterHotkeys_()
    {
        // 1. register the default hotkeys
        foreach (var item in _defaultMenuList)
        {
            foreach (var hk in item.Hotkeys)
            {
                Content.KeyboardAccelerators.Add(hk.Data);

                // save to the maps
                _hotkeyMap.TryAdd(hk, item);
            }
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

                // save the button text
                if (string.IsNullOrWhiteSpace(item.OnClick.LangKey))
                {
                    item.OnClick.LangKey = item.Text;
                }

                // save custom hotkey to the map
                _ = _hotkeyMap.Remove(hk);
                _ = _hotkeyMap.TryAdd(hk, item.OnClick);
            }
        }
    }



    /// <summary>
    /// Gets the menu action
    /// </summary>
    public static HotkeySingleAction? GetMenuAction(LangId? langKey)
    {
        if (langKey is null) return null;

        var action = _menuMap.GetValueOrDefault(langKey.Value);

        return action;
    }



}
