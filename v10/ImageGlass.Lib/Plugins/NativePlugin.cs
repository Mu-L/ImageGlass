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
    public string PluginId { get; }
    public string LibraryPath { get; }
    public nint LibraryHandle { get; }
    public IGPluginApi* PluginApi { get; }
    public List<NativeCodecEntry> Codecs { get; } = [];


    public NativePlugin(string pluginId, string libraryPath, nint libraryHandle, IGPluginApi* pluginApi)
    {
        PluginId = pluginId;
        LibraryPath = libraryPath;
        LibraryHandle = libraryHandle;
        PluginApi = pluginApi;
    }


    protected override void OnDisposing()
    {
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
    public readonly nint CodecApiPtr;
    public readonly CodecPluginCapability Capability;

    public NativeCodecEntry(nint codecApiPtr, CodecPluginCapability capability)
    {
        CodecApiPtr = codecApiPtr;
        Capability = capability;
    }
}

