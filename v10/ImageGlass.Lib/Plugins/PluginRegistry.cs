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
using ImageGlass.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ImageGlass.Plugins;


/// <summary>
/// Loads native (in-process) codec plugins, validates their ABI surface,
/// and produces <see cref="NativeCodecProxy"/> instances ready to register in the codec registry.
/// </summary>
public sealed unsafe class PluginRegistry : PhDisposable
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, NativePlugin> _plugins = new(StringComparer.Ordinal);

    private static int DecodeMajor(int abiVersion) => abiVersion / 1_000_000;


    /// <summary>
    /// Tracks per-plugin crash markers and session-disabled state.
    /// Owned by this registry so that quarantine policy and the loaded plugin
    /// table share the same lifetime.
    /// </summary>
    public PluginFailureManager FailureManager { get; } = new();


    /// <summary>
    /// Loads the native plugin and probes its codecs. Returns null on any failure.
    /// </summary>
    internal NativePlugin? LoadAndProbe(PluginManifest manifest, string pluginDir)
    {
        if (manifest.Kind != IGPluginKind.Codec) return null;
        if (string.IsNullOrEmpty(manifest.Executable))
        {
            Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' has no Executable; skipping.");
            return null;
        }

        if (FailureManager.IsQuarantined(manifest.Id))
        {
            Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' is quarantined; skipping.");
            return null;
        }

        var libraryPath = Path.Combine(pluginDir, manifest.Executable);
        if (!File.Exists(libraryPath))
        {
            Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' library not found: {libraryPath}");
            return null;
        }

        nint libHandle = 0;
        IGPluginApi* pluginApi = null;
        try
        {
            // 1. Load the library
            libHandle = NativeLibrary.Load(libraryPath);

            // 2. Resolve the entry point
            if (!NativeLibrary.TryGetExport(libHandle, IGNativeAbi.ENTRY_POINT_NAME, out var entryAddr))
            {
                Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' missing export '{IGNativeAbi.ENTRY_POINT_NAME}'.");
                NativeLibrary.Free(libHandle);
                return null;
            }
            var entry = (delegate* unmanaged[Cdecl]<int, IGHostApi*, IGPluginApi*>)entryAddr;

            // 3. Negotiate ABI
            var hostApi = PluginHostApiTable.Get();
            try
            {
                pluginApi = entry(IGNativeAbi.IG_PLUGIN_ABI_VERSION, hostApi);
            }
            catch (Exception ex)
            {
                FailureManager.Quarantine(manifest.Id, $"entry point threw: {ex.Message}");
                NativeLibrary.Free(libHandle);
                return null;
            }
            if (pluginApi == null)
            {
                Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' entry point returned null (rejected).");
                NativeLibrary.Free(libHandle);
                return null;
            }

            // 4. Validate plugin ABI
            if (pluginApi->StructSize != sizeof(IGPluginApi))
            {
                Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' StructSize mismatch (got {pluginApi->StructSize}, expected {sizeof(IGPluginApi)}).");
                NativeLibrary.Free(libHandle);
                return null;
            }
            if (DecodeMajor(pluginApi->AbiVersion) != IGNativeAbi.IG_PLUGIN_ABI_MAJOR)
            {
                Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' ABI major mismatch (plugin={pluginApi->AbiVersion}, host={IGNativeAbi.IG_PLUGIN_ABI_VERSION}).");
                NativeLibrary.Free(libHandle);
                return null;
            }

            // 5. Optional initialize
            if (pluginApi->Initialize != null)
            {
                IGStatus initStatus;
                try { initStatus = pluginApi->Initialize(); }
                catch (Exception ex)
                {
                    FailureManager.Quarantine(manifest.Id, $"Initialize threw: {ex.Message}");
                    NativeLibrary.Free(libHandle);
                    return null;
                }
                if (initStatus != IGStatus.OK)
                {
                    Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' Initialize returned {initStatus}.");
                    NativeLibrary.Free(libHandle);
                    return null;
                }
            }

            var handle = new NativePlugin(manifest.Id, libraryPath, libHandle, pluginApi);

            // 6. Enumerate codecs
            var capabilities = new List<CodecPluginCapability>(pluginApi->Info.CodecCount);
            for (var i = 0; i < pluginApi->Info.CodecCount; i++)
            {
                IGCodecApi* codecApi = null;
                IGStatus status;
                try { status = pluginApi->GetCodec(i, &codecApi); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' GetCodec[{i}] threw: {ex.Message}");
                    continue;
                }
                if (status != IGStatus.OK || codecApi == null) continue;
                if (codecApi->GetCapability == null) continue;


                IGCodecCapability cap = default;
                try { status = codecApi->GetCapability(&cap); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' codec[{i}].GetCapability threw: {ex.Message}");
                    continue;
                }
                if (status != IGStatus.OK) continue;


                var managed = MarshalCapability(in cap);
                capabilities.Add(managed);
                handle.Codecs.Add(new NativeCodecEntry((nint)codecApi, managed));
            }

            // 7. Register with loader
            lock (_lock)
            {
                if (_plugins.TryGetValue(manifest.Id, out var existing))
                {
                    existing.Dispose();
                }
                _plugins[manifest.Id] = handle;
            }

            return handle;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PluginRegistry] '{manifest.Id}' load failed: {ex.Message}");
            FailureManager.Quarantine(manifest.Id, $"load failed: {ex.Message}");

            try
            {
                if (libHandle != 0)
                {
                    NativeLibrary.Free(libHandle);
                }
            }
            catch { }

            return null;
        }
    }


    /// <summary>
    /// Builds <see cref="NativeCodecProxy"/> instances for every codec advertised by the plugin.
    /// </summary>
    internal IEnumerable<NativeCodecProxy> CreateProxies(NativePlugin handle)
    {
        var list = new List<NativeCodecProxy>(handle.Codecs.Count);
        foreach (var entry in handle.Codecs)
        {
            unsafe
            {
                list.Add(new NativeCodecProxy(handle, (IGCodecApi*)entry.CodecApiPtr, entry.Capability, FailureManager));
            }
        }
        return list;
    }


    private static CodecPluginCapability MarshalCapability(in IGCodecCapability cap)
    {
        var exts = new List<string>(cap.ExtensionCount);
        if (cap.Extensions != null)
        {
            for (var i = 0; i < cap.ExtensionCount; i++)
            {
                exts.Add(cap.Extensions[i].ToManaged());
            }
        }

        return new CodecPluginCapability
        {
            CodecId = cap.CodecId.ToManaged(),
            CodecName = cap.CodecName.ToManaged(),
            MetadataPriority = cap.MetadataPriority,
            DecodePriority = cap.DecodePriority,
            SupportedExtensions = [.. exts],
            SupportsMetadata = cap.SupportsMetadata != 0,
            SupportsStaticRaster = cap.SupportsStaticRaster != 0,
            SupportsColorProfiles = cap.SupportsColorProfiles != 0,
        };
    }


    protected override void OnDisposing()
    {
        lock (_lock)
        {
            foreach (var handle in _plugins.Values)
            {
                try { handle.Dispose(); } catch { }
            }
            _plugins.Clear();
        }
        base.OnDisposing();
    }


    /// <summary>
    /// Scans the given directory for native plugin manifests
    /// (<c>imageglass.plugin.json</c>) and returns one entry per valid manifest found.
    /// Does NOT load any libraries; manifest deserialization only.
    /// </summary>
    public static List<(PluginManifest Manifest, string PluginDir)> DiscoverManifests(string pluginsDirectory)
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
                var manifest = System.Text.Json.JsonSerializer.Deserialize(json, PluginJsonContext.Default.PluginManifest);
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

}

