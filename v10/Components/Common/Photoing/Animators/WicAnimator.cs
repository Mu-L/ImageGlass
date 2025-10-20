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
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using ImageMagick;
using Microsoft.UI.Xaml.Media;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vortice;
using Vortice.Direct2D1;
using Vortice.WIC;
using WinRT;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Provides functionality to animate GIF images by decoding frames and rendering them sequentially.
/// </summary>
public partial class WicAnimator : AnimatorImpl
{
    protected IWICBitmapDecoder? _decoder;
    protected ID2D1DeviceContext? _dc;
    protected ConcurrentDictionary<int, ID2D1Bitmap1?> _decodedFrames = new();

    protected ID2D1BitmapRenderTarget? _compositeSurface;
    protected ID2D1Bitmap? _backupSurface; // stores composite before the current frame

    private Lock _lockDc = new Lock();


    /// <summary>
    /// Initializes a new instance of the <see cref="WicAnimator"/> class.
    /// </summary>
    public WicAnimator(IWICBitmapDecoder decoder, PhotoMetadata meta) : base(meta)
    {
        _decoder = decoder;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        Unload();

        _decoder?.Dispose();
        _decoder = null;

        base.OnDisposing();
    }


    /// <summary>
    /// Creates the Direct2D resources required for rendering, also disposes the old resources.
    /// </summary>
    public void Initialize(ID2D1DeviceContext dc)
    {
        _dc = dc;

        var size = new Vortice.Mathematics.SizeI((int)_meta.Width, (int)_meta.Height);
        _compositeSurface = _dc.CreateCompatibleRenderTarget(size);
    }


    /// <summary>
    /// Sets the Direct2D device context to be used for rendering operations.
    /// </summary>
    public void SetDeviceContext(ID2D1DeviceContext dc)
    {
        _dc = dc;
    }



