

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace ImageGlass.WinNT.Common;


public static class Windows_Foundation_Size_Exts
{

    /// <summary>
    /// Checks if the given size has non-positive dimensions.
    /// </summary>
    public static bool IsEmpty(this Size size)
    {
        return size.Width <= 0 || size.Height <= 0;
    }


    /// <summary>
    /// Inflates a Size object by a specified amount, increasing its dimensions.
    /// </summary>
    public static Size Inflate(this Size size, double thickness)
    {
        return size.Inflate(new Thickness(thickness));
    }


    /// <summary>
    /// Inflates a Size object by adding specified thickness values to its width and height.
    /// This results in a new Size with increased dimensions.
    /// </summary>
    public static Size Inflate(this Size size, Thickness thickness)
    {
        return new Size(
            size.Width + thickness.Left + thickness.Right,
            size.Height + thickness.Top + thickness.Bottom);
    }


}
