using System.Collections.Generic;

namespace ImageGlass.WinNT.Common;

public static partial class Config
{
    /// <summary>
    /// Gets, sets the value indicates that Windows File Explorer sort order is used if possible
    /// </summary>
    public static bool ShouldUseExplorerSortOrder { get; set; } = false;


    /// <summary>
    /// Gets, sets zoom levels of the viewer
    /// </summary>
    public static float[] ZoomLevels { get; set; } = [];


    /// <summary>
    /// Gets, sets the list of supported image formats
    /// </summary>
    public static HashSet<string> FileFormats { get; set; } = [];

}
