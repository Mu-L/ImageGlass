/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
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
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace ImageGlass.UI.Viewer;

public partial class PhotoRenderer : ICustomDrawOperation
{

    #region IDisposable Disposing

    /// <summary>
    /// Gets a value indicating whether the object has been disposed.
    /// </summary>
    public bool IsDisposed { get; protected set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            OnDisposing();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PhotoRenderer()
    {
        Dispose(false);
    }

    #endregion


    private ViewerControl _viewer;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Rect Bounds { get; set; }





    public PhotoRenderer(ViewerControl viewer)
    {
        _viewer = viewer;
    }


    /// <summary>
    /// Releases the managed objects.
    /// </summary>
    protected virtual void OnDisposing()
    {
        //
    }


    public bool Equals(ICustomDrawOperation? other) => false;


    public bool HitTest(Point p) => true;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Render(ImmediateDrawingContext c)
    {
        var leaseFeature = c.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null) return;

        using var lease = leaseFeature.Lease();
        if (lease is null) return;


        var canvas = lease.SkCanvas;
        canvas.Save();

        // TODO:
        // draw image

        using var p = new SkiaSharp.SKPaint
        {
            Color = SKColors.Yellow,
            StrokeWidth = 2,
            IsStroke = true,
        };

        canvas.DrawRect(_viewer.DrawingArea.ToSKRect(), p);

        canvas.Restore();
    }
















}
