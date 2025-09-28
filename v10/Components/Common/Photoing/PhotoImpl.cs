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
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.Photoing;


public abstract class PhotoImpl : DisposableImpl
{
    protected IDisposable? _bitmap;
    protected PhotoMetadata? _metadata;
    protected uint _width = 0;
    protected uint _height = 0;
    private string _filePath = "";
    private bool _isCurrent = false;

    private Task<PhotoMetadata>? _taskMetadata;
    protected CancellationTokenSource? _cancelPhotoLoading;



    // Public Propterties
    #region Public Propterties

    /// <summary>
    /// Gets the native bitmap.
    /// </summary>
    public virtual IDisposable? Bitmap => _bitmap;

    /// <summary>
    /// Gets the size of the <c><see cref="Bitmap"/></c>.
    /// </summary>
    public Vector2 Size => new Vector2(_width, _height);

    /// <summary>
    /// Gets the width of the <c><see cref="Bitmap"/></c>.
    /// </summary>
    public virtual uint Width => (uint)Size.X;

    /// <summary>
    /// Gets the height of the <c><see cref="Bitmap"/></c>.
    /// </summary>
    public virtual uint Height => (uint)Size.Y;

    /// <summary>
    /// Indicates whether the <see cref="Bitmap"/> is currently loaded.
    /// </summary>
    public virtual bool IsDone { get; set; } = false;



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
    public virtual Exception? Error { get; set; } = null;

    /// <summary>
    /// Gets, sets options for reading photo.
    /// </summary>
    public virtual PhotoReadOptions ReadOptions { get; set; } = new();

    /// <summary>
    /// Gets, sets the settings for reading Metadata and photo with <see cref="MagickDecoder"/>.
    /// </summary>
    public MagickReadSettings? ReadSettings { get; set; } = null;

    /// <summary>
    /// Gets image metadata.
    /// </summary>
    public virtual PhotoMetadata Metadata => _metadata!;

    /// <summary>
    /// Gets photo loading cancellation token source.
    /// </summary>
    public CancellationToken? CancelToken => _cancelPhotoLoading?.Token;

    /// <summary>
    /// Gets the hash key of the image.
    /// </summary>
    public string HashKey => BHelper.CreateUniqueFileKey(FilePath, new Vector2(Width, Height));

    #endregion // Public Propterties




    /// <summary>
    /// Initializes new instance of <see cref="PhotoImpl{T}"/>
    /// </summary>
    public PhotoImpl(string filePath = "", PhotoReadOptions? options = null)
    {
        FilePath = filePath;
        ReadOptions = options ?? new();
    }




    // Abstract / Virtual functions
    #region Abstract / Virtual functions

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
    protected virtual async Task OnDisposing(bool disposeEverything)
    {
        CancelLoading();

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


    /// <summary>
    /// Handles the decoding of image files based on their metadata.
    /// </summary>
    protected abstract Task OnDecodingAsync(PhotoMetadata meta, CancellationToken token);


    #endregion // Abstract / Virtual functions




    // Public functions
    #region Public functions

    /// <summary>
    /// Disposes the <c><see cref="Bitmap"/></c> and resets the relevant info.
    /// This method keeps the <c><see cref="Metadata"/></c> and neccessary resources.
    /// </summary>
    public virtual void Unload()
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

            // load metadata
            await LoadMetadataAsync();
            ReadOptions.FirstFrameOnly ??= Metadata.FrameCount < 2;
            progress?.Report(new PhotoLoadingEventArgs(false, this, token));


            // 2. load image data ===================
            // cancel if requested
            if (token.IsCancellationRequested) return;

            // decode the photo
            await OnDecodingAsync(Metadata, token);

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
    /// Waits until the loading process is complete: <c><see cref="IsDone"/> = true</c>.
    /// </summary>
    public async Task WaitUntilDoneLoading()
    {
        while (!IsDone)
        {
            await Task.Delay(10);
        }
    }

    #endregion // Public functions



}
