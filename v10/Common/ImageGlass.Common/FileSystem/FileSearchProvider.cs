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

namespace ImageGlass.Common.FileSystem;


/// <summary>
/// Handles file searching, filtering, and sorting based on specified criteria.
/// </summary>
public class FileSearchProvider()
{
    private static readonly Lazy<FileSearchProvider> _instance = new(() => new FileSearchProvider(), LazyThreadSafetyMode.ExecutionAndPublication);


    /// <summary>
    /// Provides a singleton instance of the <see cref="FileSearchProvider"/> class.
    /// </summary>
    public static FileSearchProvider Instance => _instance.Value;


    /// <summary>
    /// Occurs when the host is being panned.
    /// </summary>
    public event EventHandler<FilesEnumeratedEventArgs>? FilesEnumerated;


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
    /// Gets the file path comparer.
    /// </summary>
    public IComparer<string?> FilePathComparer => new StringNaturalComparer(OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the directory path comparer.
    /// </summary>
    public IComparer<string?> DirPathComparer => GroupByDir
        ? new StringNaturalComparer(OrderType == ImageOrderType.Asc, StringComparison.OrdinalIgnoreCase)
        : (IComparer<string?>)Comparer<string>.Create((a, b) => 0);

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
    public void Start(IEnumerable<string> dirs)
    {
        // get files from the given directories
        foreach (var dirPath in dirs)
        {
            OnSearching(dirPath);
        }
    }


    // Protected Functions
    #region Protected Functions

    /// <summary>
    /// Finds files in the given directory, emits <see cref="FilesEnumerated"/> event.
    /// </summary>
    /// <param name="dirPath">The current path of directory to find</param>
    protected virtual void OnSearching(string dirPath)
    {
        FindFiles__(dirPath);
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
    /// Finds files in the given directory, emits <see cref="FilesEnumerated"/> event.
    /// </summary>
    protected void FindFiles__(string dirPath)
    {
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

        // filter list
        filePaths = OnFiltering(filePaths);

        // sort list
        filePaths = OnSorting(filePaths);

        // emits results
        FilesEnumerated?.Invoke(this, new FilesEnumeratedEventArgs(filePaths));
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
