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
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageMagick;
using Microsoft.UI.Xaml.Media;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Vortice;
using Vortice.Direct2D1;
using Vortice.WIC;
using WinRT;

namespace ImageGlass.WinNT.Common.Photoing;


/// <summary>
/// Provides functionality to animate GIF images by decoding frames and rendering them sequentially.
/// </summary>
public partial class GifAnimator : AnimatorImpl
{
    private IWICBitmapDecoder? _decoder;
    private ID2D1DeviceContext? _dc;
    private ConcurrentDictionary<int, ID2D1Bitmap1?> _decodedFrames = new();

    private ID2D1BitmapRenderTarget? _compositeSurface;
    private ID2D1Bitmap? _backupSurface; // stores composite before the current frame


    /// <summary>
    /// Initializes a new instance of the <see cref="GifAnimator"/> class.
    /// </summary>
    public GifAnimator(IWICBitmapDecoder decoder, PhotoMetadata meta) : base(meta)
    {
        _decoder = decoder;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        DisposeD2dResources();

        _decoder?.Dispose();
        _decoder = null;
    }


    /// <summary>
    /// Creates the Direct2D resources required for rendering, also disposes the old resources.
    /// </summary>
    public void Initialize(ID2D1DeviceContext dc)
    {
        // dispose old Direct2D resources
        DisposeD2dResources();

        _isStarted = false;
        _isPaused = true;

        _dc = dc;
        _compositeSurface = _dc.CreateCompatibleRenderTarget();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void DecodeFrames()
    {
        // start decoding frames in background
        Task.Run(() =>
        {
            for (int i = 0; i < _frameCount; i++)
            {
                var frameIndex = i;
                DecodeFrame(frameIndex);
            }
        });
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override T? GetRenderedFrameBitmap<T>() where T : default
    {
        var frameBmp = GetFrame(_currentFrame);
        if (frameBmp is null
            || _dc is null
            || _compositeSurface is null) return default;


        // apply previous frame disposal
        if (_currentFrame > 0)
        {
            var prevFrameIndex = _currentFrame - 1;
            var prevFrameMeta = _meta.Frames[prevFrameIndex];
            var prevRect = new Vortice.Mathematics.Rect(GetFrameBounds(prevFrameIndex));
            var prevDisposal = GetFrameDisposal(prevFrameIndex);

            _compositeSurface.BeginDraw();

            // restore to background
            if (prevDisposal == ImageMagick.GifDisposeMethod.Background)
            {
                var rawRect = new RawRectF(prevRect.X, prevRect.Y, prevRect.Right, prevRect.Bottom);
                _compositeSurface.PushAxisAlignedClip(rawRect, AntialiasMode.Aliased);
                _compositeSurface.Clear(Vortice.Mathematics.Colors.Transparent);
                _compositeSurface.PopAxisAlignedClip();
            }
            // restore saved composite before last frame
            else if (prevDisposal == GifDisposeMethod.Previous && _backupSurface != null)
            {
                var backupRect = new Vortice.Mathematics.Rect(
                    _backupSurface.PixelSize.Width,
                    _backupSurface.PixelSize.Height);

                _compositeSurface.DrawBitmap(_backupSurface, 1.0f, Vortice.Direct2D1.BitmapInterpolationMode.Linear, backupRect);
            }

            _compositeSurface.EndDraw();
        }


        // save current composite if disposal 3 is coming next
        var nextFrameIndex = _currentFrame + 1;
        if (nextFrameIndex < _frameCount
            && GetFrameDisposal(nextFrameIndex) == GifDisposeMethod.Previous)
        {
            _backupSurface?.Dispose();
            _backupSurface = _compositeSurface.Bitmap;
        }


        // draw current frame to composite surface
        var currRect = new Vortice.Mathematics.Rect(GetFrameBounds(_currentFrame));
        _compositeSurface.BeginDraw();
        _compositeSurface.DrawBitmap(frameBmp,
            currRect, 1.0f,
            Vortice.Direct2D1.BitmapInterpolationMode.Linear,
            new Vortice.Mathematics.Rect(currRect.Width, currRect.Height));

        _compositeSurface.EndDraw();


        // return the bitmap
        var bmp1 = PhotoWIC.CreateD2dBitmap1(_compositeSurface.Bitmap, _dc);

        return bmp1.As<T>();
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
    private void DecodeFrame(int frameIndex)
    {
        if (_decoder is null || _dc is null) return;

        Log.Info($"Decoding frame ${frameIndex}");
        using var frameBmp = _decoder.GetFrame((uint)frameIndex);

        var bmp = PhotoWIC.CreateD2dBitmap(frameBmp, _dc);
        _decodedFrames[frameIndex] = bmp;
    }


    /// <summary>
    /// Retrieves the decoded bitmap for the specified frame index.
    /// </summary>
    private ID2D1Bitmap1? GetFrame(int frameIndex)
    {
        // try get from cache
        if (_decodedFrames.TryGetValue(frameIndex, out var bmp))
        {
            return bmp;
        }

        // no cache, decode frame
        DecodeFrame(frameIndex);

        return _decodedFrames[frameIndex];
    }


    /// <summary>
    /// Releases all Direct2D resources associated with this instance.
    /// </summary>
    private void DisposeD2dResources()
    {
        StopTimer();

        _compositeSurface?.Dispose();
        _compositeSurface = null;

        _backupSurface?.Dispose();
        _backupSurface = null;


        // dispose frames
        foreach (var frame in _decodedFrames.Values)
        {
            frame?.Dispose();
        }
        _decodedFrames.Clear();
    }

}
