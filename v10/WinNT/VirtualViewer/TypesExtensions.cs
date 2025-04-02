

using Microsoft.UI.Xaml;
using System;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass.WinNT;

public static class TypesExtensions
{

    // Windows.Foundation.Size
    #region Windows.Foundation.Size

    public static bool IsEmpty(this Size size)
    {
        return size.Width <= 0 || size.Height <= 0;
    }

    public static Size Inflate(this Size size, double thickness)
    {
        return size.Inflate(new Thickness(thickness));
    }

    public static Size Inflate(this Size size, Thickness thickness)
    {
        return new Size(
            size.Width + thickness.Left + thickness.Right,
            size.Height + thickness.Top + thickness.Bottom);
    }

    #endregion // Windows.Foundation.Size


    // Windows.Foundation.Rect
    #region Windows.Foundation.Rect

    public static bool IsEmpty(this Rect rect)
    {
        return rect.Size().IsEmpty();
    }

    public static bool IsEmpty(this Rect? rect)
    {
        if (rect == null) return true;
        return rect.Value.Size().IsEmpty();
    }

    public static Size Size(this Rect rect)
    {
        var w = Math.Max(0, rect.Width);
        var h = Math.Max(0, rect.Height);

        return new Size(w, h);
    }

    public static Point Position(this Rect rect)
    {
        return new Point(rect.X, rect.Y);
    }

    public static Rect Inflate(this Rect rect, double thickness)
    {
        return rect.Inflate(new Thickness(thickness));
    }

    public static Rect Inflate(this Rect rect, Thickness thickness)
    {
        return new Rect(
            new Point(rect.X - thickness.Left, rect.Y - thickness.Top),
            rect.Size().Inflate(thickness));
    }

    #endregion // Windows.Foundation.Rect

}
