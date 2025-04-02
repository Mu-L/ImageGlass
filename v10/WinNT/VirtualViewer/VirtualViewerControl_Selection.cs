using D2Phap.Canvas2D;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Vortice;
using Windows.Foundation;
using Windows.Globalization.Fonts;
using WinRT;
using static System.Net.Mime.MediaTypeNames;

namespace ImageGlass.WinNT;


public partial class VirtualViewerControl
{
    /// <summary>
    /// Occurs when the <see cref="ClientSelection"/> is changed.
    /// </summary>
    public event EventHandler<SelectionEventArgs>? SelectionChanged;


    /// <summary>
    /// Gets the client selection area.
    /// </summary>
    public Rect ClientSelection => RectSourceToClient(_sourceSelection);


    /// <summary>
    /// Gets, sets the image source selection. This will emit the event <see cref="SelectionChanged"/>.
    /// Use <see cref="SetSourceSelection"/> to control the selection event.
    /// </summary>
    public Rect SourceSelection
    {
        get => _sourceSelection;
        set => SetSourceSelection(value, true);
    }


    /// <summary>
    /// Gets the resizers of the selection rectangle
    /// </summary>
    public List<SelectionResizer> SelectionResizers
    {
        get
        {
            if (SourceSelection.IsEmpty()) return [];

            var resizerSize = DpiScale(12f);
            var resizerMargin = DpiScale(2f);
            var hitSize = DpiScale(resizerSize * 1.3f);


            // top left
            var topLeft = new Rect(
                ClientSelection.X + resizerMargin,
                ClientSelection.Y + resizerMargin,
                resizerSize, resizerSize);
            var topLeftHit = new Rect(
                ClientSelection.X - hitSize / 2 + resizerSize / 2,
                ClientSelection.Y - hitSize / 2 + resizerSize / 2,
                hitSize, hitSize);


            // top right
            var topRight = new Rect(
                ClientSelection.Right - resizerSize - resizerMargin,
                ClientSelection.Y + resizerMargin,
                resizerSize, resizerSize);
            var topRightHit = new Rect(
                ClientSelection.Right - hitSize / 2 - resizerSize / 2,
                ClientSelection.Y - hitSize / 2 + resizerSize / 2,
                hitSize, hitSize);


            // bottom left
            var bottomLeft = new Rect(
                ClientSelection.X + resizerMargin,
                ClientSelection.Bottom - resizerSize - resizerMargin,
                resizerSize, resizerSize);
            var bottomLeftHit = new Rect(
                ClientSelection.X - hitSize / 2 + resizerSize / 2,
                ClientSelection.Bottom - hitSize / 2 - resizerSize / 2,
                hitSize, hitSize);


            // bottom right
            var bottomRight = new Rect(
                ClientSelection.Right - resizerSize - resizerMargin,
                ClientSelection.Bottom - resizerSize - resizerMargin,
                resizerSize, resizerSize);
            var bottomRightHit = new Rect(
                ClientSelection.Right - hitSize / 2 - resizerSize / 2,
                ClientSelection.Bottom - hitSize / 2 - resizerSize / 2,
                hitSize, hitSize);


            // top
            var top = new Rect(
                ClientSelection.X + ClientSelection.Width / 2 - resizerSize / 2,
                ClientSelection.Y + resizerMargin,
                resizerSize, resizerSize);
            var topHit = new Rect(
                topLeftHit.Right,
                ClientSelection.Y - hitSize / 2 + resizerSize / 2,
                Math.Abs(topRightHit.X - topLeftHit.Right), hitSize);


            // right
            var right = new Rect(
                    ClientSelection.Right - resizerSize - resizerMargin,
                    ClientSelection.Y + ClientSelection.Height / 2 - resizerSize / 2,
                    resizerSize, resizerSize);
            var rightHit = new Rect(
                    ClientSelection.Right - hitSize / 2 - resizerSize / 2,
                    topRightHit.Bottom,
                    hitSize, Math.Abs(bottomRightHit.Y - topRightHit.Bottom));


            // bottom
            var bottom = new Rect(
                    ClientSelection.X + ClientSelection.Width / 2 - resizerSize / 2,
                    ClientSelection.Bottom - resizerSize - resizerMargin,
                    resizerSize, resizerSize);
            var bottomHit = new Rect(
                    bottomLeftHit.Right,
                    ClientSelection.Bottom - hitSize / 2 - resizerSize / 2,
                    Math.Abs(bottomRightHit.X - bottomLeftHit.Right), hitSize);


            // left
            var left = new Rect(
                    ClientSelection.X + resizerMargin,
                    ClientSelection.Y + ClientSelection.Height / 2 - resizerSize / 2,
                    resizerSize, resizerSize);
            var leftHit = new Rect(
                    ClientSelection.X - hitSize / 2 + resizerSize / 2,
                    topLeftHit.Bottom,
                    hitSize, Math.Abs(bottomLeftHit.Y - topLeftHit.Bottom));


            // 8 resizers
            return [
                // bottom-right is in higher layer
                new(SelectionResizerType.BottomRight, bottomRight, bottomRightHit),
                new(SelectionResizerType.TopLeft, topLeft, topLeftHit),
                new(SelectionResizerType.BottomLeft, bottomLeft, bottomLeftHit),
                new(SelectionResizerType.TopRight, topRight, topRightHit),

                new(SelectionResizerType.Right, right, rightHit),
                new(SelectionResizerType.Bottom, bottom, bottomHit),
                new(SelectionResizerType.Left, left, leftHit),
                new(SelectionResizerType.Top, top, topHit),
            ];
        }
    }


