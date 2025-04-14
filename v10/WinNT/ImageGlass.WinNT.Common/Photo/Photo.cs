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
    protected override void OnDisposing()
    {
        base.OnDisposing();
        DisposeNativeResources();
    }


    /// <summary>
    /// Loads photo.
    /// </summary>
    public override async Task LoadAsync(PhotoReadOptions? options = null)
    {
        DisposeNativeResources();

        await LoadAsync___(options);
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


    #endregion // Override Functions



    // Private Functions
    #region Private Functions

    /// <summary>
    /// Loads photo.
    /// </summary>
    private async Task LoadAsync___(PhotoReadOptions? options = null)
    {
        CancelPhotoLoading();
        _tokenSrcPhoto = new();

        // reset dispose status
        IsDisposed = false;

        // reset done status
        IsDone = false;

        // reset error
        Error = null;

        options ??= new();
        
        await LoadMetadataAsync(options);

        try
        {
            // load image data
            options.FirstFrameOnly ??= Metadata?.FrameCount < 2;

            // cancel if requested
            _tokenSrcPhoto.Token.ThrowIfCancellationRequested();


            // load image
            using var wicFactory = new IWICImagingFactory2();
            using var decoder = wicFactory.CreateDecoderFromFileName(FilePath);
            var frameBmp = decoder.GetFrame((uint)(options.FrameIndex ?? 0));

            _bitmap = frameBmp;

            //ImgData = await PhotoCodec.LoadAsync(FilePath, options, null, _tokenSrc?.Token);

            //// update metadata for JXR format
            //if (Metadata.FileExtension == ".JXR")
            //{
            //    Metadata.RenderedWidth = Metadata.OriginalWidth = (uint)(ImgData.Image?.Width ?? 0);
            //    Metadata.RenderedHeight = Metadata.OriginalHeight = (uint)(ImgData.Image?.Height ?? 0);
            //}

            // cancel if requested
            _tokenSrcPhoto.Token.ThrowIfCancellationRequested();

            // done loading
            IsDone = true;
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Error($"Cancelled loading: {FilePath}");

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


