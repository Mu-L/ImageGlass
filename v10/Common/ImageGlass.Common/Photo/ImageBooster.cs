

using System.ComponentModel;
using Microsoft.Extensions.Caching.Memory;

namespace ImageGlass.Common;


public partial class ImageBooster<TBitmap> : IDisposable
    where TBitmap : IDisposable
{

    #region IDisposable Disposing

    public bool IsDisposed { get; private set; } = false;


    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            _worker.DoWork -= Worker_DoWork;
            _worker.RunWorkerCompleted -= Worker_RunWorkerCompleted;

            _worker.Dispose();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ImageBooster()
    {
        Dispose(false);
    }

    #endregion



    private readonly BackgroundWorker _worker = new()
    {
        WorkerReportsProgress = true,
        WorkerSupportsCancellation = true,
    };

    private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private OrderedDictionary<string, PhotoImpl<TBitmap>> _list = new();
    

    /// <summary>
    /// Occurs when the image is loaded.
    /// </summary>
    public event EventHandler<EventArgs>? OnFinishLoadingImage;
    




    public ImageBooster()
    {
        _worker.DoWork += Worker_DoWork;
        _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
    }


    private void Worker_DoWork(object? sender, DoWorkEventArgs e)
    {

    }


    private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {

    }



    public async Task<PhotoImpl<TBitmap>?> GetAsync(int index)
    {
        //try
        //{
        //    var pair = _list.GetAt(index);
            
        //    if (!pair.Value.IsDone)
        //    {
        //        await pair.Value.LoadAsync(pair.Key);
        //    }

        //    return pair.Value;
        //}
        //catch (ArgumentOutOfRangeException) { }

        return null;
    }


}



