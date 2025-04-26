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
using System.Diagnostics;
using System.Numerics;

namespace ImageGlass.Common.Photoing;

/// <summary>
/// Provides functionality for animating image frames with customizable frame delays and looping behavior.
/// </summary>
public abstract class AnimatorImpl : IDisposable
{

    #region IDisposable Disposing
    public bool IsDisposed { get; protected set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            StopTimer();

            _currentFrame = 0;
            _frameCount = 0;
            _loopCount = 0;
            _currentLoop = 0;

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

    ~AnimatorImpl()
    {
        Dispose(false);
    }

    #endregion


    protected PhotoMetadata _meta;
    protected uint _frameCount = 0;
    protected uint _loopCount = 0; // 0 - infinite loop
    protected int _currentFrame = 0;
    protected int _currentLoop = 0;

    protected bool _isStarted = false; // check if animation is started
    protected bool _isPaused = true;
    protected Stopwatch _stopwatch = new();
    protected TimeSpan _lastFrameTime = TimeSpan.Zero;
    protected TimeSpan _pauseStartTime = TimeSpan.Zero;


    /// <summary>
    /// Occurs when the image frame is changed.
    /// </summary>
    public event TEventHandler<AnimatorImpl, AnimatorFrameChangedEventArgs>? FrameChanged;

    /// <summary>
    /// Occurs when the animation stopped or the loop has completed.
    /// </summary>
    public event TEventHandler<AnimatorImpl, EventArgs>? Stopped;



    /// <summary>
    /// Initialize new instance of <see cref="AnimatorImpl"/>.
    /// </summary>
    public AnimatorImpl(PhotoMetadata meta)
    {
        _meta = meta;
        _frameCount = meta.FrameCount;
        _loopCount = meta.AnimationLoop;
    }


    /// <summary>
    /// Begins decoding frames in the background.
    /// </summary>
    protected abstract void DecodeFrames();


    /// <summary>
    /// Start the animator timer.
    /// </summary>
    protected abstract void StartTimer();


    /// <summary>
    /// Stop the animator timer.
    /// </summary>
    protected abstract void StopTimer();


    /// <summary>
    /// Renders the current frame of the animation and returns the resulting bitmap.
    /// </summary>
    public abstract T? GetRenderedFrameBitmap<T>() where T : IDisposable;



    // Public methods
    #region Public methods

    /// <summary>
    /// Restarts animation.
    /// </summary>
    public void Restart()
    {
        Pause();

        _stopwatch.Restart();

        _lastFrameTime = TimeSpan.Zero;
        _pauseStartTime = TimeSpan.Zero;
        _lastFrameTime = TimeSpan.Zero;

        Play();
    }


    /// <summary>
    /// Starts or resumes the animation.
    /// </summary>
    public void Play()
    {
        if (!_isStarted)
        {
            DecodeFrames();

            _isStarted = true;
            _stopwatch.Restart();
            _lastFrameTime = TimeSpan.Zero;
        }

        if (_isPaused)
        {
            // shift lastFrameTime forward by pause duration to preserve correct timing
            var pausedDuration = _stopwatch.Elapsed - _pauseStartTime;
            _lastFrameTime += pausedDuration;
        }

        _isPaused = false;
        StartTimer();
    }


    /// <summary>
    /// Pauses the animation.
    /// </summary>
    public void Pause()
    {
        if (!_isStarted || _isPaused) return;

        StopTimer();
        _isPaused = true;
        _pauseStartTime = _stopwatch.Elapsed;
    }


    /// <summary>
    /// Seeks the animation to the specified frame index.
    /// </summary>
    public void SeekToFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= _frameCount) return;

        _currentFrame = frameIndex;

        // Reset timing for accurate delay tracking
        _lastFrameTime = _stopwatch.Elapsed;

        // frame changed
        OnFrameChanged(new AnimatorFrameChangedEventArgs()
        {
            CurrentFrame = _currentFrame,
            CurrentLoop = _currentLoop,
            FrameCount = _frameCount,
            LoopCount = _loopCount,
        });
    }


    #endregion // Public methods



    // Protected virtual methods
    #region Protected virtual methods


    /// <summary>
    /// Occurs when the <see cref="AnimatorImpl"/> instance is being disposed.
    /// </summary>
    protected virtual void OnDisposing()
    {
        //
    }


    /// <summary>
    /// Handles the timer tick event to update the animation state.
    /// </summary>
    /// <remarks>
    /// If the maximum loop count is reached, the animation stops,
    /// and the <see cref="Stopped"/> event is raised.
    /// </remarks>
    protected virtual void OnTimerTicked()
    {
        if (_isPaused) return;

        var frameDelay = GetFrameDelay(_currentFrame);
        var now = _stopwatch.Elapsed;

        // check if it's time to update frame
        if ((now - _lastFrameTime) >= frameDelay)
        {
            _lastFrameTime = now;

            // frame changed
            OnFrameChanged(new AnimatorFrameChangedEventArgs()
            {
                CurrentFrame = _currentFrame,
                CurrentLoop = _currentLoop,
                FrameCount = _frameCount,
                LoopCount = _loopCount,
            });

            _currentFrame++;

            // check for loop
            if (_currentFrame >= _frameCount)
            {
                _currentFrame = 0;
                _currentLoop++;

                if (_loopCount > 0 && _currentLoop >= _loopCount)
                {
                    Pause();

                    // loop ended
                    OnStopped(EventArgs.Empty);
                }
            }
        }
    }


    /// <summary>
    /// Gets the delay duration for a specific animation frame.
    /// </summary>
    protected virtual TimeSpan GetFrameDelay(int frameIndex)
    {
        var ticksPerMs = 1000 / _meta.Frames[frameIndex].AnimationTicksPerSecond;
        var rawDelay = _meta.Frames[frameIndex].AnimationDelay;

        // set minimum delay time if frame delay time is too small
        if (rawDelay <= 2) rawDelay = 10;

        var delayMs = rawDelay * ticksPerMs;

        return TimeSpan.FromMilliseconds(delayMs);
    }


    /// <summary>
    /// Retrieves the bounds of the specified frame as a <see cref="Vector4"/>
    /// <c>(X, Y, Z = Width, W = Height)</c>.
    /// </summary>
    protected Vector4 GetFrameBounds(int frameIndex)
    {
        var frameMeta = _meta.Frames[_currentFrame];

        return new Vector4(frameMeta.X, frameMeta.Y, frameMeta.Width, frameMeta.Height);
    }


    /// <summary>
    /// Retrieves the disposal method for the specified frame in the GIF animation.
    /// </summary>
    protected GifDisposeMethod GetFrameDisposal(int frameIndex)
    {
        return _meta.Frames[_currentFrame].GifDisposeMethod;
    }


    /// <summary>
    /// Raises <c><see cref="FrameChanged"/></c> event.
    /// </summary>
    protected virtual void OnFrameChanged(AnimatorFrameChangedEventArgs e)
    {
        FrameChanged?.Invoke(this, e);
    }


    /// <summary>
    /// Raises <c><see cref="Stopped"/></c> event.
    /// </summary>
    protected virtual void OnStopped(EventArgs e)
    {
        Stopped?.Invoke(this, e);
    }


    #endregion // Protected virtual methods



}
