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
using ImageGlass.WinNT.Common;
using ImageGlass.WinNT.Common.Photoing;
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


namespace ImageGlass.WinNT;

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

    private Progress<PhotoLoadingEventArgs> _loadingProgress;
    private bool _isPreviewing = false;
    private CancellationTokenSource? _cancelPreview;
    private readonly SemaphoreSlim _lockCancelPreview = new(1, 1);
    private readonly Lock _lockSource = new();
    private readonly Lock _lockPreview = new();

    private ImageInterpolation _interpolationScaleDown = ImageInterpolation.MultiSampleLinear;
    private ImageInterpolation _interpolationScaleUp = ImageInterpolation.NearestNeighbor;


    // control
    private Color _accentColor = Colors.Blue;
    private InputSystemCursorShape _cursor = InputSystemCursorShape.Arrow;



    public event EventHandler<EventArgs>? Error;


    public double FontSize { get; set; } = 13;
    public double FontSize_Dpi => this.DpiScale(FontSize);



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



    public VirtualViewerControl()
    {
        ManipulationMode = ManipulationModes.Scale
            | ManipulationModes.TranslateX | ManipulationModes.TranslateY
            | ManipulationModes.TranslateInertia;

        _loadingProgress = new(Photo_Loading);
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
        base.OnUnloaded();

        UnloadPhoto();
        DisposeCheckerboardBrushes();
    }


    [MemberNotNull(nameof(_cancelPreview))]
    [MemberNotNullWhen(false, nameof(_photo))]
    private void UnloadPhoto()
    {
        CancelPreview();

        // reset selection
        SetSourceSelection(Rect.Empty, false);
        SetBitmapSize(0, 0, false);

        // dispose animator
        if (_animator is not null)
        {
            _animator.FrameChanged -= Animator_FrameChanged;
            _animator.Unload();
            _animator = null;
        }

        // dispose native resources of photo
        DisposeNativePhotoResources();

        _photo = null;
    }


    private void DisposeNativePhotoResources()
    {
        // dispose preview bitmap
        _bmpPreview?.Dispose();
        _bmpPreview = null;

        // dispose native bitmap
        _bmpSource?.Dispose();
        _bmpSource = null;
    }


    protected virtual void OnError(EventArgs e)
    {
        Error?.Invoke(this, e);
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


        // debug
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
            """,
            "Consolas", FontSize_Dpi, DrawingArea, Colors.Magenta);


        //// draw zoomed point
        //var zoomPoint = DpiScale(_zooming.ZoomedPoint);
        //e.DrawEllipse(zoomPoint.X, zoomPoint.Y, 8f, Colors.White, Colors.Red, 3f);

        //// draw dest rect
        //e.DrawRectangle(_destRect, 0, Colors.Cyan);

        //// draw SwapChainSize
        //e.DrawRectangle(e.Sender.Bounds, 0, Colors.Yellow, Colors.Transparent, 3f);

        //// draw DrawingArea
        //e.DrawRectangle(DrawingArea, 0, Colors.GreenYellow, Colors.Transparent, 3f);

        base.OnRender(e);
    }


    protected virtual void DrawImageLayer(SwapChainCanvasRenderEventArgs e)
    {
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
        // draw preview bitmap
        else if (_bmpPreview is not null)
        {
            lock (_lockPreview)
            {
                if (_bmpPreview is not null)
                {
                    e.DrawBitmap(_bmpPreview, _destRect, _srcRect, (InterpolationMode)CurrentInterpolation);
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
    /// Cancels any ongoing preview operation.
    /// </summary>
    [MemberNotNull(nameof(_cancelPreview))]
    private void CancelPreview()
    {
        _cancelPreview?.Cancel();
        _cancelPreview?.Dispose();
        _cancelPreview = new();
    }




    public void SetPhoto(Photo? inputPhoto)
    {
        // unload current photo resources
        UnloadPhoto();

        // start loading new photo
        _photo = inputPhoto;

        if (_photo is null)
        {
            Refresh(true);
            return;
        }

        // photo is cached
        if (_photo.IsDone)
        {
            _ = HandlePhotoLoadedAsync(new PhotoLoadingEventArgs(_photo));
        }
        // photo is not cached
        else
        {
            _ = _photo.LoadAsync(true, null, _loadingProgress);
        }
    }


    private async void Photo_Loading(PhotoLoadingEventArgs e)
    {
        // previewing
        if (!e.IsDone)
        {
            await HandlePhotoPreviewAsync(e, _cancelPreview?.Token ?? default);
        }
        // load full resolution photo
        else
        {
            //await Task.Run(async () =>
            //{
            //    await Task.Delay(5000);
            //    await HandlePhotoLoadedAsync(e);
            //});
            await HandlePhotoLoadedAsync(e);
        }
    }



    private async Task HandlePhotoPreviewAsync(PhotoLoadingEventArgs e, CancellationToken token)
    {
        try
        {
            // cancel if requested
            token.ThrowIfCancellationRequested();

            var previewHeight = Math.Min(DrawingArea.Height, e.Metadata.Height) / DpiY;

            // try to get photo preview
            using var wicThumb = await e.Metadata.GetPreviewAsync(previewHeight, token);
            _isPreviewing = true;


            if (wicThumb is not null)
            {
                // get thumbnail size
                var prevWidth = wicThumb?.Size.Width ?? (int)e.Metadata.Width;
                var prevHeight = wicThumb?.Size.Height ?? (int)e.Metadata.Height;
                SetBitmapSize(prevWidth, prevHeight, true);


                // cancel if requested
                token.ThrowIfCancellationRequested();

                _bmpPreview = PhotoWIC.CreateD2dBitmap(wicThumb, D2dContext);
            }
            else
            {
                _isPreviewing = false;
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Log.Info($"{nameof(HandlePhotoPreviewAsync)}: Cancelled previewing {e.Metadata.FilePath}");

            _bmpPreview?.Dispose();
            _bmpPreview = null;
            _isPreviewing = false;
        }
        catch (Exception ex)
        {
            Log.Error(ex);

            _bmpPreview?.Dispose();
            _bmpPreview = null;
            _isPreviewing = false;
        }


        // calculate viewport of preview
        if (_isPreviewing)
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
        await _lockCancelPreview.WaitAsync();

        // back up size of preview image
        var prevSize = BitmapSize;
        var hasSource = false;

        try
        {
            // cancel the preview process
            CancelPreview();

            Log.Info($"{nameof(HandlePhotoLoadedAsync)}: Loading full resolution photo...");
            if (e.Photo.Bitmap is null) return;

            // update bitmap size
            SetBitmapSize(e.Photo.Size.ToSize(), true);


            // native bitmap is a single-frame bitmap
            if (e.Photo.Bitmap is IWICBitmapSource bmpSrc)
            {
                // create new native bitmap
                _bmpSource = await Task
                  .Run(() => PhotoWIC.CreateD2dBitmap(bmpSrc, D2dContext))
                  .ConfigureAwait(true);

                hasSource = _bmpSource != null;
            }
            // native bitmap is a animated bitmap
            else if (e.Photo.Bitmap is WicAnimator animator)
            {
                _animator = animator;
                _animator.FrameChanged += Animator_FrameChanged;
                _animator.Initialize(D2dContext);
                _animator.Play();

                hasSource = true;
            }
            // native bitmap is a multi-frame bitmap
            else if (e.Photo.Bitmap is IWICBitmapDecoder decoder)
            {
                using var frame = decoder.GetFrame(0);

                _bmpSource = await Task
                  .Run(() => PhotoWIC.CreateD2dBitmap(frame, D2dContext))
                  .ConfigureAwait(true);

                hasSource = _bmpSource != null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);

            _bmpSource?.Dispose();
            _bmpSource = null;
        }
        finally
        {
            _lockCancelPreview.Release();
        }


        // calculate the source viewport to match with the preview
        if (hasSource)
        {
            // apply color effect
            _bmpSource = ApplyColorManagementEffect(_bmpSource);


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

                _bmpPreview?.Dispose();
                _bmpPreview = null;
            }
            else
            {
                _isPreviewing = false;
                Refresh(true);
            }
        }


        BHelper.GcCollect();
    }

    private void Animator_FrameChanged(AnimatorImpl sender, AnimatorFrameChangedEventArgs e)
    {
        DisposeNativePhotoResources();

        _bmpSource = (sender as WicAnimator)!.GetRenderedFrameBitmap1();
        Invalidate();
    }

    private ID2D1Bitmap1? ApplyColorManagementEffect(ID2D1Bitmap1? bmpD2)
    {
        if (_photo == null) return bmpD2;

        // no embedded color profile
        if (_photo.Metadata.ColorSpace == ImageMagick.ColorSpace.CMYK
            || _photo.Metadata.ColorProfileData is null) return bmpD2;

        Log.Info($"{nameof(ApplyColorManagementEffect)}: Applying color management effect...");

        // create color management effect
        using var colorEffect = new ColorManagement(D2dContext);
        colorEffect.SetInput(0, _bmpSource, false);


        // create destination color context
        ID2D1ColorContext? destColorContext = null;
        if (WindowColorProfileProvider.Instance.Data != null)
        {
            destColorContext = D2dContext.CreateColorContext(ColorSpace.Custom, WindowColorProfileProvider.Instance.Data);
        }
        //else if (_photo.PixelFormatInfo?.NumericRepresentation == PixelFormatNumericRepresentation.PixelFormatNumericRepresentationFloat)
        //{
        //    destColorContext = D2dContext.CreateColorContext(ColorSpace.ScRgb, []);
        //}
        else
        {
            destColorContext = D2dContext.CreateColorContext(ColorSpace.Srgb, []);
        }

        // create source color context
        using var srcColorContext = D2dContext.CreateColorContext(ColorSpace.Custom, _photo.Metadata.ColorProfileData);


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
