/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using ImageGlass.Win64.Common;
using System;
using Windows.Foundation;

namespace ImageGlass.WinNT;


public partial class VirtualViewerControl
{

    // DPI Scaling
    #region DPI Scaling

    /// <summary>
    /// Scales the given number based on the DPI scaling factor.
    /// </summary>
    public T DpiScale<T>(T number)
    {
        var type = typeof(T);
        var value = float.Parse($"{number}", System.Globalization.NumberStyles.Number) * CompositionScaleX;

        return (T)Convert.ChangeType(value, type);
    }


    /// <summary>
    /// Scales the given size based on the DPI scaling factor.
    /// </summary>
    public Size DpiScale(Size size)
    {
        return new Size(DpiScale(size.Width), DpiScale(size.Height));
    }


    /// <summary>
    /// Scales the given point based on the DPI scaling factor.
    /// </summary>
    public Point DpiScale(Point p)
    {
        return new Point(DpiScale(p.X), DpiScale(p.Y));
    }


    /// <summary>
    /// Scales the dimensions and position of a rectangle based on the DPI scaling factor.
    /// </summary>
    public Rect DpiScale(Rect rect)
    {
        return new Rect(
            DpiScale(new Point(rect.X, rect.Y)),
            DpiScale(new Size(rect.Width, rect.Height))
        );
    }

    #endregion // DPI Scaling




    // Coordinate converters
    #region Coordinate converters

    /// <summary>
    /// Computes the location of the client point into image source coords.
    /// </summary>
    public Point PointClientToSource(Point clientPoint)
    {
        var x = (clientPoint.X - _destRect.X) / _zooming.Factor + _srcRect.X;
        var y = (clientPoint.Y - _destRect.Y) / _zooming.Factor + _srcRect.Y;

        return new Point(x, y);
    }


    /// <summary>
    /// Computes and scale the rectangle of the client to image source coords
    /// </summary>
    public Rect RectClientToSource(Rect rect)
    {
        var safeRect = rect.Safe();
        var p1 = PointClientToSource(new Point(safeRect.X, safeRect.Y));
        var p2 = PointClientToSource(new Point(safeRect.Right, safeRect.Bottom));


        // get the min int value
        var floorP1 = new Point(
            (float)Math.Floor(Math.Round(p1.X, 1)),
            (float)Math.Floor(Math.Round(p1.Y, 1)));

        if (floorP1.X < 0) floorP1.X = 0;
        if (floorP1.Y < 0) floorP1.Y = 0;
        if (floorP1.X > BitmapSize.Width) floorP1.X = BitmapSize.Width;
        if (floorP1.Y > BitmapSize.Height) floorP1.Y = BitmapSize.Height;

        if (p1 == p2)
        {
            return new Rect(floorP1, new Size(0, 0));
        }


        // get the max int value
        var ceilP2 = new Point(
            (float)Math.Ceiling(Math.Round(p2.X, 1)),
            (float)Math.Ceiling(Math.Round(p2.Y, 1)));
        if (ceilP2.X < 0) ceilP2.X = 0;
        if (ceilP2.Y < 0) ceilP2.Y = 0;
        if (ceilP2.X > BitmapSize.Width) ceilP2.X = BitmapSize.Width;
        if (ceilP2.Y > BitmapSize.Height) ceilP2.Y = BitmapSize.Height;


        var width = Math.Max(0, ceilP2.X - floorP1.X);
        var height = Math.Max(0, ceilP2.Y - floorP1.Y);

        // the selection area is where the p1 and p2 intersected.
        return new Rect(floorP1.X, floorP1.Y, width, height);
    }



    /// <summary>
    /// Computes the location of the image source point into client coords.
    /// </summary>
    public Point PointSourceToClient(Point srcPoint)
    {
        var x = (srcPoint.X - _srcRect.X) * _zooming.Factor + _destRect.X;
        var y = (srcPoint.Y - _srcRect.Y) * _zooming.Factor + _destRect.Y;

        return new Point(x, y);
    }


    /// <summary>
    /// Computes and scale the rectangle of the image source to client coords
    /// </summary>
    public Rect RectSourceToClient(Rect rect)
    {
        var safeRect = rect.Safe();

        var loc = PointSourceToClient(new Point(safeRect.X, safeRect.Y));
        var size = new Size(safeRect.Width * _zooming.Factor, safeRect.Height * _zooming.Factor);

        return new Rect(loc, size);
    }

    #endregion // Coordinate converters


}


