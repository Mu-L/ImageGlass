/*
ImageGlass - A Fast, Seamless Photo Viewer
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
using ImageGlass.Common.ServiceProviders.FileSearchService;
using ImageGlass.Mac.Common.Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Mac.Common.ServiceProviders;


/// <summary>
/// macOS file search provider. Identical to the base provider except that, under
/// the App Sandbox, it ensures the app has been granted access to each folder
/// (prompting once and persisting a security-scoped bookmark) before enumerating
/// it. Outside the sandbox (e.g. the Developer ID / DMG build) it behaves exactly
/// like the base provider — no prompts.
/// </summary>
internal sealed class MacFileSearchProvider : FileSearchProvider
{
    private readonly MacFolderAccessManager _access = new();


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override async Task SearchAsync(IEnumerable<string> dirs, FileSearchOptions options,
        Action<FileSearchingEventArgs>? progressFn = null)
    {
        // Snapshot so we can gate access before the base enumerates.
        var dirList = dirs.ToList();

        if (MacSandbox.IsSandboxed)
        {
            foreach (var dir in dirList)
            {
                // Best-effort: a denied folder simply yields an empty list (the
                // opened file itself still displays via its own powerbox grant).
                try { await _access.EnsureAccessAsync(dir).ConfigureAwait(false); }
                catch { }
            }
        }

        await base.SearchAsync(dirList, options, progressFn).ConfigureAwait(false);
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        _access.ReleaseAll();
        base.OnDisposing();
    }

}
