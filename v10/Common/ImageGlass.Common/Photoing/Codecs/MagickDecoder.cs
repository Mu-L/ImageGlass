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
using System.Collections.Immutable;

namespace ImageGlass.Common.Photoing;


public class MagickDecoder
{

    /// <summary>
    /// Parse <see cref="PhotoReadOptions"/> to <see cref="MagickReadSettings"/>
    /// </summary>
    public static MagickReadSettings ParseSettings(PhotoReadOptions? options, bool writePurpose, string filePath = "")
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
    /// Loads metadata from file.
    /// </summary>
    /// <param name="filePath">Full path of the file</param>
    public static async Task<PhotoMetadata> LoadMetadataAsync(
        string? filePath,
        PhotoReadOptions? options = null,
        MagickReadSettings? readSettings = null,
        CancellationToken token = default)
    {
        FileInfo? fi = null;
        filePath ??= string.Empty;
        var meta = new PhotoMetadata() { FilePath = filePath };

        // 0. get file info
        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();

            fi = new FileInfo(filePath);

            meta.FileName = fi.Name;
            meta.FileExtension = fi.Extension.ToUpperInvariant();
            meta.FolderPath = fi.DirectoryName ?? string.Empty;
            meta.FolderName = fi.Directory?.Name ?? string.Empty;

            meta.FileSize = fi.Length;
            meta.FileCreationTime = fi.CreationTime;
            meta.FileLastWriteTime = fi.LastWriteTime;
            meta.FileLastAccessTime = fi.LastAccessTime;
        }
        catch { }
        if (fi == null) return meta;

        var settings = readSettings ?? ParseSettings(options, false, filePath);
        using var imgC = new MagickImageCollection();

        try
        {
            imgC.Ping(filePath, settings);
        }
        catch { }
        if (imgC.Count == 0) return meta;


        // 1. calculate the specific frame index
        var frameIndex = options?.FrameIndex ?? 0;

        // make sure frame index is within range
        if (frameIndex >= imgC.Count) frameIndex = 0;
        else if (frameIndex < 0) frameIndex = imgC.Count - 1;

        meta.FrameIndex = (uint)frameIndex;


        var readingTask = Task.Run(() =>
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();


            // 2. read metadata of all frames
            try
            {
                meta.FrameIndex = 0;
                meta.FrameCount = imgC.Count;
                meta.Frames = imgC.Select(item => new FrameMetadata()
                {
                    BackgroundColor = item.BackgroundColor ?? MagickColors.Transparent,
                    Width = item.Page.Width,
                    Height = item.Page.Height,
                    X = item.Page.X,
                    Y = item.Page.Y,

                    AnimationDelay = item.AnimationDelay,
                    AnimationTicksPerSecond = (uint)item.AnimationTicksPerSecond,
                    AnimationLoop = item.AnimationIterations,
                    GifDisposeMethod = item.GifDisposeMethod,
                }).ToImmutableList();
            }
            catch { }


            // cancel if requested
            token.ThrowIfCancellationRequested();


            // 3. read metadata of a specific frame
            try
            {
                // image size
                meta.OriginalWidth = imgC[frameIndex].Page.Width;
                meta.OriginalHeight = imgC[frameIndex].Page.Height;


                // image color
                meta.HasAlpha = imgC.Any(i => i.HasAlpha);
                meta.ColorSpace = imgC[frameIndex].ColorSpace;
                meta.CanAnimate = CheckAnimatedFormat(imgC, meta.FileExtension);

                // get RAW thumbnail
                meta.RawThumbnail = imgC[frameIndex].GetProfile("dng:thumbnail");
            }
            catch { }


            // cancel if requested
            token.ThrowIfCancellationRequested();


            // 4 read frame exif profile
            try
            {
                if (imgC[frameIndex].GetExifProfile() is IExifProfile exifProfile)
                {
                    meta.ExifProfile = exifProfile;

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
            }
            catch { }


            // cancel if requested
            token.ThrowIfCancellationRequested();


            // 5 read color profile
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

        }, token);



        try
        {
            await readingTask;
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Info($"Cancelled LoadMetadataAsync() for {filePath}");
        }

