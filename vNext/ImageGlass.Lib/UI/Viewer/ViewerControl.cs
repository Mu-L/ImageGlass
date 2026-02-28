/*
ImageGlass - A lightweight, versatile image viewer
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
using ImageGlass.Common.Extensions;
using ImageGlass.Common.OsApi;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.UI.Viewer.ZoomAndPan;
using SkiaSharp;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.UI.Viewer;

public partial class ViewerControl : PhControl
{
    private CancellationTokenSource? _cancelPreview;
    internal InterlockedBool _isPreviewing = new(false);
    internal InterlockedBool _isFirstDraw = new(false);
    internal PhotoLoadingOptions _loadingOptions = new();

    private Point? _lastMousePanPoint = null; // mouse panning


    // events
    public event TEventHandler<ViewerControl, PhotoLoadingEventArgs>? PhotoLoading;
    public event TEventHandler<ViewerControl, AnimatorFrameChangedEventArgs>? PhotoAnimatorFrameChanged;



    #region Public Properties

    /// <summary>
    /// Gets the drawing area.
    /// </summary>
    public Rect DrawingArea { get; private set; }


    /// <summary>
    /// Gets the current photo.
    /// </summary>
    public Photo? Photo { get; private set; }


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
    public InterlockedBool ShouldLoadFullResolution = new(true);


    /// <summary>
    /// Checks if the photo is animating.
    /// </summary>
    public bool IsImageAnimating { get; protected set; } = false;


    /// <summary>
    /// Gets the value indicates whether the color is inverted by <see cref="InvertColor(bool)"/>.
    /// </summary>
    public bool IsColorInverted { get; protected set; } = false;


    #endregion // Public Properties



    #region Override Methods

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Core.ColorProfileChanged += Core_ColorProfileChanged;

        RegisterTouchGestures();
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Core.ColorProfileChanged -= Core_ColorProfileChanged;

        UnregisterTouchGestures();

        DisposeCheckerboard();
        DisposeNativePhotoResources();
    }


    private void Core_ColorProfileChanged(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            // skip animated images
            if (_animator is not null) return;

            // dispose tile cache (will be rebuilt after next first draw)
            _mipmapCache?.Dispose();
            _mipmapCache = null;

            SKImageRef.ImageLease? srcLease = null;

            try
            {
                srcLease = _imgSource?.Acquire();
                var srcImage = srcLease?.Image;

                // apply new color space for source image
                if (TryApplySkiaColorSpace(srcImage, out var imgFrameColored))
                {
                    SKImageRef.Set(ref _imgSource, imgFrameColored);
                }
            }
            finally
            {
                srcLease?.Dispose();
            }

            // clear the render image
            SKImageRef.Set(ref _imgRender, null);
            InvalidateVisual();
        }
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

                // redraw the control on resizing
                var shouldResetZoom = Photo is not null && !_zooming.IsManual;
                Refresh(shouldResetZoom, false, true);
            });
        }
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var p = e.GetCurrentPoint(this);
        var requestRerender = false;

        // set the init point for panning
        if (p.Pointer.Type == PointerType.Mouse)
        {
            var canPanByMouse = !EnableSelection || e.Properties.IsMiddleButtonPressed;

            if (canPanByMouse)
            {
                _lastMousePanPoint = p.Position;
            }
            else if (EnableSelection)
            {
                requestRerender = OnSelectionBegin(p);
            }
        }

        // request re-render control
        if (requestRerender) InvalidateVisual();
    }


    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (CurrentSelectionAction != SelectionAction.None)
        {
            _ = OnSelectionEnd(true);
        }
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

        _zooming.ZoomedPoint = p.Position;


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
            requestRerender = OnSelectionUpdating(p.Position);
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
        lock (_lock)
        {
            _cancelPreview?.Cancel();
            _cancelPreview?.Dispose();
            _cancelPreview = new();
        }
    }


    /// <summary>
    /// Disposes the native photo resources.
    /// </summary>
    private void DisposeNativePhotoResources()
    {
        lock (_lock)
        {
            // dispose tile cache first (it holds a KeepAlive on the source)
            _mipmapCache?.Dispose();
            _mipmapCache = null;

            _animator?.Dispose();
            _animator = null;

            // dispose native bitmap
            SKImageRef.Set(ref _imgSource, null);
            SKImageRef.Set(ref _imgRender, null);
        }
    }


    /// <summary>
    /// Unloads the photo and cancels any ongoing loading operation.
    /// </summary>
    [MemberNotNull(nameof(_cancelPreview))]
    public void UnloadPhoto()
    {
        lock (_lock)
        {
            CancelPreview();
            StopAnimator();
            SourceKind = PhotoSource.None;
            Photo?.CancelLoading();
            Photo?.Unload();

            // reset
            AnimationSource = AnimationSources.None;
            ClearPhotoTransforms();
            _loadingOptions = new();
            _enablePanningVelocity = false;
            SetSourceSelection(new Rect(), false);
            BitmapSize = new();

            // dispose native resources of photo
            DisposeNativePhotoResources();
        }
    }


    /// <summary>
    /// Resets all photo transformation settings to their default values.
    /// </summary>
    public void ClearPhotoTransforms()
    {
        IsColorInverted = false;
    }


    /// <summary>
    /// Sets a photo to render.
    /// </summary>
    public async Task SetPhotoAsync(Photo? inputPhoto, PhotoLoadingOptions? options = null)
    {
        lock (_lock)
        {
            // unload current photo resources
            UnloadPhoto();

            if (inputPhoto is null)
            {
                Photo = null;
                Refresh(true);
                return;
            }


            SourceKind = PhotoSource.Native;
            _loadingOptions = options ?? new();
            _enablePanningVelocity = true;
            Photo = inputPhoto;
        }


        // photo is loaded, render full resolution directly from cache
        if (_loadingOptions.UseCache && inputPhoto.State == PhotoState.Loaded && ShouldLoadFullResolution)
        {
            var token = inputPhoto.CancelToken ?? default;
            await HandlePhotoLoadedAsync(new(PhotoState.Loaded, inputPhoto, token));
        }
        else
        {
            // force reload when quick browsing to render preview instead of full photo
            var useCache = _loadingOptions.UseCache && ShouldLoadFullResolution;
            await LoadPhotoAsync(
                useCache: useCache,
                skipLoadingEvent: !_loadingOptions.ResetZoom);
        }
    }


    /// <summary>
    /// Loads photo data and renders to the viewer
    /// </summary>
    public async Task LoadPhotoAsync(bool useCache, bool skipLoadingEvent)
    {
        if (Photo is null) return;

        await Photo.LoadAsync(useCache, OnPhotoLoadingProgressAsync, skipLoadingEvent);
    }


    /// <summary>
    /// Processes photo loading progress.
    /// </summary>
    private async Task OnPhotoLoadingProgressAsync(PhotoLoadingEventArgs e)
    {
        // previewing
        if (e.State == PhotoState.Preview)
        {
            await HandlePhotoPreviewAsync(e);
        }
        // load full resolution photo
        else
        {
            await HandlePhotoLoadedAsync(e);
        }
    }


    /// <summary>
    /// Handles previewing the current photo.
    /// </summary>
    private async Task HandlePhotoPreviewAsync(PhotoLoadingEventArgs e)
    {
        if (!ShouldLoadFullResolution) e.Photo.CancelLoading();

        // 1. skip the preview if it's not enable or in zoom lock mode
        if (!EnableImagePreview || ZoomMode == ZoomMode.LockZoom)
        {
            // raise event
            _isPreviewing.SetFalse();
            OnPhotoLoading(e);
            return;
        }


        SKImage? imgPreview = null;
        var token = _cancelPreview?.Token ?? default;
        var hasPreview = false;


        // 2. try to get the preview bitmap
        try
        {
            // try to get photo preview
            if (e.Photo.GalleryThumbnail is not null)
            {
                using var skBmp = SkiaCodec.FromBitmap(e.Photo.GalleryThumbnail);
                imgPreview = SkiaCodec.ToSKImage(skBmp);
            }
            else
            {
                var previewHeight = Math.Min(Math.Min(DrawingArea.Height, e.Metadata.Height), 700);
                imgPreview = await Core.PreviewProvider!.GetPreviewAsync(e.Metadata, previewHeight, token);
            }
            hasPreview = imgPreview is not null;

            // cancel if requested
            if (token.IsCancellationRequested)
            {
                HandleCancelLoading();
                return;
            }

            if (hasPreview)
            {
                // get thumbnail size
                var prevWidth = imgPreview?.Width ?? (int)e.Metadata.Width;
                var prevHeight = imgPreview?.Height ?? (int)e.Metadata.Height;
                BitmapSize = new(prevWidth, prevHeight);

                // cancel if requested
                if (token.IsCancellationRequested)
                {
                    HandleCancelLoading();
                    return;
                }
            }
        }
        catch
        {
            HandleCancelLoading();
        }


        // 3. calculate viewport of preview
        if (hasPreview)
        {
            lock (_lock)
            {
                // set preview source
                SKImageRef.Set(ref _imgSource, imgPreview);

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
        }


        // raise event
        _isPreviewing.Set(hasPreview);
        OnPhotoLoading(e);


        void HandleCancelLoading()
        {
            hasPreview = false;
            imgPreview?.Dispose();
            imgPreview = null;
        }
    }


    /// <summary>
    /// Handles rendering the current photo.
    /// </summary>
    private async Task HandlePhotoLoadedAsync(PhotoLoadingEventArgs e)
    {
        if (!ShouldLoadFullResolution) return;


        // 1. back up size of preview image
        var prevSize = BitmapSize;

        // source
        SKImage? imgFrame = null;
        SkiaAnimator? animator = null;
        var hasSource = false;

        try
        {
            // 2. check if photo error
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


            // 3. create the native bitmap
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
                    imgFrame = e.Photo.GetFrame(0);

                    // apply color space
                    if (TryApplySkiaColorSpace(imgFrame, out var imgFrameColored))
                    {
                        // don't dispose the clipboard photo
                        if (!e.Photo.IsClipboard)
                        {
                            imgFrame?.Dispose();
                        }

                        imgFrame = imgFrameColored;
                    }

                    hasSource = imgFrame != null;
                }
            }


            // cancel if requested
            if (e.CancelToken.IsCancellationRequested)
            {
                HandleCancelLoaded(true);
                return;
            }


            // 4. cancel the preview process
            CancelPreview();


            lock (_lock)
            {
                // update bitmap size after the preview is cancelled
                BitmapSize = e.Photo.Size;

                // 5. calculate the source viewport to match with the preview
                if (hasSource)
                {
                    // 5.1 set non-animated source
                    _isFirstDraw.SetTrue();
                    SKImageRef.Set(ref _imgSource, imgFrame);


                    // 5.2 set animator
                    if (animator is not null)
                    {
                        _animator?.FrameChanged -= Animator_FrameChanged;
                        _animator = animator;
                        _animator.FrameChanged += Animator_FrameChanged;
                        StartAnimator();
                    }


                    // 5.3 if user zoomed and panned the preview
                    if (_isPreviewing
                        && _zooming.IsManual
                        && ZoomMode != ZoomMode.LockZoom)
                    {
                        _isPreviewing.SetFalse();

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
                        _logicalSrcPoint = SrcRect.Position;
                        _zooming.Factor *= diffRatio.Width;
                        _zooming.ZoomedPoint = new();

                        // trigger zoom changed event
                        ZoomChanged?.Invoke(this, new ViewerZoomEventArgs()
                        {
                            ZoomFactor = _zooming.Factor,
                            IsManualZoom = false,
                            IsZoomModeChange = false,
                            IsPreviewingImage = _isPreviewing,
                            ChangeSource = ZoomChangeSource.Unknown,
                        });

                        Refresh(false);
                    }
                    else
                    {
                        _isPreviewing.SetFalse();
                        Refresh(_loadingOptions.ResetZoom);
                    }
                }
            }
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

            imgFrame?.Dispose();
            imgFrame = null;

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
        // update the frame bitmap
        var renderedFrame = sender.GetRenderedFrameBitmap(e.CurrentFrame);

        lock (_lock)
        {
            SourceKind = PhotoSource.Native;
            SKImageRef.Set(ref _imgSource, renderedFrame);
            SKImageRef.Set(ref _imgRender, renderedFrame, _imgSource);
        }

        InvalidateVisual();
        PhotoAnimatorFrameChanged?.Invoke(this, e);
    }


    /// <summary>
    /// Gets a rendered bitmap of the current image or the selected region.
    /// </summary>
    public SKBitmap? GetRenderedBitmap(bool selectionOnly = false)
    {
        SKImageRef.ImageLease? imgLease = null;
        Rect selectionRect;

        try
        {
            lock (_lock)
            {
                var imageRef = _imgRender ?? _imgSource;
                if (imageRef is null) return null;

                // Acquire a lease to keep the image alive while we copy pixels.
                imgLease = imageRef.Acquire();
                var leaseImage = imgLease?.Image;
                if (leaseImage is null || leaseImage.IsDisposed()) return null;
                if (selectionOnly && SourceSelection.IsEmpty) return null;

                // Determine the source rectangle to copy (in source image coords).
                selectionRect = selectionOnly
                    ? SourceSelection.Normalize()
                    : new Rect(0, 0, leaseImage.Width, leaseImage.Height);
            }

            // Validate the leased image again after exiting the lock.
            var img = imgLease?.Image;
            if (img is null || img.IsDisposed()) return null;

            // Intersect selection with actual image bounds to avoid out-of-range
            // reads and to handle partially out-of-bounds selections.
            var bounds = new Rect(0, 0, img.Width, img.Height);
            selectionRect = selectionRect.GetIntersection(bounds);
            if (selectionRect.IsEmpty) return null;

            // prepare output bitmap
            var rect = selectionRect.ToSKRectI();
            var info = new SKImageInfo(rect.Width, rect.Height, img.ColorType, img.AlphaType, img.ColorSpace);
            var bmpOutput = new SKBitmap(info);

            // copy the image pixels to the output bitmap
            if (!img.ReadPixels(info, bmpOutput.GetPixels(), bmpOutput.RowBytes, rect.Left, rect.Top))
            {
                bmpOutput.Dispose();
                return null;
            }

            return bmpOutput;
        }
        finally
        {
            imgLease?.Dispose();
        }
    }


    /// <summary>
    /// Start animating the image if it can animate.
    /// </summary>
    public void StartAnimator()
    {
        if (IsImageAnimating || _animator is null || SourceKind == PhotoSource.None)
            return;

        lock (_lock)
        {
            _animator.Play();
            IsImageAnimating = true;
        }
    }


    /// <summary>
    /// Stop animating the image.
    /// </summary>
    public void StopAnimator()
    {
        lock (_lock)
        {
            _animator?.Pause();
            IsImageAnimating = false;
        }
    }


    /// <summary>
    /// Attempts to apply the destination Skia color profile to the current photo.
    /// </summary>
    private bool TryApplySkiaColorSpace(SKImage? srcImage, out SKImage? output)
    {
        output = null;
        if (!Core.IsDestColorProfileSupported) return false;

        // if always apply color profile
        // or only apply color profile if there is an embedded profile
        if (Core.Config.ShouldUseColorProfileForAll || Photo?.Metadata?.SkiaColorSpace is not null)
        {
            // apply new color space for source image
            if (SkiaCodec.TryApplyColorSpace(srcImage, Core.DestColorProfile, out var imgFrameColored))
            {
                output = imgFrameColored;
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Inverts image colors.
    /// </summary>
    public bool InvertColor(bool requestRerender = true)
    {
        lock (_lock)
        {
            // do nothing for animated images or when there is no source
            if (_animator is not null) return false;

            var srcImage = (_imgRender ?? _imgSource)?.Image;
            var invertedImage = SkiaCodec.InvertImageColors(srcImage);
            if (invertedImage.IsDisposed()) return false;

            // update the render cache, keep _imgSource intact
            SKImageRef.Set(ref _imgRender, invertedImage);
            _mipmapCache?.Dispose();
            _mipmapCache = null;

            IsColorInverted = !IsColorInverted;
        }


        // render the transformation
        if (requestRerender)
        {
            Refresh(resetZoom: false);
        }

        return true;
    }


    /// <summary>
    /// Rotates the image.
    /// </summary>
    public bool RotateImage(double degree, bool requestRerender = true)
    {
        lock (_lock)
        {
            // do nothing for animated images or when there is no source
            if (_animator is not null) return false;

            var srcImage = (_imgRender ?? _imgSource)?.Image;
            var rotatedImage = SkiaCodec.RotateImage(srcImage, degree);
            if (rotatedImage.IsDisposed()) return false;

            // update the render cache, keep _imgSource intact
            SKImageRef.Set(ref _imgRender, rotatedImage);
            _mipmapCache?.Dispose();
            _mipmapCache = null;

            // update source size
            BitmapSize = new(rotatedImage.Width, rotatedImage.Height);
        }

        // render the transformation
        if (requestRerender)
        {
            Refresh();
        }

        return true;
    }


    /// <summary>
    /// Flips the image.
    /// </summary>
    public bool FlipImage(FlipOptions options, bool requestRerender = true)
    {
        lock (_lock)
        {
            // do nothing for animated images or when there is no source
            if (_animator is not null) return false;

            var srcImage = (_imgRender ?? _imgSource)?.Image;
            var flippedImage = SkiaCodec.FlipImage(srcImage, options);
            if (flippedImage.IsDisposed()) return false;

            // update the render cache, keep _imgSource intact
            SKImageRef.Set(ref _imgRender, flippedImage);
            _mipmapCache?.Dispose();
            _mipmapCache = null;
        }

        // render the transformation
        if (requestRerender)
        {
            Refresh(resetZoom: false);
        }

        return true;
    }


    /// <summary>
    /// Filters image color channels.
    /// </summary>
    public bool FilterColorChannels(ColorChannels colors, bool requestRerender = true)
    {
        lock (_lock)
        {
            // 1. do nothing for animated images or when there is no source
            if (_animator is not null) return false;

            var srcImage = _imgSource?.Image;
            if (srcImage.IsDisposed()) return false;


            // 2. reset render cache to start from original source
            SKImageRef.Set(ref _imgRender, null);
            _mipmapCache?.Dispose();
            _mipmapCache = null;


            // 3. skip filtering when all channels (RGBA) are selected
            if (!colors.HasFlag(ColorChannels.RGBA))
            {
                var filteredImage = SkiaCodec.FilterImageColorChannels(srcImage, colors);
                if (filteredImage.IsDisposed()) return false;

                SKImageRef.Set(ref _imgRender, filteredImage);
            }
        }


        // 4. render the transformation
        if (requestRerender)
        {
            Refresh(false);
        }

        return true;
    }


    #endregion // Control Methods


}
