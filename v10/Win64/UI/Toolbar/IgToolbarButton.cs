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
using Microsoft.UI.Xaml.Media;

namespace ImageGlass.Win64.UI;

public partial class IgToolbarButton : AppBarButton
{

    /// <summary>
    /// Gets or sets the interaction state of the gallery button item.
    /// </summary>
    public IgButtonStates State
    {
        get => (IgButtonStates)GetValue(StateProperty);
        set
        {
            SetValue(StateProperty, value);
            UpdateStyle();
        }
    }
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(
            nameof(State),
            typeof(IgButtonStates),
            typeof(GalleryButtonItem),
            new PropertyMetadata(IgButtonStates.Normal));


    /// <summary>
    /// Gets, sets the value indicating that the button is checkable.
    /// </summary>
    public bool IsCheckable
    {
        get => (bool)GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }
    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register(
            nameof(IsCheckable),
            typeof(bool),
            typeof(IgToolbarButton),
            new PropertyMetadata(default));


    /// <summary>
    /// Gets or sets the check state of the button.
    /// </summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set
        {
            SetValue(IsCheckedProperty, value);

            // set selected styles
            UpdateStyle();
        }
    }
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(IgToolbarButton),
            new PropertyMetadata(default));


    public IgToolbarButton()
    {
        DefaultStyleKey = nameof(IgToolbarButton);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // reset button width
        Width = double.NaN;


        // Border lement: remove background transition
        if (GetTemplateChild("AppBarButtonInnerBorder") is Border borderEl)
        {
            borderEl.BackgroundTransition = null;
            borderEl.BorderThickness = new Thickness(1);

            // Set min size of button to the size of icon
            if (GetTemplateChild("ContentViewbox") is Viewbox iconViewBox)
            {
                var iconHeight = iconViewBox.Height;
                MinWidth = MinHeight = iconHeight * 2.5;
            }
        }

        // remove default hover style
        if (GetTemplateChild("PointerOver") is VisualState vsHover)
        {
            vsHover.Setters.RemoveAt(1);
            vsHover.Setters.RemoveAt(0);
        }

        // remove default pressed style
        if (GetTemplateChild("Pressed") is VisualState vsPressed)
        {
            vsPressed.Setters.RemoveAt(1);
            vsPressed.Setters.RemoveAt(0);
        }

        UpdateStyle();
    }


    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Windows.System.VirtualKey.Space
            || e.Key == Windows.System.VirtualKey.Enter)
        {
            State ^= IgButtonStates.Hovered;
            State |= IgButtonStates.Pressed;
            ClickMode = ClickMode.Press;

            if (IsCheckable) IsChecked = !IsChecked;
        }
    }

    protected override void OnPreviewKeyUp(KeyRoutedEventArgs e)
    {
        base.OnPreviewKeyUp(e);

        if (e.Key == Windows.System.VirtualKey.Space
            || e.Key == Windows.System.VirtualKey.Enter)
        {
            State ^= IgButtonStates.Pressed;
            State ^= IgButtonStates.Hovered;
            ClickMode = ClickMode.Release;
        }
    }


    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        State ^= IgButtonStates.Normal;
        State |= IgButtonStates.Hovered;
        ClickMode = ClickMode.Hover;

        base.OnPointerEntered(e);
        UpdateStyle();
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        State ^= IgButtonStates.Hovered;
        State |= IgButtonStates.Normal;
        ClickMode = ClickMode.Release;

        base.OnPointerExited(e);
        UpdateStyle();
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        State ^= IgButtonStates.Hovered;
        State |= IgButtonStates.Pressed;
        ClickMode = ClickMode.Press;
        if (IsCheckable) IsChecked = !IsChecked;

        base.OnPointerPressed(e);
        UpdateStyle();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        State ^= IgButtonStates.Pressed;
        if (e.Pointer.IsInContact) State |= IgButtonStates.Hovered;
        else State ^= IgButtonStates.Hovered;
        ClickMode = ClickMode.Release;

        base.OnPointerReleased(e);
        UpdateStyle();
    }


    private void UpdateStyle()
    {
        // normal style
        Brush? bgBrush = null;
        Brush? borderBrush = null;


        // selected style
        if (IsChecked)
        {
            bgBrush = (Brush)(Application.Current.Resources["IgButtonBackgroundSelected"]);
        }


        // hover style
        if (State.HasFlag(IgButtonStates.Hovered))
        {
            bgBrush = borderBrush = (Brush)(Application.Current.Resources["IgButtonBackgroundHovered"]);
        }

        // pressed style
        else if (State.HasFlag(IgButtonStates.Pressed))
        {
            bgBrush = borderBrush = (Brush)(Application.Current.Resources["IgButtonBackgroundPressed"]);
        }


        if (IsChecked)
        {
            borderBrush = (Brush)(Application.Current.Resources["IgButtonBorderSelected"]);
        }


        Background = bgBrush;
        BorderBrush = borderBrush;
    }

}

