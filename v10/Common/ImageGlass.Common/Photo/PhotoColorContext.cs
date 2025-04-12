namespace ImageGlass.Common;



public class PhotoColorContext(PhotoColorSpace colorSpace, byte[]? profile) : IDisposable
{

    #region IDisposable Disposing

    public bool IsDisposed { get; private set; } = false;


    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            Profile = null;
            ColorSpace = PhotoColorSpace.Unknown;
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PhotoColorContext()
    {
        Dispose(false);
    }

    #endregion


    public PhotoColorSpace ColorSpace { get; private set; } = colorSpace;


    public byte[]? Profile { get; private set; } = profile;


    public PhotoColorContext() : this(PhotoColorSpace.Unknown, null) { }

}


public enum PhotoColorSpace
{
    Unknown = 0,
    sRGB = 1,
    AdobeRGB = 2,
    Uncalibrated = 3,
}