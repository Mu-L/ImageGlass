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
using Cysharp.Text;
using ImageMagick;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.WIC;
using Windows.Graphics.Imaging;

namespace ImageGlass.Common.Photoing;

public partial class Photo : DisposableImpl
{
    // private properties
    private PhotoMetadata? _metadata;
    private uint _width = 0;
    private uint _height = 0;
    private string _filePath = "";
    private bool _isCurrent = false;

    private ImageSource? _galleryThumbnail;

    private Task? _taskThumbnail;
    private Task<PhotoMetadata>? _taskMetadata;
    private CancellationTokenSource? _cancelPhotoLoading;

    // track pending tasks
    private ConcurrentDictionary<Guid, bool> _taskRefs = new();



    #region Public Propterties

    /// <summary>
    /// Gets the native bitmap.
    /// </summary>
    public IDisposable? Bitmap { get; private set; } = null;

    /// <summary>
    /// Gets the size of the photo.
    /// </summary>
    public Vector2 Size => new Vector2(_width, _height);

    /// <summary>
    /// Gets the width of the photo.
    /// </summary>
    public uint Width => (uint)Size.X;

    /// <summary>
    /// Gets the height of the photo.
    /// </summary>
    public uint Height => (uint)Size.Y;

    /// <summary>
    /// Gets the loading state of the photo.
    /// </summary>
    public PhotoLoadingState State { get; set; } = PhotoLoadingState.None;



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

