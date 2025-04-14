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
            OnDisposing();
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
    protected CancellationTokenSource? _tokenSrc;


    public virtual T? Bitmap => _bitmap;

    public virtual int Width => 0;

    public virtual int Height => 0;

    public virtual bool IsDone { get; set; } = false;

    public virtual string FilePath { get; set; } = string.Empty;

    public virtual Exception? Error { get; set; } = null;

    public virtual IgMetadata? Metadata { get; set; } = null;

    public string HashKey => _hashKey.Value;


    public PhotoImpl(string filePath = "")
    {
        FilePath = filePath;
        _tokenSrc ??= new();
        _hashKey = new Lazy<string>(() => BHelper.CreateUniqueFileKey(FilePath, new Vector2(Width, Height)));
    }


    /// <summary>
    /// Handles the disposal of resources when an object is being disposed.
    /// </summary>
    protected virtual void OnDisposing()
    {
        CancelLoading();

        _bitmap?.Dispose();
        _bitmap = default(T);
    }


    /// <summary>
    /// Not implemented. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    public virtual Task LoadAsync(uint frameIndex = 0)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Not implemented. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    public virtual Task LoadMetadataAsync(uint frameIndex = 0)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void CancelLoading()
    {
        try
        {
            _tokenSrc?.Cancel();
        }
        catch (ObjectDisposedException) { }
    }


    /// <summary>
    /// Not implemented. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    public virtual void Unload()
    {
        throw new NotImplementedException();
    }



}
