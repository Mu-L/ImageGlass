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
using Microsoft.UI.Xaml.Controls;

namespace ImageGlass.UI;

public partial class IgToggleMenuItem : ToggleMenuFlyoutItem
{
    private MenuItemHelper _helper;


    /// <summary>
    /// Gets, sets the language key for localization.
    /// </summary>
    public LangId? LangKey
    {
        get => _helper.LangKey;
        set => _helper.LangKey = value;
    }


    /// <summary>
    /// Gets, sets the language param for localization.
    /// </summary>
    public object? LangParams
    {
        get => _helper.LangParams;
        set => _helper.LangParams = value;
    }


    public IgToggleMenuItem()
    {
        DefaultStyleKey = typeof(IgToggleMenuItem);
        _helper = new(this);
    }


}
