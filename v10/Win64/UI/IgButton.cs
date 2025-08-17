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

namespace ImageGlass.Win64.UI;

public partial class IgButton : Button
{
    protected readonly IgClickable _clickable;


    /// <summary>
    /// Gets, sets the value indicating that the button is checked on click.
    /// </summary>
    public bool IsCheckOnClick
    {
        get => (bool)GetValue(IsCheckOnClickProperty);
        set
        {
            _clickable.IsCheckOnClick = value;
            SetValue(IsCheckOnClickProperty, value);
        }
    }
    public static readonly DependencyProperty IsCheckOnClickProperty =
        DependencyProperty.Register(
            nameof(IsCheckOnClick),
            typeof(bool),
            typeof(IgButton),
            new PropertyMetadata(false));


    /// <summary>
    /// Gets or sets the check state of the gallery button item.
    /// </summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set
        {
            _clickable.IsChecked = value;
            SetValue(IsCheckedProperty, value);
        }
    }
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(IgButton),
            new PropertyMetadata(false));


    /// <summary>
    /// Gets or sets the check state of the control.
    /// </summary>
    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }
    public static readonly DependencyProperty IdProperty =
        DependencyProperty.Register(
            nameof(Id),
            typeof(string),
            typeof(IgButton),
            new PropertyMetadata(""));


    /// <summary>
    /// Gets or sets the check state of the control.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(IgButton),
            new PropertyMetadata(""));



    public IgButton()
    {
        _clickable = new IgClickable(this);
        DefaultStyleKey = typeof(IgButton);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _clickable.UpdateStyle();
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

    public void SetButtonsState(IgButtonStates states)
    {
        _clickable.ButtonStates = states;
    }
}
