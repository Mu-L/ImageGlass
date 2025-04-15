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

using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageMagick;
using SharpGen.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vortice.WIC;


namespace ImageGlass.WinNT.Common;


public partial class Photo : PhotoImpl<IWICBitmapSource>
{
    // private properties
    private PhotoColorProfile? _colorContext;
    private IWICPixelFormatInfo2? _pixelFormatInfo;


    // Public Properties
    #region Public Properties

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override IWICBitmapSource? Bitmap => _bitmap;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override int Width => _bitmap?.Size.Width ?? 0;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override int Height => _bitmap?.Size.Height ?? 0;


    /// <summary>
    /// Gets the color profile of the photo.
    /// </summary>
    public PhotoColorProfile ColorProfile => LazyInitializer.EnsureInitialized(
        ref _colorContext, GetColorProfile);


    /// <summary>
    /// Gets the pixel information of the photo.
    /// </summary>
    public IWICPixelFormatInfo2? PixelFormatInfo => LazyInitializer.EnsureInitialized(
        ref _pixelFormatInfo, LoadPixelInfo);

    #endregion // Public Properties



    /// <summary>
    /// Initializes a new instance using the specified file path for the image file.
    /// </summary>
    public Photo(string filePath = "") : base(filePath) { }


    /// <summary>
    /// Initializes a new instance using a bitmap source for rendering.
    /// </summary>
    public Photo(IWICBitmapSource wicSrc)
    {
        DisposeNativeResources();

        _bitmap = wicSrc;
    }



    // Override Functions
    #region Override Functions

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing(bool disposeMetadata)
    {
        base.OnDisposing(disposeMetadata);

        // dispose native resources
        DisposeNativeResources();
    }


    /// <summary>
    /// Loads photo.
    /// </summary>
    public override Task LoadAsync(PhotoReadOptions? newOptions = null)
    {
        DisposeNativeResources();

        return base.LoadAsync(newOptions);
    }



    /// <summary>
    /// Handles the decoding of image files based on their metadata.
    /// </summary>
    protected override async Task OnDecodingAsync(IgMetadata meta, CancellationToken token)
    {
        var extWIC = new string[] { ".HEIC" };

        // use WIC decoders
        if (meta.ColorSpace != ColorSpace.CMYK && extWIC.Contains(meta.FileExtension))
        {
            using var wicFactory = new IWICImagingFactory2();
            using var decoder = wicFactory.CreateDecoderFromFileName(FilePath);
            var frameBmp = decoder.GetFrame((uint)(_metadata?.FrameIndex ?? 0));

            _bitmap = frameBmp;
            return;
        }


        // use default decoders
        var data = await MagickDecoder.DecodeImageAsync(meta, ReadOptions, ReadSettings, null, token);

        // multi-frame
        if (data.MultiFrameImage != null)
        {
            // animated format
            if (meta.CanAnimate)
            {
                //// fall back to use Magick.NET
                //data.MultiFrameImage.Coalesce();
                //var frames = data.MultiFrameImage.AsEnumerable().Select(frame =>
                //{
                //    var duration = frame.AnimationDelay > 0 ? frame.AnimationDelay : 10;
                //    duration = duration * 1000 / (uint)frame.AnimationTicksPerSecond;

                //    return new AnimatedImgFrame(frame.ToBitmap(), duration);
                //});

                //Source = new AnimatedImg(frames, data.FrameCount);
            }

            // multi-frame formats
            else
            {
                //var bytes = data.MultiFrameImage.ToByteArray(MagickFormat.Tiff);
                //Source = WicBitmapDecoder.Load(new MemoryStream(bytes) { Position = 0 });
            }
        }

        // single-frame formats
        else
        {
            var bmpSrc = data.SingleFrameImage?.ToBitmapSourceWithDensity();
            _bitmap = Photo.ToWicBitmapSource(bmpSrc, meta.HasAlpha);
        }
    }


    #endregion // Override Functions



    // Private Functions
    #region Private Functions

    /// <summary>
    /// Releases unmanaged resources.
    /// </summary>
    private void DisposeNativeResources()
    {
        // dispose color contexts
        _colorContext?.Dispose();
        _colorContext = null;

        // dispose pixel format info
        _pixelFormatInfo?.Dispose();
        _pixelFormatInfo = null;
    }


    /// <summary>
    /// Gets color profile of the photo.
    /// </summary>
    private PhotoColorProfile GetColorProfile()
    {
        if (_bitmap == null) return new();

        using var wicFactory = new IWICImagingFactory2();
        var frame = _bitmap.As<IWICBitmapFrameDecode>();

        try
        {
            var contexts = frame.TryGetColorContexts(wicFactory) ?? [];
            var bestProfile = FindBestColorProfile(contexts);
            byte[]? profileBytes = null;


            // get color profile
            if (bestProfile?.Profile is not null)
            {
                using var ms = new MemoryStream();
                bestProfile.Profile.CopyTo(ms);
                profileBytes = ms.ToArray();
            }


            // get color space
            var exitColorSpace = bestProfile?.ExifColorSpace;
            var colorSpace = PhotoColorSpace.None;

            if (exitColorSpace == 1)
            {
                colorSpace = PhotoColorSpace.sRGB;
            }
            else if (exitColorSpace == 2
                || (profileBytes != null && Encoding.ASCII.GetString(profileBytes).Contains("Adobe RGB")))
            {
                colorSpace = PhotoColorSpace.AdobeRGB;
            }
            else if (exitColorSpace == 0xFFFF)
            {
                colorSpace = PhotoColorSpace.Uncalibrated;
            }
            else if (exitColorSpace != null)
            {
                colorSpace = PhotoColorSpace.Unknown;
            }


            var colorContext = new PhotoColorProfile(colorSpace, profileBytes, bestProfile);


            // dispose native color contexts
            foreach (var ctx in contexts)
            {
                if (ctx.NativePointer != bestProfile?.NativePointer)
                {
                    ctx.Dispose();
                }
            }

            return colorContext;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return new PhotoColorProfile();
    }


    /// <summary>
    /// Finds the best color profile.
    /// </summary>
    private static IWICColorContext? FindBestColorProfile(IWICColorContext[]? contexts)
    {
        IWICColorContext? bestProfile = null;
        if (contexts == null) return bestProfile;


        // get the last non-uncalibrated color context
        // https://stackoverflow.com/a/70215280/403671
        foreach (var ctx in contexts.Reverse())
        {
            // Uncalibrated
            if (ctx.ExifColorSpace == 0xFFFF)
                continue;

            if (ctx.Type == Vortice.WIC.ColorContextType.Profile)
            {
                bestProfile = ctx;
            }
        }


        return bestProfile;
    }


    /// <summary>
    /// Loads pixel format information for a bitmap.
    /// </summary>
    private IWICPixelFormatInfo2 LoadPixelInfo()
    {
        if (_bitmap == null) return new IWICPixelFormatInfo2(IntPtr.Zero);

        using var wicFactory = new IWICImagingFactory2();

        var comInfo = wicFactory.CreateComponentInfo(_bitmap.PixelFormat);
        return comInfo.As<IWICPixelFormatInfo2>();
    }


    #endregion // Private Functions


}


