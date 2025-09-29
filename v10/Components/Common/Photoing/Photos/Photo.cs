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
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Vortice.WIC;
using Windows.Graphics.Imaging;

namespace ImageGlass.Common.Photoing;

public partial class Photo : DisposableImpl
{
    // private properties

    private IDisposable? _bitmap;
    private PhotoMetadata? _metadata;
    private uint _width = 0;
    private uint _height = 0;
    private string _filePath = "";
    private bool _isCurrent = false;

    private PhotoColorProfile? _colorContext;
    private IWICPixelFormatInfo2? _pixelFormatInfo;
    private ImageSource? _galleryThumbnail;

    private Task? _taskThumbnail;
    private Task<PhotoMetadata>? _taskMetadata;
    private CancellationTokenSource? _cancelPhotoLoading;



    #region Public Propterties

    /// <summary>
    /// Gets the native bitmap.
    /// </summary>
    public IDisposable? Bitmap => _bitmap;

    /// <summary>
    /// Gets the size of the <c><see cref="Bitmap"/></c>.
    /// </summary>
    public Vector2 Size => new Vector2(_width, _height);

    /// <summary>
    /// Gets the width of the <c><see cref="Bitmap"/></c>.
    /// </summary>
    public uint Width => (uint)Size.X;

    /// <summary>
    /// Gets the height of the <c><see cref="Bitmap"/></c>.
    /// </summary>
    public uint Height => (uint)Size.Y;

    /// <summary>
    /// Indicates whether the <see cref="Bitmap"/> is currently loaded.
    /// </summary>
    public bool IsDone { get; set; } = false;



