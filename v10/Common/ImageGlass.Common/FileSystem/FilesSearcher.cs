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
public partial class FilesSearcher() : DisposableImpl
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
    protected IComparer<string?> FilePathComparer => new StringNaturalComparer(OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the directory path comparer.
    /// </summary>
    protected IComparer<string?> DirPathComparer => GroupByDir
        ? new StringNaturalComparer(OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase)
        : (IComparer<string?>)Comparer<string>.Create((a, b) => 0);

    #endregion // Protected Properties


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Specifies the order in which images are sorted.
    /// Defaults to <c><see cref="ImageOrderBy.Name"/></c>.
    /// </summary>
    public ImageOrderBy OrderBy { get; set; } = ImageOrderBy.Name;

    /// <summary>
    /// Represents the order type for images.
    /// Defaults to <c><see cref="ImageOrderType.Asc"/></c>.
    /// </summary>
    public ImageOrderType OrderType { get; set; } = ImageOrderType.Asc;

    /// <summary>
    /// Defines the mode of string comparison used.
    /// Defaults to <c><see cref="StringComparison.OrdinalIgnoreCase"/></c>
    /// </summary>
    public StringComparison CompareMode { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Indicates whether to group items by their directory. Defaults to <c>true</c>.
    /// </summary>
    public bool GroupByDir { get; set; } = true;

    /// <summary>
    /// Indicates whether to search in subdirectories. Defaults to <c>false</c>.
    /// </summary>
    public bool SearchSubDirectories { get; set; } = false;

    /// <summary>
    /// Indicates whether hidden items should be included. Defaults to <c>false</c>.
    /// </summary>
    public bool IncludeHidden { get; set; } = false;

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
    public async Task StartAsync(IEnumerable<string> dirs)
    {
        await _lockSearching.WaitAsync();

        try
        {
            // cancel ongoing search
            CancelSearching();

            // get files from the given directories
            CurrentBatchIndex = 0;
            CurrentBatchCount = (uint)dirs.Count();

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
        return fileList;
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
        if (!IncludeHidden) skipAttrs |= FileAttributes.Hidden;

        // search files
        var filePaths = Directory.EnumerateFiles(dirPath, "*", new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            AttributesToSkip = skipAttrs,
            RecurseSubdirectories = SearchSubDirectories,
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
        if (OrderBy == ImageOrderBy.FileSize)
        {
            if (OrderType == ImageOrderType.Desc)
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
        if (OrderBy == ImageOrderBy.DateCreated)
        {
            if (OrderType == ImageOrderType.Desc)
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
        if (OrderBy == ImageOrderBy.Extension)
        {
            if (OrderType == ImageOrderType.Desc)
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
        if (OrderBy == ImageOrderBy.DateAccessed)
        {
            if (OrderType == ImageOrderType.Desc)
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
        if (OrderBy == ImageOrderBy.DateModified)
        {
            if (OrderType == ImageOrderType.Desc)
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
        if (OrderBy == ImageOrderBy.Random)
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
