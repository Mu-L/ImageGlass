/*
ImageGlass - A lightweight, versatile image viewer
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
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
using Avalonia;
using Avalonia.Media.Imaging;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Types;
using ImageMagick;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.Photoing;

public partial class Photo : PhDisposable
{
    // private properties
    private uint _width = 0;
    private uint _height = 0;
    private int _frameIndex = -1;

    private Task<PhotoMetadata>? _taskMetadata;
    private CancellationTokenSource? _cancelPhotoLoading;
    private readonly Lock _lock = new();
    private int _loadGeneration;

    // track pending tasks
    private ConcurrentDictionary<Guid, bool> _taskRefs = new();
    private CancellationTokenSource? _cancelThumbnailLoading;

    /// <summary>
    /// Prevents duplicate concurrent loads of the same photo's thumbnail.
    /// </summary>
    private readonly SemaphoreSlim _thumbnailLock = new(1, 1);

    /// <summary>
    /// Limits how many different Photo instances
    /// load thumbnails concurrently across the entire app.
    /// </summary>
    private static readonly SemaphoreSlim _thumbnailThrottleLock = new(4, 4);



    #region Public Propterties

    /// <summary>
    /// Gets the native bitmap,
    /// either <see cref="SKImage"/>,
    /// or <see cref="SkiaAnimator"/>.
    /// </summary>
    public IDisposable? Bitmap { get; private set; } = null;

    /// <summary>
    /// Gets the size of the photo.
    /// </summary>
    public Size Size => new Size(_width, _height);

    /// <summary>
    /// Gets the width of the photo.
    /// </summary>
    public uint Width => (uint)Size.Width;

    /// <summary>
    /// Gets the height of the photo.
    /// </summary>
    public uint Height => (uint)Size.Height;

    /// <summary>
    /// Gets the current frame index of this photo.
    /// </summary>
    public int FrameIndex => _frameIndex;

    /// <summary>
    /// Gets the loading state of the photo.
    /// </summary>
    public PhotoState State { get; set; } = PhotoState.None;

    /// <summary>
    /// Gets the codec that was used to decode the photo.
    /// </summary>
    public DecodedCodec DecodedBy { get; private set; } = DecodedCodec.Unknown;

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
    /// Checks if this photo is a clipboard photo.
    /// </summary>
    public bool IsClipboard => string.IsNullOrEmpty(FilePath);


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
                OnPropertyChanged(nameof(DirPath));
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
    /// Gets original dir path. E.g: <c>"C:\Album"</c>.
    /// </summary>
    public string DirPath => Path.GetDirectoryName(FilePath) ?? string.Empty;

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
    public string GalleryFileTitle => $"{FileTitle}.";

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
    public PhotoMetadata Metadata
    {
        get; private set
        {
            if (field == value) return;
            try
            {
                field.Dispose();
                field = value;
                _ = OnPropertyChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌❌❌: Metadata setter: {ex.Message}");
            }
        }
    } = new();

    /// <summary>
    /// Gets photo loading cancellation token source.
    /// </summary>
    public CancellationToken? CancelToken => _cancelPhotoLoading?.Token;

    /// <summary>
    /// Gets, sets the image source for gallery thumbnail.
    /// </summary>
    public Bitmap? GalleryThumbnail
    {
        get; set
        {
            if (field == value) return;

            try
            {
                var old = field;
                field = value;
                _ = OnPropertyChanged();
                old?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌❌❌: GalleryThumbnail setter: {ex.Message}");
            }
        }
    }

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
    public Photo(SKBitmap? bmp, PhotoState state = PhotoState.Loaded)
    {
        InitializePhoto(bmp, bmp?.Width ?? 0, bmp?.Height ?? 0, null, state);
    }


    /// <summary>
    /// Initializes a new single-frame photo using a image source for rendering.
    /// </summary>
    public Photo(SKImage? img, PhotoState state = PhotoState.Loaded)
    {
        InitializePhoto(img, img?.Width ?? 0, img?.Height ?? 0, null, state);
    }


    /// <summary>
    /// Initializes a new single-frame using a image source for rendering.
    /// </summary>
    public Photo(SKImage? img, PhotoMetadata? meta, PhotoState state = PhotoState.Loaded)
    {
        InitializePhoto(img, 0, 0, meta, state);
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
            await UnloadThumbnailAsync().ConfigureAwait(false);
            _thumbnailLock.Dispose();

            if (_taskMetadata is not null)
            {
                await _taskMetadata;
            }

            Metadata.Dispose();
            Metadata = new();

            _cancelPhotoLoading?.Dispose();
            _cancelPhotoLoading = null;
        }
    }


    #endregion // Override Functions



    #region Private Functions

    private void InitializePhoto(IDisposable? src, int width, int height, PhotoMetadata? meta, PhotoState state = PhotoState.Loaded)
    {
        // set Bitmap
        if (src is null) Bitmap = null;
        else if (src is SKImage img) Bitmap = img;
        else if (src is SKBitmap bmp) Bitmap = SkiaCodec.ToSKImage(bmp);
        else if (src is AnimatorImpl animator) Bitmap = animator;
        else throw new ArgumentException("IGE: Unsupported bitmap source", nameof(src));


        Metadata.Dispose();
        Metadata = meta ?? new()
        {
            Width = (uint)width,
            Height = (uint)height,
            FrameCount = 1,
        };

        _width = (uint)Metadata.Width;
        _height = (uint)Metadata.Height;

        State = state;
    }


    /// <summary>
    /// Handles the decoding of image files based on their metadata.
    /// </summary>
    private async Task OnDecodingAsync(PhotoMetadata meta, CancellationToken token)
    {
        // get embedded thumbnail requirements
        var loadRawThumbnailOnly = ReadOptions.OnlyLoadRawPreview
            && meta.RawThumbnail is not null;
        var loadOtherThumbnailOnly = ReadOptions.OnlyLoadNonRawPreview
            && (meta.ExifProfile?.ThumbnailLength ?? 0) > 0;

        // check if we can use Skia to decode
        var useSkiaCodec = Core.Config.NativeCodecReadFormats.Contains(meta.FileExtension)
            && SkiaCodec.CanRead(meta)
            && Core.IsDestColorProfileSupported
            && !loadRawThumbnailOnly
            && !loadOtherThumbnailOnly;


        // use native codec
        if (useSkiaCodec)
        {
            DecodedBy = DecodedCodec.Skia;
            await LoadWithSkiaCodecAsync(meta, token);
        }

        // use Magick codec
        else
        {
            DecodedBy = DecodedCodec.Magick;
            await LoadWithMagickAsync(meta, token);
        }
    }


    /// <summary>
    /// Loads an image using native codec.
    /// </summary>
    private async Task LoadWithSkiaCodecAsync(PhotoMetadata meta, CancellationToken token)
    {
        ReadCodec = PhotoCodec.Native;
        var result = await SkiaCodec.LoadAsync(meta, ReadOptions, token);

        _width = (uint)result.Size.Width;
        _height = (uint)result.Size.Height;

        if (result.Animator is not null)
        {
            result.Animator.FrameChanged -= OnAnimatorFrameChanged;
            result.Animator.FrameChanged += OnAnimatorFrameChanged;
            Bitmap = result.Animator;
        }
        else if (result.SingleFrame is not null) Bitmap = result.SingleFrame;
        else Bitmap = null;
    }


    /// <summary>
    /// Loads an image using Magick.
    /// </summary>
    private async Task LoadWithMagickAsync(PhotoMetadata meta, CancellationToken token)
    {
        ReadCodec = PhotoCodec.Magick;
        using var data = await MagickCodec.DecodeImageAsync(meta, ReadOptions, ReadSettings, null, token);


        // always load single frame
        var img = SkiaCodec.FromMagick(data.SingleFrame, meta.SkiaColorSpace, meta.IsHdr);
        _width = (uint)(img?.Width ?? 0);
        _height = (uint)(img?.Height ?? 0);

        Bitmap = img;
    }


    /// <summary>
    /// Keeps <see cref="_frameIndex"/> in sync during animation playback.
    /// </summary>
    private void OnAnimatorFrameChanged(AnimatorImpl sender, AnimatorFrameChangedEventArgs e)
    {
        _frameIndex = (int)e.CurrentFrame;
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
        State = PhotoState.None;
        Error = null;

        // unload image
        await OnDisposing(false);
    }


    /// <summary>
    /// Disposes the bitmap only and keeps other relevent data.
    /// </summary>
    public void UnloadBitmap()
    {
        if (Bitmap is SkiaAnimator animator)
        {
            animator.FrameChanged -= OnAnimatorFrameChanged;
        }

        Bitmap?.Dispose();
        Bitmap = null;
        _frameIndex = -1;
    }


    /// <summary>
    /// Stops any ongoing photo loading process.
    /// </summary>
    [MemberNotNull(nameof(_cancelPhotoLoading))]
    public virtual CancellationToken CancelLoading()
    {
        lock (_lock)
        {
            _cancelPhotoLoading?.Cancel();
            _cancelPhotoLoading?.Dispose();
            _cancelPhotoLoading = new();

            return _cancelPhotoLoading.Token;
        }
    }


    /// <summary>
    /// Loads photo from file.
    /// </summary>
    public virtual async Task LoadAsync(bool useCache,
        Func<PhotoLoadingEventArgs, Task>? handleProgressFn = null,
        bool skipLoadingEvent = false)
    {
        // use cached data
        if (useCache && State != PhotoState.None) return;
        var token = CancelLoading();
        var myGeneration = Interlocked.Increment(ref _loadGeneration);

        try
        {
            // reset dispose status
            _isDisposed.SetFalse();
            State = PhotoState.None;
            Error = null;


            // 1. load metadata ===================
            // cancel if requested
            if (token.IsCancellationRequested) return;

            // load metadata
            await LoadMetadataAsync(useCache);

            if (!skipLoadingEvent)
            {
                if (handleProgressFn is not null)
                {
                    await handleProgressFn(new(PhotoState.Preview, this, token));
                }
            }


            // 2. load image data ===================
            // cancel if requested
            if (token.IsCancellationRequested) return;

            // decode the photo on a dedicated thread to avoid thread pool starvation
            // (thumbnail loading can saturate the thread pool when OS thumbnail cache is disabled)
            Error = await Task.Factory.StartNew(async () =>
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
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

            // cancel if requested
            if (token.IsCancellationRequested) return;


            // done loading
            State = PhotoState.Loaded;
            if (handleProgressFn is not null)
            {
                await handleProgressFn(new(PhotoState.Loaded, this, token));
            }
        }
        catch (Exception ex)
        {
            Error = ex;
            State = PhotoState.Loaded;
            if (handleProgressFn is not null)
            {
                await handleProgressFn(new(PhotoState.Loaded, this, token));
            }
        }
        finally
        {
            // only unload if no newer load has started on this Photo;
            // a newer LoadAsync increments _loadGeneration, so if ours
            // is stale, calling Unload would cancel the newer load
            if (token.IsCancellationRequested
                && Volatile.Read(ref _loadGeneration) == myGeneration)
            {
                Unload();
            }
        }
    }


    /// <summary>
    /// Loads <c><see cref="Metadata"/></c> for the photo.
    /// Returns the cached metadata if it's not null and up-to-date.
    /// </summary>
    public async Task LoadMetadataAsync(bool useCache, PhotoReadOptions? newOptions = null)
    {
        var meta = await Task.Run(() => LoadMetadataAsync__(useCache, newOptions)).ConfigureAwait(false);

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
                else if (SkiaCodec.CanPing(FilePath))
                {
                    _taskMetadata = Task.Run(() => SkiaCodec.LoadMetadataAsync(FilePath, ReadOptions));
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
            Debug.WriteLine($"❌❌❌ {nameof(LoadMetadataAsync__)}: {ex.Message}");
        }

        return null;
    }


    /// <summary>
    /// Gets an image frame from the photo.
    /// </summary>
    public async Task<SKImage?> GetFrameAsync(uint frameIndex)
    {
        var newFrameIndex = (int)frameIndex;

        // 1. animated formats: delegate to animator's own frame cache
        if (Bitmap is SkiaAnimator animator)
        {
            _frameIndex = newFrameIndex;
            return animator.GetRenderedFrameBitmap(frameIndex);
        }

        // 2. cache hit: requested frame is already loaded in Bitmap
        if (frameIndex == _frameIndex && Bitmap is SKImage cachedImg)
        {
            return cachedImg;
        }


        // 3. single-frame image: nothing else to decode
        if (Metadata.FrameCount <= 1)
        {
            _frameIndex = newFrameIndex;
            return Bitmap as SKImage;
        }


        // 4. multi-frame: decode the requested frame via Magick
        var newFrame = await Task.Factory.StartNew(async () =>
        {
            var options = ReadOptions with { FrameIndex = newFrameIndex };
            using var data = await MagickCodec.DecodeImageAsync(Metadata, options, ReadSettings,
                null, CancellationToken.None);
            var frameImg = SkiaCodec.FromMagick(data.SingleFrame, Metadata.SkiaColorSpace, Metadata.IsHdr);

            return frameImg;
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();


        lock (_lock)
        {
            if (IsDisposed)
            {
                newFrame?.Dispose();
                return null;
            }

            Bitmap?.Dispose();
            Bitmap = newFrame;
            _frameIndex = newFrameIndex;
            _width = (uint)(newFrame?.Width ?? 0);
            _height = (uint)(newFrame?.Height ?? 0);
        }

        return newFrame;
    }


    /// <summary>
    /// Saves the photo to file.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public async Task SaveAsAsync(string destFilePath,
        ImgTransform transforms, uint quality,
        bool preserveModifiedDate = false, CancellationToken token = default)
    {
        var taskId = Guid.NewGuid();
        _ = _taskRefs.TryAdd(taskId, true);

        try
        {
            var lastWriteTime = File.GetLastWriteTime(destFilePath);

            // 1. save clipboard photo to file
            if (IsClipboard && Bitmap is SKImage img)
            {
                await Task.Factory.StartNew(async () =>
                {
                    await SkiaCodec.SaveAsync(img, destFilePath, transforms, quality, token);
                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            }

            // 2. save photo to file
            else
            {
                await Task.Factory.StartNew(async () =>
                {
                    // update read options
                    var readOptions = ReadOptions with
                    {
                        FrameIndex = Metadata.FrameCount > 1
                            ? -1 // save all frame
                            : ReadOptions.FrameIndex, // save only current frame
                    };

                    await MagickCodec.SaveAsync(Metadata, destFilePath, readOptions, transforms, quality, token);
                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            }


            // Issue #307: option to preserve the modified date/time
            if (preserveModifiedDate)
            {
                File.SetLastWriteTime(destFilePath, lastWriteTime);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _ = _taskRefs.TryRemove(taskId, out _);
        }
    }


    /// <summary>
    /// Signals cancellation to any in-progress thumbnail loading so it exits early.
    /// </summary>
    public void CancelThumbnailLoading()
    {
        _cancelThumbnailLoading?.Cancel();
    }


    /// <summary>
    /// Loads a thumbnail image at the specified DPI scaling factor.
    /// </summary>
    public async Task LoadThumbnailAsync(double dpi)
    {
        var thumbSize = Core.Config.ThumbnailSize * dpi * 2; // 2x bigger
        await LoadThumbnailAsync(thumbSize, useCache: true);
    }


    /// <summary>
    /// Loads the gallery thumbnail asynchronously.
    /// Uses a semaphore to ensure only one load per photo at a time,
    /// and caches the result on <see cref="GalleryThumbnail"/>.
    /// </summary>
    public async Task LoadThumbnailAsync(double thumbSize, bool useCache)
    {
        // 1. fast path: use cached thumbnail
        if (useCache && GalleryThumbnail is not null) return;
        if (IsDisposed || string.IsNullOrEmpty(FilePath)) return;

        // 2. acquire global throttle to avoid saturating the thread pool
        //    (prevents blocking the main image loading when many thumbnails load at once)
        await _thumbnailThrottleLock.WaitAsync().ConfigureAwait(false);

        try
        {
            await _thumbnailLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // 3. double-check after acquiring the lock
                if (useCache && GalleryThumbnail is not null) return;
                if (IsDisposed) return;

                // reset cancellation for this load
                _cancelThumbnailLoading?.Dispose();
                _cancelThumbnailLoading = new CancellationTokenSource();
                var token = _cancelThumbnailLoading.Token;


                // 4. load metadata if needed
                await LoadMetadataAsync(true).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;


                // 4b. try disk cache
                var diskThumb = await ThumbnailDiskCache.TryGetAsync(FilePath, (int)thumbSize, token)
                    .ConfigureAwait(false);
                if (token.IsCancellationRequested) return;

                if (diskThumb is not null)
                {
                    var avBitmapCached = await Task.Run(
                        () => SkiaCodec.ToWritableBitmap(diskThumb), token)
                        .ConfigureAwait(false);
                    diskThumb.Dispose();

                    if (token.IsCancellationRequested)
                    {
                        avBitmapCached?.Dispose();
                        return;
                    }

                    GalleryThumbnail?.Dispose();
                    GalleryThumbnail = avBitmapCached;
                    return;
                }


                // 5. get thumbnail from platform provider
                if (Core.PreviewProvider is null) return;
                using var skThumb = await Task.Run(
                    () => Core.PreviewProvider.GetThumbnailAsync(Metadata, thumbSize, token), token)
                    .ConfigureAwait(false);
                if (token.IsCancellationRequested || skThumb.IsDisposed()) return;


                // 5b. write to disk cache (fire-and-forget, encoding is synchronous)
                _ = ThumbnailDiskCache.PutAsync(FilePath, (int)thumbSize, skThumb, token);


                // 6. convert SKImage to Avalonia Bitmap
                var avBitmap = await Task.Run(
                    () => SkiaCodec.ToWritableBitmap(skThumb), token)
                    .ConfigureAwait(false);

                if (token.IsCancellationRequested)
                {
                    avBitmap?.Dispose();
                    return;
                }


                // 7. update the gallery thumbnail (triggers UI binding update)
                GalleryThumbnail?.Dispose();
                GalleryThumbnail = avBitmap;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌❌❌ {nameof(LoadThumbnailAsync)}: {ex.Message}");
            }
            finally
            {
                _thumbnailLock.Release();
            }
        }
        finally
        {
            _thumbnailThrottleLock.Release();
        }
    }


    /// <summary>
    /// Cancels pending thumbnail loading and disposes the cached thumbnail.
    /// </summary>
    public async Task UnloadThumbnailAsync()
    {
        // Signal cancellation first so any in-progress load
        // can exit early and release the lock sooner.
        _cancelThumbnailLoading?.Cancel();

        await _thumbnailLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _cancelThumbnailLoading?.Dispose();
            _cancelThumbnailLoading = null;

            GalleryThumbnail?.Dispose();
            GalleryThumbnail = null;
        }
        finally
        {
            _thumbnailLock.Release();
        }
    }


    #endregion // Public Functions


}


