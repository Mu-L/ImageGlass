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


public class PhotoMetadata : DisposableImpl
{
    // File metadata
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    /// <summary>
    /// Getss file extension in uppercase. E.g. <c>.PNG</c>
    /// </summary>
    public string FileExtension { get; private set; } = string.Empty;
    public string FolderPath { get; private set; } = string.Empty;
    public string FolderName { get; private set; } = string.Empty;
    public long FileSizeInBytes { get; private set; } = 0;

    /// <summary>
    /// The formated file size. E.g. <c>32.09 MB</c>.
    /// </summary>
    public string FileSizeFormated => BHelper.FormatSize(FileSizeInBytes);

    public DateTime FileCreationTimeUtc { get; private set; }
    public DateTime FileLastAccessTimeUtc { get; private set; }
    public DateTime FileLastWriteTimeUtc { get; private set; }
    public string FileCreationTimeFormated => BHelper.FormatDateTime(FileCreationTimeUtc.ToLocalTime());
    public string FileLastAccessTimeFormated => BHelper.FormatDateTime(FileLastAccessTimeUtc.ToLocalTime());
    public string FileLastWriteTimeFormated => BHelper.FormatDateTime(FileLastWriteTimeUtc.ToLocalTime());


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
    public uint FrameCount { get; set; } = 0;
    public uint AnimationLoop { get; set; } = 0;
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
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        if (ColorProfileData != null)
        {
            Array.Clear(ColorProfileData);
            ColorProfileData = null;
        }

        RawThumbnail = null;
        ExifProfile = null;
        Frames.Clear();
    }



    /// <summary>
    /// Sets the file path and extracts file-related metadata.
    /// </summary>
    public void SetFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var fi = new FileInfo(filePath);

            FileName = fi.Name;
            FileExtension = fi.Extension.ToUpperInvariant();
            FolderPath = fi.DirectoryName ?? string.Empty;
            FolderName = fi.Directory?.Name ?? string.Empty;

            FileSizeInBytes = fi.Length;
            FileCreationTimeUtc = fi.CreationTimeUtc;
            FileLastWriteTimeUtc = fi.LastWriteTimeUtc;
            FileLastAccessTimeUtc = fi.LastAccessTimeUtc;
        }
        catch { }
    }


    /// <summary>
    /// Checks if the file extension matches any of the specified extensions, ignoring case.
    /// </summary>
    /// <param name="exts">The file extension to compare, e.g. <c>.PNG</c>.</param>
    public bool IsOneOfExtensions(params string[] exts)
    {
        return exts.Any(ext => FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// Retrieves an embedded thumbnail from either a RAW format or an EXIF profile if exists.
    /// </summary>
    public MagickImage? GetPreview(CancellationToken token)
    {
        if (RawThumbnail is null && ExifProfile is null) return null;

        MagickImage? thumbM = null;

        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();
            Log.Info($"Retrieving embedded preview {FilePath}",
                nameof(GetPreview), nameof(PhotoMetadata));


            if (RawThumbnail is not null)
            {
                Log.Info($"\t-> from RAW format...",
                    nameof(GetPreview), nameof(PhotoMetadata));

                thumbM = new MagickImage(RawThumbnail.ToReadOnlySpan());
            }


            // cancel if requested
            token.ThrowIfCancellationRequested();

            if (thumbM is null && ExifProfile is not null)
            {
                Log.Info($"\t-> from EXIF profile...",
                    nameof(GetPreview), nameof(PhotoMetadata));

                thumbM = (MagickImage?)ExifProfile.CreateThumbnail();
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
            Log.Info($"Cancelled retrieving preview for {FilePath}",
                nameof(GetPreview), nameof(PhotoMetadata));

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

