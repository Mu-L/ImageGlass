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
public partial class FileSearcherImpl() : DisposableImpl
{
    private CancellationTokenSource? _cancelSearching;
    private SemaphoreSlim _lockSearching = new(1, 1);


    /// <summary>
    /// Occurs when files are enumerated.
    /// </summary>
    public event EventHandler<FileSearchingEventArgs>? FileSearching;


    // Protected Properties
    #region Protected Properties

    /// <summary>
    /// Gets the file path comparer.
    /// </summary>
    protected IComparer<string?> FilePathComparer => new StringNaturalComparer(Options.OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the directory path comparer.
    /// </summary>
    protected IComparer<string?> DirPathComparer => Options.GroupByDir
        ? new StringNaturalComparer(Options.OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase)
        : (IComparer<string?>)Comparer<string>.Create((a, b) => 0);

    #endregion // Protected Properties


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets the options used to configure the file search operation.
    /// </summary>
    public FilesSearchOptions Options { get; private set; } = new();

    /// <summary>
    /// Gets the index of the current batch being processed.
    /// </summary>
    public uint CurrentBatchIndex { get; protected set; } = 0;

    /// <summary>
    /// Gets the current number of batches processed.
    /// </summary>
    public uint CurrentBatchCount { get; protected set; } = 0;

    /// <summary>
    /// Gets a value indicating whether the search operation has completed.
    /// </summary>
    public bool IsSearchEnded => CurrentBatchIndex >= CurrentBatchCount;

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
    public async Task StartAsync(IEnumerable<string> dirs, FilesSearchOptions options)
    {
        await _lockSearching.WaitAsync();

        try
        {
            // cancel ongoing search
            CancelSearching();

            // set search options
            Options = options;
            CurrentBatchIndex = 0;
            CurrentBatchCount = (uint)dirs.Count();

            // get files from the given directories
            await Task.Run(() =>
            {
                foreach (var dirPath in dirs)
                {
                    OnSearching(dirPath, CurrentBatchIndex, CurrentBatchCount, _cancelSearching.Token);
                    CurrentBatchIndex++;
                }
            });
        }
        finally
        {
            _lockSearching.Release();
        }
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
        _lockSearching.Dispose();


        foreach (var eventDelegate in FileSearching?.GetInvocationList() ?? [])
        {
            FileSearching -= (EventHandler<FileSearchingEventArgs>)eventDelegate;
        }
    }


    /// <summary>
    /// Finds files in the given directory, emits <see cref="FileSearching"/> event.
    /// </summary>
    /// <param name="dirPath">The current path of directory to find</param>
    protected virtual void OnSearching(string dirPath, uint batchIndex, uint batchCount, CancellationToken token)
    {
        FindFiles__(dirPath, batchIndex, batchCount, token);
    }


    /// <summary>
    /// Filters a collection of strings and returns the filtered results.
    /// </summary>
    protected virtual IEnumerable<string> OnFiltering(IEnumerable<string> fileList)
    {
        if (Options.AllowedExtensions is null) return fileList;

        return fileList.Where(path =>
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            return Options.AllowedExtensions.Contains(ext);
        });
    }


    /// <summary>
    /// Sorts a collection of image file paths based on provided criteria.
    /// </summary>
    protected virtual OrderedParallelQuery<string> OnSorting(IEnumerable<string> fileList)
    {
        return SortFiles__(fileList);
    }


    /// <summary>
    /// Finds files in the given directory, emits <see cref="FileSearching"/> event.
    /// </summary>
    protected void FindFiles__(string dirPath, uint batchIndex, uint batchCount, CancellationToken token)
    {
        // cancel if requested
        if (token.IsCancellationRequested) return;

        // check attributes to skip
        var skipAttrs = FileAttributes.System;
        if (!Options.IncludeHidden) skipAttrs |= FileAttributes.Hidden;

        // search files
        var filePaths = Directory.EnumerateFiles(dirPath, "*", new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            AttributesToSkip = skipAttrs,
            RecurseSubdirectories = Options.SearchSubDirectories,
        });


        // cancel if requested
        if (token.IsCancellationRequested) return;

        // filter list
        filePaths = OnFiltering(filePaths);


        // cancel if requested
        if (token.IsCancellationRequested) return;

        // sort list
        filePaths = OnSorting(filePaths);


        // cancel if requested
        if (token.IsCancellationRequested) return;

        // emits results
        FileSearching?.Invoke(this, new(filePaths, batchIndex, batchCount));
    }


    /// <summary>
    /// Sorts a collection of image file paths based on provided criteria.
    /// </summary>
    protected OrderedParallelQuery<string> SortFiles__(IEnumerable<string> fileList)
    {
        var query = fileList.AsParallel();

        // sort by FileSize
        if (Options.OrderBy == ImageOrderBy.FileSize)
        {
            if (Options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).Length)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenBy(f => new FileInfo(f).Length)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
        }

        // sort by DateCreated
        if (Options.OrderBy == ImageOrderBy.DateCreated)
        {
            if (Options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).CreationTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenBy(f => new FileInfo(f).CreationTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
        }

        // sort by Extension
        if (Options.OrderBy == ImageOrderBy.Extension)
        {
            if (Options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenBy(f => new FileInfo(f).Extension, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenBy(f => new FileInfo(f).Extension, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
        }

        // sort by DateAccessed
        if (Options.OrderBy == ImageOrderBy.DateAccessed)
        {
            if (Options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).LastAccessTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenBy(f => new FileInfo(f).LastAccessTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
        }

        // sort by DateModified
        if (Options.OrderBy == ImageOrderBy.DateModified)
        {
            if (Options.OrderType == ImageOrderType.Desc)
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
            else
            {
                return query
                    .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                    .ThenBy(f => new FileInfo(f).LastWriteTimeUtc)
                    .ThenBy(f => Path.GetFileName(f), FilePathComparer);
            }
        }

        // sort by Random
        if (Options.OrderBy == ImageOrderBy.Random)
        {
            // NOTE: ignoring the 'descending order' setting
            return query
                .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
                .ThenBy(_ => Guid.NewGuid());
        }


        // sort by Name (default)
        return query
            .OrderBy(f => Path.GetDirectoryName(f), DirPathComparer)
            .ThenBy(f => Path.GetFileName(f), FilePathComparer);
    }

    #endregion // Protected Functions


}
