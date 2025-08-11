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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageGlass.WinNT;


public sealed partial class ToolbarControl : UserControl
{

    public ToolbarControl()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Gets button by ID, option to update the button.
    /// </summary>
    public ICommandBarElement? GetButton(string id, Action<ICommandBarElement>? action = null)
    {
        var cmdItem = ToolbarMain.PrimaryCommands.FirstOrDefault(i => (i as AppBarButton)?.Name == id);
        if (cmdItem == null) return null;

        if (action != null)
        {
            action(cmdItem);
        }

        return cmdItem;
    }


    /// <summary>
    /// Adds a new button to the toolbar.
    /// </summary>
    public bool AddButton(ToolbarItemModel item, int position = -1)
    {
        // check for duplicated id
        if (GetButton(item.Id) != null) return false;


        // create command bar item
        ICommandBarElement cmdItem;
        if (item.Type == ToolbarItemType.Separator)
        {
            cmdItem = new AppBarSeparator()
            {
                Name = item.Id,
            };
        }
        else
        {
            cmdItem = new IgToolbarButton()
            {
                Name = item.Id,
                Icon = new SymbolIcon(Symbol.Placeholder),
                Label = item.Text,
                LabelPosition = item.DisplayStyle == ToolbarItemDisplayStyle.ImageAndText
                    ? CommandBarLabelPosition.Default
                    : CommandBarLabelPosition.Collapsed,
                IsSelected = !string.IsNullOrWhiteSpace(item.CheckableConfigBinding),
            };
        }


        // insert to the list
        if (position >= 0)
        {
            ToolbarMain.PrimaryCommands.Insert(position, cmdItem);
        }
        else
        {
            ToolbarMain.PrimaryCommands.Add(cmdItem);
        }

        return true;
    }


    /// <summary>
    /// Adds an array of buttons to the toolbar.
    /// </summary>
    public void AddButtons(IEnumerable<ToolbarItemModel> items)
    {
        foreach (var item in items)
        {
            AddButton(item);
        }
    }


    /// <summary>
    /// Removes a button from toolbar.
    /// </summary>
    public bool RemoveButton(string id)
    {
        var cmdItem = GetButton(id);
        if (cmdItem == null) return false;

        return ToolbarMain.PrimaryCommands.Remove(cmdItem);
    }


    /// <summary>
    /// Deletes all buttons from toolbar.
    /// </summary>
    public void ClearAllButtons()
    {
        ToolbarMain.PrimaryCommands.Clear();
    }

}
