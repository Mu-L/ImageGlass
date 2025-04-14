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
using ImageMagick.Formats;

namespace ImageGlass.Common.Photoing;



public class MagickDecoder
{


    /// <summary>
    /// Loads metadata from file.
    /// </summary>
    /// <param name="filePath">Full path of the file</param>
    public static async Task<IgMetadata?> LoadMetadataAsync(string? filePath, PhotoReadOptions? options = null, CancellationToken token = default)
    {
        FileInfo? fi = null;
        filePath ??= string.Empty;
        var meta = new IgMetadata() { FilePath = filePath };

        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();

            fi = new FileInfo(filePath);
        }
        catch { }
        if (fi == null) return meta;
        var ext = fi.Extension.ToUpperInvariant();

        meta.FileName = fi.Name;
        meta.FileExtension = ext;
        meta.FolderPath = fi.DirectoryName ?? string.Empty;
        meta.FolderName = Path.GetFileName(meta.FolderPath);

        meta.FileSize = fi.Length;
        meta.FileCreationTime = fi.CreationTime;
        meta.FileLastWriteTime = fi.LastWriteTime;
        meta.FileLastAccessTime = fi.LastAccessTime;


        var settings = ParseSettings(options, false, filePath);
        using var imgC = new MagickImageCollection();

        // read metadata
        try
        {
            var allBytes = await File.ReadAllBytesAsync(filePath, token);
            imgC.Ping(allBytes, settings);

            meta.FrameIndex = 0;
            meta.FrameCount = imgC.Count;
        }
        catch { }

        if (imgC.Count == 0) return meta;


        // parse metadata
        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();

            var frameIndex = options?.FrameIndex ?? 0;

            // Check if frame index is greater than upper limit
            if (frameIndex >= imgC.Count)
                frameIndex = 0;

            // Check if frame index is less than lower limit
            else if (frameIndex < 0)
                frameIndex = imgC.Count - 1;

            meta.FrameIndex = (uint)frameIndex;
            using var imgM = imgC[frameIndex];


            // image size
            meta.OriginalWidth = imgM.BaseWidth;
            meta.OriginalHeight = imgM.BaseHeight;
            meta.RenderedWidth = imgM.Width;
            meta.RenderedHeight = imgM.Height;

            // image color
            meta.HasAlpha = imgC.Any(i => i.HasAlpha);
            meta.ColorSpace = imgM.ColorSpace.ToString();
            meta.CanAnimate = CheckAnimatedFormat(imgC, ext);

            // cancel if requested
            token.ThrowIfCancellationRequested();


            // EXIF profile
            if (imgM.GetExifProfile() is IExifProfile exifProfile)
            {
                // ExifRatingPercent
                meta.ExifRatingPercent = GetExifValue(exifProfile, ExifTag.RatingPercent);

                // ExifDateTimeOriginal
                var dt = GetExifValue(exifProfile, ExifTag.DateTimeOriginal);
                meta.ExifDateTimeOriginal = BHelper.ConvertDateTime(dt);

                // ExifDateTime
                dt = GetExifValue(exifProfile, ExifTag.DateTime);
                meta.ExifDateTime = BHelper.ConvertDateTime(dt);

                meta.ExifArtist = GetExifValue(exifProfile, ExifTag.Artist);
                meta.ExifCopyright = GetExifValue(exifProfile, ExifTag.Copyright);
                meta.ExifSoftware = GetExifValue(exifProfile, ExifTag.Software);
                meta.ExifImageDescription = GetExifValue(exifProfile, ExifTag.ImageDescription);
                meta.ExifModel = GetExifValue(exifProfile, ExifTag.Model);
                meta.ExifISOSpeed = (int?)GetExifValue(exifProfile, ExifTag.ISOSpeed);

                var rational = GetExifValue(exifProfile, ExifTag.ExposureTime);
                meta.ExifExposureTime = rational.Denominator == 0
                    ? null
                    : rational.Numerator / rational.Denominator;

                rational = GetExifValue(exifProfile, ExifTag.FNumber);
                meta.ExifFNumber = rational.Denominator == 0
                    ? null
                    : rational.Numerator / rational.Denominator;

                rational = GetExifValue(exifProfile, ExifTag.FocalLength);
                meta.ExifFocalLength = rational.Denominator == 0
                    ? null
                    : rational.Numerator / rational.Denominator;
            }
            else
            {
                //try
                //{
                //    using var fs = File.OpenRead(filePath);
                //    using var img = Image.FromStream(fs, false, false);
                //    var enc = new ASCIIEncoding();

                //    var EXIF_DateTimeOriginal = 0x9003; //36867
                //    var EXIF_DateTime = 0x0132;

                //    try
                //    {
                //        // get EXIF_DateTimeOriginal
                //        var pi = img.GetPropertyItem(EXIF_DateTimeOriginal);
                //        var dateTimeText = enc.GetString(pi.Value, 0, pi.Len - 1);

                //        if (DateTime.TryParseExact(dateTimeText, "yyyy:MM:dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var exifDateTimeOriginal))
                //        {
                //            meta.ExifDateTimeOriginal = exifDateTimeOriginal;
                //        }
                //    }
                //    catch { }


                //    try
                //    {
                //        // get EXIF_DateTime
                //        var pi = img.GetPropertyItem(EXIF_DateTime);
                //        var dateTimeText = enc.GetString(pi.Value, 0, pi.Len - 1);

                //        if (DateTime.TryParseExact(dateTimeText, "yyyy:MM:dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var exifDateTime))
                //        {
                //            meta.ExifDateTime = exifDateTime;
                //        }
                //    }
                //    catch { }
                //}
                //catch { }
            }


