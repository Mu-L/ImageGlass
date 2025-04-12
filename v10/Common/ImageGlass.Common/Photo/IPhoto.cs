namespace ImageGlass.Common;


/// <summary>
/// An interface for handling photo objects.
/// </summary>
/// <typeparam name="T">Represents the type of the native bitmap associated with the photo.</typeparam>
public interface IPhoto<T> : IDisposable
{
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
    void Load(string filePath, uint frameIndex = 0);

}

