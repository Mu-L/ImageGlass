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
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortice.WIC;


namespace ImageGlass.WinNT.Common;


public partial class Photo : PhotoImpl<IDisposable>
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
        _width = wicSrc.Size.Width;
        _height = wicSrc.Size.Height;

        _metadata?.Dispose();
        _metadata = new IgMetadata();
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
        var extWIC = new string[] { ".GIF", ".WEBP", ".HEIC" };


        // use WIC decoders
        if (meta.ColorSpace != ImageMagick.ColorSpace.CMYK && extWIC.Contains(meta.FileExtension))
        {
            await LoadWithWicAsync(meta, token);
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
    private async Task LoadWithWicAsync(IgMetadata meta, CancellationToken token)
    {
        _bitmap = await Task.Run(() =>
        {
            using var wicFactory = new IWICImagingFactory2();
            using var decoder = wicFactory.CreateDecoderFromFileName(meta.FilePath);
            var frameBmp = decoder.GetFrame((uint)meta.FrameIndex + 1);

            _width = frameBmp.Size.Width;
            _height = frameBmp.Size.Height;

            return frameBmp;
        }, token).ConfigureAwait(false);


    }


    /// <summary>
    /// Loads an image using Magick.
    /// </summary>
    private async Task LoadWithMagickAsync(IgMetadata meta, CancellationToken token)
    {
        var data = await MagickDecoder.DecodeImageAsync(meta, ReadOptions, ReadSettings, null, token);

        // multi-frame
        if (data.MultiFrameImage != null)
        {
            _width = (int)meta.OriginalWidth;
            _height = (int)meta.OriginalHeight;

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

            _width = (int)(bmpSrc?.PixelWidth ?? 0);
            _height = (int)(bmpSrc?.PixelHeight ?? 0);
        }
    }


    #endregion // Private Functions


}


