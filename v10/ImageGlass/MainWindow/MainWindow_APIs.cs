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
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ImageGlass;

public partial class MainWindow
{
    private FrozenDictionary<API, IIgCommand> _apis => new Dictionary<API, IIgCommand>()
    {
        { API.IG_OpenMainMenu,          IgCommands.Create(IG_OpenMainMenu) },

        { API.IG_OpenFolder,            IgCommands.Create(IG_OpenFolderAsync) },
        { API.IG_OpenPath,              IgCommands.Create(IG_OpenPath) },


        // Main Menu
        { API.IG_OpenFile,              IgCommands.Create(IG_OpenFileAsync) },
        { API.IG_SaveAs,                IgCommands.Create(IG_SaveAsAsync) },
        { API.IG_OpenWith,              IgCommands.Create(IG_OpenWithAsync) },
        { API.IG_Refresh,               IgCommands.Create(IG_Refresh) },
        { API.IG_Reload,                IgCommands.Create(IG_Reload) },
        { API.IG_ReloadList,            IgCommands.Create(IG_ReloadList) },
        { API.IG_Unload,                IgCommands.Create(IG_UnloadAsync) },              


        // Navigation
        { API.IG_ViewNext,              IgCommands.Create(IG_ViewNext) },
        { API.IG_ViewPrevious,          IgCommands.Create(IG_ViewPrevious) },
        { API.IG_Goto,                  IgCommands.Create(IG_GoTo) },
        { API.IG_GotoFirst,             IgCommands.Create(IG_GoToFirst) },
        { API.IG_GotoLast,              IgCommands.Create(IG_GoToLast) },

        { API.IG_ViewByStep,            IgCommands.Create(IG_ViewByStep) },
        { API.IG_ViewByIndex,           IgCommands.Create(IG_ViewByIndex) },


        // Zoom
        { API.IG_CustomZoom,            IgCommands.Create(IG_CustomZoom) },
        { API.IG_SetZoom,               IgCommands.Create(IG_SetZoom) },
        { API.IG_ZoomIn,                IgCommands.Create(IG_ZoomIn) },
        { API.IG_ZoomOut,               IgCommands.Create(IG_ZoomOut) },
        { API.IG_SetZoomMode,           IgCommands.Create(IG_SetZoomMode) },


        // Panning
        { API.IG_PanLeft,               IgCommands.Create(IG_PanLeft) },
        { API.IG_PanRight,              IgCommands.Create(IG_PanRight) },
        { API.IG_PanUp,                 IgCommands.Create(IG_PanUp) },
        { API.IG_PanDown,               IgCommands.Create(IG_PanDown) },
        { API.IG_PanToLeft,             IgCommands.Create(IG_PanToLeft) },
        { API.IG_PanToRight,            IgCommands.Create(IG_PanToRight) },
        { API.IG_PanToTop,              IgCommands.Create(IG_PanToTop) },
        { API.IG_PanToBottom,           IgCommands.Create(IG_PanToBottom) },


        // Image
        { API.IG_Rename,                IgCommands.Create(IG_RenameAsync) },
        { API.IG_OpenLocation,          IgCommands.Create(IG_OpenLocation) },
        { API.IG_OpenProperties,        IgCommands.Create(IG_OpenProperties) },


        // Clipboard
        { API.IG_PasteImage,            IgCommands.Create(IG_PasteImageAsync) },
        { API.IG_CopyImagePixels,       IgCommands.Create(IG_CopyImagePixelsAsync) },
        { API.IG_CopyFiles,             IgCommands.Create(IG_CopyFilesAsync) },
        { API.IG_CutFiles,              IgCommands.Create(IG_CutFilesAsync) },
        { API.IG_CopyImagePath,         IgCommands.Create(IG_CopyImagePath) },
        { API.IG_ClearClipboard,        IgCommands.Create(IG_ClearClipboardAsync) },


        // Layout
        { API.IG_ToggleToolbar,         IgCommands.Create(IG_ToggleToolbar) },
        { API.IG_ToggleGallery,         IgCommands.Create(IG_ToggleGallery) },
        { API.IG_ToggleCheckerboard,    IgCommands.Create(IG_ToggleCheckerboard) },
        { API.IG_ToggleWindowTopMost,   IgCommands.Create(IG_ToggleWindowTopMost) },


        // Exit
        { API.IG_Exit,                  IgCommands.Create(IG_Exit) },

    }.ToFrozenDictionary();


    /// <summary>
    /// Gets the API command.
    /// </summary>
    public IIgCommand? GetApiCommand(string? apiName)
    {
        if (Enum.TryParse<API>(apiName, out var api))
        {
            return GetApiCommand(api);
        }

        return null;
    }



    /// <summary>
    /// Gets the API command.
    /// </summary>
    public IIgCommand? GetApiCommand(API api)
    {
        return _apis.GetValueOrDefault(api);
    }



    /// <summary>
    /// Executes the given built-in API command.
    /// </summary>
    public async Task<ActionResult> RunApiAsync(API api, string? args = null)
    {
        var cmd = GetApiCommand(api);

        return await RunApiCommandAsync_(cmd, args);
    }



    /// <summary>
    /// Executes the given built-in API command.
    /// </summary>
    public async Task<ActionResult> RunApiAsync(string? apiName, string? args = null)
    {
        var cmd = GetApiCommand(apiName);

        return await RunApiCommandAsync_(cmd, args);
    }



    /// <summary>
    /// Executes the API command and returns results
    /// </summary>
    private static async Task<ActionResult> RunApiCommandAsync_(IIgCommand? cmd, string? args = null)
    {
        // get the command from API name
        if (cmd is null)
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
    /// Executes a single action, shows error popup.
    /// </summary>
    public async Task RunActionAsync(SingleAction? ac)
    {
        _ = await RunActionAsync(ac, true);
    }



    /// <summary>
    /// Executes a single action.
    /// </summary>
    public async Task<Exception?> RunActionAsync(SingleAction? ac, bool showError)
    {
        if (string.IsNullOrWhiteSpace(ac?.Executable)) return null;

        // 1. run the current action
        var acResults = await RunApiAsync(ac.Executable, ac.Argument);


        // 2. exit if the action was cancelled.
        if (acResults.ExitCode == ActionExitCode.Cancelled) return null;


        // 3. run next action on success
        if (acResults.ExitCode == ActionExitCode.Success)
        {
            return await RunActionAsync(ac.NextAction, showError);
        }


        // 4. if there was an error from API
        Exception? error = null;
        if (acResults.ExitCode == ActionExitCode.Error && acResults.Error != null)
        {
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
                var errorMsg = AP.Config.Lang[LangId._UserAction_Win32ExeError, ac.Executable];
                error = new Win32Exception(errorMsg);
            }
        }


        // 6. show error message
        if (error is not null && showError)
        {
            // get the language string for error title
            var errorTitle = AP.Config.Lang[ac.LangKey];

            _ = await ModalWindow.ShowErrorAsync(this, errorTitle, error.Message);
        }

        return error;
    }



}
