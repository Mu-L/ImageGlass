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
using ImageGlass.Common.Types;
using ImageGlass.SDK.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Tools;


/// <summary>
/// Information about a running external tool process.
/// </summary>
internal sealed class ToolProcessInfo
{
    public required ExternalTool Tool { get; init; }
    public required Process Process { get; init; }
    public required NamedPipeServerStream PipeServer { get; init; }
    public required string PipeName { get; init; }
    public required ToolPipeServer PipeHandler { get; init; }
}


/// <summary>
/// Manages external tool processes: spawning, pipe server, and lifecycle.
/// External tools are registered explicitly in <c>Config.Tools</c>; this manager
/// does not perform any folder discovery.
/// </summary>
public sealed class ToolProcessManager : PhDisposable
{
    private readonly Dictionary<string, ToolProcessInfo> _processes = [];
    private readonly Lock _lock = new();


    /// <summary>
    /// Starts a tool process: creates named pipe, spawns process, and waits for connection.
    /// </summary>
    internal async Task<ToolProcessInfo?> StartToolAsync(ExternalTool tool)
    {
        if (string.IsNullOrEmpty(tool.Executable)) return null;

        // macOS limits Unix domain socket paths to ~104 chars — keep pipe names short
        var pipeName = $"ig_{Guid.NewGuid().ToString("N")[..8]}";

        var pipeServer = new NamedPipeServerStream(
            pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        var workingDir = Path.GetDirectoryName(tool.Executable) ?? string.Empty;
        var args = $"--pipe {pipeName}";
        if (!string.IsNullOrEmpty(tool.Arguments))
        {
            args = $"{tool.Arguments} {args}";
        }

        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = tool.Executable,
                Arguments = args,
                UseShellExecute = false,
                WorkingDirectory = workingDir,
            });
        }
        catch
        {
            await pipeServer.DisposeAsync();
            return null;
        }

        if (process is null)
        {
            await pipeServer.DisposeAsync();
            return null;
        }

        // Wait for tool to connect (10 second timeout)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await pipeServer.WaitForConnectionAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Tool didn't connect in time
            try { process.Kill(); } catch { }
            await pipeServer.DisposeAsync();
            return null;
        }

        var pipeHandler = new ToolPipeServer(pipeServer, tool.ToolId);

        var info = new ToolProcessInfo
        {
            Tool = tool,
            Process = process,
            PipeServer = pipeServer,
            PipeName = pipeName,
            PipeHandler = pipeHandler,
        };

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => CleanupExitedProcess(tool.ToolId, info);

        lock (_lock)
        {
            _processes[tool.ToolId] = info;
        }

        return info;
    }


    /// <summary>
    /// Sends shutdown to tool and kills the process if it doesn't exit gracefully.
    /// <summary>
    /// Sends shutdown to tool and kills the process if it doesn't exit gracefully.
    /// </summary>
    public async Task StopToolAsync(string toolId)
    {
        ToolProcessInfo? info;
        lock (_lock)
        {
            if (!_processes.Remove(toolId, out info)) return;
        }

        try
        {
            if (!info.Process.HasExited)
            {
                info.PipeHandler.SendEvent(MessageTypes.SHUTDOWN);
            }

            // Wait a short time for graceful exit
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                if (!info.Process.HasExited)
                {
                    await info.Process.WaitForExitAsync(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                try { info.Process.Kill(); } catch { }
            }
        }
        catch { }
        finally
        {
            DisposeProcessInfo(info);
        }
    }


    /// <summary>Stops all running tool processes.</summary>
    public async Task StopAllAsync()
    {
        string[] ids;
        lock (_lock)
        {
            ids = [.. _processes.Keys];
        }

        foreach (var id in ids)
        {
            await StopToolAsync(id);
        }
    }


    /// <summary>Gets information about a running tool, or null.</summary>
    internal ToolProcessInfo? GetRunningTool(string toolId)
    {
        ToolProcessInfo? exitedInfo = null;

        lock (_lock)
        {
            _processes.TryGetValue(toolId, out var info);
            if (info is not null && info.Process.HasExited)
            {
                _processes.Remove(toolId);
                exitedInfo = info;
                info = null;
            }

            if (exitedInfo is null)
            {
                return info;
            }
        }

        DisposeProcessInfo(exitedInfo);
        return null;
    }


    /// <summary>Gets whether a tool process is currently running.</summary>
    public bool IsRunning(string toolId)
    {
        return GetRunningTool(toolId) is not null;
    }


    /// <summary>Broadcasts an event to all running tool processes.</summary>
    public void BroadcastToAll(string type, object? payload = null)
    {
        ToolProcessInfo[] infos;
        lock (_lock)
        {
            infos = [.. _processes.Values];
        }

        foreach (var info in infos)
        {
            if (info.Process.HasExited)
            {
                CleanupExitedProcess(info.Tool.ToolId, info);
                continue;
            }

            try
            {
                info.PipeHandler.SendEvent(type, payload);
            }
            catch
            {
                CleanupExitedProcess(info.Tool.ToolId, info);
            }
        }
    }


    /// <summary>Broadcasts an event only to tools that have subscribed to it.</summary>
    public void BroadcastToSubscribed(string type, object? payload, Func<ToolEventSubscriptions, bool> filter)
    {
        ToolProcessInfo[] infos;
        lock (_lock)
        {
            infos = [.. _processes.Values];
        }

        foreach (var info in infos)
        {
            if (info.Process.HasExited)
            {
                CleanupExitedProcess(info.Tool.ToolId, info);
                continue;
            }

            try
            {
                if (filter(info.PipeHandler.Subscriptions))
                {
                    info.PipeHandler.SendEvent(type, payload);
                }
            }
            catch
            {
                CleanupExitedProcess(info.Tool.ToolId, info);
            }
        }
    }


    private void CleanupExitedProcess(string toolId, ToolProcessInfo info)
    {
        var shouldDispose = false;

        lock (_lock)
        {
            if (_processes.TryGetValue(toolId, out var current) && ReferenceEquals(current, info))
            {
                _processes.Remove(toolId);
                shouldDispose = true;
            }
        }

        if (shouldDispose)
        {
            DisposeProcessInfo(info);
        }
    }


    private static void DisposeProcessInfo(ToolProcessInfo info)
    {
        try { info.PipeHandler.Dispose(); } catch { }
        try { info.PipeServer.Dispose(); } catch { }
        try { info.Process.Dispose(); } catch { }
    }


    protected override void OnDisposing()
    {
        // Synchronously dispose — StopAllAsync should have been called before
        lock (_lock)
        {
            foreach (var info in _processes.Values)
            {
                try
                {
                    info.PipeHandler.Dispose();
                    info.PipeServer.Dispose();
                    try { info.Process.Kill(); } catch { }
                    info.Process.Dispose();
                }
                catch { }
            }
            _processes.Clear();
        }
    }
}