    /// <summary>
    /// Gets, sets value indicating if the photo is current index.
    /// </summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent != value)
            {
                _isCurrent = value;
                OnPropertyChanged(nameof(IsCurrent));
            }
        }
    }

    /// <summary>
    /// Gets file path of the photo. E.g. <c>"C:\Album\My photo.png"</c>.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (!_filePath.Equals(value, StringComparison.Ordinal))
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));

                OnPropertyChanged(nameof(Extension));
                OnPropertyChanged(nameof(FileTitle));
                OnPropertyChanged(nameof(GalleryFileTitle));
                OnPropertyChanged(nameof(GalleryFileExtension));
            }
        }
    }

    /// <summary>
    /// Gets original file extension. E.g: <c>".png"</c>.
    /// </summary>
    public string Extension => Path.GetExtension(FilePath);

    /// <summary>
    /// Gets original file name without extension. E.g. <c>"My photo"</c>.
    /// </summary>
    public string FileTitle => Path.GetFileNameWithoutExtension(FilePath);

    /// <summary>
    /// Gets the file name without extension and including a trailing dot. E.g. <c>"My photo."</c>.
    /// </summary>
    public string GalleryFileTitle => FileTitle + ".";

    /// <summary>
    /// Gets file extension without dot. E.g. <c>"png"</c>.
    /// </summary>
    public string GalleryFileExtension => Extension.Length > 1 ? Extension.Substring(1) : string.Empty;



    /// <summary>
    /// Gets the error details.
    /// </summary>
    public Exception? Error { get; set; } = null;

    /// <summary>
    /// Gets, sets options for reading photo.
    /// </summary>
    public PhotoReadOptions ReadOptions { get; set; } = new();

    /// <summary>
    /// Gets, sets the settings for reading Metadata and photo with <see cref="MagickDecoder"/>.
    /// </summary>
    public MagickReadSettings? ReadSettings { get; set; } = null;

    /// <summary>
    /// Gets image metadata.
    /// </summary>
    public PhotoMetadata Metadata => _metadata!;

    /// <summary>
    /// Gets photo loading cancellation token source.
    /// </summary>
    public CancellationToken? CancelToken => _cancelPhotoLoading?.Token;

    /// <summary>
    /// Gets the hash key of the image.
    /// </summary>
    public string HashKey => BHelper.CreateUniqueFileKey(FilePath, new Vector2(Width, Height));



    /// <summary>
    /// Gets, sets the image source for gallery thumbnail.
    /// </summary>
    public ImageSource? GalleryThumbnail
    {
        get => _galleryThumbnail;
        set
        {
            if (_galleryThumbnail != value)
            {
                _galleryThumbnail = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsGalleryThumbnailLoaded));
                _ = OnPropertyChanged(nameof(IsGalleryThumbnailLoading));
            }
        }
    }

    public bool IsGalleryThumbnailLoaded => GalleryThumbnail != null;

    public bool IsGalleryThumbnailLoading => !IsGalleryThumbnailLoaded;


    #endregion // Public Propterties



    #region Instance Initilization

    /// <summary>
    /// Initializes new instance of <see cref="Photo"/>
    /// </summary>
    public Photo() { }


    /// <summary>
    /// Initializes new instance of <see cref="Photo"/>
    /// </summary>
    public Photo(string filePath = "", PhotoReadOptions? options = null)
    {
        FilePath = filePath;
        ReadOptions = options ?? new();
    }


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

    #endregion // Instance Initilization



    #region Override Functions

    /// <summary>
    /// <inheritdoc/>
    /// Calling this function also disposes <see cref="Metadata"/> object.
    /// </summary>
    protected override async void OnDisposing()
    {
        base.OnDisposing();

        await OnDisposing(true);
    }


    /// <summary>
    /// Handles the disposal of resources when an object is being disposed.
    /// </summary>
    /// <param name="disposeEverything">
    /// Option to dispose everything or only the <see cref="Bitmap"/> object.
    /// </param>
    private async Task OnDisposing(bool disposeEverything)
    {
        CancelLoading();
        DisposeNativeResources();

        _bitmap?.Dispose();
        _bitmap = null;

        // dispose everything
        if (disposeEverything)
        {
            if (_taskMetadata is not null)
            {
                await _taskMetadata;
            }

            _metadata?.Dispose();
            _metadata = null;

            _cancelPhotoLoading?.Dispose();
            _cancelPhotoLoading = null;
        }
    }


    #endregion // Override Functions



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
    /// Handles the decoding of image files based on their metadata.
    /// </summary>
    private async Task OnDecodingAsync(PhotoMetadata meta, CancellationToken token)
    {
        var wicExts = new string[] { ".GIF", ".GIFV", ".WEBP", ".FAX", ".JXR", ".APNG" };

        // use WIC decoders
        if (meta.ColorSpace != ImageMagick.ColorSpace.CMYK && meta.IsOneOfExtensions(wicExts))
        {
            await LoadWithWICAsync(meta, token);
        }

        // use default decoders
        else
        {
            await LoadWithMagickAsync(meta, token);
        }
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
                // .WEBP
                else if (meta.IsOneOfExtensions(".WEBP"))
                {
                    return new WebpAnimator(decoder, meta);
                }
                // use default WIC animator
                else
                {
                    return new WicAnimator(decoder, meta);
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



    #region Public Functions

    /// <summary>
    /// Disposes the <c><see cref="Bitmap"/></c> and resets the relevant info.
    /// This method keeps the <c><see cref="Metadata"/></c> and neccessary resources.
    /// </summary>
    public void Unload()
    {
        // reset info
        IsDone = false;
        Error = null;

        // unload image
        _ = OnDisposing(false);
    }


    /// <summary>
    /// Stops any ongoing photo loading process.
    /// </summary>
    [MemberNotNull(nameof(_cancelPhotoLoading))]
    public virtual void CancelLoading()
    {
        _cancelPhotoLoading?.Cancel();
        _cancelPhotoLoading = new();
    }


    /// <summary>
    /// Loads <c><see cref="Bitmap"/></c> from file.
    /// </summary>
    public virtual async Task LoadAsync(bool useCache,
        PhotoReadOptions? newOptions = null,
        IProgress<PhotoLoadingEventArgs>? progress = null)
    {
        // use cached data
        if (useCache && IsDone) return;

        CancelLoading();
        DisposeNativeResources();
        var token = _cancelPhotoLoading.Token;

        try
        {
            // reset dispose status
            IsDisposed = false;
            IsDone = false;
            Error = null;
            ReadOptions = newOptions ?? ReadOptions;


            // 1. load metadata ===================
            // cancel if requested
            if (token.IsCancellationRequested) return;

            // load metadata off-thread
            await Task.Run(() => LoadMetadataAsync(), token);
            ReadOptions.FirstFrameOnly ??= Metadata.FrameCount < 2;
            progress?.Report(new PhotoLoadingEventArgs(false, this, token));


            // 2. load image data ===================
            // cancel if requested
            if (token.IsCancellationRequested) return;

            // decode the photo off-thread
            await Task.Run(() => OnDecodingAsync(Metadata, token), token);

            // cancel if requested
            if (token.IsCancellationRequested) return;

            // done loading
            IsDone = true;

            progress?.Report(new PhotoLoadingEventArgs(true, this, token));
        }
        catch (Exception ex)
        {
            Error = ex;
            IsDone = true;
        }
        finally
        {
            if (token.IsCancellationRequested) Unload();
        }
    }


    /// <summary>
    /// Loads <c><see cref="Metadata"/></c> for the photo.
    /// Returns the cached metadata if it's not null and up-to-date.
    /// </summary>
    public async Task LoadMetadataAsync(PhotoReadOptions? newOptions = null)
    {
        try
        {
            ReadOptions = newOptions ?? ReadOptions;
            ReadSettings ??= MagickDecoder.ParseSettings(ReadOptions, false, FilePath);

            // if already started loading, wait for the task completes
            if (_taskMetadata is not null
                && _taskMetadata.Status != TaskStatus.Canceled
                && _taskMetadata.Status != TaskStatus.Faulted)
            {
                _metadata = await _taskMetadata;
                return;
            }


            // check if the current Metadata is outdated or not
            var hasOutdatedCache = _metadata is null;

            if (_metadata is not null)
            {
                try
                {
                    var fi = new FileInfo(FilePath);
                    hasOutdatedCache = _metadata.FileLastWriteTimeUtc < fi.LastWriteTimeUtc;
                }
                catch { }
            }


            // load the metadata if it's outdated
            if (hasOutdatedCache)
            {
                _taskMetadata = MagickDecoder.LoadMetadataAsync(FilePath, ReadOptions, ReadSettings);
                _metadata = await _taskMetadata;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }


    /// <summary>
    /// Starts loading thumbnail off-thread.
    /// </summary>
    public async Task StartLoadingGalleryThumbnail(double size, IProgress<ThumbnailLoadedEventArgs> progress)
    {
        if (GalleryThumbnail is not null) return;


        // if already started loading, wait for the task completes
        if (_taskThumbnail is not null
            && _taskThumbnail.Status != TaskStatus.Canceled
            && _taskThumbnail.Status != TaskStatus.Faulted)
        {
            await _taskThumbnail;
            return;
        }


        _taskThumbnail = Task.Run(async () =>
        {
            SoftwareBitmap? softwareBmp = null;

            try
            {
                // ensure metadata is loaded
                await LoadMetadataAsync();

                // load thumbnail
                using var wicBmp = await Metadata.GetThumbnailAsync(size);

                // convert to software bitmap
                softwareBmp = await wicBmp.ToSoftwareBitmapAsync();
            }
            catch
            {
                softwareBmp?.Dispose();
                softwareBmp = null;
            }
            finally
            {
                progress.Report(new ThumbnailLoadedEventArgs(this, softwareBmp));
            }
        });
    }


    #endregion // Public Functions


}


