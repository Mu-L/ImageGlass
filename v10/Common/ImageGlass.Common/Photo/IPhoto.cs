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
namespace ImageGlass.Common;


/// <summary>
/// An interface for handling photo objects.
/// </summary>
/// <typeparam name="T">Represents the type of the native bitmap associated with the photo.</typeparam>
public interface IPhoto<T> : IDisposable where T : IDisposable
{
    /// <summary>
    /// Gets file path of the photo.
    /// </summary>
    string FilePath { get; set; }


    /// <summary>
    /// Gets file extension. E.g: <c>.png</c>.
    /// </summary>
    public string Extension => Path.GetExtension(FilePath);


    /// <summary>
    /// Gets the error details.
    /// </summary>
    Exception? Error { get; set; }


    /// <summary>
    /// Gets, sets image metadata
    /// </summary>
    public IgMetadata? Metadata { get; set; }


    /// <summary>
    /// Indicates whether the <see cref="Bitmap"/> is currently loaded.
    /// </summary>
    bool IsDone { get; set; }


    /// <summary>
    /// Gets the hash key of the image.
    /// </summary>
    public string HashKey { get; }


    /// <summary>
    /// Gets the native bitmap.
    /// </summary>
    T? Bitmap { get; }


    /// <summary>
    /// Gets the width of the photo
    /// </summary>
    int Width { get; }


    /// <summary>
    /// Gets the height of the photo.
    /// </summary>
    int Height { get; }


    /// <summary>
    /// Loads photo from file.
    /// </summary>
    Task LoadAsync(uint frameIndex = 0);


    /// <summary>
    /// Loads <c><see cref="Metadata"/></c> of photo.
    /// </summary>
    Task LoadMetadataAsync(uint frameIndex = 0);


    /// <summary>
    /// Stops any ongoing loading process.
    /// </summary>
    void CancelLoading();


    /// <summary>
    /// Unload the image and reset the relevant info
    /// </summary>
    void Unload();
}


public interface IPhoto : IPhoto<IDisposable>
{

}

