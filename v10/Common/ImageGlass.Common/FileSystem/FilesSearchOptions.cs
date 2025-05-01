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

public class FilesSearchOptions
{
    /// <summary>
    /// Gets or sets the collection of allowed file extensions.
    /// Defaults to <c>null</c> - allow all.
    /// </summary>
    public IEnumerable<string>? AllowedExtensions { get; set; } = null;

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
}
