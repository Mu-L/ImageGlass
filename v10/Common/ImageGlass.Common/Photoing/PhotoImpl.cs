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
using ImageGlass.Common.Photoing;
using ImageMagick;
using System.Numerics;

namespace ImageGlass.Common;



public class PhotoImpl : IPhoto<IDisposable>
{

    #region IDisposable Disposing

    public bool IsDisposed { get; protected set; } = false;


    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            OnDisposing(true);
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PhotoImpl()
    {
        Dispose(false);
    }

    #endregion


    protected IDisposable? _bitmap;
    protected uint _width = 0;
    protected uint _height = 0;
    protected PhotoMetadata? _metadata;
    protected CancellationTokenSource? _tokenSrcPhoto;
    protected CancellationTokenSource? _tokenSrcMetadata;

    protected event TEventHandler<PhotoImpl, PhotoLoadingEventArgs>? _onLoading;


    /// <summary>
    /// An event that occurs during the decoding of a photo.
    /// </summary>
    public event TEventHandler<PhotoImpl, PhotoLoadingEventArgs>? Loading
    {
        add { _onLoading += value; }
        remove { _onLoading -= value; }
    }


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
        _tokenSrcPhoto ??= new();
        _tokenSrcMetadata ??= new();

        FilePath = filePath;
        ReadOptions = options ?? new();
    }


    /// <summary>
    /// Handles the disposal of resources when an object is being disposed.
    /// </summary>
    /// <param name="disposeMetadata">
    /// Option to dispose <see cref="Metadata"/> object.
    /// </param>
    protected virtual void OnDisposing(bool disposeMetadata)
    {
        CancelPhotoLoading();
        CancelMetadataLoading();

        _bitmap?.Dispose();
        _bitmap = null;

        if (disposeMetadata)
        {
            _metadata?.Dispose();
            _metadata = null;
        }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual async Task LoadAsync(PhotoReadOptions? newOptions = null)
    {
        CancelPhotoLoading();
        _tokenSrcPhoto = new();

        // reset dispose status
        IsDisposed = false;
        IsDone = false;
        Error = null;
        ReadOptions = newOptions ?? ReadOptions;


        try
        {
            // 1. load metadata ===================
            // cancel if requested
            _tokenSrcPhoto.Token.ThrowIfCancellationRequested();

            // load metadata
            await LoadMetadataAsync();

            // load image data
            ReadOptions.FirstFrameOnly ??= Metadata.FrameCount < 2;

            OnLoading(new PhotoLoadingEventArgs(this));



            // 2. load image data ===================
            // cancel if requested
            _tokenSrcPhoto.Token.ThrowIfCancellationRequested();

            // decode the photo
            await OnDecodingAsync(Metadata, _tokenSrcPhoto.Token);

            // cancel if requested
            _tokenSrcPhoto.Token.ThrowIfCancellationRequested();

            // done loading
            IsDone = true;

            OnLoading(new PhotoLoadingEventArgs(this));
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Error($"Cancelled {nameof(LoadAsync)} for {FilePath}");

            Unload();
            Dispose();
        }
        catch (Exception ex)
        {
            Error = ex;
            IsDone = true;

            Log.Error(ex);
        }
    }


    /// <summary>
    /// Emits <see cref="Loading"/> event.
    /// </summary>
    protected virtual void OnLoading(PhotoLoadingEventArgs e)
    {
        _onLoading?.Invoke(this, e);
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
    /// </summary>
    public async Task LoadMetadataAsync(PhotoReadOptions? newOptions = null)
    {
        CancelMetadataLoading();
        _tokenSrcMetadata = new();

        ReadOptions = newOptions ?? ReadOptions;
        ReadSettings ??= MagickDecoder.ParseSettings(ReadOptions, false, FilePath);

        _metadata = await MagickDecoder.LoadMetadataAsync(FilePath,
            ReadOptions, ReadSettings, _tokenSrcMetadata.Token);
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
    public virtual void CancelPhotoLoading()
    {
        try
        {
            _tokenSrcPhoto?.Cancel();
            _tokenSrcPhoto?.Dispose();
            _tokenSrcPhoto = null;
        }
        catch (ObjectDisposedException) { }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void CancelMetadataLoading()
    {
        try
        {
            _tokenSrcMetadata?.Cancel();
            _tokenSrcMetadata?.Dispose();
            _tokenSrcMetadata = null;
        }
        catch (ObjectDisposedException) { }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void Unload(bool disposeMetadata = false)
    {
        // reset info
        IsDone = false;
        Error = null;

        // unload image
        OnDisposing(disposeMetadata);
    }



}
