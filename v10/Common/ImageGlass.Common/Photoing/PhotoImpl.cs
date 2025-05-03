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
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace ImageGlass.Common.Photoing;


public class PhotoImpl : DisposableImpl, IPhoto<IDisposable>
{
    protected IDisposable? _bitmap;
    protected uint _width = 0;
    protected uint _height = 0;
    protected PhotoMetadata? _metadata;

    protected CancellationTokenSource? _cancelPhotoLoading;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual IDisposable? Bitmap => _bitmap;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Vector2 Size => new Vector2(_width, _height);

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual uint Width => (uint)Size.X;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual uint Height => (uint)Size.Y;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual bool IsDone { get; set; } = false;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual Exception? Error { get; set; } = null;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual PhotoReadOptions ReadOptions { get; set; } = new();

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public MagickReadSettings? ReadSettings { get; set; } = null;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual PhotoMetadata Metadata => _metadata!;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string HashKey => BHelper.CreateUniqueFileKey(FilePath, new Vector2(Width, Height));



    /// <summary>
    /// Initializes new instance of <see cref="PhotoImpl{T}"/>
    /// </summary>
    public PhotoImpl(string filePath = "", PhotoReadOptions? options = null)
    {
        FilePath = filePath;
        ReadOptions = options ?? new();
    }


    /// <summary>
    /// <inheritdoc/>
    /// Calling this function also disposes <see cref="Metadata"/> object.
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        OnDisposing(true);
    }


    /// <summary>
    /// Handles the disposal of resources when an object is being disposed.
    /// </summary>
    /// <param name="disposeEverything">
    /// Option to dispose everything or only the <see cref="Bitmap"/> object.
    /// </param>
    protected virtual void OnDisposing(bool disposeEverything)
    {
        CancelLoading();

        _bitmap?.Dispose();
        _bitmap = null;

        // dispose everything
        if (disposeEverything)
        {
            _metadata?.Dispose();
            _metadata = null;

            _cancelPhotoLoading?.Dispose();
            _cancelPhotoLoading = null;
        }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    [MemberNotNull(nameof(_cancelPhotoLoading))]
    public virtual void CancelLoading()
    {
        _cancelPhotoLoading?.Cancel();
        _cancelPhotoLoading = new();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual async Task LoadAsync(bool useCache,
        PhotoReadOptions? newOptions = null,
        IProgress<PhotoLoadingEventArgs>? progress = null)
    {
        // use cached data
        if (useCache && IsDone) return;

        try
        {
            CancelLoading();
            var token = _cancelPhotoLoading.Token;

            // reset dispose status
            IsDisposed = false;
            IsDone = false;
            Error = null;
            ReadOptions = newOptions ?? ReadOptions;


            // 1. load metadata ===================
            // cancel if requested
            token.ThrowIfCancellationRequested();

            // load metadata
            await LoadMetadataAsync();

            // load image data
            ReadOptions.FirstFrameOnly ??= Metadata.FrameCount < 2;

            progress?.Report(new PhotoLoadingEventArgs(this, token));


            // 2. load image data ===================
            // cancel if requested
            token.ThrowIfCancellationRequested();

            // decode the photo
            await OnDecodingAsync(Metadata, token);

            // cancel if requested
            token.ThrowIfCancellationRequested();

            // done loading
            IsDone = true;

            progress?.Report(new PhotoLoadingEventArgs(this, token));
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Info($"Cancelled loading {FilePath}", nameof(LoadAsync), nameof(PhotoImpl));
            Unload();
        }
        catch (Exception ex)
        {
            Error = ex;
            IsDone = true;

            Log.Error(ex);
        }
    }


    /// <summary>
    /// Not implemented. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    protected virtual Task OnDecodingAsync(PhotoMetadata meta, CancellationToken token)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// <inheritdoc/>
    /// Returns the cached metadata if it's not null and up-to-date.
    /// </summary>
    public async Task LoadMetadataAsync(PhotoReadOptions? newOptions = null)
    {
        try
        {
            ReadOptions = newOptions ?? ReadOptions;
            ReadSettings ??= MagickDecoder.ParseSettings(ReadOptions, false, FilePath);

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
                _metadata = await MagickDecoder.LoadMetadataAsync(FilePath, ReadOptions, ReadSettings);
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


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void Unload()
    {
        // reset info
        IsDone = false;
        Error = null;

        // unload image
        OnDisposing(false);
    }


}