    /// <summary>
    /// Gets, sets selection aspect ratio.
    /// If Width or Height is <c>less than or equals 0</c>, we will use free aspect ratio.
    /// </summary>
    public Size SelectionAspectRatio { get; set; } = new();


    /// <summary>
    /// Enables or disables the selection.
    /// </summary>
    public bool EnableSelection
    {
        get => _enableSelection;
        set
        {
            _enableSelection = value;
            if (!_enableSelection && Parent != null)
            {
                Cursor = InputSystemCursorShape.Arrow;
            }
        }
    }


    /// <summary>
    /// Gets the current action of selection.
    /// </summary>
    public SelectionAction CurrentSelectionAction { get; private set; } = SelectionAction.None;




    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        var cursor = e.GetCurrentPoint(this);

        _mouseDownPoint = cursor.Position;
        _isLeftButtonPressed = cursor.Properties.IsLeftButtonPressed;

        var canSelect = EnableSelection && _isLeftButtonPressed;
        var requestRerender = false;

        if (_bmpD2d != null)
        {
            requestRerender = requestRerender || (canSelect && !SourceSelection.IsEmpty());
            _selectedResizer = SelectionResizers.Find(i => i.HitRegion.Contains(cursor.Position));
            _canDrawSelection = canSelect && !_isSelectionHovered && _hoveredResizer == null;

            if (canSelect)
            {
                _srcSelectionBeforeMoved = new Rect(_sourceSelection.Position(), _sourceSelection.Size());

                // resize selection
                if (canSelect && _hoveredResizer != null)
                {
                    CurrentSelectionAction = SelectionAction.Resizing;
                }

                // draw selection
                else if (canSelect && _isSelectionHovered)
                {
                    CurrentSelectionAction = SelectionAction.Drawing;
                }
            }
        }

