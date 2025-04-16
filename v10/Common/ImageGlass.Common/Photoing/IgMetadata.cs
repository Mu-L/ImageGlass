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
using ImageMagick;
using System.Collections.Immutable;

namespace ImageGlass.Common;


public class IgMetadata : IDisposable
{

    #region IDisposable Disposing

    public bool IsDisposed { get; protected set; } = false;


    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            if (ColorProfileData != null)
            {
                Array.Clear(ColorProfileData);
                ColorProfileData = null;
            }

            RawThumbnail = null;
            ExifProfile = null;
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~IgMetadata()
    {
        Dispose(false);
    }

    #endregion



    // File metadata
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;

    public DateTime FileCreationTime { get; set; } // local time
    public DateTime FileLastAccessTime { get; set; } // local time
    public DateTime FileLastWriteTime { get; set; } // local time
    public string FileCreationTimeFormated => BHelper.FormatDateTime(FileCreationTime);
    public string FileLastAccessTimeFormated => BHelper.FormatDateTime(FileLastAccessTime);
    public string FileLastWriteTimeFormated => BHelper.FormatDateTime(FileLastWriteTime);

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; } = 0;

    /// <summary>
    /// The formated file size. E.g. <c>32.09 MB</c>.
    /// </summary>
    public string FileSizeFormated => BHelper.FormatSize(FileSize);



    /// <summary>
    /// Gets the original width before processing orientation.
    /// </summary>
    public uint OriginalWidth { get; set; } = 0;

    /// <summary>
    /// Gets the original height before processing orientation.
    /// </summary>
    public uint OriginalHeight { get; set; } = 0;

    /// <summary>
    /// Gets the frame index of this metadata.
    /// </summary>
    public uint FrameIndex { get; set; } = 0;
    public int FrameCount { get; set; } = 0;
    public IImmutableList<FrameMetadata> Frames { get; set; } = [];
    public bool HasAlpha { get; set; } = false;
    public bool CanAnimate { get; set; } = false;

    public ColorSpace ColorSpace { get; set; } = ColorSpace.Undefined;
    public string ColorProfileName { get; set; } = string.Empty;

    public byte[]? ColorProfileData { get; set; } = null;

    public IImageProfile? RawThumbnail { get; set; } = null;


    // EXIF metadata
    public IExifProfile? ExifProfile { get; set; } = null;
    public int ExifRatingPercent { get; set; } = 0;
    public DateTime? ExifDateTimeOriginal { get; set; } = null; // local time
    public DateTime? ExifDateTime { get; set; } = null; // local time
    public string? ExifImageDescription { get; set; } = null;
    public string? ExifModel { get; set; } = null;
    public string? ExifArtist { get; set; } = null;
    public string? ExifCopyright { get; set; } = null;
    public string? ExifSoftware { get; set; } = null;
    public float? ExifExposureTime { get; set; } = null;
    public float? ExifFNumber { get; set; } = null;
    public int? ExifISOSpeed { get; set; } = null;
    public float? ExifFocalLength { get; set; } = null;

}


public class FrameMetadata
{
    public IMagickColor<byte> BackgroundColor { get; set; } = MagickColors.Transparent;
    public uint Width { get; set; } = 0;
    public uint Height { get; set; } = 0;
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;

    public uint AnimationDelay { get; set; } = 0;
    public uint AnimationTicksPerSecond { get; set; } = 0;
    public uint AnimationLoop { get; set; } = 0;
    public GifDisposeMethod GifDisposeMethod { get; set; } = GifDisposeMethod.Undefined;

}

