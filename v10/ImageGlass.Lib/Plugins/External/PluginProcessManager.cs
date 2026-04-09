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
using ImageGlass.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Plugins.External;


/// <summary>
/// Information about a running external plugin process.
/// </summary>
internal sealed class PluginProcessInfo
{
    public required PluginManifest Manifest { get; init; }
    public required Process Process { get; init; }
    public required NamedPipeServerStream PipeServer { get; init; }
    public required string PipeName { get; init; }
    public required PluginPipeServer PipeHandler { get; init; }
}


/// <summary>
/// Manages external plugin processes: discovery, spawning, pipe server, and lifecycle.
/// </summary>
public sealed class PluginProcessManager : PhDisposable
{
    private readonly Dictionary<string, PluginProcessInfo> _processes = [];
    private readonly Lock _lock = new();


    /// <summary>
    /// Scans the plugins directory for manifest files and returns
    /// tuples of (manifest, pluginDir) for each valid plugin found.
    /// Does NOT start processes — just discovers available plugins.
    /// </summary>
    public static List<(PluginManifest Manifest, string PluginDir)> DiscoverPlugins(string pluginsDirectory)
    {
        var results = new List<(PluginManifest, string)>();
        if (!Directory.Exists(pluginsDirectory)) return results;

        foreach (var dir in Directory.EnumerateDirectories(pluginsDirectory))
        {
            var manifestPath = Path.Combine(dir, PluginManifest.FILE_NAME);
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize(json, PluginJsonContext.Default.PluginManifest);
                if (manifest is not null && !string.IsNullOrEmpty(manifest.Id))
                {
                    results.Add((manifest, dir));
                }
            }
            catch
            {
                // skip malformed manifests
            }
        }

        return results;
    }


    /// <summary>
    /// Starts a plugin process: creates named pipe, spawns process, and waits for connection.
    /// </summary>
    internal async Task<PluginProcessInfo?> StartPluginAsync(PluginManifest manifest, string pluginDir)
    {
        // macOS limits Unix domain socket paths to ~104 chars — keep pipe names short
        var pipeName = $"ig_{Guid.NewGuid().ToString("N")[..8]}";

        var pipeServer = new NamedPipeServerStream(
            pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        // Spawn plugin process
        var exePath = Path.Combine(pluginDir, manifest.Executable);
        var args = $"--pipe {pipeName}";

        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = false,
                WorkingDirectory = pluginDir,
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

        // Wait for plugin to connect (10 second timeout)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await pipeServer.WaitForConnectionAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Plugin didn't connect in time
            try { process.Kill(); } catch { }
            await pipeServer.DisposeAsync();
            return null;
        }

        var pipeHandler = new PluginPipeServer(pipeServer, manifest.Id);

        var info = new PluginProcessInfo
        {
            Manifest = manifest,
            Process = process,
            PipeServer = pipeServer,
            PipeName = pipeName,
            PipeHandler = pipeHandler,
        };

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => CleanupExitedProcess(manifest.Id, info);

        lock (_lock)
        {
            _processes[manifest.Id] = info;
        }

        return info;
    }


    /// <summary>
    /// Sends shutdown to plugin and kills the process if it doesn't exit gracefully.
    /// </summary>
    public async Task StopPluginAsync(string pluginId)
    {
        PluginProcessInfo? info;
        lock (_lock)
        {
            if (!_processes.Remove(pluginId, out info)) return;
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


    /// <summary>Stops all running plugin processes.</summary>
    public async Task StopAllAsync()
    {
        string[] ids;
        lock (_lock)
        {
            ids = [.. _processes.Keys];
        }

        foreach (var id in ids)
        {
            await StopPluginAsync(id);
        }
    }


    /// <summary>Gets information about a running plugin, or null.</summary>
    internal PluginProcessInfo? GetRunningPlugin(string pluginId)
    {
        PluginProcessInfo? exitedInfo = null;

        lock (_lock)
        {
            _processes.TryGetValue(pluginId, out var info);
            if (info is not null && info.Process.HasExited)
            {
                _processes.Remove(pluginId);
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


    /// <summary>Gets whether a plugin process is currently running.</summary>
    public bool IsRunning(string pluginId)
    {
        return GetRunningPlugin(pluginId) is not null;
    }


    /// <summary>Broadcasts an event to all running plugin processes.</summary>
    public void BroadcastToAll(string type, object? payload = null)
    {
        PluginProcessInfo[] infos;
        lock (_lock)
        {
            infos = [.. _processes.Values];
        }

        foreach (var info in infos)
        {
            if (info.Process.HasExited)
            {
                CleanupExitedProcess(info.Manifest.Id, info);
                continue;
            }

            try
            {
                info.PipeHandler.SendEvent(type, payload);
            }
            catch
            {
                CleanupExitedProcess(info.Manifest.Id, info);
            }
        }
    }


    /// <summary>Broadcasts an event only to plugins that have subscribed to it.</summary>
    public void BroadcastToSubscribed(string type, object? payload, Func<PluginEventSubscriptions, bool> filter)
    {
        PluginProcessInfo[] infos;
        lock (_lock)
        {
            infos = [.. _processes.Values];
        }

        foreach (var info in infos)
        {
            if (info.Process.HasExited)
            {
                CleanupExitedProcess(info.Manifest.Id, info);
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
                CleanupExitedProcess(info.Manifest.Id, info);
            }
        }
    }


    private void CleanupExitedProcess(string pluginId, PluginProcessInfo info)
    {
        var shouldDispose = false;

        lock (_lock)
        {
            if (_processes.TryGetValue(pluginId, out var current) && ReferenceEquals(current, info))
            {
                _processes.Remove(pluginId);
                shouldDispose = true;
            }
        }

        if (shouldDispose)
        {
            DisposeProcessInfo(info);
        }
    }


    private static void DisposeProcessInfo(PluginProcessInfo info)
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
