/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
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

using System.Diagnostics.CodeAnalysis;

namespace ImageGlass.Common.FileSystem;


/// <summary>
/// Handles file searching, filtering, and sorting based on specified criteria.
/// </summary>
public abstract partial class FileSearcherImpl<TOptions>() : DisposableImpl
    where TOptions : FileSearchOptions
{
    protected CancellationTokenSource? _cancelSearching;


    /// <summary>
    /// Occurs when files are enumerated.
    /// </summary>
    public event EventHandler<FileSearchingEventArgs>? FileSearching;



    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether the search operation has completed.
    /// </summary>
    public bool IsSearchEnded { get; protected set; } = false;

    #endregion // Public Properties



    /// <summary>
    /// Starts files finding process in 3 steps:
    /// <list type="number">
    ///   <item><c><see cref="OnSearching"/></c></item>
    ///   <item><c><see cref="OnFiltering"/></c></item>
    ///   <item><c><see cref="OnSorting"/></c></item>
    /// </list>
    /// </summary>
    /// <param name="dirs">List of directories to search for files</param>
    public virtual void StartAsync(IEnumerable<string> dirs, TOptions options)
    {
        // cancel ongoing search
        CancelSearching();
        IsSearchEnded = false;


        // get files from the given directories
        foreach (var dirPath in dirs)
        {
            OnSearching(dirPath, options, _cancelSearching.Token);
        }

        IsSearchEnded = true;
    }


    /// <summary>
    /// Cancels an ongoing file searching operation.
    /// </summary>
    [MemberNotNull(nameof(_cancelSearching))]
    public void CancelSearching()
    {
        _cancelSearching?.Cancel();
        _cancelSearching?.Dispose();
        _cancelSearching = new();
    }



    // Protected Functions
    #region Protected Functions

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        CancelSearching();


        foreach (var eventDelegate in FileSearching?.GetInvocationList() ?? [])
        {
            FileSearching -= (EventHandler<FileSearchingEventArgs>)eventDelegate;
        }
    }


    /// <summary>
    /// Finds files in the given directory, emits <see cref="FileSearching"/> event.
    /// </summary>
    /// <param name="dirPath">The current path of directory to find</param>
    protected virtual void OnSearching(string dirPath, TOptions options, CancellationToken token)
    {
        FindFiles(dirPath, options, token);
    }


    /// <summary>
    /// Filters a collection of strings and returns the filtered results.
    /// </summary>
    protected virtual IEnumerable<string> OnFiltering(IEnumerable<string> fileList,
        TOptions options)
    {
        if (options.AllowedExtensions is null) return fileList;

        return fileList.Where(path =>
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            return options.AllowedExtensions.Contains(ext);
        });
    }


    /// <summary>
    /// Sorts a collection of image file paths based on provided criteria.
    /// </summary>
    protected virtual OrderedParallelQuery<string> OnSorting(IEnumerable<string> fileList,
        TOptions options)
    {
        return SortFiles(fileList, options);
    }


    /// <summary>
    /// Finds files in the given directory, emits <see cref="FileSearching"/> event.
    /// </summary>
    protected void FindFiles(string dirPath, TOptions options, CancellationToken token)
    {
        // cancel if requested
        if (token.IsCancellationRequested) return;

        // check attributes to skip
        var skipAttrs = FileAttributes.System;
        if (!options.IncludeHidden) skipAttrs |= FileAttributes.Hidden;

        // search files
        var filePaths = Directory.EnumerateFiles(dirPath, "*", new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            AttributesToSkip = skipAttrs,
            RecurseSubdirectories = options.SearchSubDirectories,
        });


        // cancel if requested
        if (token.IsCancellationRequested) return;

        // filter list
        filePaths = OnFiltering(filePaths, options);


        // cancel if requested
        if (token.IsCancellationRequested) return;

        // sort list
        filePaths = OnSorting(filePaths, options);


        // cancel if requested
        if (token.IsCancellationRequested) return;

        // emits results
        OnFileSearching(new FileSearchingEventArgs(filePaths, IsSearchEnded));
    }


    protected virtual void OnFileSearching(FileSearchingEventArgs e)
    {
        FileSearching?.Invoke(this, e);
    }


    /// <summary>
    /// Sorts a collection of image file paths based on provided criteria.
    /// </summary>
    public static OrderedParallelQuery<string> SortFiles(IEnumerable<string> fileList, TOptions options)
    {
        var query = fileList.AsParallel();

        // Gets the file path comparer.
        var filePathComparer = new StringNaturalComparer(options.OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase);

        // Gets the directory path comparer.
        var dirPathComparer = options.GroupByDir
            ? new StringNaturalComparer(options.OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase)
            : (IComparer<string?>)Comparer<string>.Create((a, b) => 0);


        // sort by FileSize
        if (options.OrderBy == ImageOrderBy.FileSize)
        {
            if (options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).Length)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenBy(f => new FileInfo(f).Length)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
        }

        // sort by DateCreated
        if (options.OrderBy == ImageOrderBy.DateCreated)
        {
            if (options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).CreationTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenBy(f => new FileInfo(f).CreationTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
        }

        // sort by Extension
        if (options.OrderBy == ImageOrderBy.Extension)
        {
            if (options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenBy(f => new FileInfo(f).Extension, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenBy(f => new FileInfo(f).Extension, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
        }

        // sort by DateAccessed
        if (options.OrderBy == ImageOrderBy.DateAccessed)
        {
            if (options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).LastAccessTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenBy(f => new FileInfo(f).LastAccessTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
        }

        // sort by DateModified
        if (options.OrderBy == ImageOrderBy.DateModified)
        {
            if (options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                    .ThenBy(f => new FileInfo(f).LastWriteTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), filePathComparer);
            }
        }

        // sort by Random
        if (options.OrderBy == ImageOrderBy.Random)
        {
            // NOTE: ignoring the 'descending order' setting
            return query
                .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
                .ThenBy(_ => Guid.NewGuid());
        }


        // sort by Name (default)
        return query
            .OrderBy(f => Path.GetDirectoryName(f), dirPathComparer)
            .ThenBy(f => Path.GetFileName(f), filePathComparer);
    }

    #endregion // Protected Functions


}
