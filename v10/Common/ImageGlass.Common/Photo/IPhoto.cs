using System.Numerics;

namespace ImageGlass.Common;


/// <summary>
/// An interface for handling photo objects.
/// </summary>
/// <typeparam name="T">Represents the type of the native bitmap associated with the photo.</typeparam>
public interface IPhoto<T> : IDisposable where T : IDisposable
{
    /// <summary>
    /// Gets file path of the photo.
    /// </summary>
    string FilePath { get; set; }


    /// <summary>
    /// Gets file extension. E.g: <c>.png</c>.
    /// </summary>
    public string Extension => Path.GetExtension(FilePath);


    /// <summary>
    /// Gets the error details.
    /// </summary>
    Exception? Error { get; set; }


    /// <summary>
    /// Gets, sets image metadata
    /// </summary>
    public IgMetadata? Metadata { get; set; }


    /// <summary>
    /// Indicates whether the <see cref="Bitmap"/> is currently loaded.
    /// </summary>
    bool IsDone { get; set; }


    /// <summary>
    /// Gets the hash key of the image.
    /// </summary>
    public string HashKey { get; }


    /// <summary>
    /// Gets the native bitmap.
    /// </summary>
    T? Bitmap { get; }


    /// <summary>
    /// Gets the width of the photo
    /// </summary>
    int Width { get; }


    /// <summary>
    /// Gets the height of the photo.
    /// </summary>
    int Height { get; }


    /// <summary>
    /// Loads photo from file.
    /// </summary>
    void Load(uint frameIndex = 0);


    /// <summary>
    /// Loads photo from file.
    /// </summary>
    Task LoadAsync(uint frameIndex = 0);


    /// <summary>
    /// Stops any ongoing loading process.
    /// </summary>
    void CancelLoading();


    /// <summary>
    /// Unload the image and reset the relevant info
    /// </summary>
    void Unload();
}


public interface IPhoto : IPhoto<IDisposable>
{

}


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
    public virtual void Load(uint frameIndex = 0)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Not implemented. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    public virtual Task LoadAsync(uint frameIndex = 0)
    {
        throw new NotImplementedException();
    }


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
