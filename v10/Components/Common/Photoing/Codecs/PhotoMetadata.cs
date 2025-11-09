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
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImageGlass.Common.Photoing;


public partial class PhotoMetadata : DisposableImpl
{

    // File metadata
    public string FilePath
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;

            SetFilePath__(value);
            _ = OnPropertyChanged(value, oldValue);
            _ = OnPropertyChanged(nameof(FileName));
            _ = OnPropertyChanged(nameof(FileExtension));
            _ = OnPropertyChanged(nameof(FolderPath));
            _ = OnPropertyChanged(nameof(FolderName));
            _ = OnPropertyChanged(nameof(FileSizeInBytes));
            _ = OnPropertyChanged(nameof(FileSizeFormated));
            _ = OnPropertyChanged(nameof(FileCreationTimeUtc));
            _ = OnPropertyChanged(nameof(FileLastAccessTimeUtc));
            _ = OnPropertyChanged(nameof(FileLastWriteTimeUtc));
            _ = OnPropertyChanged(nameof(FileCreationTimeFormated));
            _ = OnPropertyChanged(nameof(FileLastAccessTimeFormated));
            _ = OnPropertyChanged(nameof(FileLastWriteTimeFormated));
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
    public uint OriginalWidth
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;

    /// <summary>
    /// Gets the original height before processing orientation.
    /// </summary>
    public uint OriginalHeight
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;

    /// <summary>
    /// Gets the desired width after processing orientation.
    /// </summary>
    public uint Width
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;

    /// <summary>
    /// Gets the desired height after processing orientation.
    /// </summary>
    public uint Height
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;

    /// <summary>
    /// Gets the frame index of this metadata.
    /// </summary>
    public uint FrameIndex
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;
    public uint FrameCount
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;
    public uint AnimationLoop
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;
    public IImmutableList<FrameMetadata> Frames
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = [];
    public bool HasAlpha
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = false;
    public bool CanAnimate
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = false;
    public OrientationType Orientation
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = OrientationType.Undefined;



    public ColorSpace ColorSpace
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = ColorSpace.Undefined;
    public string ColorProfileName
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = string.Empty;

    public byte[]? ColorProfileData
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;

    public IImageProfile? RawThumbnail
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;


    // EXIF metadata
    public IExifProfile? ExifProfile
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public int ExifRatingPercent
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = 0;
    public DateTime? ExifDateTimeOriginal
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null; // local time
    public DateTime? ExifDateTime
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null; // local time
    public string? ExifImageDescription
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public string? ExifModel
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public string? ExifArtist
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public string? ExifCopyright
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public string? ExifSoftware
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public float? ExifExposureTime
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public float? ExifFNumber
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public int? ExifISOSpeed
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;
    public float? ExifFocalLength
    {
        get; set
        {
            if (field == value) return;
            var oldValue = field;
            field = value;
            _ = OnPropertyChanged(value, oldValue);
        }
    } = null;



    public PhotoMetadata() { }

    public PhotoMetadata(string? filePath)
    {
        FilePath = filePath ?? string.Empty;
    }



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
            thumbM.Orientation = Orientation;
            thumbM?.AutoOrient();
        }

        return thumbM;
    }


}

