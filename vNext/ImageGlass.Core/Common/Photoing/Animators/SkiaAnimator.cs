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
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using SkiaSharp;
using System.Threading;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Provides functionality to animate GIF images by decoding frames and rendering them sequentially.
/// </summary>
public partial class SkiaAnimator : AnimatorImpl
{
    private readonly SKCodec? _codec;
    private readonly SKImage?[] _frameCache;
    private Timer? _timer;
    private int _isProcessing;



    /// <summary>
    /// Initializes a new instance of the <see cref="SkiaAnimator"/> class.
    /// </summary>
    public SkiaAnimator(SKCodec? srcCodec, PhotoMetadata meta) : base(meta)
    {
        _codec = srcCodec;
        _frameCache = new SKImage[meta.FrameCount];
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        _timer?.Dispose();
        _timer = null;

        // clear cached frames
        for (int i = 0; i < _frameCache.Length; i++)
        {
            _frameCache[i]?.Dispose();
            _frameCache[i] = null;
        }

        base.OnDisposing();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void StartTimer()
    {
        _timer ??= new Timer(Timer_Ticked, null, Timeout.Infinite, Timeout.Infinite);
        _timer.Change(0, 1); // 1ms interval for high resolution
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void StopTimer()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }



    private void Timer_Ticked(object? state)
    {
        // Prevent re-entrancy
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
            return;

        try
        {
            OnTimerTicked();
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);
        }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <returns></returns>
    public override SKImage? GetRenderedFrameBitmap()
    {
        if (_codec is null) return null;

        // use cached frame first
        if (_frameCache[_currentFrame] is not null)
        {
            return _frameCache[_currentFrame];
        }


        // decode the frame
        var info = _codec.Info;
        using var frameBitmap = new SKBitmap(info.Width, info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        var priorFrame = _currentFrame > 0 ? _currentFrame - 1 : -1;
        var options = new SKCodecOptions(_currentFrame, priorFrame);
        var result = _codec.GetPixels(info, frameBitmap.GetPixels(), options);
        if (result != SKCodecResult.Success) return null;


        // convert to immutable SKImage
        var frameImage = SKImage.FromBitmap(frameBitmap);
        _frameCache[_currentFrame] = frameImage;

        return frameImage;
    }


}