    /// <summary>
    /// Stops the animation, releases the decoded frames and native resources.
    /// Use <c><see cref="Initialize(ID2D1DeviceContext)"/></c> to re-initialize the instance again.
    /// </summary>
    public void Unload()
    {
        StopTimer();

        _compositeSurface?.Dispose();
        _compositeSurface = null;

        _backupSurface?.Dispose();
        _backupSurface = null;


        // dispose frames
        _isDecoded = false;

        for (int i = 0; i < _decodedFrames.Count; i++)
        {
            var frame = _decodedFrames.GetValueOrDefault(i);
            frame?.Dispose();
            frame = null;
        }
        _decodedFrames.Clear();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override T? GetRenderedFrameBitmap<T>() where T : default
    {
        var bmp = GetRenderedFrameBitmap1();
        if (bmp is null) return default;

        return GetRenderedFrameBitmap1().As<T>();
    }


    /// <summary>
    /// Renders the current frame of the animation and returns it as a Direct2D bitmap.
    /// </summary>
    public virtual ID2D1Bitmap1? GetRenderedFrameBitmap1()
    {
        if (_dc.IsDisposed() || _compositeSurface.IsDisposed()) return null;

        var frameBmp = GetFrame(_currentFrame);
        if (frameBmp is null) return null;


        // 1. apply previous frame disposal
        ApplyFrameDisposal(_compositeSurface);


        // 2. draw current frame to composite surface
        RenderFrame(frameBmp, _compositeSurface, _dc);


        // 3. get the rendered bitmap
        var bmp1 = PhotoWIC.CreateD2dBitmap1(_compositeSurface.Bitmap, _dc);

        return bmp1;
    }


    /// <summary>
    /// Applies the disposal method of the previous frame to the composite surface.
    /// </summary>
    protected virtual void ApplyFrameDisposal(ID2D1BitmapRenderTarget surface)
    {
        // 1. get frame metadata
        var prevFrameIndex = _currentFrame - 1;
        if (prevFrameIndex < 0) prevFrameIndex = (int)_frameCount - 1;

        var prevDisposal = GetFrameDisposal(prevFrameIndex);
        var prevRect = new Vortice.Mathematics.Rect(GetFrameBounds(prevFrameIndex));


        // 2. render frame bitmap
        surface.BeginDraw();

        // 2.1 clear background
        if (prevDisposal == GifDisposeMethod.Background)
        {
            var bgColor = Vortice.Mathematics.Colors.Transparent;
            var rawRect = new RawRectF(prevRect.X, prevRect.Y, prevRect.Right, prevRect.Bottom);

            surface.PushAxisAlignedClip(rawRect, AntialiasMode.Aliased);
            surface.Clear(bgColor);
            surface.PopAxisAlignedClip();
        }

        // 2.2 restore saved composite before last frame
        else if (prevDisposal == GifDisposeMethod.Previous && _backupSurface != null)
        {
            var backupRect = new Vortice.Mathematics.Rect(
                _backupSurface.PixelSize.Width,
                _backupSurface.PixelSize.Height);

            surface.DrawBitmap(_backupSurface, 1.0f, Vortice.Direct2D1.BitmapInterpolationMode.Linear, backupRect);
        }

        surface.EndDraw();
    }


    /// <summary>
    /// Renders a single frame onto the specified Direct2D surfaces.
    /// </summary>
    /// <param name="frameBmp">The bitmap representing the current frame to be rendered.</param>
    /// <param name="surface">The render target surface where the frame will be composited.</param>
    /// <param name="dc">The Direct2D device context used for rendering operations.</param>
    protected virtual void RenderFrame(ID2D1Bitmap1 frameBmp,
        ID2D1BitmapRenderTarget surface, ID2D1DeviceContext dc)
    {
        // 1. save current composite if disposal 'Previous' is coming next
        var nextFrameIndex = _currentFrame + 1;
        if (nextFrameIndex >= _frameCount) nextFrameIndex = 0;
        if (GetFrameDisposal(nextFrameIndex) == GifDisposeMethod.Previous)
        {
            _backupSurface?.Dispose();
            _backupSurface = PhotoWIC.CreateD2dBitmap1(surface.Bitmap, dc);
        }


        // 2. get frame bounds
        var currRect = new Vortice.Mathematics.Rect(GetFrameBounds(_currentFrame));

        // 3. draw current frame to composite surface
        surface.BeginDraw();
        surface.DrawBitmap(frameBmp,
            currRect, 1.0f,
            Vortice.Direct2D1.BitmapInterpolationMode.Linear,
            new Vortice.Mathematics.Rect(frameBmp.Size.ToVector2()));

        //// debug
        //using var debugBrush = dc.CreateSolidColorBrush(Vortice.Mathematics.Colors.Red);
        //var debugRect = currRect;
        //debugRect.Inflate(-1, -1);
        //var debugFramRect = new Vortice.RawRectF(
        //    debugRect.X, debugRect.Y,
        //    debugRect.Right, debugRect.Bottom);
        //surface.DrawRectangle(debugFramRect, debugBrush);

        surface.EndDraw();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void StartTimer()
    {
        StopTimer();
        CompositionTarget.Rendering += OnRendering;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void StopTimer()
    {
        CompositionTarget.Rendering -= OnRendering;
    }


    private void OnRendering(object? sender, object e)
    {
        OnTimerTicked();
    }


    /// <summary>
    /// Decodes the specified frame of an image and stores it.
    /// </summary>
    private ID2D1Bitmap1? DecodeFrame(int frameIndex)
    {
        using var frameBmp = _decoder?.GetFrame((uint)frameIndex);
        if (!_dc.IsDisposed())
        {
            _decodedFrames[frameIndex] = frameBmp.ToD2Bitmap(_dc);
        }

        return _decodedFrames[frameIndex];
    }


    /// <summary>
    /// Retrieves the decoded bitmap for the specified frame index.
    /// </summary>
    private ID2D1Bitmap1? GetFrame(int frameIndex)
    {
        ID2D1Bitmap1? frameD2d = null;

        // try get from cache
        if (!_decodedFrames.TryGetValue(frameIndex, out frameD2d))
        {
            // no cache, decode frame
            frameD2d = DecodeFrame(frameIndex);
        }

        return frameD2d;
    }

}