            // cancel if requested
            token.ThrowIfCancellationRequested();

            // Color profile
            if (imgM.GetColorProfile() is IColorProfile colorProfile)
            {
                meta.ColorProfile = colorProfile.ColorSpace.ToString();

                if (!string.IsNullOrWhiteSpace(colorProfile.Description))
                {
                    meta.ColorProfile = $"{colorProfile.Description} ({meta.ColorProfile})";
                }
            }
        }
        catch { }

        return meta;
    }




    /// <summary>
    /// Parse <see cref="PhotoReadOptions"/> to <see cref="MagickReadSettings"/>
    /// </summary>
    private static MagickReadSettings ParseSettings(PhotoReadOptions? options, bool writePurpose, string filePath = "")
    {
        options ??= new();

        var ext = Path.GetExtension(filePath).ToUpperInvariant();
        var settings = new MagickReadSettings
        {
            // https://github.com/dlemstra/Magick.NET/issues/1077
            SyncImageWithExifProfile = true,
            SyncImageWithTiffProperties = true,
        };

        if (ext.Equals(".SVG", StringComparison.OrdinalIgnoreCase))
        {
            settings.SetDefine("svg:xml-parse-huge", "true");
            settings.Format = MagickFormat.Rsvg;
            settings.BackgroundColor = MagickColors.Transparent;
        }
        else if (ext.Equals(".SVGZ", StringComparison.OrdinalIgnoreCase))
        {
            settings.Format = MagickFormat.Svgz;
            settings.BackgroundColor = MagickColors.Transparent;
        }
        else if (ext.Equals(".HEIC", StringComparison.OrdinalIgnoreCase))
        {
            settings.SetDefines(new HeicReadDefines()
            {
                MaxChildrenPerBox = 500,
            });
        }
        else if (ext.Equals(".JP2", StringComparison.OrdinalIgnoreCase))
        {
            settings.SetDefines(new Jp2ReadDefines
            {
                QualityLayers = 100,
            });
        }
        else if (ext.Equals(".TIF", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".TIFF", StringComparison.OrdinalIgnoreCase))
        {
            settings.SetDefines(new TiffReadDefines
            {
                IgnoreTags = [
                    // Issue https://github.com/d2phap/ImageGlass/issues/1454
                    "34022", // ColorTable
                    "34025", // ImageColorValue
                    "34026", // BackgroundColorValue

                    // Issue https://github.com/d2phap/ImageGlass/issues/1181
                    "32928",

                    // Issue https://github.com/d2phap/ImageGlass/issues/1583
                    "32932", // Wang Annotation
                    // Issue https://github.com/d2phap/ImageGlass/issues/1617
                    "34031", // TrapIndicator
                ],
            });
        }
        else if (ext.Equals(".APNG", StringComparison.OrdinalIgnoreCase))
        {
            settings.Format = MagickFormat.APng;
        }

        if (options.Width > 0 && options.Height > 0)
        {
            settings.Width = options.Width;
            settings.Height = options.Height;

            if (ext == ".JPG" || ext == ".JPEG" || ext == ".JPE" || ext == ".JFIF")
            {
                settings.SetDefines(new JpegReadDefines()
                {
                    Size = new MagickGeometry(options.Width, options.Height),
                });
            }
        }

        // Fixed #708: length and filesize do not match
        settings.SetDefines(new BmpReadDefines
        {
            IgnoreFileSize = true,
        });

        // Fix RAW color
        settings.SetDefines(new DngReadDefines()
        {
            UseCameraWhiteBalance = true,
            OutputColor = DngOutputColor.SRGB,
            ReadThumbnail = true,
        });



        if (writePurpose)
        {
            if (ext == ".TIF" || ext == ".TIFF")
            {
                settings.SetDefines(new TiffWriteDefines
                {
                    WriteLayers = true,
                    PreserveCompression = true,
                });
            }
            else if (ext == ".WEBP")
            {
                settings.SetDefines(new WebPWriteDefines
                {
                    Lossless = true,
                    ThreadLevel = true,
                    AlphaQuality = 100,
                });
            }
        }

        return settings;
    }


    /// <summary>
    /// Get EXIF value.
    /// </summary>
    private static T? GetExifValue<T>(IExifProfile? profile, ExifTag<T> tag, T? defaultValue = default)
    {
        if (profile == null) return default;

        var exifValue = profile.GetValue(tag);
        if (exifValue == null) return defaultValue;

        return exifValue.Value;
    }


    /// <summary>
    /// Checks if the image data is animated format.
    /// </summary>
    /// <param name="imgC"></param>
    /// <param name="ext">File extension, e.g: <c>.gif</c></param>
    private static bool CheckAnimatedFormat(MagickImageCollection imgC, string? ext)
    {
        var isAnimatedExtension = ext == ".GIF" || ext == ".GIFV" || ext == ".WEBP" || ext == ".JXL";

        var canAnimate = imgC.Count > 1
            && (isAnimatedExtension || imgC.Any(i => i.GifDisposeMethod != GifDisposeMethod.Undefined));

        return canAnimate;
    }




}

