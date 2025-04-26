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
using ImageGlass.WinNT.Common.Photoing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortice.WIC;


namespace ImageGlass.WinNT.Common;


public partial class Photo : PhotoImpl
{
    // private properties
    private PhotoColorProfile? _colorContext;
    private IWICPixelFormatInfo2? _pixelFormatInfo;


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
        _width = (uint)wicSrc.Size.Width;
        _height = (uint)wicSrc.Size.Height;

        _metadata?.Dispose();
        _metadata = new PhotoMetadata();
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
    protected override async Task OnDecodingAsync(PhotoMetadata meta, CancellationToken token)
    {
        var extWIC = new string[] { ".GIF", ".GIFV", ".WEBP", ".FAX", ".JXR", ".APNG" };


        // use WIC decoders
        if (meta.ColorSpace != ImageMagick.ColorSpace.CMYK && extWIC.Contains(meta.FileExtension))
        {
            await LoadWithWICAsync(meta, token);
        }

        // use default decoders
        else
        {
            await LoadWithMagickAsync(meta, token);
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
    /// Loads an image using WIC.
    /// </summary>
    private async Task LoadWithWICAsync(PhotoMetadata meta, CancellationToken token)
    {
        _bitmap = await Task.Run<IDisposable>(() =>
        {
            using var wicFactory = new IWICImagingFactory2();
            var decoder = wicFactory.CreateDecoderFromFileName(meta.FilePath);

            // 1. read animated formats
            if (meta.CanAnimate)
            {
                _width = meta.Width;
                _height = meta.Height;

                // .GIF
                if (meta.IsOneOfExtensions(".GIF", ".GIFV"))
                {
                    return new GifAnimator(decoder, meta);
                }
                else
                {
                    return new GifAnimator(decoder, meta);
                }
            }

            // 2. read non-animated multi-frame formats
            if (meta.FrameCount > 1)
            {
                _width = meta.Width;
                _height = meta.Height;

                return decoder;
            }


            // 3. read single-frame formats
            var frameBmp = decoder.GetFrame(meta.FrameIndex);

            _width = (uint)frameBmp.Size.Width;
            _height = (uint)frameBmp.Size.Height;

            decoder.Dispose();
            decoder = null;

            return frameBmp;
        }, token).ConfigureAwait(false);
    }


    /// <summary>
    /// Loads an image using Magick.
    /// </summary>
    private async Task LoadWithMagickAsync(PhotoMetadata meta, CancellationToken token)
    {
        using var data = await MagickDecoder.DecodeImageAsync(meta, ReadOptions, ReadSettings, null, token);

        // multi-frame
        if (data.MultiFrameImage != null)
        {
            _width = meta.Width;
            _height = meta.Width;

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
                var bytes = data.MultiFrameImage.ToByteArray(ImageMagick.MagickFormat.Tiff);
                _bitmap = PhotoWIC.ConvertFromBytesToDecoder(bytes);
            }
        }

        // single-frame formats
        else
        {
            var wicBmp = PhotoWIC.ConvertFromMagick(data.SingleFrameImage);

            _bitmap = wicBmp;
            _width = (uint)(wicBmp?.Size.Width ?? 0);
            _height = (uint)(wicBmp?.Size.Height ?? 0);
        }
    }


    #endregion // Private Functions


}


