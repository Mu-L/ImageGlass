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
using ImageGlass.SDK;
using ImageGlass.SDK.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ImageGlass.Plugins.Native;


/// <summary>
/// Loads native (in-process) codec plugins, validates their ABI surface,
/// and produces <see cref="NativeCodecProxy"/> instances ready to register in the codec registry.
/// </summary>
public sealed unsafe class NativePluginLoader : Common.Types.PhDisposable
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, NativePluginHandle> _loaded = new(StringComparer.Ordinal);
    private readonly NativePluginQuarantine _quarantine;

    public NativePluginLoader(NativePluginQuarantine quarantine)
    {
        _quarantine = quarantine;
    }


    /// <summary>
    /// Loads the native plugin and probes its codecs. Returns null on any failure.
    /// </summary>
    internal NativePluginHandle? LoadAndProbe(PluginManifest manifest, string pluginDir)
    {
        if (manifest.Kind != IGPluginKind.Native) return null;
        if (string.IsNullOrEmpty(manifest.Executable))
        {
            Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' has no Executable; skipping.");
            return null;
        }

        if (_quarantine.IsQuarantined(manifest.Id))
        {
            Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' is quarantined; skipping.");
            return null;
        }

        var libraryPath = Path.Combine(pluginDir, manifest.Executable);
        if (!File.Exists(libraryPath))
        {
            Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' library not found: {libraryPath}");
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
                Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' missing export '{IGNativeAbi.ENTRY_POINT_NAME}'.");
                NativeLibrary.Free(libHandle);
                return null;
            }
            var entry = (delegate* unmanaged[Cdecl]<int, IGHostApi*, IGPluginApi*>)entryAddr;

            // 3. Negotiate ABI
            var hostApi = NativeHostApiTable.Get();
            try
            {
                pluginApi = entry(IGNativeAbi.IG_PLUGIN_ABI_VERSION, hostApi);
            }
            catch (Exception ex)
            {
                _quarantine.Quarantine(manifest.Id, $"entry point threw: {ex.Message}");
                NativeLibrary.Free(libHandle);
                return null;
            }
            if (pluginApi == null)
            {
                Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' entry point returned null (rejected).");
                NativeLibrary.Free(libHandle);
                return null;
            }

            // 4. Validate plugin ABI
            if (pluginApi->StructSize != sizeof(IGPluginApi))
            {
                Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' StructSize mismatch (got {pluginApi->StructSize}, expected {sizeof(IGPluginApi)}).");
                NativeLibrary.Free(libHandle);
                return null;
            }
            if (DecodeMajor(pluginApi->AbiVersion) != IGNativeAbi.IG_PLUGIN_ABI_MAJOR)
            {
                Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' ABI major mismatch (plugin={pluginApi->AbiVersion}, host={IGNativeAbi.IG_PLUGIN_ABI_VERSION}).");
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
                    _quarantine.Quarantine(manifest.Id, $"Initialize threw: {ex.Message}");
                    NativeLibrary.Free(libHandle);
                    return null;
                }
                if (initStatus != IGStatus.OK)
                {
                    Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' Initialize returned {initStatus}.");
                    NativeLibrary.Free(libHandle);
                    return null;
                }
            }

            var handle = new NativePluginHandle(manifest.Id, libraryPath, libHandle, pluginApi);

            // 6. Enumerate codecs
            var capabilities = new List<CodecPluginCapability>(pluginApi->Info.CodecCount);
            for (var i = 0; i < pluginApi->Info.CodecCount; i++)
            {
                IGCodecApi* codecApi = null;
                IGStatus status;
                try { status = pluginApi->GetCodec(i, &codecApi); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' GetCodec[{i}] threw: {ex.Message}");
                    continue;
                }
                if (status != IGStatus.OK || codecApi == null) continue;
                if (codecApi->GetCapability == null) continue;

                IGCodecCapability cap = default;
                try { status = codecApi->GetCapability(&cap); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' codec[{i}].GetCapability threw: {ex.Message}");
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
                if (_loaded.TryGetValue(manifest.Id, out var existing))
                {
                    existing.Dispose();
                }
                _loaded[manifest.Id] = handle;
            }

            return handle;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NativePluginLoader] '{manifest.Id}' load failed: {ex.Message}");
            _quarantine.Quarantine(manifest.Id, $"load failed: {ex.Message}");
            try { if (libHandle != 0) NativeLibrary.Free(libHandle); } catch { }
            return null;
        }
    }


    /// <summary>
    /// Builds <see cref="NativeCodecProxy"/> instances for every codec advertised by the plugin.
    /// </summary>
    internal IEnumerable<NativeCodecProxy> CreateProxies(NativePluginHandle handle)
    {
        var list = new List<NativeCodecProxy>(handle.Codecs.Count);
        foreach (var entry in handle.Codecs)
        {
            unsafe
            {
                list.Add(new NativeCodecProxy(handle, (IGCodecApi*)entry.CodecApiPtr, entry.Capability, _quarantine));
            }
        }
        return list;
    }


    private static int DecodeMajor(int abiVersion) => abiVersion / 1_000_000;


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
            SupportsAnimation = cap.SupportsAnimation != 0,
            SupportsVector = cap.SupportsVector != 0,
            SupportsHdr = cap.SupportsHdr != 0,
            SupportsColorProfiles = cap.SupportsColorProfiles != 0,
        };
    }


    protected override void OnDisposing()
    {
        lock (_lock)
        {
            foreach (var handle in _loaded.Values)
            {
                try { handle.Dispose(); } catch { }
            }
            _loaded.Clear();
        }
        base.OnDisposing();
    }
}
