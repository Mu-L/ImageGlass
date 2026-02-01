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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ImageGlass.Common;
using ImageGlass.Common.OsApi;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using SkiaSharp;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.UI.Viewer;

public partial class ViewerControl : PhControl
{
    private Photo? _photo;
    private CancellationTokenSource? _cancelPreview;
    private InterlockedBool _isPreviewing = new();

    private Point? _lastMousePanPoint = null; // mouse panning


    // events
    public event TEventHandler<ViewerControl, PhotoLoadingEventArgs>? PhotoLoading;



    #region Public Properties

    /// <summary>
    /// Gets the drawing area.
    /// </summary>
    public Rect DrawingArea { get; private set; }


    /// <summary>
    /// Gets the bitmap size.
    /// </summary>
    public Size BitmapSize { get; private set; }


    /// <summary>
    /// Gets the photo source for renderer.
    /// </summary>
    public PhotoSource SourceKind { get; private set; } = PhotoSource.None;


    /// <summary>
    /// Gets, sets value indicates whether full resolution is loaded or not.
    /// </summary>
    public bool ShouldLoadFullResolution { get; set; } = true;


    #endregion // Public Properties



    #region Override Methods

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        RegisterTouchGestures();
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        UnregisterTouchGestures();


        _photoRenderer?.Dispose();
        _photoRenderer = null;

