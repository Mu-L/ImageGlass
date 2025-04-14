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
using System.Numerics;

namespace ImageGlass.Common;



public class PhotoImpl<T> : IPhoto<T> where T : IDisposable
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


    protected T? _bitmap;
    protected Lazy<string> _hashKey;
    protected CancellationTokenSource? _tokenSrcPhoto;
    protected CancellationTokenSource? _tokenSrcMetadata;


    public virtual T? Bitmap => _bitmap;

    public virtual int Width => 0;

    public virtual int Height => 0;

    public virtual bool IsDone { get; set; } = false;

    public virtual string FilePath { get; set; } = string.Empty;

    public virtual Exception? Error { get; set; } = null;

    public virtual PhotoReadOptions ReadOptions { get; set; } = new();

    public virtual IgMetadata? Metadata { get; set; } = null;

    public string HashKey => _hashKey.Value;



    /// <summary>
    /// Initializes new instance of <see cref="PhotoImpl{T}"/>
    /// </summary>
    public PhotoImpl(string filePath = "", PhotoReadOptions? options = null)
    {
        _tokenSrcPhoto ??= new();
        _tokenSrcMetadata ??= new();

        FilePath = filePath;
        ReadOptions = options ?? new();
        _hashKey = new Lazy<string>(() => BHelper.CreateUniqueFileKey(FilePath, new Vector2(Width, Height)));
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
        _bitmap = default(T);

        if (disposeMetadata)
        {
            Metadata?.Dispose();
            Metadata = null;
        }
    }


    /// <summary>
    /// Not implemented. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    public virtual Task LoadAsync(PhotoReadOptions? newOptions = null)
    {
        ReadOptions = newOptions ?? ReadOptions;

        throw new NotImplementedException();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual async Task LoadMetadataAsync(PhotoReadOptions? newOptions = null)
    {
        CancelMetadataLoading();
        _tokenSrcMetadata = new();

        ReadOptions = newOptions ?? ReadOptions;
        Metadata = await MagickDecoder.LoadMetadataAsync(FilePath, ReadOptions, _tokenSrcMetadata.Token);
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
