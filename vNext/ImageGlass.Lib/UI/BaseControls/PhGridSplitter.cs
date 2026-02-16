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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ImageGlass.Common;
using ImageGlass.Common.Extensions;
using System;

namespace ImageGlass.UI;

public class PhGridSplitter : GridSplitter
{
    protected override Type StyleKeyOverride => typeof(GridSplitter);


    private bool _isPointerDown = false;
    private IBrush _bgNormal = Brushes.Transparent;
    private IBrush _bgHover = Brushes.Transparent;
    private IBrush _bgPressed = Brushes.Transparent;



    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        UploadThemeColors();
        Background = _bgNormal;

        Core.ThemeChanged += Core_ThemeChanged;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        Core.ThemeChanged -= Core_ThemeChanged;
    }


    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Background = _bgHover;
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        _isPointerDown = true;
        Background = _bgPressed;
    }


    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPointerDown) Background = _bgPressed;
    }


    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPointerDown = false;

        if (IsPointerOver) Background = _bgHover;
        else Background = _bgNormal;
    }


    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Background = _bgNormal;
    }


    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPointerDown = false;
    }


    private void Core_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        UploadThemeColors();
        Background = _bgNormal;
    }


    private void UploadThemeColors()
    {
        _bgNormal = Core.Theme.ComputedColors.GalleryBgColor
            .Blend(Core.Theme.InvertedBaseColor, 0.9)
            .ToBrush();

        _bgHover = Core.Theme.ComputedColors.GalleryBgColor
            .Blend(Core.Theme.InvertedBaseColor, 0.8)
            .ToBrush();

        _bgPressed = Core.Theme.ComputedColors.GalleryBgColor
            .Blend(Core.Theme.InvertedBaseColor, 0.7)
            .ToBrush();
    }

}
