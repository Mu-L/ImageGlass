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
using System.Runtime.InteropServices;

namespace ImageGlass.Plugins;


/// <summary>
/// Wraps a single loaded native plugin shared library and the function pointers it returned.
/// </summary>
internal sealed unsafe class NativePlugin : PhDisposable
{
    /// <summary>
    /// Gets the manifest ID of the loaded plugin.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets the absolute path to the loaded shared library.
    /// </summary>
    public string LibraryPath { get; }

    /// <summary>
    /// Gets the native library handle returned by <see cref="NativeLibrary.Load(string)"/>.
    /// </summary>
    public nint LibraryHandle { get; }

    /// <summary>
    /// Gets the root API table exported by the plugin.
    /// </summary>
    public IGPluginApi* PluginApi { get; }

    /// <summary>
    /// Gets the codec entries advertised by the plugin during probing.
    /// </summary>
    public List<NativeCodecEntry> Codecs { get; } = [];


    /// <summary>
    /// Creates a wrapper around one loaded native plugin instance.
    /// </summary>
    public NativePlugin(string pluginId, string libraryPath, nint libraryHandle, IGPluginApi* pluginApi)
    {
        PluginId = pluginId;
        LibraryPath = libraryPath;
        LibraryHandle = libraryHandle;
        PluginApi = pluginApi;
    }


    /// <summary>
    /// Shuts the plugin down and releases the native library handle.
    /// </summary>
    protected override void OnDisposing()
    {
        // Give the plugin a chance to release its own process-lifetime state.
        try
        {
            if (PluginApi != null && PluginApi->Shutdown != null)
            {
                PluginApi->Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NativePlugin] Shutdown threw for '{PluginId}': {ex.Message}");
        }

        // Release the loaded shared library after shutdown has completed.
        try
        {
            if (LibraryHandle != 0) NativeLibrary.Free(LibraryHandle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NativePlugin] NativeLibrary.Free threw for '{PluginId}': {ex.Message}");
        }

        base.OnDisposing();
    }
}


/// <summary>
/// Holds a single codec API pointer alongside its capability descriptor.
/// Wraps the pointer in <see cref="nint"/> so it can be used as a generic type argument.
/// </summary>
internal readonly struct NativeCodecEntry
{
    /// <summary>
    /// Gets the raw pointer to the codec-specific API table.
    /// </summary>
    public readonly nint CodecApiPtr;

    /// <summary>
    /// Gets the managed capability snapshot gathered during probing.
    /// </summary>
    public readonly CodecPluginCapability Capability;

    /// <summary>
    /// Creates one plugin-codec entry pairing the API pointer with its capability data.
    /// </summary>
    public NativeCodecEntry(nint codecApiPtr, CodecPluginCapability capability)
    {
        CodecApiPtr = codecApiPtr;
        Capability = capability;
    }
}

