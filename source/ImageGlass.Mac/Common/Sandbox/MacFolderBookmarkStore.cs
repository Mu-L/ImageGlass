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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace ImageGlass.Mac.Common.Sandbox;


/// <summary>
/// Source-generated JSON context for the bookmark sidecar (AOT/trim-safe — no reflection).
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class MacBookmarkJsonContext : JsonSerializerContext
{
}


/// <summary>
/// Persists macOS security-scoped folder bookmarks to a sidecar JSON file in the
/// app's config directory (inside the sandbox container). Maps a granted folder
/// path to its Base64-encoded bookmark bytes so access can be re-established on
/// later launches without re-prompting the user.
/// </summary>
internal sealed class MacFolderBookmarkStore
{
    private static readonly string _filePath = Path.Combine(BHelper.ConfigPath, "mac-folder-bookmarks.json");
    private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();


    /// <summary>
    /// Loads persisted bookmarks from disk. Safe to call once at startup.
    /// </summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;

            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize(json, MacBookmarkJsonContext.Default.DictionaryStringString);
            if (data is null) return;

            lock (_lock)
            {
                _map.Clear();
                foreach (var kv in data) _map[kv.Key] = kv.Value;
            }
        }
        catch { }
    }


    /// <summary>
    /// Returns a bookmark whose granted folder equals <paramref name="folderPath"/>
    /// or is an ancestor of it (an ancestor grant covers the child folder).
    /// </summary>
    public bool TryGetCovering(string folderPath, out string base64Bookmark)
    {
        lock (_lock)
        {
            // exact match first
            if (_map.TryGetValue(folderPath, out var exact))
            {
                base64Bookmark = exact;
                return true;
            }

            // then any ancestor grant
            foreach (var kv in _map)
            {
                if (IsSameOrAncestor(kv.Key, folderPath))
                {
                    base64Bookmark = kv.Value;
                    return true;
                }
            }
        }

        base64Bookmark = string.Empty;
        return false;
    }


    /// <summary>
    /// Stores a bookmark for the granted folder and persists to disk.
    /// </summary>
    public void Set(string grantedFolderPath, string base64Bookmark)
    {
        lock (_lock)
        {
            _map[grantedFolderPath] = base64Bookmark;
            Save();
        }
    }


    /// <summary>
    /// Removes a stored bookmark (e.g. after it failed to resolve) and persists.
    /// </summary>
    public void Remove(string grantedFolderPath)
    {
        lock (_lock)
        {
            if (_map.Remove(grantedFolderPath)) Save();
        }
    }


    /// <summary>
    /// Returns whether <paramref name="ancestor"/> equals or contains <paramref name="child"/>.
    /// </summary>
    public static bool IsSameOrAncestor(string ancestor, string child)
    {
        ancestor = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ancestor));
        child = Path.TrimEndingDirectorySeparator(Path.GetFullPath(child));

        if (string.Equals(ancestor, child, StringComparison.OrdinalIgnoreCase)) return true;

        return child.StartsWith(ancestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Writes the current map to disk. Caller must hold <see cref="_lock"/>.
    /// </summary>
    private void Save()
    {
        try
        {
            Directory.CreateDirectory(BHelper.ConfigPath);
            var json = JsonSerializer.Serialize(_map, MacBookmarkJsonContext.Default.DictionaryStringString);
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }

}