        return meta;
    }


    /// <summary>
    /// Reads and processes image file with Magick.NET.
    /// </summary>
    public static async Task<IgMagickReadData> DecodeImageAsync(
        PhotoMetadata meta,
        PhotoReadOptions options, MagickReadSettings? settings,
        ImgTransform? transform, CancellationToken cancelToken)
    {
        settings ??= ParseSettings(options, false, meta.FilePath);
        var result = new IgMagickReadData();

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
                ProcessMagickImage((MagickImage)imgFrameM, options, meta, false);

                // apply transformation
                if (i == transform?.FrameIndex || transform?.FrameIndex == -1)
                {
                    TransformImage(imgFrameM, transform);
                }

                i++;
            }

            result.MultiFrameImage = imgColl;
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
        var thumbM = ProcessMagickImage(imgM, options, meta, true);
        if (thumbM != null) imgM = thumbM;


        // 3.4 apply final changes
        TransformImage(imgM, transform);
        result.SingleFrameImage = imgM;

        return result;
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
            return ColorProfile.SRGB;
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
            && (isAnimatedExtension || imgC.Any(i => i.AnimationDelay > 0));

        return canAnimate;
    }


    /// <summary>
    /// Processes single-frame Magick image.
    /// Returns thumbnail image if requested.
    /// </summary>
    /// <param name="refImgM">Input Magick image to process</param>
    private static MagickImage? ProcessMagickImage(MagickImage refImgM, PhotoReadOptions options, PhotoMetadata meta, bool requestThumbnail)
    {
        IMagickImage? thumbM = null;


        // Use embedded thumbnails if specified
        if (requestThumbnail && meta.ExifProfile != null && options.UseEmbeddedThumbnailOtherFormats)
        {
            // Fetch the embedded thumbnail
            thumbM = meta.ExifProfile.CreateThumbnail();
            if (thumbM != null
                && thumbM.Width > options.EmbeddedThumbnailMinWidth
                && thumbM.Height > options.EmbeddedThumbnailMinHeight)
            {
                if (options.CorrectRotation) thumbM.AutoOrient();

                ApplySizeSettings(thumbM, options);
            }
            else
            {
                thumbM?.Dispose();
                thumbM = null;
            }
        }

        // Revert to source image if an embedded thumbnail with required size was not found.
        if (!requestThumbnail || thumbM == null)
        {
            // resize the image
            ApplySizeSettings(refImgM, options);

            // for HEIC/HEIF, PreserveOrientation must be false
            // see https://github.com/d2phap/ImageGlass/issues/1928
            if (options.CorrectRotation) refImgM.AutoOrient();


            // make sure the output color space is not CMYK
            if (meta.ColorSpace == ColorSpace.CMYK && meta.ColorProfileData is not null)
            {
                var colorProfile = new ColorProfile(meta.ColorProfileData);
                refImgM.TransformColorSpace(colorProfile, ColorProfile.SRGB);
            }
        }


        return (MagickImage?)thumbM;
    }


    /// <summary>
    /// Applies the size settings
    /// </summary>
    private static void ApplySizeSettings(IMagickImage imgM, PhotoReadOptions options)
    {
        if (options.Width > 0 && options.Height > 0)
        {
            if (imgM.BaseWidth > options.Width || imgM.BaseHeight > options.Height)
            {
                imgM.Thumbnail(options.Width, options.Height);
            }
        }
    }


    /// <summary>
    /// Applies changes from <see cref="ImgTransform"/>.
    /// </summary>
    private static void TransformImage(IMagickImage imgM, ImgTransform? transform = null)
    {
        if (transform == null) return;

        // rotate
        if (transform.Rotation != 0)
        {
            imgM.Rotate(transform.Rotation);
        }

        // flip
        if (transform.Flips.HasFlag(FlipOptions.Horizontal))
        {
            imgM.Flop();
        }
        if (transform.Flips.HasFlag(FlipOptions.Vertical))
        {
            imgM.Flip();
        }

        // invert color
        if (transform.IsColorInverted)
        {
            imgM.Negate(Channels.RGB);
        }
    }

}





/// <summary>
/// Contains Magick.NET data after the image file loaded.
/// </summary>
public class IgMagickReadData : IDisposable
{

    #region IDisposable Disposing

    public bool IsDisposed { get; protected set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (disposing)
        {
            // Free any other managed objects here.
            MultiFrameImage?.Dispose();
            MultiFrameImage = null;

            SingleFrameImage?.Dispose();
            SingleFrameImage = null;
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~IgMagickReadData()
    {
        Dispose(false);
    }

    #endregion


    public MagickImageCollection? MultiFrameImage { get; set; } = null;
    public MagickImage? SingleFrameImage { get; set; } = null;

}

