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
using SharpGen.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.WIC;


namespace ImageGlass.WinNT.Common;


public partial class Photo : PhotoImpl<IWICBitmapSource>
{
    private PhotoColorProfile? _colorContext;
    private IWICPixelFormatInfo2? _pixelFormatInfo;


    public override IWICBitmapSource? Bitmap => _bitmap;

    public override int Width => _bitmap?.Size.Width ?? 0;

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


    // Public Functions
    #region Public Functions

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void Load(uint frameIndex = 0)
    {
        DisposeNativeResources();

        _ = LoadAsync___(frameIndex);
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override async Task LoadAsync(uint frameIndex = 0)
    {
        DisposeNativeResources();

        await Task.Run(() => LoadAsync___(frameIndex));
    }


    /// <summary>
    /// Unload the image and reset the relevant info
    /// </summary>
    public override void Unload()
    {
        // reset info
        IsDone = false;
        Error = null;

        // dispose native resources
        DisposeNativeResources();

        // unload image
        _bitmap?.Dispose();
        _bitmap = null;
    }


    /// <summary>
    /// Converts the current bitmap to a 32bpp PBGRA format.
    /// </summary>
    public Photo? ConvertTo32bppPBGRA()
    {
        if (_bitmap == null) return null;

        try
        {
            var newBmp = WIC.WICConvertBitmapSource(
                Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA,
                _bitmap);

            return new Photo(newBmp);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }


    /// <summary>
    /// Creates a render target from a bitmap source for drawing operations.
    /// </summary>
    public ID2D1RenderTarget? CreateDirect2dRenderTarget()
    {
        ID2D1RenderTarget? target = null;

        try
        {
            using var factory = D2D1.D2D1CreateFactory<ID2D1Factory8>(FactoryType.MultiThreaded);

            target = factory.CreateWicBitmapRenderTarget(_bitmap.As<IWICBitmap>(),
                new(Vortice.DCommon.PixelFormat.Premultiplied));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return target;
    }


    /// <summary>
    /// Creates a Direct2D bitmap from an existing bitmap if available.
    /// </summary>
    public ID2D1Bitmap1? CreateDirect2dBitmap(ID2D1DeviceContext dc, BitmapProperties1? bmpProps = null)
    {
        if (_bitmap == null) return null;

        try
        {
            var newPhoto = ConvertTo32bppPBGRA();
            if (newPhoto == null) return null;

            return dc.CreateBitmapFromWicBitmap(newPhoto.Bitmap, bmpProps);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return null;
    }


    #endregion // Public Functions


    // Private Functions
    #region Private Functions

    private async Task LoadAsync___(uint frameIndex)
    {
        // reset dispose status
        IsDisposed = false;

        // reset done status
        IsDone = false;

        // reset error
        Error = null;

        //options ??= new();

        try
        {
            //// load image data
            //Metadata ??= PhotoCodec.LoadMetadata(FilePath, options);
            //FrameCount = Metadata?.FrameCount ?? 0;

            //if (options.FirstFrameOnly == null)
            //{
            //    options = options with
            //    {
            //        FirstFrameOnly = FrameCount < 2,
            //    };
            //}

            // cancel if requested
            if (_tokenSrc is not null && _tokenSrc.IsCancellationRequested)
            {
                _tokenSrc.Token.ThrowIfCancellationRequested();
            }

            // load image
            using var wicFactory = new IWICImagingFactory2();
            using var decoder = wicFactory.CreateDecoderFromFileName(FilePath);
            var frameBmp = decoder.GetFrame(frameIndex);

            _bitmap = frameBmp;

            //ImgData = await PhotoCodec.LoadAsync(FilePath, options, null, _tokenSrc?.Token);

            //// update metadata for JXR format
            //if (Metadata.FileExtension == ".JXR")
            //{
            //    Metadata.RenderedWidth = Metadata.OriginalWidth = (uint)(ImgData.Image?.Width ?? 0);
            //    Metadata.RenderedHeight = Metadata.OriginalHeight = (uint)(ImgData.Image?.Height ?? 0);
            //}

            // cancel if requested
            if (_tokenSrc is not null && _tokenSrc.IsCancellationRequested)
            {
                _tokenSrc.Token.ThrowIfCancellationRequested();
            }

            // done loading
            IsDone = true;
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Unload();
            Dispose();
        }
        catch (Exception ex)
        {
            // save the error
            Error = ex;

            // done loading
            IsDone = true;

            Log.Error(ex);
        }
    }



    protected override void OnDisposing()
    {
        base.OnDisposing();
        DisposeNativeResources();
    }


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
            var contexts = frame.TryGetColorContexts(wicFactory);
            var bestProfile = FindBestColorProfile(contexts);


            // get color profile
            using var ms = new MemoryStream();
            bestProfile?.Profile?.CopyTo(ms);
            var profileBytes = ms.ToArray();


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