                OnPropertyChanged(nameof(IsClipboard));
                OnPropertyChanged(nameof(Extension));
                OnPropertyChanged(nameof(FileTitle));
                OnPropertyChanged(nameof(GalleryFileTitle));
                OnPropertyChanged(nameof(GalleryFileExtension));
            }
        }
    }

    /// <summary>
    /// Checks if this photo is a clipboard photo.
    /// </summary>
    public bool IsClipboard => string.IsNullOrEmpty(FilePath);

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
    public string GalleryFileTitle => ZString.Concat(FileTitle, '.');

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
    /// Gets, sets the settings for reading Metadata and photo with <see cref="MagickCodec"/>.
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
    /// Gets, sets the thumbnail of photo.
    /// </summary>
    public SoftwareBitmap? ThumbnailBitmap
    {
        get; set
        {
            if (field != value)
            {
                field?.Dispose();
                field = value;
                _ = OnPropertyChanged();
            }
        }
    }


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
    public Photo(IDisposable? bmp, PhotoMetadata? meta, PhotoLoadingState state = PhotoLoadingState.Loaded)
    {
        _metadata?.Dispose();
        _metadata = meta ?? new();

        Bitmap = bmp;
        _width = (uint)_metadata.Width;
        _height = (uint)_metadata.Height;

        State = state;
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
    /// Option to dispose everything or only the Bitmap object.
    /// </param>
    private async Task OnDisposing(bool disposeEverything)
    {
        CancelLoading();
        UnloadBitmap();

        // dispose everything
        if (disposeEverything)
        {
            if (_taskMetadata is not null)
            {
                await _taskMetadata;
            }

            _metadata?.Dispose();
            _metadata = null;
            ThumbnailBitmap = null;

            _cancelPhotoLoading?.Dispose();
            _cancelPhotoLoading = null;
        }
    }


    #endregion // Override Functions



    #region Private Functions


    /// <summary>
    /// Handles the decoding of image files based on their metadata.
    /// </summary>
    private async Task OnDecodingAsync(PhotoMetadata meta, CancellationToken token)
    {
        var useWicCodec = WicCodec.CanRead(meta);

        // use WIC codec
        if (useWicCodec)
        {
            await LoadWithWICAsync(meta, token);
        }

        // use Magick codec
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
        var result = await WicCodec.LoadAsync(meta, token);

        _width = (uint)result.Size.Width;
        _height = (uint)result.Size.Height;

        if (result.Animator is not null) Bitmap = result.Animator;
        else if (!result.MultiFrames.IsDisposed()) Bitmap = result.MultiFrames;
        else if (!result.SingleFrame.IsDisposed()) Bitmap = result.SingleFrame;
        else Bitmap = null;
    }


    /// <summary>
    /// Loads an image using Magick.
    /// </summary>
    private async Task LoadWithMagickAsync(PhotoMetadata meta, CancellationToken token)
    {
        using var data = await MagickCodec.DecodeImageAsync(meta, ReadOptions, ReadSettings, null, token);

        // multi-frame
        if (data.MultiFrames != null)
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
                var bytes = data.MultiFrames.ToByteArray(MagickFormat.Tiff);
                Bitmap = PhotoWIC.CreateDecoder(bytes);
            }
        }

        // single-frame formats
        else
        {
            var wicBmp = PhotoWIC.ConvertFromMagick(data.SingleFrame);

            Bitmap = wicBmp;
            _width = (uint)(wicBmp?.Size.Width ?? 0);
            _height = (uint)(wicBmp?.Size.Height ?? 0);
        }
    }


    #endregion // Private Functions



    #region Public Functions

    /// <summary>
    /// Disposes the Bitmap and resets the relevant data.
    /// This method keeps the Metadata and neccessary resources.
    /// </summary>
    public async void Unload()
    {
        // wait for all pending tasks are done
        while (!_taskRefs.IsEmpty)
        {
            await Task.Delay(10);
        }

        // reset info
        State = PhotoLoadingState.None;
        Error = null;

        // unload image
        await OnDisposing(false);
    }


    /// <summary>
    /// Disposes the bitmap only and keeps other relevent data.
    /// </summary>
    public void UnloadBitmap()
    {
        Bitmap?.Dispose();
        Bitmap = null;
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
    /// Loads photo from file.
    /// </summary>
    public virtual async Task LoadAsync(bool useCache,
        PhotoReadOptions? newOptions = null,
        IProgress<PhotoLoadingEventArgs>? progress = null,
        bool skipLoadingEvent = false)
    {
        // use cached data
        if (useCache && State != PhotoLoadingState.None) return;

        CancelLoading();
        var token = _cancelPhotoLoading.Token;

        try
        {
            // reset dispose status
            IsDisposed = false;
            State = PhotoLoadingState.None;
            Error = null;
            ReadOptions = newOptions ?? ReadOptions;


            // 1. load metadata ===================
            // cancel if requested
            if (token.IsCancellationRequested) return;

            // load metadata off-thread
            await Task.Run(() => LoadMetadataAsync(), token);
            ReadOptions.FirstFrameOnly ??= Metadata.FrameCount < 2;

            if (!skipLoadingEvent)
            {
                progress?.Report(new PhotoLoadingEventArgs(PhotoLoadingState.Loading, this, token));
            }


            // 2. load image data ===================
            // cancel if requested
            if (token.IsCancellationRequested) return;

            // decode the photo off-thread
            Error = await Task.Run(async () =>
            {
                try
                {
                    await OnDecodingAsync(Metadata, token);
                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }, token);

            // cancel if requested
            if (token.IsCancellationRequested) return;


            // done loading
            State = PhotoLoadingState.Loaded;
            progress?.Report(new PhotoLoadingEventArgs(PhotoLoadingState.Loaded, this, token));
        }
        catch (Exception ex)
        {
            Error = ex;
            State = PhotoLoadingState.Loaded;
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
            ReadSettings ??= MagickCodec.ParseSettings(ReadOptions, false, FilePath);

            // if already started loading, wait for the task completes
            if (_taskMetadata is not null
                && _taskMetadata.Status != TaskStatus.Canceled
                && _taskMetadata.Status != TaskStatus.Faulted)
            {
                _metadata = await _taskMetadata;
                return;
            }


            // check if the current Metadata is outdated or not
            var hasOutdatedCache = _metadata?.IsOutdated() ?? true;


            // load the metadata if it's outdated
            if (hasOutdatedCache)
            {
                _taskMetadata = MagickCodec.LoadMetadataAsync(FilePath, ReadOptions, ReadSettings);
                _metadata = await _taskMetadata;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌❌❌ {nameof(LoadMetadataAsync)}: {ex.Message}");
        }
    }


    /// <summary>
    /// Starts loading thumbnail off-thread.
    /// </summary>
    public async Task StartLoadingGalleryThumbnail(double size,
        IProgress<ThumbnailLoadedEventArgs> progress)
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
            var taskId = Guid.NewGuid();
            _ = _taskRefs.TryAdd(taskId, true);

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
                _ = _taskRefs.TryRemove(taskId, out _);
            }
        });
    }


    /// <summary>
    /// Gets Direct2D bitmap.
    /// </summary>
    public async Task<ID2D1Bitmap1?> GetD2BitmapAsync(
        ID3D11Device d3Device, ID2D1DeviceContext d2Context, uint frameIndex = 0)
    {
        var taskId = Guid.NewGuid();
        _ = _taskRefs.TryAdd(taskId, true);

        try
        {
            // native bitmap is a single-frame bitmap
            if (Bitmap is IWICBitmapSource srcBmp)
            {
                if (srcBmp.IsDisposed()) return null;

                var d2Bmp = await srcBmp.ToD2BitmapAsync(d2Context, d3Device);
                return d2Bmp;
            }

            // native bitmap is a multi-frame bitmap
            if (Bitmap is IWICBitmapDecoder decoder)
            {
                if (decoder.IsDisposed()) return null;

                using var frameBmp = decoder.GetFrame(frameIndex);
                var d2Bmp = await frameBmp.ToD2BitmapAsync(d2Context, d3Device);

                return d2Bmp;
            }

            return null;
        }
        finally
        {
            _ = _taskRefs.TryRemove(taskId, out _);
        }
    }


    /// <summary>
    /// Saves the photo to file.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public async Task SaveAsAsync(string destFilePath, ImgTransform transforms, int quality, CancellationToken token = default)
    {
        var taskId = Guid.NewGuid();
        _ = _taskRefs.TryAdd(taskId, true);

        try
        {
            // 1. save clipboard photo to file
            if (IsClipboard && Bitmap is IWICBitmapSource wicBmp)
            {
                await WicCodec.SaveAsync(wicBmp, destFilePath, transforms, (uint)quality, token);
            }

            // 2. save photo file to file
            else
            {
                await MagickCodec.SaveAsync(Metadata, destFilePath, ReadOptions, transforms, (uint)quality, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _ = _taskRefs.TryRemove(taskId, out _);
        }
    }


    #endregion // Public Functions


}


