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
using Microsoft.UI.Xaml.Controls;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace ImageGlass.UI;

public partial class MenuItemHelper : IgReactive
{
    private MenuFlyoutItem _menuItem;


    /// <summary>
    /// Gets, sets the language key for localization.
    /// </summary>
    public LangId? LangKey
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                LocalizeText();

                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the language param for localization.
    /// </summary>
    public object? LangParams
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                LocalizeText();

                _ = OnPropertyChanged();
                OnPropertyChanged(nameof(Text));
            }
        }
    }


    public MenuItemHelper(MenuFlyoutItem mnu)
    {
        _menuItem = mnu;

        _menuItem.Loaded += MenuItem_Loaded;
        _menuItem.Unloaded += MenuItem_Unloaded;
    }


    private void MenuItem_Loaded(object sender, RoutedEventArgs e)
    {
        LocalizeText();

        AP.LanguageChanged += AP_LanguageChanged;
    }


    private void MenuItem_Unloaded(object sender, RoutedEventArgs e)
    {
        _menuItem.Loaded -= MenuItem_Loaded;
        _menuItem.Unloaded -= MenuItem_Unloaded;

        AP.LanguageChanged -= AP_LanguageChanged;
    }


    private void AP_LanguageChanged(object? sender, EventArgs e)
    {
        LocalizeText();
    }


    /// <summary>
    /// Localize menu item text.
    /// </summary>
    private void LocalizeText()
    {
        var localizedText = AP.Config.Lang[LangKey, LangParams];
        if (string.IsNullOrWhiteSpace(localizedText)) return;

        _menuItem.Text = localizedText;
    }

}
