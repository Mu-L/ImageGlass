
namespace ImageGlass.WinNT;


/// <summary>
/// Zoom modes
/// </summary>
public enum ZoomMode
{
    AutoZoom = 1,
    LockZoom = 2,
    ScaleToWidth = 3,
    ScaleToHeight = 4,
    ScaleToFit = 5,
    ScaleToFill = 6,
}


/// <summary>
/// Interpolation modes.
/// These values are based on <see cref="D2Phap.InterpolationMode"/>.
/// </summary>
public enum ImageInterpolation : int
{
    /// <summary>
    /// Pixelated scaling down (poor quality) and up.
    /// </summary>
    NearestNeighbor = 0,

    /// <summary>
    /// Pixelated scaling down (poor quality), smooth scaling up (normal quality).
    /// </summary>
    Linear = 1,

    /// <summary>
    /// Pixelated scaling down (poor quality), smooth scaling up (better quality).
    /// </summary>
    Cubic = 2,

    /// <summary>
    /// Smooth scaling down (the best), smooth scaling up (normal quality).
    /// </summary>
    MultiSampleLinear = 3,

    /// <summary>
    /// Smooth scaling down (normal quality) and up (normal quality).
    /// </summary>
    Antisotropic = 4,

    /// <summary>
    /// Smooth scaling down (normal quality) and up (better quality).
    /// </summary>
    HighQualityBicubic = 5,
}


/// <summary>
/// Specifies the display styles for the background texture grid
/// </summary>
public enum CheckerboardMode
{
    /// <summary>
    /// No background.
    /// </summary>
    None = 0,

    /// <summary>
    /// Background is displayed in the control's client area.
    /// </summary>
    Client = 1,

    /// <summary>
    /// Background is displayed only in the image region.
    /// </summary>
    Image = 2,
}

