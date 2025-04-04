

using Microsoft.UI.Xaml;
using System;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass.WinNT;

public static class TypesExtensions
{

    // Windows.Foundation.Size ================================================
    #region Windows.Foundation.Size

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

    #endregion // Windows.Foundation.Size



    // Windows.Foundation.Rect ================================================
    #region Windows.Foundation.Rect

    /// <summary>
    /// Checks if the specified rectangle has no area.
    /// </summary>
    public static bool IsEmpty(this Rect rect)
    {
        return rect.Size().IsEmpty();
    }


    /// <summary>
    /// Gets the size of a rectangle by ensuring width and height are non-negative.
    /// </summary>
    public static Size Size(this Rect rect)
    {
        var w = Math.Max(0, rect.Width);
        var h = Math.Max(0, rect.Height);

        return new Size(w, h);
    }


    /// <summary>
    /// Gets the position of a rectangle as a point.
    /// </summary>
    public static Point Position(this Rect rect)
    {
        return new Point(rect.X, rect.Y);
    }


    /// <summary>
    /// Ensures the rectangle location and size are not negative infinite.
    /// </summary>
    public static Rect Safe(this Rect rect)
    {
        var x = rect.X == double.NegativeInfinity ? 0 : rect.X;
        var y = rect.Y == double.NegativeInfinity ? 0 : rect.Y;
        var w = Math.Max(0, rect.Width);
        var h = Math.Max(0, rect.Height);

        return new Rect(rect.X, rect.Y, w, h);
    }


    /// <summary>
    /// Determines if two rectangles overlap in a 2D space.
    /// </summary>
    public static bool IntersectsWith(this Rect rect, Rect rect2)
    {
        if (rect.Width < 0f || rect2.Width < 0f) return false;

        if (rect2.X <= rect.X + rect.Width
            && rect2.X + rect2.Width >= rect.X
            && rect2.Y <= rect.Y + rect.Height)
        {
            return rect2.Y + rect2._height >= rect.Y;
        }

        return false;
    }


    /// <summary>
    /// Calculates the intersection of a rectangle with a specified width and height.
    /// </summary>
    public static Rect GetIntersection(this Rect rect, double width, double height)
    {
        return rect.GetIntersection(new Rect(0, 0, width, height));
    }


    /// <summary>
    /// Calculates the intersection of two rectangles and returns the intersected area.
    /// </summary>
    public static Rect GetIntersection(this Rect rect, Rect rect2)
    {
        var outputRect = rect;

        if (rect.IntersectsWith(rect2))
        {
            var num = Math.Max(rect.X, rect2.X);
            var num2 = Math.Max(rect.Y, rect2.Y);

            outputRect.Width = Math.Max(Math.Min(rect.X + rect.Width, rect2.X + rect2.Width) - num, 0.0);
            outputRect.Height = Math.Max(Math.Min(rect.Y + rect.Height, rect2.Y + rect2.Height) - num2, 0.0);
            outputRect.X = num;
            outputRect.Y = num2;
        }

        return outputRect;
    }


    /// <summary>
    /// Inflates a rectangle by a specified thickness, expanding its dimensions outward.
    /// </summary>
    public static Rect Inflate(this Rect rect, double thickness)
    {
        return rect.Inflate(new Thickness(thickness));
    }


    /// <summary>
    /// Expands the dimensions of a rectangle by specified thickness values on each side.
    /// </summary>
    public static Rect Inflate(this Rect rect, Thickness thickness)
    {
        return new Rect(
            new Point(rect.X - thickness.Left, rect.Y - thickness.Top),
            rect.Size().Inflate(thickness));
    }

    #endregion // Windows.Foundation.Rect

}
