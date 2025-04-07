

using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace ImageGlass.WinNT.Common;


public static class IRandomAccessStream_Exts
{

    /// <summary>
    /// Reads all bytes from a random access stream asynchronously.
    /// </summary>
    public static async Task<byte[]> ReadBytesAsync(this IRandomAccessStream stream)
    {
        using var reader = new DataReader(stream);
        var bytes = new byte[stream.Size];

        await reader.LoadAsync((uint)stream.Size).AsTask().ConfigureAwait(false);
        reader.ReadBytes(bytes);

        return bytes;
    }

}
