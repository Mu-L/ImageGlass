/*
ImageGlass - A lightweight, versatile image viewer
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
using ImageGlass.Common.Types;
using ImageMagick;
using SkiaSharp;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImageGlass.Common.Photoing;


public partial class PhotoMetadata : DisposableImpl
{

    #region Public Properties

    #region File metadata
    public string FilePath
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;

            SetFilePath__(value);
        }
    } = string.Empty;

    public string FileName { get; private set; } = string.Empty;
    /// <summary>
    /// Getss file extension in lowercase. E.g. <c>.png</c>
    /// </summary>
    public string FileExtension { get; private set; } = string.Empty;
    public string FolderPath { get; private set; } = string.Empty;
    public string FolderName { get; private set; } = string.Empty;
    public long FileSizeInBytes { get; private set; } = 0;

    /// <summary>
    /// The formated file size. E.g. <c>32.09 MB</c>.
    /// </summary>
    public string FileSizeFormatted => BHelper.FormatSize(FileSizeInBytes);

    public DateTime FileCreationTimeUtc { get; private set; }
    public DateTime FileLastAccessTimeUtc { get; private set; }
    public DateTime FileLastWriteTimeUtc { get; private set; }
    public string FileCreationTimeFormatted => BHelper.FormatDateTime(FileCreationTimeUtc.ToLocalTime());
    public string FileLastAccessTimeFormatted => BHelper.FormatDateTime(FileLastAccessTimeUtc.ToLocalTime());
    public string FileLastWriteTimeFormatted => BHelper.FormatDateTime(FileLastWriteTimeUtc.ToLocalTime());

    #endregion // File metadata


    #region Bitmap information

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
    public string FrameCountFormatted => FrameCount > 1 ? FrameCount.ToString() : string.Empty;
    public uint AnimationLoop { get; set; } = 0;
    public IImmutableList<FrameMetadata> Frames { get; set; } = [];
    public bool HasAlpha { get; set; } = false;
    public bool CanAnimate { get; set; } = false;
    public SKEncodedOrigin Orientation { get; set; } = SKEncodedOrigin.Default;

    #endregion // Bitmap information


    #region Color information

    public ColorSpace ColorSpace { get; set; } = ColorSpace.Undefined;
    public string ColorProfileName { get; set; } = string.Empty;

    public byte[]? ColorProfileData { get; set; } = null;

    public IImageProfile? RawThumbnail { get; set; } = null;

    #endregion // Color information


    #region EXIF metadata

    public IExifProfile? ExifProfile { get; set; } = null;
    public int ExifRatingPercent { get; set; } = 0;
    public string ExifRatingFormatted => BHelper.FormatStarRatingText(ExifRatingPercent);
    public DateTime? ExifDateTimeOriginal { get; set; } = null; // local time
    public DateTime? ExifDateTime { get; set; } = null; // local time

    public string ExifDateTimeOriginalFormatted => BHelper.FormatDateTime(ExifDateTimeOriginal?.ToLocalTime());
    public string ExifDateTimeFormatted => BHelper.FormatDateTime(ExifDateTime?.ToLocalTime());

    public string? ExifImageDescription { get; set; } = null;
    public string? ExifModel { get; set; } = null;
    public string? ExifArtist { get; set; } = null;
    public string? ExifCopyright { get; set; } = null;
    public string? ExifSoftware { get; set; } = null;
    public float? ExifExposureTime { get; set; } = null;
    public float? ExifFNumber { get; set; } = null;
    public int? ExifISOSpeed { get; set; } = null;
    public float? ExifFocalLength { get; set; } = null;

    #endregion // EXIF metadata

    #endregion // Public Properties



    public PhotoMetadata() { }

    public PhotoMetadata(string? filePath)
    {
        FilePath = filePath ?? string.Empty;
    }



    #region Methods

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
        FrameCount = 0;
        Frames.Clear();
    }


    /// <summary>
    /// Sets the file path and extracts file-related metadata.
    /// </summary>
    private void SetFilePath__(string? filePath)
    {
        try
        {
            var fi = new FileInfo(filePath!);

            FileName = fi.Name;
            FileExtension = fi.Extension.ToLowerInvariant();
            FolderPath = fi.DirectoryName ?? string.Empty;
            FolderName = fi.Directory?.Name ?? string.Empty;

            FileSizeInBytes = fi.Length;
            FileCreationTimeUtc = fi.CreationTimeUtc;
            FileLastWriteTimeUtc = fi.LastWriteTimeUtc;
            FileLastAccessTimeUtc = fi.LastAccessTimeUtc;
        }
        catch
        {
            FileName = string.Empty;
            FileExtension = string.Empty;
            FolderPath = string.Empty;
            FolderName = string.Empty;

            FileSizeInBytes = 0;
            FileCreationTimeUtc = DateTime.MinValue;
            FileLastWriteTimeUtc = DateTime.MinValue;
            FileLastAccessTimeUtc = DateTime.MinValue;
        }
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
    /// Checks if the metadata is outdated.
    /// </summary>
    public bool IsOutdated()
    {
        if (FrameCount == 0) return true;

        // check if the current Metadata is outdated or not
        var hasOutdatedCache = true;

        try
        {
            var fi = new FileInfo(FilePath);
            hasOutdatedCache = FileLastWriteTimeUtc < fi.LastWriteTimeUtc;
        }
        catch { }

        return hasOutdatedCache;
    }


    /// <summary>
    /// Retrieves an embedded thumbnail from either a RAW format or an EXIF profile if exists.
    /// </summary>
    public MagickImage? GetEmbeddedPreview()
    {
        if (RawThumbnail is null && ExifProfile is null) return null;

        MagickImage? thumbM = null;


        // 1. try get from RAW format
        if (RawThumbnail is not null)
        {
            thumbM = new MagickImage(RawThumbnail.ToReadOnlySpan());
        }


        // 2. try get from EXIF profile
        if (thumbM is null && ExifProfile is not null)
        {
            thumbM = (MagickImage?)ExifProfile.CreateThumbnail();
        }


        // 3. fix orientation
        if (thumbM is not null)
        {
            thumbM.Orientation = SkiaCodec.ToMagickOrientation(Orientation);
            thumbM?.AutoOrient();
        }

        return thumbM;
    }

    #endregion // Methods



}

