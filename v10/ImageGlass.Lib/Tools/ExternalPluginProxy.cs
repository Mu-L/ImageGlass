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
using ImageGlass.SDK;
using ImageGlass.UI.Viewer;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Plugins.External;


/// <summary>
/// Proxy that satisfies the <see cref="IPlugin"/> interface for a non-hosted
/// external (out-of-process) plugin. Method calls are forwarded to the plugin
/// process via named pipe.
/// </summary>
internal sealed class ExternalPluginProxy : IPlugin
{
    private readonly PluginManifest _manifest;
    private readonly PluginProcessManager _processManager;
    private readonly string _pluginDir;

    public string PluginId => _manifest.Id;
    public bool IsHosted => false;
    public object? Settings => null;
    public ViewerControl Viewer { get; set; } = null!;

    /// <summary>The original manifest for menu building.</summary>
    internal PluginManifest Manifest => _manifest;

    /// <summary>
    /// The directory where this plugin resides (contains the executable and manifest).
    /// </summary>
    internal string PluginDir => _pluginDir;


    public ExternalPluginProxy(PluginManifest manifest, PluginProcessManager processManager, string pluginDir)
    {
        _manifest = manifest;
        _processManager = processManager;
        _pluginDir = pluginDir;
    }


    public async Task ExecuteAsync(PluginExecutionContext context)
    {
        // Start the plugin process if not already running
        var info = _processManager.GetRunningPlugin(PluginId);
        if (info is null)
        {
            info = await _processManager.StartPluginAsync(_manifest, _pluginDir);
            if (info is null) return;

            // Send INIT
            info.PipeHandler.SendEvent(MessageTypes.INIT, new PluginInitPayload
            {
                PluginId = PluginId,
                DataDirectory = _pluginDir,
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

        // Tell the plugin to execute
        info.PipeHandler.SendEvent(MessageTypes.EXECUTE);
    }
}
