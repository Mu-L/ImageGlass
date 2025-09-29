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
using D2Phap.Canvas2D;
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.WIC;
using Windows.Foundation;
using Windows.UI;


namespace ImageGlass.UI;

public partial class VirtualViewerControl : SwapChainCanvas
{
    // drawing image
    private Photo? _photo;
    private Size _bitmapSize = new Size();
    private ID2D1Bitmap1? _bmpSource;
    private ID2D1Bitmap1? _bmpPreview;
    private WicAnimator? _animator;
    private Rect _srcRect = new();
    private Rect _destRect = new();

    private readonly Lock _lockSource = new();
    private readonly Lock _lockPreview = new();
    private ImageInterpolation _interpolationScaleDown = ImageInterpolation.MultiSampleLinear;
    private ImageInterpolation _interpolationScaleUp = ImageInterpolation.NearestNeighbor;

    // loading
    private CancellationTokenSource? _cancelPreview;
    private bool _isPreviewing = false;

    // control
    private Color _accentColor = Colors.Blue;
    private InputSystemCursorShape _cursor = InputSystemCursorShape.Arrow;


    #region Public Properties

    public double FontSize { get; set; } = 13;
    public double FontSize_Dpi => this.DpiScale(FontSize);


    /// <summary>
    /// Gets, sets value indicates whether full resolution is loaded or not.
    /// </summary>
    public bool ShouldLoadFullResolution
    {
        get; set
        {
            if (field != value)
            {
                field = value;

                if (value is true && _photo is not null)
                {
                    // load full resolution, skip loading event
                    _ = LoadPhotoAsync(_photo, true);
                }
            }
        }
    }


    /// <summary>
    /// Gets the drawing area.
    /// </summary>
    public Rect DrawingArea => new(
        Padding.Left,
        Padding.Top,
        Math.Max(0, Bounds_Dpi.Width - Padding.Left - Padding.Right),
        Math.Max(0, Bounds_Dpi.Height - Padding.Top - Padding.Bottom));


    /// <summary>
    /// Gets the size of the bitmap.
    /// Use <c><see cref="SetBitmapSize(Size, bool)"/></c> to set the value.
    /// </summary>
    public Size BitmapSize => _bitmapSize;


    /// <summary>
    /// Checks if the width or height of the bitmap exceeds the maximum allowed hardware dimensions.
    /// </summary>
    private bool IsNativeBitmapSizeExceeded => _bitmapSize.Width > MAX_HARDWARE_BITMAP_DIMENSION
        || _bitmapSize.Height > MAX_HARDWARE_BITMAP_DIMENSION;


    /// <summary>
    /// Gets, sets interpolation mode used when the
    /// <see cref="ZoomFactor"/> is less than or equal <c>1.0f</c>.
    /// </summary>
    public ImageInterpolation InterpolationScaleDown
    {
        get => _interpolationScaleDown;
        set
        {
            if (_interpolationScaleDown != value)
            {
                _interpolationScaleDown = value;
                Invalidate();
            }
        }
    }


    /// <summary>
    /// Gets, sets interpolation mode used when the
    /// <see cref="ZoomFactor"/> is greater than <c>1.0f</c>.
    /// </summary>
    public ImageInterpolation InterpolationScaleUp
    {
        get => _interpolationScaleUp;
        set
        {
            if (_interpolationScaleUp != value)
            {
                _interpolationScaleUp = value;
                Invalidate();
            }
        }
    }


    /// <summary>
    /// Gets the current <see cref="ImageInterpolation"/> mode.
    /// </summary>
    public ImageInterpolation CurrentInterpolation
    {
        get
        {
            if (ZoomFactor < 1f) return _interpolationScaleDown;
            if (ZoomFactor > 1f) return _interpolationScaleUp;

            return ImageInterpolation.NearestNeighbor;
        }
    }


    /// <summary>
    /// Gets or sets the cursor shape for the input system.
    /// </summary>
    public InputSystemCursorShape Cursor
    {
        get => _cursor;
        set
        {
            if (_cursor != value)
            {
                _cursor = value;

                ProtectedCursor?.Dispose();
                ProtectedCursor = null;

                ProtectedCursor = InputSystemCursor.Create(_cursor);
            }
        }
    }


