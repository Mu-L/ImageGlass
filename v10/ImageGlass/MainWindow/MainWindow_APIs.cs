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
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ImageGlass;

public partial class MainWindow
{
    private FrozenDictionary<string, IIgCommand> _apis => new Dictionary<string, IIgCommand>(StringComparer.OrdinalIgnoreCase)
    {
        // Main Menu
        {nameof(API.IG_OpenFile), IgCommands.Create(IG_OpenFileAsync)},
        {nameof(API.IG_OpenFolder), IgCommands.Create(IG_OpenFolderAsync)},
        {nameof(API.IG_OpenPath), IgCommands.Create(IG_OpenPath)},


        // Navigation
        {nameof(API.IG_ViewByStep), IgCommands.Create(IG_ViewByStep)},
        {nameof(API.IG_ViewByIndex), IgCommands.Create(IG_ViewByIndex)},


        // Layout
        {nameof(API.IG_ToggleCheckerboard), IgCommands.Create(IG_ToggleCheckerboard)},


        // Exit
        {nameof(API.IG_Exit), IgCommands.Create(IG_Exit)},
    }.ToFrozenDictionary();


    /// <summary>
    /// Gets the API command.
    /// </summary>
    public IIgCommand? GetApiCommand(API api)
    {
        return GetApiCommand(api.ToString());
    }


    /// <summary>
    /// Gets the API command.
    /// </summary>
    public IIgCommand? GetApiCommand(string? apiName)
    {
        // get the command from API name
        _apis.TryGetValue(apiName ?? "", out var cmd);

        return cmd;
    }


    /// <summary>
    /// Executes the given built-in API command.
    /// </summary>
    public async Task<ActionResult> RunApiAsync(API api, string? args = null)
    {
        return await RunApiAsync(api.ToString(), args);
    }


    /// <summary>
    /// Executes the given built-in API command.
    /// </summary>
    public async Task<ActionResult> RunApiAsync(string? apiName, string? args = null)
    {
        // get the command from API name
        if (GetApiCommand(apiName) is not IIgCommand cmd)
            return new ActionResult(ActionExitCode.ApiNotFound);

        // check if the command can be executed
        if (!cmd.CanExecute(args))
            return new ActionResult(ActionExitCode.Cancelled);

        try
        {
            // execute the command
            if (cmd.IsAsync)
            {
                await cmd.ExecuteAsync(args);
            }
            else
            {
                cmd.Execute(args);
            }
        }
        catch (Exception ex)
        {
            return new ActionResult(ActionExitCode.Error, ex);
        }

        return new ActionResult(ActionExitCode.Success);
    }


    /// <summary>
    /// Executes a single action.
    /// </summary>
    public async Task<Exception?> RunActionAsync(SingleAction? ac)
    {
        if (string.IsNullOrWhiteSpace(ac?.Executable)) return null;

        // 1. run the current action
        var acResults = await RunApiAsync(ac.Executable, ac.Argument);


        // 2. exit if the action was cancelled.
        if (acResults.ExitCode == ActionExitCode.Cancelled) return null;


        // 3. run next action on success
        if (acResults.ExitCode == ActionExitCode.Success)
        {
            return await RunActionAsync(ac.NextAction);
        }


        // 4. if there was an error from API
        Exception? error = null;
        if (acResults.ExitCode == ActionExitCode.Error && acResults.Error != null)
        {
            // TODO: show error message
            error = acResults.Error;
        }


        // 5. if action name is not an built-in API
        // try to run with Shell
        else if (acResults.ExitCode == ActionExitCode.ApiNotFound)
        {
            var args = string.Join("", ac.Argument) ?? string.Empty;
            var exeInfo = BHelper.BuildExeArgs(ac.Executable, args, AP.Photos.CurrentFilePath);

            var exeCode = await BHelper.RunExeCmd(exeInfo.Executable, exeInfo.Args, false, false);
            if (exeCode != IgExitCode.Done)
            {
                var errorMsg = AP.Config.Lang["_._UserAction._Win32ExeError", ac.Executable];
                error = new Win32Exception(errorMsg);
            }
        }


        return error;
    }



}