        if (requestRerender) Invalidate();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        _mouseDownPoint = null;
        _isLeftButtonPressed = false;
    }

    protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    {
        base.OnPointerCanceled(e);

        _mouseDownPoint = null;
        _isLeftButtonPressed = false;
        _mouseMovePoint = null;
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        var cursor = e.GetCurrentPoint(this);

        _mouseMovePoint = cursor.Position;
        CurrentSelectionAction = SelectionAction.None;

        var canSelect = EnableSelection && cursor.Properties.IsLeftButtonPressed;
        var requestRerender = false;


        if (cursor.Properties.IsLeftButtonPressed)
        {
            // resize the selection
            if (_selectedResizer != null)
            {
                CurrentSelectionAction = SelectionAction.Resizing;
                ResizeSelection(cursor.Position, _selectedResizer.Type);
                requestRerender = true;
            }
            // draw new selection
            else if (_canDrawSelection)
            {
                CurrentSelectionAction = SelectionAction.Drawing;
                UpdateSelectionByMousePosition();
                requestRerender = true;
            }
            // move selection
            else if (canSelect)
            {
                CurrentSelectionAction = SelectionAction.Moving;
                MoveSelection(cursor.Position);
                requestRerender = true;
            }
        }



        // change cursor
        if (!EnableSelection)
        {
            Cursor = InputSystemCursorShape.Arrow;
        }
        else
        {
            // set resizer cursor
            var hoveredResizer = SelectionResizers.Find(i => i.HitRegion.Contains(cursor.Position));

            if (hoveredResizer != null)
            {
                Cursor = hoveredResizer.Cursor;
            }
            else if (ClientSelection.Contains(cursor.Position))
            {
                Cursor = InputSystemCursorShape.SizeAll;
            }
            else
            {
                Cursor = InputSystemCursorShape.Cross;
            }


            // redraw the canvas
            var isSelectionHovered = ClientSelection.Contains(cursor.Position);
            requestRerender = requestRerender
                || _isSelectionHovered != isSelectionHovered
                || _hoveredResizer != hoveredResizer;


            _isSelectionHovered = isSelectionHovered;
            _hoveredResizer = hoveredResizer;
        }


        // request re-render control
        if (requestRerender) Invalidate();
    }


    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        _mouseMovePoint = null;
    }



    /// <summary>
    /// Draw selection layer
    /// </summary>
    protected virtual void DrawSelectionLayer(SwapChainCanvasRenderEventArgs g)
    {
        //if (UseWebview2 || Source == ImageSource.Null || SourceSelection.IsEmpty()) return;
        if (!EnableSelection || SourceSelection.IsEmpty()) return;

        // draw the clip selection region
        using var selectionGeo = g.GetCombinedRectanglesGeometry(ClientSelection, _destRect, 0, 0, Vortice.Direct2D1.CombineMode.Xor);
        g.DrawGeometry(selectionGeo, Colors.Transparent, Colors.Black.WithAlpha(_isLeftButtonPressed ? 100 : 180));


        // draw selection grid, resizers
        if (_isLeftButtonPressed || _isSelectionHovered || _hoveredResizer != null)
        {
            var width3 = ClientSelection.Width / 3;
            var height3 = ClientSelection.Height / 3;


            // draw grid, ignore alpha value
            for (int i = 1; i < 3; i++)
            {
                g.DrawLine(
                    ClientSelection.X + (i * width3),
                    ClientSelection.Y,
                    ClientSelection.X + (i * width3),
                    ClientSelection.Y + ClientSelection.Height, Colors.Black.WithAlpha(200),
                    0.4f);
                g.DrawLine(
                    ClientSelection.X + (i * width3),
                    ClientSelection.Y,
                    ClientSelection.X + (i * width3),
                    ClientSelection.Y + ClientSelection.Height, Colors.White.WithAlpha(200),
                    0.4f);
                g.DrawLine(
                    ClientSelection.X + (i * width3),
                    ClientSelection.Y,
                    ClientSelection.X + (i * width3),
                    ClientSelection.Y + ClientSelection.Height, AccentColor.WithAlpha(200),
                    0.4f);


                g.DrawLine(
                    ClientSelection.X,
                    ClientSelection.Y + (i * height3),
                    ClientSelection.X + ClientSelection.Width,
                    ClientSelection.Y + (i * height3), Colors.Black.WithAlpha(200),
                    0.4f);
                g.DrawLine(
                    ClientSelection.X,
                    ClientSelection.Y + (i * height3),
                    ClientSelection.X + ClientSelection.Width,
                    ClientSelection.Y + (i * height3), Colors.White.WithAlpha(200),
                    0.4f);
                g.DrawLine(
                    ClientSelection.X,
                    ClientSelection.Y + (i * height3),
                    ClientSelection.X + ClientSelection.Width,
                    ClientSelection.Y + (i * height3), AccentColor.WithAlpha(200),
                    0.4f);
            }

            var fontSize = 13;

            // draw selection size
            var text = $"{_sourceSelection.Width} x {_sourceSelection.Height}";
            var textSize = g.MeasureText(text, FontFamily.XamlAutoFontFamily.Source, DpiScale(fontSize));
            var textPadding = new Thickness(10, 5, 10, 5);
            var textX = ClientSelection.X + (ClientSelection.Width / 2 - textSize.Width / 2);
            var textY = ClientSelection.Y + (ClientSelection.Height / 2 - textSize.Height / 2);
            var textBgRect = new Rect(
                textX - textPadding.Left,
                textY - textPadding.Top,
                textSize.Width + textPadding.Left + textPadding.Right,
                textSize.Height + textPadding.Top + textPadding.Bottom);

            if (textBgRect.Width + 10 < ClientSelection.Width
                && textBgRect.Height + 10 < ClientSelection.Height)
            {
                g.DrawRectangle(textBgRect, (float)textSize.Height / 5, Colors.White.WithAlpha(100), Colors.Black.WithAlpha(100));
                g.DrawRectangle(textBgRect, (float)textSize.Height / 5, AccentColor.WithAlpha(100), AccentColor.WithAlpha(150));
                g.DrawText(text, FontFamily.XamlAutoFontFamily.Source, DpiScale(fontSize), textX, textY, Colors.White);
            }


            // draw resizers with layer order
            var resizers = SelectionResizers;
            resizers.Reverse();

            foreach (var rItem in resizers)
            {
                var hideTopBottomResizers = ClientSelection.Width < rItem.IndicatorRegion.Width * 5;
                if (hideTopBottomResizers
                    && (rItem.Type == SelectionResizerType.Top
                    || rItem.Type == SelectionResizerType.Bottom)) continue;

                var hideLeftRightResizers = ClientSelection.Height < rItem.IndicatorRegion.Height * 5;
                if (hideLeftRightResizers
                    && (rItem.Type == SelectionResizerType.Left
                    || rItem.Type == SelectionResizerType.Right)) continue;

                // hover style
                var resizerRect = rItem.IndicatorRegion;
                var fillColor = Colors.White.WithAlpha(200);
                if (rItem.Type == _hoveredResizer?.Type)
                {
                    resizerRect.Inflate(DpiScale(1.45f));
                    fillColor = AccentColor.WithAlpha(200);
                }

                g.DrawEllipse(resizerRect, Colors.White.WithAlpha(50), Colors.Black.WithAlpha(200), 8f);
                g.DrawEllipse(resizerRect, AccentColor.WithAlpha(255), fillColor, 2f);

                
                //// draw debug Hit region
                //if (EnableDebug)
                //{
                    g.DrawRectangle(rItem.HitRegion, 0, Colors.Red);
                //}
            }
        }

        // draw the selection border
        var borderWidth = (_isSelectionHovered && _isLeftButtonPressed) ? 0.6f : 0.3f;
        g.DrawRectangle(ClientSelection, 0, Colors.White, null, borderWidth);
        g.DrawRectangle(ClientSelection, 0, AccentColor, null, borderWidth);

    }



    /// <summary>
    /// Select image source area.
    /// </summary>
    public void SetSourceSelection(Rect srcRect, bool triggerEvent = true)
    {
        srcRect.Intersect(new Rect(0, 0, (int)SourceWidth, (int)SourceHeight));
        _sourceSelection = srcRect;

        if (triggerEvent)
        {
            SelectionChanged?.Invoke(this, new SelectionEventArgs(ClientSelection, SourceSelection));
        }
    }


    /// <summary>
    /// Updates <see cref="ClientSelection"/> using <see cref="BHelper.GetSelection"/>.
    /// </summary>
    public void UpdateSelectionByMousePosition()
    {
        if (_mouseDownPoint == null || _mouseMovePoint == null) return;

        var cliRect = GetSelection__(
            DpiScale(_mouseDownPoint.Value),
            DpiScale(_mouseMovePoint.Value),
            SelectionAspectRatio,
            SourceWidth, SourceHeight, _destRect);

        // limit the selected area to the image
        cliRect.Intersect(_destRect);

        var srcRect = RectClientToSource(cliRect);
        SetSourceSelection(srcRect, true);
    }


    /// <summary>
    /// Gets selection rectangle from 2 points.
    /// </summary>
    /// <param name="point1">The first point</param>
    /// <param name="point2">The second point</param>
    /// <param name="aspectRatio">Aspect ratio</param>
    /// <param name="limitRect">The rectangle to limit the selection</param>
    private static Rect GetSelection__(Point? point1, Point? point2,
        Size aspectRatio, double srcWidth, double srcHeight, Rect limitRect)
    {
        var selectedArea = new Rect();
        var fromPoint = point1 ?? new Point();
        var toPoint = point2 ?? new Point();

        // swap fromPoint and toPoint value if toPoint is less than fromPoint
        if (toPoint.X < fromPoint.X)
        {
            var tempX = fromPoint.X;
            fromPoint.X = toPoint.X;
            toPoint.X = tempX;
        }
        if (toPoint.Y < fromPoint.Y)
        {
            var tempY = fromPoint.Y;
            fromPoint.Y = toPoint.Y;
            toPoint.Y = tempY;
        }

        var width = Math.Abs(fromPoint.X - toPoint.X);
        var height = Math.Abs(fromPoint.Y - toPoint.Y);

        selectedArea.X = fromPoint.X;
        selectedArea.Y = fromPoint.Y;
        selectedArea.Width = width;
        selectedArea.Height = height;

        // limit the selected area to the limitRect
        selectedArea.Intersect(limitRect);


        // free aspect ratio
        if (aspectRatio.Width <= 0 || aspectRatio.Height <= 0)
            return selectedArea;


        var wRatio = aspectRatio.Width / aspectRatio.Height;
        var hRatio = aspectRatio.Height / aspectRatio.Width;

        // update selection size according to the ratio
        if (wRatio > hRatio)
        {
            selectedArea.Height = selectedArea.Width / wRatio;

            if (selectedArea.Bottom >= limitRect.Bottom)
            {
                var maxHeight = limitRect.Bottom - selectedArea.Y;
                selectedArea.Width = maxHeight * wRatio;
                selectedArea.Height = maxHeight;
            }
        }
        else
        {
            selectedArea.Width = selectedArea.Height / hRatio;

            if (selectedArea.Right >= limitRect.Right)
            {
                var maxWidth = limitRect.Right - selectedArea.X; ;
                selectedArea.Width = maxWidth;
                selectedArea.Height = maxWidth * hRatio;
            }
        }

        return selectedArea;
    }



    /// <summary>
    /// Moves the current selection to the given location
    /// </summary>
    public void MoveSelection(Point clientPoint)
    {
        if (!EnableSelection || _mouseDownPoint == null) return;

        var srcPoint = PointClientToSource(clientPoint);
        var srcMouseDownPoint = PointClientToSource(_mouseDownPoint.Value);


        // get the distance the source rect moved
        var dX = srcMouseDownPoint.X - _srcSelectionBeforeMoved.X;
        var dY = srcMouseDownPoint.Y - _srcSelectionBeforeMoved.Y;


        // get the new selection start point
        var newSrcPoint = new Point(srcPoint.X - dX, srcPoint.Y - dY);


        // limit the new selection to the image source
        if (newSrcPoint.X < 0) newSrcPoint.X = 0; // left edge
        if (newSrcPoint.Y < 0) newSrcPoint.Y = 0; // right edge

        // right edge
        if (newSrcPoint.X + _srcSelectionBeforeMoved.Width > SourceWidth)
        {
            newSrcPoint.X = SourceWidth - _srcSelectionBeforeMoved.Width;
        }
        // bottom edge
        if (newSrcPoint.Y + _srcSelectionBeforeMoved.Height > SourceHeight)
        {
            newSrcPoint.Y = SourceHeight - _srcSelectionBeforeMoved.Height;
        }


        // set the final source selection after moved
        var srcRect = new Rect(
            newSrcPoint.X, newSrcPoint.Y,
            _srcSelectionBeforeMoved.Width, _srcSelectionBeforeMoved.Height);

        SetSourceSelection(srcRect, true);
    }


    /// <summary>
    /// Resizes the current selection.
    /// </summary>
    public void ResizeSelection(Point clientCursorPoint, SelectionResizerType direction)
    {
        if (!EnableSelection || _mouseDownPoint == null) return;

        var srcPoint = PointClientToSource(clientCursorPoint);
        var srcMouseDownPoint = PointClientToSource(_mouseDownPoint.Value);
        var newSrcRect = SourceSelection;
        var finalSrcRect = new Rect();


        #region 1. Get correct size and location of new selection

        var isTopDirections = direction == SelectionResizerType.Top
            || direction == SelectionResizerType.TopLeft
            || direction == SelectionResizerType.TopRight;

        var isBottomDirections = direction == SelectionResizerType.Bottom
            || direction == SelectionResizerType.BottomLeft
            || direction == SelectionResizerType.BottomRight;

        var isLeftDirections = direction == SelectionResizerType.Left
            || direction == SelectionResizerType.TopLeft
            || direction == SelectionResizerType.BottomLeft;

        var isRightDirections = direction == SelectionResizerType.Right
            || direction == SelectionResizerType.TopRight
            || direction == SelectionResizerType.BottomRight;


        // top resizers
        if (isTopDirections)
        {
            var gapY = _srcSelectionBeforeMoved.Y - srcMouseDownPoint.Y;
            var dH = srcPoint.Y - _srcSelectionBeforeMoved.Y + gapY;

            newSrcRect.Y = _srcSelectionBeforeMoved.Y + dH;
            newSrcRect.Height = _srcSelectionBeforeMoved.Height - dH;
        }

        // right resizers
        if (isRightDirections)
        {
            var gapX = _srcSelectionBeforeMoved.Right - srcMouseDownPoint.X;
            var dW = srcPoint.X - _srcSelectionBeforeMoved.Right + gapX;

            newSrcRect.Width = _srcSelectionBeforeMoved.Width + dW;
        }

        // bottom resizers
        if (isBottomDirections)
        {
            var gapY = _srcSelectionBeforeMoved.Bottom - srcMouseDownPoint.Y;
            var dH = srcPoint.Y - _srcSelectionBeforeMoved.Bottom + gapY;

            newSrcRect.Height = _srcSelectionBeforeMoved.Height + dH;
        }

        // left resizers
        if (isLeftDirections)
        {
            var gapX = _srcSelectionBeforeMoved.X - srcMouseDownPoint.X;
            var dW = srcPoint.X - _srcSelectionBeforeMoved.X + gapX;

            newSrcRect.X = _srcSelectionBeforeMoved.X + dW;
            newSrcRect.Width = _srcSelectionBeforeMoved.Width - dW;
        }


        if (newSrcRect.Width < 0) newSrcRect.Width = 0;
        if (newSrcRect.Height < 0) newSrcRect.Height = 0;


        // limit the selected client rect to the image source
        newSrcRect.Intersect(new(0, 0, SourceWidth, SourceHeight));

        #endregion // 1. Get correct size and location of new selection


        #region 2. Handle Aspect ratio

        // update selection size according to the ratio
        if (SelectionAspectRatio.Width > 0 && SelectionAspectRatio.Height > 0)
        {
            var wRatio = SelectionAspectRatio.Width / SelectionAspectRatio.Height;
            var hRatio = SelectionAspectRatio.Height / SelectionAspectRatio.Width;

            if (wRatio > hRatio)
            {
                if (direction == SelectionResizerType.Top
                    || direction == SelectionResizerType.TopRight
                    || direction == SelectionResizerType.TopLeft
                    || direction == SelectionResizerType.Bottom
                    || direction == SelectionResizerType.BottomLeft
                    || direction == SelectionResizerType.BottomRight)
                {
                    newSrcRect.Width = newSrcRect.Height / hRatio;

                    if (newSrcRect.Right >= SourceWidth)
                    {
                        var maxWidth = SourceWidth - newSrcRect.X; ;
                        newSrcRect.Width = maxWidth;
                        newSrcRect.Height = maxWidth * hRatio;
                    }
                }
                else
                {
                    newSrcRect.Height = newSrcRect.Width / wRatio;
                }


                if (newSrcRect.Bottom >= SourceHeight)
                {
                    var maxHeight = SourceHeight - newSrcRect.Y;
                    newSrcRect.Width = maxHeight * wRatio;
                    newSrcRect.Height = maxHeight;
                }
            }
            else
            {
                if (direction == SelectionResizerType.Left
                    || direction == SelectionResizerType.TopLeft
                    || direction == SelectionResizerType.BottomLeft
                    || direction == SelectionResizerType.Right
                    || direction == SelectionResizerType.TopRight
                    || direction == SelectionResizerType.BottomRight)
                {
                    newSrcRect.Height = newSrcRect.Width / wRatio;

                    if (newSrcRect.Bottom >= SourceHeight)
                    {
                        var maxHeight = SourceHeight - newSrcRect.Y;
                        newSrcRect.Width = maxHeight * wRatio;
                        newSrcRect.Height = maxHeight;
                    }
                }
                else
                {
                    newSrcRect.Width = newSrcRect.Height / hRatio;
                }


                if (newSrcRect.Right >= SourceWidth)
                {
                    var maxWidth = SourceWidth - newSrcRect.X;
                    newSrcRect.Width = maxWidth;
                    newSrcRect.Height = maxWidth * hRatio;
                }
            }
        }

        #endregion // 2. Handle Aspect ratio


        #region 3. Convert float values to int

        // round the values of location & size
        if (isTopDirections || isLeftDirections)
        {
            finalSrcRect = new Rect(
                (int)newSrcRect.X, (int)newSrcRect.Y,
                (int)Math.Ceiling(newSrcRect.Width),
                (int)Math.Ceiling(newSrcRect.Height));
        }
        else
        {
            finalSrcRect = new Rect(
                (int)newSrcRect.X, (int)newSrcRect.Y,
                (int)Math.Round(newSrcRect.Width),
                (int)Math.Round(newSrcRect.Height));
        }

        #endregion // 3. Convert float values to int


        #region 4. Handle small size (<= 1px)

        // limit the size to 1 pixel
        if (finalSrcRect.Width <= 1) finalSrcRect.Width = 1;
        if (finalSrcRect.Height <= 1) finalSrcRect.Height = 1;


        // make sure selection rect is not moved when size <= 1

        // top, top-left, left
        if (direction == SelectionResizerType.Top
            || direction == SelectionResizerType.TopLeft
            || direction == SelectionResizerType.Left)
        {
            if (finalSrcRect.Width <= 1)
            {
                finalSrcRect.X = (int)_srcSelectionBeforeMoved.Right - 1;
            }
            if (finalSrcRect.Height <= 1)
            {
                finalSrcRect.Y = (int)_srcSelectionBeforeMoved.Bottom - 1;
            }
        }

        // right, bottom-right, bottom
        else if (direction == SelectionResizerType.Right
            || direction == SelectionResizerType.BottomRight
            || direction == SelectionResizerType.Bottom)
        {
            if (finalSrcRect.Width <= 1)
            {
                finalSrcRect.X = (int)_srcSelectionBeforeMoved.X;
            }
            if (finalSrcRect.Height <= 1)
            {
                finalSrcRect.Y = (int)_srcSelectionBeforeMoved.Y;
            }
        }

        // top-right
        else if (direction == SelectionResizerType.TopRight)
        {
            if ((finalSrcRect.Width <= 1 && finalSrcRect.Height <= 1)
                || (finalSrcRect.Width > 1 && finalSrcRect.Height <= 1))
            {
                finalSrcRect.X = (int)_srcSelectionBeforeMoved.Left;
                finalSrcRect.Y = (int)_srcSelectionBeforeMoved.Bottom - 1;
            }
            else if (finalSrcRect.Width <= 1 && finalSrcRect.Height > 1)
            {
                finalSrcRect.X = (int)_srcSelectionBeforeMoved.Left;
            }
        }
        // bottom-left
        else
        {
            if (finalSrcRect.Width <= 1)
            {
                finalSrcRect.X = (int)_srcSelectionBeforeMoved.Right - 1;
            }
        }

        #endregion // 4. Handle small size (<= 1px)


        SetSourceSelection(finalSrcRect, true);
    }


}