    /// <summary>
    /// Gets, sets accent color.
    /// </summary>
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            //if (Web2 != null) Web2.AccentColor = _accentColor;
        }
    }

    #endregion // Public Properties


    public VirtualViewerControl()
    {
        ManipulationMode = ManipulationModes.Scale
            | ManipulationModes.TranslateX | ManipulationModes.TranslateY
            | ManipulationModes.TranslateInertia;
    }


    protected override void OnDeviceChanged(DeviceChangeReason reason)
    {
        base.OnDeviceChanged(reason);

        if (reason == DeviceChangeReason.Direct2DResized)
        {
            // set new device context for animator
            _animator?.SetDeviceContext(D2dContext);
        }
        else
        {
            // dispose native resources of photo
            DisposeNativePhotoResources();
            DisposeCheckerboardBrushes();
        }
    }


    protected override void OnUnloaded()
    {
        UnloadPhoto();
        DisposeCheckerboardBrushes();

        base.OnUnloaded();
    }


    private void DisposeNativePhotoResources()
    {
        // flush pending D3D11 context
        _d3dContext?.ClearState();
        _d3dContext?.Flush();

        // dispose preview bitmap
        _bmpPreview?.Dispose();
        _bmpPreview = null;

        // dispose native bitmap
        _bmpSource?.Dispose();
        _bmpSource = null;
    }


    protected override void OnResized(SizeChangedEventArgs e)
    {
        base.OnResized(e);
        if (e.NewSize.IsEmpty()) return;

        // update drawing regions
        CalculateDrawingRegion();

        // redraw the control on resizing if it's not manual zoom
        if (_photo != null && !_zooming.IsManual)
        {
            Refresh(true, false, true);
        }
    }


    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        var cursor = e.GetCurrentPoint(this);
        var requestRerender = OnSelectionBegin(cursor);

        // request re-render control
        if (requestRerender) Invalidate();
    }


    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        var pointer = e.GetCurrentPoint(this);
        var requestRerender = OnSelectionUpdating(pointer);

        // request re-render control
        if (requestRerender) Invalidate();
    }


    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        OnSelectionEnd(true);
    }


    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) Invalidate();

        base.OnPointerReleased(e);
    }


    protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    {
        var requestRerender = OnSelectionEnd(false);
        if (requestRerender) Invalidate();

        base.OnPointerCanceled(e);
    }


    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var pPoint = e.GetCurrentPoint(this);
        var delta = pPoint.Properties.MouseWheelDelta;
        var position = pPoint.Position;
        var isUsingTouchpad = Math.Abs(delta) != 120;


        // Touchpad scrolling
        if (isUsingTouchpad)
        {
            // Horizontal mouse wheel
            if (pPoint.Properties.IsHorizontalMouseWheel)
            {
                _ = PanTo(delta, 0, position);
                return;
            }
            //// Vertical mouse wheel
            //else
            //{
            //    _ = PanTo(0, -delta, position);
            //    return;
            //}
        }

        // Zooming
        _ = ZoomByDeltaToPoint(delta, position);
    }


    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        base.OnDoubleTapped(e);

        // Touch double-tap:
        // enable double-tapping for drawing selection
        OnSelectionBeginWithTouch(e);
    }


    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        // 1. check if the manipulation is a pinch gesture
        var isPintching = e.Delta.Expansion != 0;

        // 2. check if we're drawing selection
        if (CurrentSelectionAction != SelectionAction.None && !isPintching)
        {
            // stop velocity
            e.Complete();
        }
        else
        {
            // 2. perform panning
            PanTo(-e.Delta.Translation.X, -e.Delta.Translation.Y, e.Position, !isPintching);


            // 3. perform zooming for Pinch gesture
            if (isPintching)
            {
                var scaleDelta = e.Delta.Expansion * 8;
                _ = ZoomByDeltaToPoint(scaleDelta, e.Position);
            }

            // stop velocity if requested
            if (!_enablePanningVelocity) e.Complete();
        }

        e.Handled = true;
    }


    protected override void OnRender(SwapChainCanvasRenderEventArgs e)
    {
        // draw checkerboard
        DrawCheckerboardLayer(e);


        // draw image
        DrawImageLayer(e);


        // Draw selection layer
        OnSelectionDrawing(e);


        base.OnRender(e);


        // debug
        if (!EnableDebug) return;
        e.DrawText(
            $"""
            Control Size = {ActualWidth} x {ActualHeight}
            DrawingArea = {DrawingArea}
            Image size = {BitmapSize}
            _srcRect = {_srcRect}
            _destRect = {_destRect}
            _zoomFactor = {_zooming.Factor}
            _zoomedPoint = {_zooming.ZoomedPoint}
            _isPreviewing = {_isPreviewing}
            ERROR = {_photo?.Error?.ToString()}
            """,
            "Consolas", FontSize_Dpi, DrawingArea, Colors.Magenta);


        //// draw SwapChainSize
        //e.DrawRectangle(e.Sender.Bounds, 0, Colors.Yellow, Colors.Transparent, 3f);

        //// draw DrawingArea
        //e.DrawRectangle(DrawingArea, 0, Colors.GreenYellow, Colors.Transparent, 3f);

        //// draw zoomed point
        //var zoomPoint = DpiScale(_zooming.ZoomedPoint);
        //e.DrawEllipse(zoomPoint.X, zoomPoint.Y, 8f, Colors.White, Colors.Red, 3f);

        //// draw dest rect
        //e.DrawRectangle(_destRect, 0, Colors.Cyan);
    }


    protected virtual void DrawImageLayer(SwapChainCanvasRenderEventArgs e)
    {
        // draw preview bitmap
        if (_bmpPreview is not null)
        {
            lock (_lockPreview)
            {
                if (_bmpPreview is not null)
                {
                    e.DrawBitmap(_bmpPreview, _destRect, _srcRect, (InterpolationMode)CurrentInterpolation);
                }
            }
        }

        // draw full resolution bitmap
        if (_bmpSource is not null)
        {
            lock (_lockSource)
            {
                if (_bmpSource is not null)
                {
                    e.DrawBitmap(_bmpSource, _destRect, _srcRect, (InterpolationMode)CurrentInterpolation);
                }
            }
        }
    }



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
        if (resetZoom)
        {
            SetZoomMode(null, isManualZoom, zoomedByResizing);
        }

        Invalidate();
    }



    /// <summary>
    /// Sets value for <c><see cref="BitmapSize"/></c>.
    /// </summary>
    private void SetBitmapSize(double width, double height, bool updateAcceleration)
    {
        SetBitmapSize(new Size(width, height), updateAcceleration);
    }


    /// <summary>
    /// Sets value for <c><see cref="BitmapSize"/></c>.
    /// </summary>
    private void SetBitmapSize(Size value, bool updateAcceleration)
    {
        if (_bitmapSize.Width != value.Width || _bitmapSize.Height != value.Height)
        {
            _bitmapSize = value;

            if (updateAcceleration)
            {
                UseHardwareAcceleration = !IsNativeBitmapSizeExceeded;
            }
        }
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
    /// Unloads the photo and cancels any ongoing loading operation.
    /// </summary>
    [MemberNotNull(nameof(_cancelPreview))]
    public void UnloadPhoto()
    {
        CancelPreview();
        _photo?.CancelLoading();
        _photo?.Unload();

        // reset selection
        _enablePanningVelocity = false;
        SetSourceSelection(Rect.Empty, false);
        SetBitmapSize(0, 0, false);

        // dispose native resources of photo
        DisposeNativePhotoResources();
    }


    /// <summary>
    /// Sets a photo to render.
    /// </summary>
    public void SetPhoto(Photo? inputPhoto)
    {
        // unload current photo resources
        UnloadPhoto();

        if (inputPhoto is null)
        {
            _photo = null;
            Refresh(true);
            return;
        }


        // photo is cached
        if (inputPhoto.IsDone)
        {
            var token = inputPhoto.CancelToken ?? default;
            _ = HandlePhotoLoadedAsync(new PhotoLoadingEventArgs(true, inputPhoto, token));
        }
        // photo is not cached
        else
        {
            _ = LoadPhotoAsync(inputPhoto);
        }

        _enablePanningVelocity = true;
        _photo = inputPhoto;
    }


    private async Task LoadPhotoAsync(Photo? inputPhoto, bool skipLoadingEvent = false)
    {
        if (inputPhoto is null) return;

        var loadingProgress = new Progress<PhotoLoadingEventArgs>(Photo_Loading);
        await inputPhoto.LoadAsync(true, null, loadingProgress, skipLoadingEvent);
    }


    private async void Photo_Loading(PhotoLoadingEventArgs e)
    {
        // previewing
        if (!e.IsDone)
        {
            await HandlePhotoPreviewAsync(e);
        }
        // load full resolution photo
        else
        {
            await HandlePhotoLoadedAsync(e);
        }
    }



    private async Task HandlePhotoPreviewAsync(PhotoLoadingEventArgs e)
    {
        if (!ShouldLoadFullResolution) e.Photo.CancelLoading();

        IWICBitmapSource? wicThumb = null;
        ID2D1Bitmap1? previewBitmap = null;

        var token = _cancelPreview?.Token ?? default;
        var hasPreview = false;


        try
        {
            var previewHeight = Math.Min(DrawingArea.Height, e.Metadata.Height) / DpiY;

            // try to get photo preview
            wicThumb = await e.Metadata.GetPreviewAsync(previewHeight, token);
            hasPreview = !wicThumb.IsDisposed();


            // cancel if requested
            if (token.IsCancellationRequested)
            {
                HandleCancelLoading();
                return;
            }

            if (hasPreview)
            {
                // get thumbnail size
                var prevWidth = wicThumb?.Size.Width ?? (int)e.Metadata.Width;
                var prevHeight = wicThumb?.Size.Height ?? (int)e.Metadata.Height;
                SetBitmapSize(prevWidth, prevHeight, true);


                // create new native bitmap for previewing off-thread
                previewBitmap = await wicThumb.ToD2BitmapAsync(_d3dDevice!, D2dContext);

                // cancel if requested
                if (token.IsCancellationRequested)
                {
                    HandleCancelLoading();
                    return;
                }

                //set preview source
                _bmpPreview = previewBitmap;
            }
        }
        catch
        {
            HandleCancelLoading();
        }
        finally
        {
            wicThumb?.Dispose();
            wicThumb = null;
        }


        void HandleCancelLoading()
        {
            hasPreview = false;
            previewBitmap?.Dispose();
            previewBitmap = null;
        }

        // calculate viewport of preview
        _isPreviewing = hasPreview;

        if (hasPreview)
        {
            var desiredSrcZoomFactor = CalculateZoomFactor(ZoomMode, e.Metadata.Width, e.Metadata.Height);

            // if the source size is bigger than viewport
            if (desiredSrcZoomFactor < 1)
            {
                // fit the thumbnail to the viewport
                var fitZoomFactor = CalculateZoomFactor(ZoomMode.ScaleToFit, BitmapSize.Width, BitmapSize.Height);

                SetZoomFactor(fitZoomFactor, false);
            }
            // both preview and source size are smaller than viewport
            else
            {
                SetZoomFactor(1, false);
            }

            Refresh(false);
        }
    }


    private async Task HandlePhotoLoadedAsync(PhotoLoadingEventArgs e)
    {
        if (!ShouldLoadFullResolution) return;


        // back up size of preview image
        var prevSize = BitmapSize;

        // source
        ID2D1Bitmap1? bitmap = null;
        WicAnimator? animator = null;
        var hasSource = false;

        try
        {
            // cancel if requested
            if (e.CancelToken.IsCancellationRequested)
            {
                HandleCancellLoaded(true);
                return;
            }


            // create the native bitmap
            if (e.Photo.Bitmap is not null)
            {
                // update bitmap size, check if we can use the hardware acceleration
                SetBitmapSize(e.Photo.Size.ToSize(), true);


                // native bitmap is a animated bitmap
                if (e.Photo.Bitmap is WicAnimator wicAnimator)
                {
                    animator = wicAnimator;
                    hasSource = true;
                }

                // native bitmap is a single-frame bitmap
                else
                {
                    // convert WIC bitmap to D2 bitmap off-thread
                    bitmap = await e.Photo.GetD2BitmapAsync(_d3dDevice!, D2dContext);
                    hasSource = bitmap != null;
                }
            }


            // cancel if requested
            if (e.CancelToken.IsCancellationRequested)
            {
                HandleCancellLoaded(true);
                return;
            }

            if (e.Photo.Error is not null) throw e.Photo.Error;

            // cancel the preview process
            CancelPreview();
            _bmpPreview?.Dispose();
            _bmpPreview = null;

            // update bitmap size after the preview is cancelled
            SetBitmapSize(e.Photo.Size.ToSize(), true);

            // calculate the source viewport to match with the preview
            if (hasSource)
            {
                // set the source
                _bmpSource = bitmap;
                _bmpSource = ApplyColorManagementEffect(_bmpSource, e.Photo);


                // set animator
                if (animator is not null)
                {
                    _animator = animator;
                    _animator.FrameChanged -= Animator_FrameChanged;
                    _animator.FrameChanged += Animator_FrameChanged;
                    _animator.Initialize(D2dContext);
                    _animator.Play();
                }


                if (_isPreviewing)
                {
                    var diffRatio = new Size(
                        prevSize.Width / BitmapSize.Width,
                        prevSize.Height / BitmapSize.Height);

                    // calculate new source rect
                    var newSrcRect = _srcRect;
                    newSrcRect.X /= diffRatio.Width;
                    newSrcRect.Y /= diffRatio.Height;
                    newSrcRect.Width /= diffRatio.Width;
                    newSrcRect.Height /= diffRatio.Height;

                    // update zoom source
                    _srcRect = newSrcRect.Safe();
                    _zooming.Factor *= diffRatio.Width;

                    // make sure all zoomed point and viewport are synced
                    // by manually applying a very small zoom factor
                    var isManualZoom = _zooming.IsManual;
                    ZoomByDeltaToPoint(double.Epsilon, _zooming.ZoomedPoint, false);
                    _zooming.IsManual = isManualZoom;

                    _isPreviewing = false;
                    Refresh(!isManualZoom);
                }
                else
                {
                    _isPreviewing = false;
                    Refresh(true);
                }
            }


            BHelper.GcCollect();
        }
        catch (Exception ex)
        {
            HandleCancellLoaded(false);
        }


        void HandleCancellLoaded(bool userCancelled)
        {
            if (userCancelled) e.Photo.Unload();

            bitmap?.Dispose();
            bitmap = null;

            animator?.Dispose();
            animator = null;
        }
    }


    private void Animator_FrameChanged(AnimatorImpl sender, AnimatorFrameChangedEventArgs e)
    {
        DisposeNativePhotoResources();

        // update the frame bitmap
        _bmpSource = (sender as WicAnimator)!.GetRenderedFrameBitmap1();
        Invalidate();
    }


    private ID2D1Bitmap1? ApplyColorManagementEffect(ID2D1Bitmap1? bmpD2, Photo? photo)
    {
        if (photo is null) return bmpD2;

        // no embedded color profile
        if (photo.Metadata.ColorSpace == ImageMagick.ColorSpace.CMYK
            || photo.Metadata.ColorProfileData is null) return bmpD2;


        // create color management effect
        using var colorEffect = new ColorManagement(D2dContext);
        colorEffect.SetInput(0, _bmpSource, false);


        // create destination color context
        ID2D1ColorContext? destColorContext = null;
        if (AP.ColorProfileService.Data != null)
        {
            destColorContext = D2dContext.CreateColorContext(ColorSpace.Custom, AP.ColorProfileService.Data);
        }
        //else if (photo.PixelFormatInfo?.NumericRepresentation == PixelFormatNumericRepresentation.PixelFormatNumericRepresentationFloat)
        //{
        //    destColorContext = D2dContext.CreateColorContext(ColorSpace.ScRgb, []);
        //}
        else
        {
            destColorContext = D2dContext.CreateColorContext(ColorSpace.Srgb, []);
        }

        // create source color context
        using var srcColorContext = D2dContext.CreateColorContext(ColorSpace.Custom, photo.Metadata.ColorProfileData);


        // set color effect
        colorEffect.SourceColorContext = srcColorContext;
        colorEffect.DestinationColorContext = destColorContext;


        var newBmpD2 = colorEffect.GetD2D1Bitmap1(D2dContext);

        // dispose resources
        bmpD2?.Dispose();
        bmpD2 = null;
        srcColorContext?.Dispose();
        destColorContext?.Dispose();

        return newBmpD2;
    }


}
