/*
ImageGlass Project - Image viewer for Windows
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
using D2Phap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.FileSystem;


public partial class FileSearcher : FileSearcherImpl<FileShellSearchOptions>
{

    /// <summary>
    /// Searches files from the provided directories.
    /// If <see cref="FileShellSearchOptions.UseExplorerSortOrder"/> is <c>true</c>,
    /// it follows there steps:
    /// 
    /// <list type="number">
    /// <item>
    ///   Get files from provided shell view of <see cref="FileShellSearchOptions.ForegroundShell"/>,
    ///   ignoring the param <c><paramref name="dirs"/></c>.
    /// </item>
    /// <item>
    ///   If not (or error), try to get the shell view
    ///   from each param <c><paramref name="dirs"/></c> provided, then do step 1.
    /// </item>
    /// <item>
    ///   If not shell view from step 2, use the normal searching process:
    /// </item>
    /// </list>
    /// 
    /// <inheritdoc/>
    /// </summary>
    public override async Task SearchAsync(IEnumerable<string> dirs, FileShellSearchOptions options, IProgress<FileSearchingEventArgs> progress)
    {
        // cancel ongoing search
        CancelSearching();
        IsSearchEnded = false;


        // 1. get files from the foreground window
        if (options.ForegroundShell != null && options.UseExplorerSortOrder)
        {
            try
            {
                // get shell folder
                var folderShell = GetShellFolderView(null, options.ForegroundShell);

                FindFiles_WithShell(folderShell.View, folderShell.DirPath, options, progress, _cancelSearching.Token);
                return;
            }
            catch (COMException) { }
        }


        // 2. get files from the given directories
        var fvMap = new ConcurrentDictionary<string, ExplorerFolderView?>();

        if (options.UseExplorerSortOrder)
        {
            // find and save all shell folder view
            foreach (var dirPath in dirs)
            {
                var fv = GetShellFolderView(dirPath, null).View;
                _ = fvMap.TryAdd(dirPath, fv);
            }
        }


        // 3. start searching in a separate thread
        await Task.Run(() =>
        {
            foreach (var dirPath in dirs)
            {
                var folderShellView = fvMap.GetValueOrDefault(dirPath);

                // with shell
                if (folderShellView != null)
                {
                    FindFiles_WithShell(folderShellView, dirPath, options, progress, _cancelSearching.Token);
                }

                // without shell
                else
                {
                    FindFiles(dirPath, options, progress, _cancelSearching.Token);
                }
            }
        });


        // 4. end searching
        IsSearchEnded = true;

        // dipose shell objects
        foreach (var fv in fvMap)
        {
            fv.Value?.Dispose();
        }
        fvMap.Clear();
    }


    /// <summary>
    /// Finds files in the given <see cref="ExplorerFolderView"/>.
    /// Use the <see cref="FilesEnumerated"/> event to get results.
    /// </summary>
    private void FindFiles_WithShell(ExplorerFolderView? fv, string? rootDir, FileShellSearchOptions options, IProgress<FileSearchingEventArgs> progress, CancellationToken token)
    {
        // if no folder view
        if (fv is null)
        {
            // use .NET
            if (!string.IsNullOrWhiteSpace(rootDir))
            {
                FindFiles(rootDir, options, progress, token);
            }
            return;
        }


        // 1. get & filter files from shell folder view
        var filePaths = fv.GetItems(FolderItemViewOptions.SVGIO_FLAG_VIEWORDER)
            .Where(path =>
            {
                // ignore special folders
                if (path.StartsWith(EggShell.SPECIAL_DIR_PREFIX, StringComparison.InvariantCultureIgnoreCase)) return false;

                try
                {
                    // get path attributes
                    var attrs = File.GetAttributes(path);

                    // path is dir
                    if (attrs.HasFlag(FileAttributes.Directory)) return false;

                    // path is hidden
                    if (!options.IncludeHidden && attrs.HasFlag(FileAttributes.Hidden)) return false;
                }
                catch
                {
                    return false;
                }

                // filter extensions
                if (options.AllowedExtensions is not null)
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();

                    return options.AllowedExtensions.Contains(ext);
                }

                return true;
            });


        // cancel if requested
        if (token.IsCancellationRequested) return;


        // 3. emits results
        progress.Report(new FileSearchingEventArgs(filePaths, IsSearchEnded));


        // cancel if requested
        if (token.IsCancellationRequested) return;


        // 4. search all sub-directories if root dir is not empty
        if (options.SearchSubDirectories && !string.IsNullOrWhiteSpace(rootDir))
        {
            // search files for the sub dirs
            // get sub folders
            var subDirList = Directory.EnumerateDirectories(rootDir, "*", new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                AttributesToSkip = options.IncludeHidden
                    ? FileAttributes.System
                    : FileAttributes.System | FileAttributes.Hidden,
                RecurseSubdirectories = false,
            });

            // find files in sub-folders
            foreach (var dirPath in subDirList)
            {
                FindFiles(dirPath, options, progress, token);
            }
        }
    }


    /// <summary>
    /// Gets the <see cref="ExplorerFolderView"/> from the given dir path.
    /// </summary>
    /// <remarks>🔴 NOTE: Must run on UI thread.</remarks>
    /// <exception cref="COMException"></exception>
    private static (ExplorerFolderView? View, string DirPath) GetShellFolderView(string? rootDir, ExplorerView? foregroundShell)
    {
        var folderPath = "";
        ExplorerFolderView? folderView = null;
        using var shell = new EggShell();


        // if no dir path, get the explorer's folder view where the application opened from
        if (string.IsNullOrWhiteSpace(rootDir))
        {
            if (foregroundShell?.GetTabFolderView() is ExplorerFolderView fv)
            {
                folderPath = foregroundShell.GetTabViewPath();
                folderView = fv;
            }
        }
        else if (!Path.EndsInDirectorySeparator(rootDir))
        {
            rootDir += Path.DirectorySeparatorChar;
        }


        rootDir ??= "";

        // find the folder view from the opening explorer windows
        if (folderView == null)
        {
            // find the explorer's folder view for each directory
            shell.WithOpeningWindows(ev =>
            {
                var windowPath = ev.GetTabViewPath();
                if (!Path.EndsInDirectorySeparator(windowPath))
                {
                    windowPath += Path.DirectorySeparatorChar;
                }

                // get the folder view for the input dir
                if (rootDir.Equals(windowPath, StringComparison.InvariantCultureIgnoreCase)
                    && ev.GetTabFolderView() is ExplorerFolderView fv)
                {
                    folderPath = windowPath;
                    folderView = fv;
                    return true;
                }

                return false;
            }, true);
        }


        return (folderView, folderPath);
    }

}
