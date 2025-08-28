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
using ImageGlass.Win64.Common.Commands;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace ImageGlass;

public partial class MainWindow
{
    private Dictionary<string, ICommand> _apis = new(StringComparer.OrdinalIgnoreCase);


    public ICommand? GetApiCommand(API api)
    {
        return GetApiCommand(api.ToString());
    }


    public ICommand? GetApiCommand(string apiName)
    {
        // get the command from API name
        _apis.TryGetValue(apiName, out var cmd);

        return cmd;
    }


    public void RunApi(API api, string? args = null)
    {
        // get the command from API name
        if (GetApiCommand(api) is not ICommand cmd) return;

        // check if the command can be executed
        if (!cmd.CanExecute(args)) return;

        // execute the command
        cmd.Execute(args);
    }


    public void CreateAppAPIs()
    {
        // Main Menu
        _apis.Add(nameof(API.IG_OpenFile), new RelayCommand(() => IG_OpenFile()));


    }


}
