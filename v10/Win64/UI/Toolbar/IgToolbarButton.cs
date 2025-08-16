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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace ImageGlass.Win64.UI;

public partial class IgToolbarButton : AppBarButton
{
    private readonly IgClickable _clickable;


    /// <summary>
    /// Gets, sets the value indicating that the button is checkable.
    /// </summary>
    public bool IsCheckable
    {
        get => _clickable.IsCheckOnClick;
        set => _clickable.IsCheckOnClick = value;
    }


    /// <summary>
    /// Gets or sets the check state of the button.
    /// </summary>
    public bool IsChecked
    {
        get => _clickable.IsChecked;
        set => _clickable.IsChecked = value;
    }


    public IgToolbarButton()
    {
        _clickable = new IgClickable(this);
        DefaultStyleKey = typeof(IgToolbarButton);
    }

    public IgToolbarButton(string? svgIconPath) : this()
    {
        SetSvgIcon(svgIconPath);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // reset button width
        Width = double.NaN;


        // Border element: remove background transition
        if (GetTemplateChild("AppBarButtonInnerBorder") is Border borderEl)
        {
            borderEl.BackgroundTransition = null;
            borderEl.BorderThickness = new Thickness(1);

            // Set min size of button to the size of icon
            if (GetTemplateChild("ContentViewbox") is Viewbox iconViewBox)
            {
                var iconHeight = iconViewBox.Height;
                MinWidth = iconHeight * 2.5;
            }
        }

        // remove default style: PointerOver
        if (GetTemplateChild("PointerOver") is VisualState vsHover)
        {
            vsHover.Setters.RemoveAt(1);
            vsHover.Setters.RemoveAt(0);
        }

        // remove default style: Pressed
        if (GetTemplateChild("Pressed") is VisualState vsPressed)
        {
            vsPressed.Setters.RemoveAt(1);
            vsPressed.Setters.RemoveAt(0);
        }

        // remove default style: OverflowPointerOver
        if (GetTemplateChild("OverflowPointerOver") is VisualState vsOverflowHover)
        {
            vsOverflowHover.Setters.RemoveAt(1);
            vsOverflowHover.Setters.RemoveAt(0);
        }

        // remove default style: OverflowPressed
        if (GetTemplateChild("OverflowPressed") is VisualState vsOverflowPressed)
        {
            vsOverflowPressed.Setters.RemoveAt(1);
            vsOverflowPressed.Setters.RemoveAt(0);
        }

        _clickable.UpdateStyle();
    }


    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        _clickable.SetStateForPreviewKeyDown(e);
    }


    protected override void OnPreviewKeyUp(KeyRoutedEventArgs e)
    {
        base.OnPreviewKeyUp(e);
        _clickable.SetStateForPreviewKeyUp(e);
    }


    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerEntered();
        base.OnPointerEntered(e);

        _clickable.UpdateStyle();
    }


    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerExited();
        base.OnPointerExited(e);

        _clickable.UpdateStyle();
    }


    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerPressed();
        base.OnPointerPressed(e);

        _clickable.UpdateStyle();
    }


    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        _clickable.SetStateForPointerReleased(e);
        base.OnPointerReleased(e);

        _clickable.UpdateStyle();
    }


    /// <summary>
    /// Sets button icon from a SVG file.
    /// </summary>
    public void SetSvgIcon(string? svgPath)
    {
        // set default icon
        if (string.IsNullOrWhiteSpace(svgPath))
        {
            Icon = new SymbolIcon(Symbol.Placeholder);
            return;
        }

        // set new icon
        var svgSource = new SvgImageSource(new Uri(svgPath));
        Icon = new ImageIcon()
        {
            Source = svgSource,
        };
    }

}

