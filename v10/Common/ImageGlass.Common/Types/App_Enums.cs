namespace ImageGlass.Common;

/// <summary>
/// Window backdrop effect.
/// </summary>
public enum BackdropStyle
{
    /// <summary>
    /// Use default setting of Windows.
    /// </summary>
    None = 0,

    /// <summary>
    /// Mica effect.
    /// </summary>
    Mica = 2,

    /// <summary>
    /// Acrylic effect.
    /// </summary>
    Acrylic = 3,

    /// <summary>
    /// Draw the backdrop material effect corresponding to a window with a tabbed title bar.
    /// </summary>
    MicaAlt = 4,
}
