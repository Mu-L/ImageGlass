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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.Photoing;

public static partial class MagickCodec
{
    /// <summary>
    /// Indicates whether <see cref="MagickCodec"/> is initialized or not.
    /// </summary>
    public static bool IsInitialized { get; private set; } = false;


    /// <summary>
    /// Initializes ImageMagick decoder with OpenCL support.
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized) return;

#if DEBUG
        MagickNET.SetLogEvents(LogEventTypes.Exception);
#endif

        if (!ImageMagick.OpenCL.IsEnabled)
        {
            ImageMagick.OpenCL.IsEnabled = true;
        }

        IsInitialized = true;
    }


    /// <summary>
    /// Parse <see cref="PhotoReadOptions"/> to <see cref="MagickReadSettings"/>.
    /// </summary>
    public static MagickReadSettings ParseSettings(PhotoReadOptions? options,
        bool writePurpose, string filePath = "")
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
    /// Loads photo metadata from file path.
    /// </summary>
    public static async Task<PhotoMetadata> LoadMetadataAsync(string? filePath,
        PhotoReadOptions? options = null,
        MagickReadSettings? readSettings = null,
        CancellationToken token = default)
    {
        filePath ??= string.Empty;
        var meta = new PhotoMetadata();

        // 0. get file info
        meta.SetFilePath(filePath);
        if (string.IsNullOrWhiteSpace(filePath)) return meta;

        // 1. parse Magick settings
        var settings = readSettings ?? ParseSettings(options, false, filePath);
        using var imgC = new MagickImageCollection();

        // 2. ping data
        try
        {
            imgC.Ping(filePath, settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌❌❌ {nameof(LoadMetadataAsync)}: {ex.Message}");
        }

        // 3. load metadata
        meta = await LoadMetadataAsync(imgC, options, readSettings, token);
        meta.SetFilePath(filePath);

        return meta;
    }


    /// <summary>
    /// Loads photo metadata from byte array.
    /// </summary>
    public static async Task<PhotoMetadata> LoadMetadataAsync(byte[]? bytes,
        PhotoReadOptions? options = null,
        MagickReadSettings? readSettings = null,
        CancellationToken token = default)
    {
        var meta = new PhotoMetadata();
        if (bytes is null || bytes.Length == 0) return meta;

        // 1. parse Magick settings
        var settings = readSettings ?? ParseSettings(options, false);
        using var imgC = new MagickImageCollection();

        // 2. ping data
        try
        {
            imgC.Ping(bytes, settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌❌❌ {nameof(LoadMetadataAsync)}: {ex.Message}");
        }

        // 3. load metadata
        meta = await LoadMetadataAsync(imgC, options, readSettings, token);

        return meta;
    }


    /// <summary>
    /// Loads photo metadata from Magick instance.
    /// </summary>
    public static async Task<PhotoMetadata> LoadMetadataAsync(MagickImageCollection imgC,
        PhotoReadOptions? options = null,
        MagickReadSettings? readSettings = null,
        CancellationToken token = default)
    {
        var meta = new PhotoMetadata();
        if (imgC.Count == 0) return meta;


        // 1. calculate the specific frame index
        var frameIndex = options?.FrameIndex ?? 0;

        // make sure frame index is within range
        if (frameIndex >= imgC.Count) frameIndex = 0;
        else if (frameIndex < 0) frameIndex = imgC.Count - 1;

        meta.FrameIndex = (uint)frameIndex;


        var readingTask = Task.Run(() =>
        {
            // 2. read metadata of all frames
            try
            {
                meta.FrameIndex = 0;
                meta.FrameCount = (uint)imgC.Count;
                meta.AnimationLoop = imgC[0].AnimationIterations;
                meta.Frames = imgC.Select(item => new FrameMetadata()
                {
                    BackgroundColor = (MagickColor?)item.BackgroundColor ?? MagickColors.Transparent,
                    Width = item.Width,
                    Height = item.Height,
                    X = item.Page.X,
                    Y = item.Page.Y,

                    AnimationDelay = item.AnimationDelay,
                    AnimationTicksPerSecond = (uint)item.AnimationTicksPerSecond,
                    GifDisposeMethod = item.GifDisposeMethod,
                }).ToImmutableList();
            }
            catch { }


            // 3. read metadata of a specific frame
            try
            {
                // image size
                meta.OriginalWidth = meta.Width = imgC[frameIndex].Page.Width;
                meta.OriginalHeight = meta.Height = imgC[frameIndex].Page.Height;
                meta.Orientation = imgC[frameIndex].Orientation;

                // correct the image size according to orientation
                if (meta.Orientation != OrientationType.Undefined // no tag: Undefined
                    && meta.Orientation != OrientationType.TopLeft // Do nothing
                    && meta.Orientation != OrientationType.TopRight // Flip horizontally
                    && meta.Orientation != OrientationType.BottomLeft // Flip vertically
                )
                {
                    // swap width and height
                    meta.Width = meta.OriginalHeight;
                    meta.Height = meta.OriginalWidth;
                }


                // image color
                meta.HasAlpha = imgC.Any(i => i.HasAlpha);
                meta.ColorSpace = imgC[frameIndex].ColorSpace;
                meta.CanAnimate = CheckAnimatedFormat__(imgC, meta.FileExtension);

                // get RAW thumbnail
                meta.RawThumbnail = imgC[frameIndex].GetProfile("dng:thumbnail");
            }
            catch { }


            // cancel if requested
            if (token.IsCancellationRequested) return;


            // 4. read frame exif profile
            try
            {
                if (imgC[frameIndex].GetExifProfile() is IExifProfile exifProfile)
                {
                    meta.ExifProfile = exifProfile;

                    // ExifRatingPercent
                    meta.ExifRatingPercent = GetExifValue__(exifProfile, ExifTag.RatingPercent);

                    // ExifDateTimeOriginal
                    var dt = GetExifValue__(exifProfile, ExifTag.DateTimeOriginal);
                    meta.ExifDateTimeOriginal = BHelper.ConvertDateTime(dt);

                    // ExifDateTime
                    dt = GetExifValue__(exifProfile, ExifTag.DateTime);
                    meta.ExifDateTime = BHelper.ConvertDateTime(dt);

                    meta.ExifArtist = GetExifValue__(exifProfile, ExifTag.Artist);
                    meta.ExifCopyright = GetExifValue__(exifProfile, ExifTag.Copyright);
                    meta.ExifSoftware = GetExifValue__(exifProfile, ExifTag.Software);
                    meta.ExifImageDescription = GetExifValue__(exifProfile, ExifTag.ImageDescription);
                    meta.ExifModel = GetExifValue__(exifProfile, ExifTag.Model);
                    meta.ExifISOSpeed = (int?)GetExifValue__(exifProfile, ExifTag.ISOSpeed);

                    var rational = GetExifValue__(exifProfile, ExifTag.ExposureTime);
                    meta.ExifExposureTime = rational.Denominator == 0
                        ? null
                        : rational.Numerator / rational.Denominator;

                    rational = GetExifValue__(exifProfile, ExifTag.FNumber);
                    meta.ExifFNumber = rational.Denominator == 0
                        ? null
                        : rational.Numerator / rational.Denominator;

                    rational = GetExifValue__(exifProfile, ExifTag.FocalLength);
                    meta.ExifFocalLength = rational.Denominator == 0
                        ? null
                        : rational.Numerator / rational.Denominator;
                }
            }
            catch { }


            // cancel if requested
            if (token.IsCancellationRequested) return;


            // 5. read color profile
            try
            {
                // Color profile
                if (imgC[frameIndex].GetColorProfile() is IColorProfile colorProfile)
                {
                    meta.ColorSpace = colorProfile.ColorSpace;
                    meta.ColorProfileName = colorProfile.Description ?? "";
                    meta.ColorProfileData = colorProfile.ToByteArray();
                }
            }
            catch { }

        }, token).ConfigureAwait(false);


        await readingTask;

        return meta;
    }


    /// <summary>
    /// Reads and processes image file with Magick.NET.
    /// </summary>
    public static async Task<MagickDecoderOutput> DecodeImageAsync(
        PhotoMetadata meta,
        PhotoReadOptions options, MagickReadSettings? settings,
        ImgTransform? transform, CancellationToken cancelToken)
    {
        settings ??= ParseSettings(options, false, meta.FilePath);
        var result = new MagickDecoderOutput();

        // 1. standardize first frame reading option
        bool readFirstFrameOnly;
        if (options.FirstFrameOnly == null)
        {
            readFirstFrameOnly = meta.FrameCount < 2;
        }
        else
        {
            readFirstFrameOnly = options.FirstFrameOnly.Value;
        }


        // 2. read all frames if requested
        if (meta.FrameCount > 1 && readFirstFrameOnly is false)
        {
            var imgColl = new MagickImageCollection();
            await imgColl.ReadAsync(meta.FilePath, settings, cancelToken);

            var i = 0;
            foreach (var imgFrameM in imgColl)
            {
                ProcessMagickImage__((MagickImage)imgFrameM, options, meta, false);

                // apply transformation
                if (i == transform?.FrameIndex || transform?.FrameIndex == -1)
                {
                    TransformImage__(imgFrameM, transform);
                }

                i++;
            }

            result.MultiFrames = imgColl;
            return result;
        }


        // 3. read a single frame only
        var imgM = new MagickImage();
        var hasRequestedThumbnail = false;

        // 3.1 read embedded thumbnail only
        if (options.UseEmbeddedThumbnailRawFormats is true)
        {
            try
            {
                // try to get thumbnail
                if (meta.RawThumbnail != null)
                {
                    var thumbSpan = meta.RawThumbnail.ToReadOnlySpan();

                    imgM.Dispose();
                    imgM.Ping(thumbSpan);

                    // check min size
                    if (imgM.Width > options.EmbeddedThumbnailMinWidth
                        && imgM.Height > options.EmbeddedThumbnailMinHeight)
                    {
                        imgM.Read(thumbSpan, settings);
                        hasRequestedThumbnail = true;
                    }
                }
            }
            catch { }
        }


        // 3.2 read full image data
        if (!hasRequestedThumbnail)
        {
            imgM.Dispose();
            await imgM.ReadAsync(meta.FilePath, settings, cancelToken);
        }


        // 3.3 process image
        var thumbM = ProcessMagickImage__(imgM, options, meta, true);
        if (thumbM != null) imgM = thumbM;


        // 3.4 apply final changes
        TransformImage__(imgM, transform);
        result.SingleFrame = imgM;

        return result;
    }


    /// <summary>
    /// Gets thumbnail from the given image path.
    /// </summary>
    public static async Task<byte[]?> QuickDecodeAsync(string filePath, MagickFormat outputFormat,
        double desiredWidth, double desiredHeight,
        double minSize = 0, double maxSize = double.PositiveInfinity,
        CancellationToken token = default)
    {
        using var imgM = await QuickDecodeAsync(filePath, desiredWidth, desiredHeight, minSize, maxSize, token);

        return imgM?.ToByteArray(outputFormat);
    }


    /// <summary>
    /// Gets thumbnail from the given image path.
    /// </summary>
    public static async Task<MagickImage?> QuickDecodeAsync(string filePath,
        double desiredWidth, double desiredHeight,
        double minSize = 0, double maxSize = double.PositiveInfinity,
        CancellationToken token = default)
    {
        var options = new PhotoReadOptions()
        {
            Width = (uint)desiredWidth,
            Height = (uint)desiredHeight,
        };
        var settings = ParseSettings(options, false, filePath);

        try
        {
            var imgM = new MagickImage();
            imgM.Ping(filePath);

            // check the dimention constraint
            if (imgM.Width < minSize
                || imgM.Height < minSize
                || imgM.Width > maxSize
                || imgM.Height > maxSize) return null;

            await imgM.ReadAsync(filePath, settings, token);

            return imgM;
        }
        catch { }

        return null;
    }


    /// <summary>
    /// Decodes photo from base64 string.
    /// </summary>
    public static async Task<Photo?> DecodeBase64Async(string base64)
    {
        // 1. convert base64 string to bytes
        var (MimeType, ByteData) = ConvertBase64ToBytes(base64);
        if (string.IsNullOrEmpty(MimeType)) return null;


        // 2. convert Mime type to Magick format
        // supported MIME types:
        // https://www.iana.org/assignments/media-types/media-types.xhtml#image
        var format = ConvertMimeTypeToMagickFormat__(MimeType);


        // 3. create settings
        IDisposable? bmpSrc = null;
        var readSettings = new MagickReadSettings { Format = format };
        if (readSettings.Format == MagickFormat.Rsvg)
        {
            readSettings.BackgroundColor = MagickColors.Transparent;
        }


        // 4. load bitmap from bytes
        switch (format)
        {
            // 4.1 use WIC for multiple-frame formats
            case MagickFormat.Gif:
            case MagickFormat.Gif87:
            case MagickFormat.Tif:
            case MagickFormat.Tiff64:
            case MagickFormat.Tiff:
            case MagickFormat.Ico:
            case MagickFormat.Icon:
                bmpSrc = PhotoWIC.CreateDecoder(ByteData);
                break;

            // 4.2 use Magick for the rest
            default:
                using (var imgM = new MagickImage(ByteData, readSettings))
                {
                    bmpSrc = PhotoWIC.ConvertFromMagick(imgM);
                }
                break;
        }


        // 4. wrap the bitmap as photo object.
        var meta = await LoadMetadataAsync(ByteData, null, readSettings);
        var photo = new Photo(bmpSrc, meta);

        return photo;
    }


    /// <summary>
    /// Converts base64 string to byte array, returns MIME type and raw data in byte array.
    /// </summary>
    public static (string MimeType, byte[] ByteData) ConvertBase64ToBytes(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentNullException(nameof(content));
        }

        // data:image/svg-xml;base64,xxxxxxxx
        // type is optional
        var base64DataUriRegex = CreateBase64DataUriRegex__();
        var match = base64DataUriRegex.Match(content);

        if (!match.Success)
        {
            throw new FormatException("The base64 content is invalid.");
        }


        var base64Data = match.Groups["data"].Value;
        var byteData = Convert.FromBase64String(base64Data);
        var mimeType = match.Groups["type"].Value.ToLowerInvariant();

        if (mimeType.Length == 0)
        {
            // use default PNG MIME type
            mimeType = "image/png";
        }

        return (mimeType, byteData);
    }


    /// <summary>
    /// Checks if the format can be written.
    /// </summary>
    public static bool CanWrite(string destFilePath)
    {
        return MagickFormatInfo.Create(destFilePath)?.SupportsWriting ?? false;
    }


    /// <summary>
    /// Save the photo to file.
    /// </summary>
    /// <param name="meta">Source metadata</param>
    /// <param name="destFilePath">Destination filename</param>
    /// <param name="options">Options for reading image file</param>
    /// <param name="transform">Changes for writing image file</param>
    /// <param name="quality">Quality</param>
    /// <exception cref="Exception"></exception>
    public static async Task SaveAsync(PhotoMetadata meta, string destFilePath, PhotoReadOptions options,
        ImgTransform? transform = null, uint quality = 100, CancellationToken token = default)
    {
        var ext = Path.GetExtension(destFilePath);

        try
        {
            // 1. check if format is supported
            if (!CanWrite(destFilePath))
            {
                throw new FormatException("IGE_001: Unsupported image format.");
            }


            // 2. read the photo
            var settings = ParseSettings(options, true, meta.FilePath);
            using var result = await DecodeImageAsync(meta, options with
            {
                // Magick.NET auto-corrects the rotation when saving,
                // so we don't need to correct it manually.
                CorrectRotation = false,
            }, settings, transform, token);


            // 3. save the photo to file
            if (result.MultiFrames is not null)
            {
                await result.MultiFrames.WriteAsync(destFilePath, token);
            }
            else if (result.SingleFrame is not null)
            {
                result.SingleFrame.Quality = quality;

                // resize ICO file if it's larger than 256
                if (ext.Equals(".ICO", StringComparison.OrdinalIgnoreCase))
                {
                    var imgW = result.SingleFrame.Width;
                    var imgH = result.SingleFrame.Height;
                    const int MAX_ICON_SIZE = 256;

                    if (imgW > MAX_ICON_SIZE || imgH > MAX_ICON_SIZE)
                    {
                        var iconSize = GetMaxImageRenderSize__(imgW, imgH, MAX_ICON_SIZE);
                        result.SingleFrame.Scale((uint)iconSize.Width, (uint)iconSize.Height);
                    }
                }

                await result.SingleFrame.WriteAsync(destFilePath, token);
            }
        }
        catch (OperationCanceledException) { }
    }


    /// <summary>
    /// Exports image frames to files, using Magick.NET
    /// </summary>
    /// <param name="srcFilePath">The full path of source file</param>
    /// <param name="destFolder">The destination folder to save to</param>
    public static async IAsyncEnumerable<(int FrameNumber, string FileName)> SaveFramesAsync(string srcFilePath,
        string destFolder, [EnumeratorCancellation] CancellationToken token = default)
    {
        // create dirs unless it does not exist
        Directory.CreateDirectory(destFolder);

        using var imgColl = new MagickImageCollection(srcFilePath);
        var index = 0;

        foreach (var imgM in imgColl)
        {
            index++;
            imgM.Quality = 100;
            var newFilename = string.Empty;

            try
            {
                newFilename = Path.GetFileNameWithoutExtension(srcFilePath)
                    + " - " + index.ToString($"D{imgColl.Count.ToString().Length}")
                    + ".png";
                var destFilePath = Path.Combine(destFolder, newFilename);

                await imgM.WriteAsync(destFilePath, MagickFormat.Png, token);
            }
            catch (OperationCanceledException) { break; }
            catch { }

            yield return (index, newFilename);
        }
    }








    /// <summary>
    /// Get color profile
    /// </summary>
    /// <param name="nameOrPath">Name or Full path of color profile</param>
    public static ColorProfile? GetColorProfile(string nameOrPath)
    {
        var currentMonitorProfile = nameof(ColorProfileOption.CurrentMonitorProfile);

        if (nameOrPath.Equals(currentMonitorProfile, StringComparison.InvariantCultureIgnoreCase))
        {
            // TODO:
            //var winHandle = Process.GetCurrentProcess().MainWindowHandle;
            //var colorProfilePath = DisplayApi.GetMonitorColorProfileFromWindow(winHandle);

            //if (string.IsNullOrEmpty(colorProfilePath))
            //{
            //    return ColorProfile.SRGB;
            //}

            //return new ColorProfile(colorProfilePath);
            return ColorProfiles.SRGB;
        }
        else if (File.Exists(nameOrPath))
        {
            return new ColorProfile(nameOrPath);
        }
        else
        {
            // get all profile names in Magick.NET
            var profiles = typeof(ColorProfile).GetProperties();
            var result = Array.Find(profiles, i => string.Equals(i.Name, nameOrPath, StringComparison.InvariantCultureIgnoreCase));

            if (result != null)
            {
                try
                {
                    return (ColorProfile?)result.GetValue(result);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        return null;
    }



}

