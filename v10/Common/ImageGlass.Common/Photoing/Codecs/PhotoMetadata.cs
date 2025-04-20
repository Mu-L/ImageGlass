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

namespace ImageGlass.Common.Photoing;


public class PhotoMetadata : IDisposable
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
            Frames.Clear();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PhotoMetadata()
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
    /// Gets the desired width after processing orientation.
    /// </summary>
    public uint Width { get; set; } = 0;

    /// <summary>
    /// Gets the desired height after processing orientation.
    /// </summary>
    public uint Height { get; set; } = 0;

    /// <summary>
    /// Gets the frame index of this metadata.
    /// </summary>
    public uint FrameIndex { get; set; } = 0;
    public int FrameCount { get; set; } = 0;
    public IImmutableList<FrameMetadata> Frames { get; set; } = [];
    public bool HasAlpha { get; set; } = false;
    public bool CanAnimate { get; set; } = false;
    public OrientationType Orientation { get; set; } = OrientationType.Undefined;



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



    /// <summary>
    /// Retrieves a byte array representing a preview image from either a RAW format or an EXIF profile.
    /// </summary>
    public IMagickImage<byte>? GetPreview(CancellationToken token)
    {
        if (RawThumbnail is null && ExifProfile is null) return null;

        IMagickImage<byte>? thumbM = null;

        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();

            if (RawThumbnail is not null)
            {
                Log.Info("Retrieving embedded preview from RAW format...");

                thumbM = new MagickImage(RawThumbnail.ToReadOnlySpan());
            }


            // cancel if requested
            token.ThrowIfCancellationRequested();

            if (thumbM is null && ExifProfile is not null)
            {
                Log.Info("Retrieving embedded preview from EXIF profile...");

                thumbM = ExifProfile.CreateThumbnail();
            }


            if (thumbM is not null)
            {
                thumbM.Orientation = Orientation;
                thumbM?.AutoOrient();
            }

            return thumbM;
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Info($"Cancelled {nameof(GetPreview)}!");

            thumbM?.Dispose();
            thumbM = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }


        return thumbM;
    }


}