        DisposeCheckerboard();
        DisposeNativePhotoResources();
    }


    protected override void OnIgDpiChanged()
    {
        base.OnIgDpiChanged();

        DisposeCheckerboard();
        InvalidateVisual();
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // update drawing area
        if (e.Property == PaddingProperty || e.Property == BoundsProperty)
        {
            Dispatcher.UIThread.Post(() =>
            {
                DrawingArea = Bounds.Deflate(Padding);
                CalculateDrawingRegion();

                // redraw the control on resizing if it's not manual zoom
                if (_photo is not null && !_zooming.IsManual)
                {
                    Refresh(true, false, true);
                }
            });
        }
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var p = e.GetCurrentPoint(this);

        // set the init point for panning
        if (p.Pointer.Type == PointerType.Mouse)
        {
            var canPanByMouse = !EnableSelection || e.Properties.IsMiddleButtonPressed;
            if (canPanByMouse)
            {
                _lastMousePanPoint = p.Position;
            }
        }

        var requestRerender = OnSelectionBegin(p);

        // request re-render control
        if (requestRerender) InvalidateVisual();
    }


    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _ = OnSelectionEnd(true);
    }


    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        // reset the panning point
        _lastMousePanPoint = null;


        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) InvalidateVisual();

        base.OnPointerReleased(e);
    }


    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        // reset the panning point
        _lastMousePanPoint = null;


        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) InvalidateVisual();

        base.OnPointerCaptureLost(e);
    }


    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var p = e.GetCurrentPoint(this);
        var requestRerender = false;


        // if user is panning with mouse
        if (_lastMousePanPoint is not null)
        {
            var hDistance = _lastMousePanPoint.Value.X - p.Position.X;
            var vDistance = _lastMousePanPoint.Value.Y - p.Position.Y;
            _lastMousePanPoint = p.Position;

            requestRerender = PanTo(hDistance, vDistance, p.Position);
        }
        else
        {
            requestRerender = OnSelectionUpdating(p);
        }


        // request re-render control
        if (requestRerender) InvalidateVisual();
    }


    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var delta = e.Delta.Y;
        var position = e.GetPosition(this);
        var isUsingTouchpad = Math.Abs(e.Delta.Y) != 1;


        // Touchpad scrolling
        if (isUsingTouchpad)
        {
            // Scroll Left/Right: Pan horizontally
            if (Math.Abs(e.Delta.X) > 0 && Math.Abs(e.Delta.Y) < 0.5)
            {
                PanTo(e.Delta.X * -50, e.Delta.Y * -50, position);
                return;
            }

            // Scroll Up/Down: Zoom
            delta *= 70;
        }
        // Mouse wheel
        else
        {
            delta *= SystemInfo.MouseWheelScrollDelta;
        }

        // Zooming
        _ = ZoomByDeltaToPoint(delta, position);
    }


    /// <summary>
    /// Raises <see cref="PhotoLoading"/> event.
    /// </summary>
    protected virtual void OnPhotoLoading(PhotoLoadingEventArgs e)
    {
        PhotoLoading?.Invoke(this, e);
    }

    #endregion // Override Methods



    #region Control Methods

    /// <summary>
    /// Forces the control to reset zoom mode and invalidate itself.
    /// </summary>
    public void Refresh()
    {
        Refresh(true);
    }


    /// <summary>
    /// Forces the control to invalidate itself.
    /// </summary>
    public void Refresh(bool resetZoom = true, bool isManualZoom = false, bool zoomedByResizing = false)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (resetZoom)
            {
                SetZoomMode(null, isManualZoom, zoomedByResizing);
            }

            InvalidateVisual();
        });
    }


    /// <summary>
    /// Cancels any ongoing photo previewing operation.
    /// </summary>
    [MemberNotNull(nameof(_cancelPreview))]
    private void CancelPreview()
    {
        _cancelPreview?.Cancel();
        _cancelPreview?.Dispose();
        _cancelPreview = new();
    }


    /// <summary>
    /// Disposes the native photo resources.
    /// </summary>
    private void DisposeNativePhotoResources()
    {
        // dispose preview bitmap
        _imgPreview?.Dispose();
        _imgPreview = null;

        // dispose native bitmap
        _imgSource?.Dispose();
        _imgSource = null;
    }


    /// <summary>
    /// Unloads the photo and cancels any ongoing loading operation.
    /// </summary>
    [MemberNotNull(nameof(_cancelPreview))]
    public void UnloadPhoto()
    {
        CancelPreview();
        SourceKind = PhotoSource.None;
        _photo?.CancelLoading();
        _photo?.Unload();

        // reset
        AnimationSource = AnimationSources.None;
        _enablePanningVelocity = false;
        SetSourceSelection(new Rect(), false);
        BitmapSize = new();

        // dispose native resources of photo
        DisposeNativePhotoResources();
    }


    /// <summary>
    /// Sets a photo to render.
    /// </summary>
    public async Task SetPhotoAsync(Photo? inputPhoto, bool useCache)
    {
        // unload current photo resources
        UnloadPhoto();

        if (inputPhoto is null)
        {
            _photo = null;
            Refresh(true);
            return;
        }


        SourceKind = PhotoSource.Native;
        _enablePanningVelocity = true;
        _photo = inputPhoto;


        // photo is loaded
        if (useCache && inputPhoto.State == PhotoLoadingState.Loaded)
        {
            var token = inputPhoto.CancelToken ?? default;
            await HandlePhotoLoadedAsync(new(PhotoLoadingState.Loaded, inputPhoto, token));
        }
        else
        {
            await LoadPhotoAsync(inputPhoto, useCache, false);
        }
    }


    /// <summary>
    /// Loads photo data and renders to the viewer
    /// </summary>
    private async Task LoadPhotoAsync(Photo? inputPhoto, bool useCache, bool skipLoadingEvent)
    {
        if (inputPhoto is null) return;

        await inputPhoto.LoadAsync(useCache, null, OnPhotoLoadingProgressAsync, skipLoadingEvent);
    }


    /// <summary>
    /// Processes photo loading progress.
    /// </summary>
    private async Task OnPhotoLoadingProgressAsync(PhotoLoadingEventArgs e)
    {
        // previewing
        if (e.State == PhotoLoadingState.Loading)
        {
            //TODO:
            //await HandlePhotoPreviewAsync(e);
        }
        // load full resolution photo
        else
        {
            await HandlePhotoLoadedAsync(e);
        }
    }


    /// <summary>
    /// Handles previewing photo.
    /// </summary>
    private async Task HandlePhotoPreviewAsync(PhotoLoadingEventArgs e)
    {
        if (!ShouldLoadFullResolution) e.Photo.CancelLoading();

        // 1. skip the preview if it's not enable or in zoom lock mode
        if (!EnableImagePreview || ZoomMode == ZoomMode.LockZoom)
        {
            // raise event
            _isPreviewing.Clear();
            OnPhotoLoading(e);
            return;
        }


        SKBitmap? bmpPreview = null;
        SKImage? imgPreview = null;
        var token = _cancelPreview?.Token ?? default;
        var hasPreview = false;


        // 2. try to get the preview bitmap
        try
        {
            var previewHeight = Math.Min(DrawingArea.Height, e.Metadata.Height) / Dpi;

            // try to get photo preview
            bmpPreview = await Core.PreviewProvider!.GetPreviewAsync(e.Metadata, previewHeight, token);
            hasPreview = bmpPreview is not null;


            // cancel if requested
            if (token.IsCancellationRequested)
            {
                HandleCancelLoading();
                return;
            }

            if (hasPreview)
            {
                // get thumbnail size
                var prevWidth = bmpPreview?.Width ?? (int)e.Metadata.Width;
                var prevHeight = bmpPreview?.Height ?? (int)e.Metadata.Height;
                BitmapSize = new(prevWidth, prevHeight);


                // create new native bitmap for previewing off-thread
                imgPreview = SkiaCodec.ConvertToSKImage(bmpPreview);

                // cancel if requested
                if (token.IsCancellationRequested)
                {
                    HandleCancelLoading();
                    return;
                }

                // set preview source
                _imgPreview = imgPreview;
            }
        }
        catch
        {
            HandleCancelLoading();
        }
        finally
        {
            bmpPreview?.Dispose();
            bmpPreview = null;
        }


        // 3. calculate viewport of preview
        if (hasPreview)
        {
            var desiredSrcZoomFactor = CalculateZoomFactor(ZoomMode, e.Metadata.Width, e.Metadata.Height);
            var previewZoomFactor = desiredSrcZoomFactor;

            if (ZoomMode == ZoomMode.AutoZoom)
            {
                // if the source size is bigger than viewport,
                // fit the thumbnail to the viewport
                if (desiredSrcZoomFactor < 1)
                {
                    previewZoomFactor = CalculateZoomFactor(ZoomMode.ScaleToFit, BitmapSize.Width, BitmapSize.Height);
                }
                // both preview and source size are smaller than viewport
                else
                {
                    previewZoomFactor = 1;
                }
            }
            else
            {
                previewZoomFactor = CalculateZoomFactor(ZoomMode, BitmapSize.Width, BitmapSize.Height);
            }

            SetZoomFactor(previewZoomFactor, false);
        }


        // raise event
        _isPreviewing.Value = hasPreview;
        OnPhotoLoading(e);


        void HandleCancelLoading()
        {
            hasPreview = false;
            imgPreview?.Dispose();
            imgPreview = null;
        }
    }


    /// <summary>
    /// Handles loading photo.
    /// </summary>
    private async Task HandlePhotoLoadedAsync(PhotoLoadingEventArgs e)
    {
        if (!ShouldLoadFullResolution) return;


        // back up size of preview image
        var prevSize = BitmapSize;

        // source
        SKImage? bitmap = null;
        SkiaAnimator? animator = null;
        var hasSource = false;

        try
        {
            // check if photo error
            if (e.Photo.Error is not null)
            {
                HandleCancelLoaded(false);
                OnPhotoLoading(e);
                return;
            }

            // cancel if requested
            if (e.CancelToken.IsCancellationRequested)
            {
                HandleCancelLoaded(true);
                return;
            }


            // create the native bitmap
            if (e.Photo.Bitmap is not null)
            {
                // update bitmap size
                BitmapSize = e.Photo.Size;


                // native bitmap is a animated bitmap
                if (e.Photo.Bitmap is SkiaAnimator wicAnimator)
                {
                    animator = wicAnimator;
                    hasSource = true;
                }

                // native bitmap is a single-frame bitmap
                else
                {
                    // create GPU bitmap
                    bitmap = e.Photo.GetFrame();
                    hasSource = bitmap != null;
                }
            }


            // cancel if requested
            if (e.CancelToken.IsCancellationRequested)
            {
                HandleCancelLoaded(true);
                return;
            }


            // cancel the preview process
            CancelPreview();
            _imgPreview?.Dispose();
            _imgPreview = null;

            // update bitmap size after the preview is cancelled
            BitmapSize = e.Photo.Size;

            // calculate the source viewport to match with the preview
            if (hasSource)
            {
                // set the source
                _imgSource = bitmap;


                // set animator
                if (animator is not null)
                {
                    _animator = animator;
                    _animator.FrameChanged -= Animator_FrameChanged;
                    _animator.FrameChanged += Animator_FrameChanged;
                    _animator.Play();
                }


                // if user zoomed and panned the preview
                if (_isPreviewing.Value
                    && _zooming.IsManual
                    && ZoomMode != ZoomMode.LockZoom)
                {
                    var diffRatio = new Size(
                        prevSize.Width / BitmapSize.Width,
                        prevSize.Height / BitmapSize.Height);

                    // calculate new source rect
                    var newSrcRect = SrcRect;
                    newSrcRect = newSrcRect.WithX(newSrcRect.X / diffRatio.Width);
                    newSrcRect = newSrcRect.WithY(newSrcRect.Y / diffRatio.Height);
                    newSrcRect = newSrcRect.WithWidth(newSrcRect.Width / diffRatio.Width);
                    newSrcRect = newSrcRect.WithHeight(newSrcRect.Height / diffRatio.Height);

                    // update zoom source
                    SrcRect = newSrcRect.Normalize();
                    _zooming.Factor *= diffRatio.Width;
                    _zooming.ZoomedPoint = new();

                    // make sure all zoomed point and viewport are synced
                    // by manually applying a very small zoom factor
                    ZoomByDeltaToPoint(double.Epsilon, _zooming.ZoomedPoint, false);
                    _zooming.IsManual = false;

                    _isPreviewing.Clear();
                    Refresh(false);
                }
                else
                {
                    _isPreviewing.Clear();
                    Refresh(true);
                }
            }


            BHelper.GcCollect();
        }
        catch (Exception ex)
        {
            e.Photo.Error = ex; // the rendering error
            HandleCancelLoaded(false);
        }
        finally
        {
            SourceKind = hasSource ? PhotoSource.Native : PhotoSource.None;

            // raise event
            OnPhotoLoading(e);
        }


        void HandleCancelLoaded(bool userCancelled)
        {
            if (userCancelled) e.Photo.Unload();

            bitmap?.Dispose();
            bitmap = null;

            animator?.Dispose();
            animator = null;

            Refresh(userCancelled);
        }
    }


    /// <summary>
    /// Handles frame animation.
    /// </summary>
    private void Animator_FrameChanged(AnimatorImpl sender, AnimatorFrameChangedEventArgs e)
    {
        DisposeNativePhotoResources();

        // update the frame bitmap
        SourceKind = PhotoSource.Native;
        _imgSource = sender.GetRenderedFrameBitmap();

        InvalidateVisual();
    }





    #endregion // Control Methods


}
