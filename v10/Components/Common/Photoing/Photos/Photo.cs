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
using System.Linq;
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
    private uint _width = 0;
    private uint _height = 0;

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
    /// Gets the codec used to decode the photo.
    /// </summary>
    public PhotoCodec ReadCodec
    {
        get; private set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = PhotoCodec.None;


    /// <summary>
    /// Gets, sets value indicating if the photo is current index.
    /// </summary>
    public bool IsCurrent
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets file path of the photo. E.g. <c>"C:\Album\My photo.png"</c>.
    /// </summary>
    public string FilePath
    {
        get => Metadata.FilePath; set
        {
            if (!Metadata.FilePath.Equals(value, StringComparison.Ordinal))
            {
                Metadata.FilePath = value;

                OnPropertyChanged(nameof(FilePath));
                OnPropertyChanged(nameof(Metadata));

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
    /// Gets image metadata. <c>MUST</c> assign on UI thread due to reactivity.
    /// </summary>
    public PhotoMetadata Metadata
    {
        get; private set
        {
            if (field == value) return;

            DisposeThumbnail();
            field.Dispose();
            field = value;
            _ = OnPropertyChanged();
        }
    } = new();

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
        get; set
        {
            if (field != value)
            {
                field = value;
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
    /// Initializes a new single-frame photo using a bitmap source for rendering.
    /// </summary>
    public Photo(IDisposable? bmp, int width, int height, PhotoLoadingState state = PhotoLoadingState.Loaded)
    {
        Metadata.Dispose();
        Metadata = new()
        {
            Width = (uint)width,
            Height = (uint)height,
            FrameCount = 1,
        };

        Bitmap = bmp;
        _width = (uint)Metadata.Width;
        _height = (uint)Metadata.Height;

        State = state;
    }


    /// <summary>
    /// Initializes a new single-frame using a bitmap source for rendering.
    /// </summary>
    public Photo(IDisposable? bmp, PhotoMetadata? meta, PhotoLoadingState state = PhotoLoadingState.Loaded)
    {
        Metadata.Dispose();
        Metadata = meta ?? new();

        Bitmap = bmp;
        _width = (uint)Metadata.Width;
        _height = (uint)Metadata.Height;

        State = state;
    }

    #endregion // Instance Initilization



    #region Override Functions

    /// <summary>
    /// <inheritdoc/>
    /// Calling this function also disposes Metadata object.
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

            Metadata.Dispose();
            Metadata = new();

            DisposeThumbnail();

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
        var useWicCodec = meta.IsOneOfExtensions(AP.Config.WICReadFormats.ToArray())
            && WicCodec.CanRead(meta);

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
        ReadCodec = PhotoCodec.WIC;
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
        ReadCodec = PhotoCodec.Magick;
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

            // load metadata
            await LoadMetadataAsync(useCache);
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
    public async Task LoadMetadataAsync(bool useCache, PhotoReadOptions? newOptions = null)
    {
        var meta = await Task.Run(() => LoadMetadataAsync__(useCache, newOptions));

        if (meta is not null)
        {
            Metadata = meta;
        }
    }
    private async Task<PhotoMetadata?> LoadMetadataAsync__(bool useCache, PhotoReadOptions? newOptions = null)
    {
        try
        {
            ReadOptions = newOptions ?? ReadOptions;
            ReadSettings ??= MagickCodec.ParseSettings(ReadOptions, false, FilePath);

            // if already started loading, wait for the task completes
            if (useCache)
            {
                if (_taskMetadata is not null
                    && _taskMetadata.Status != TaskStatus.Canceled
                    && _taskMetadata.Status != TaskStatus.Faulted)
                {
                    return await _taskMetadata;
                }
            }
            else
            {
                _taskMetadata = null;
            }


            // check if the current Metadata is outdated or not
            var hasOutdatedCache = !useCache || Metadata.IsOutdated();

            // load the metadata if it's outdated
            if (hasOutdatedCache)
            {
                // load metadata off-thread
                if (MagickCodec.CanRead(FilePath))
                {
                    _taskMetadata = Task.Run(() => MagickCodec.LoadMetadataAsync(FilePath, ReadOptions, ReadSettings));
                }
                else if (WicCodec.CanPing(FilePath))
                {
                    _taskMetadata = Task.Run(() => WicCodec.LoadMetadataAsync(FilePath, ReadOptions));
                }
                else
                {
                    _taskMetadata = Task.Run(() => new PhotoMetadata(FilePath));
                }

                // must assign on UI thread
                return await _taskMetadata;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌❌❌ {nameof(LoadMetadataAsync)}: {ex.Message}");
        }

        return null;
    }


    /// <summary>
    /// Dispose the thumbnail of photo.
    /// </summary>
    public void DisposeThumbnail()
    {
        ThumbnailBitmap = null;
        GalleryThumbnail = null;

        _taskThumbnail = null;
    }


    /// <summary>
    /// Starts loading thumbnail off-thread.
    /// </summary>
    public async Task StartLoadingGalleryThumbnail(double size, bool useCache,
        IProgress<ThumbnailLoadedEventArgs> progress)
    {
        if (useCache)
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
        }
        else
        {
            DisposeThumbnail();
        }

        // ensure metadata is loaded
        var taskMetadata = LoadMetadataAsync(true);


        // load thumbnail off-thread
        var taskThumb = Task.Run(async () =>
        {
            var taskId = Guid.NewGuid();
            _ = _taskRefs.TryAdd(taskId, true);

            SoftwareBitmap? softwareBmp = null;

            try
            {
                // load thumbnail
                using var wicBmp = await Metadata.GetThumbnailAsync(size);

                // convert to software bitmap
                if (wicBmp is not null)
                {
                    softwareBmp = await wicBmp.ToSoftwareBitmapAsync();
                }
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


        _taskThumbnail = await taskMetadata.ContinueWith(_ => taskThumb);
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
    public async Task SaveAsAsync(string destFilePath, ImgTransform transforms, uint quality, CancellationToken token = default)
    {
        var taskId = Guid.NewGuid();
        _ = _taskRefs.TryAdd(taskId, true);

        try
        {
            var destExt = Path.GetExtension(destFilePath).ToLowerInvariant();
            var mustUseWic = IsClipboard || WicCodec.TopWriteExts.Contains(destExt);

            // 1. save clipboard photo to file
            if (mustUseWic && Bitmap is IWICBitmapSource wicBmp)
            {
                await WicCodec.SaveAsync(wicBmp, destFilePath, transforms, quality, token);
            }

            // 2. save photo file to file
            else
            {
                await MagickCodec.SaveAsync(Metadata, destFilePath, ReadOptions, transforms, quality, token);
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


