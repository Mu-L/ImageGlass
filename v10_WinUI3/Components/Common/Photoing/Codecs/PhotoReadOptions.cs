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
namespace ImageGlass.Common.Photoing;


/// <summary>
/// Settings for loading image
/// </summary>
public record PhotoReadOptions
{

    /// <summary>
    /// Gets, sets the requested width of the image.
    /// </summary>
    public uint Width { get; set; } = 0;


    /// <summary>
    /// Gets, sets the requested height of the image.
    /// </summary>
    public uint Height { get; set; } = 0;


    /// <summary>
    /// Gets, sets the value indicates whether the color profile should be ignored.
    /// </summary>
    public bool IgnoreColorProfile { get; set; } = false;


    /// <summary>
    /// Gets sets ColorProfile name of path
    /// </summary>
    public string ColorProfileName { get; set; } = nameof(ColorProfileOption.CurrentMonitorProfile);


    /// <summary>
    /// Gets, sets the value indicates if the <see cref="ColorProfileName"/>
    /// should apply to all image files
    /// </summary>
    public bool ApplyColorProfileForAll { get; set; } = false;


    /// <summary>
    /// Gets, sets the value indicates the embedded thumbnail of the RAW formats should be returned (if found).
    /// </summary>
    public bool UseEmbeddedThumbnailRawFormats { get; set; } = false;


    /// <summary>
    /// Gets, sets the value indicates the embedded thumbnail of the non-RAW formats should be returned (if found).
    /// </summary>
    public bool UseEmbeddedThumbnailOtherFormats { get; set; } = false;


    /// <summary>
    /// Gets, sets the minimum width of the embedded thumbnail to use for displaying
    /// image when the setting <see cref="UseEmbeddedThumbnailRawFormats"/> or <see cref="UseEmbeddedThumbnailOtherFormats"/> is <c>true</c>.
    /// </summary>
    public int EmbeddedThumbnailMinWidth { get; set; } = 0;


    /// <summary>
    /// Gets, sets the minimum height of the embedded thumbnail to use for displaying
    /// image when the setting <see cref="UseEmbeddedThumbnailRawFormats"/> or <see cref="UseEmbeddedThumbnailOtherFormats"/> is <c>true</c>.
    /// </summary>
    public int EmbeddedThumbnailMinHeight { get; set; } = 0;


    /// <summary>
    /// Gets, sets the value indicates that the incorrect rotation should be fixed
    /// </summary>
    public bool CorrectRotation { get; set; } = true;


    /// <summary>
    /// Gets, sets the value indicates that the first frame of the image should be returned.
    /// If it's <c>null</c>, the coder will decide.
    /// </summary>
    public bool? FirstFrameOnly { get; set; } = null;


    /// <summary>
    /// Gets, sets the requested image frame index for metadata reading.
    /// </summary>
    public int? FrameIndex { get; set; } = null;


    /// <summary>
    /// Initializes <see cref="PhotoReadOptions"/> instance.
    /// </summary>
    public PhotoReadOptions()
    {
    }
}
