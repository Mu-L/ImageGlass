/*
ImageGlass - A lightweight, versatile image viewer
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
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
using ImageGlass.SDK.Tools;
using ImageGlass.UI.Viewer;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Tools;


/// <summary>
/// Proxy that satisfies the <see cref="ITool"/> interface for a non-hosted
/// external (out-of-process) tool described by an <see cref="ExternalTool"/>
/// entry in <c>Config.Tools</c>.
///
/// When <see cref="ExternalTool.IsIntegrated"/> is <c>true</c>, the tool is launched
/// as an integrated child process connected to the host via
/// <see cref="ToolPipeServer"/>. Otherwise the executable is launched
/// detached with no IPC.
/// </summary>
internal sealed class ExternalToolProxy : ITool
{
    private readonly ExternalTool _tool;
    private readonly ToolProcessManager _processManager;

    public string ToolId => _tool.ToolId;
    public bool IsHosted => false;
    public object? Settings => null;
    public ViewerControl Viewer { get; set; } = null!;

    /// <summary>The original registration for menu building.</summary>
    internal ExternalTool Tool => _tool;


    public ExternalToolProxy(ExternalTool tool, ToolProcessManager processManager)
    {
        _tool = tool;
        _processManager = processManager;
    }


    public async Task ExecuteAsync(ToolExecutionContext context)
    {
        // Detached mode: just spawn the executable with arguments and walk away.
        if (!_tool.IsIntegrated)
        {
            LaunchDetached(context);
            return;
        }

        // Integrated mode: pipe-IPC.
        var info = _processManager.GetRunningTool(ToolId);
        if (info is null)
        {
            info = await _processManager.StartToolAsync(_tool);
            if (info is null) return;

            // Send INIT
            info.PipeHandler.SendEvent(MessageTypes.INIT, new ToolInitPayload
            {
                ToolId = ToolId,
                DataDirectory = Path.GetDirectoryName(_tool.Executable) ?? string.Empty,
                PipeName = info.PipeName,
                ThemeInfo = new ThemeInfo
                {
                    IsDarkMode = Core.Theme.Settings.IsDarkMode,
                    AccentColor = Core.AccentColor.ToString(),
                    BackgroundColor = Core.Config.BackgroundColor,
                },
            });

            // Start message loop in the background
            _ = Task.Run(() => info.PipeHandler.RunMessageLoopAsync(CancellationToken.None));
        }

        // Tell the tool to execute
        info.PipeHandler.SendEvent(MessageTypes.EXECUTE);
    }


    private void LaunchDetached(ToolExecutionContext context)
    {
        if (string.IsNullOrEmpty(_tool.Executable)) return;

        var filePath = Core.Photos?.Current?.FilePath ?? string.Empty;
        var args = (_tool.Arguments ?? string.Empty).Replace("{file_path}", filePath);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _tool.Executable,
                Arguments = args,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(_tool.Executable) ?? string.Empty,
            });
        }
        catch
        {
            // best-effort launch
        }
    }
}
