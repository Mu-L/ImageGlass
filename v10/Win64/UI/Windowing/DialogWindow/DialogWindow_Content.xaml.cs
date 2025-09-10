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
using ImageGlass.Win64.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using Windows.Foundation;
using Windows.System;

namespace ImageGlass.Win64.UI;

public sealed partial class DialogWindow_Content : IgControl
{
    internal readonly int MAX_WIDTH = 600;

    public event TypedEventHandler<Button, RoutedEventArgs>? Button1Click;
    public event TypedEventHandler<Button, RoutedEventArgs>? Button2Click;
    public event TypedEventHandler<Button, RoutedEventArgs>? Button3Click;



    #region Control Properties

    public Grid ContentEl => PART_Content;
    public StackPanel FooterEl => PART_Footer;

    public Button Button1El => PART_Button1;
    public Button Button2El => PART_Button2;
    public Button Button3El => PART_Button3;


    /// <summary>
    /// Gets, sets the content of dialog window.
    /// </summary>
    public FrameworkElement DialogContent
    {
        get => (FrameworkElement)PART_DialogContent.Content;
        set => PART_DialogContent.Content = value;
    }


    /// <summary>
    /// Gets, sets the visibility of button 1.
    /// </summary>
    public bool IsButton1Visible
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = true;
    private Visibility Button1Visibility => IsButton1Visible ? Visibility.Visible : Visibility.Collapsed;


    /// <summary>
    /// Gets, sets the visibility of button 2.
    /// </summary>
    public bool IsButton2Visible
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = false;
    private Visibility Button2Visibility => IsButton2Visible ? Visibility.Visible : Visibility.Collapsed;


    /// <summary>
    /// Gets, sets the visibility of button 3.
    /// </summary>
    public bool IsButton3Visible
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = false;
    private Visibility Button3Visibility => IsButton3Visible ? Visibility.Visible : Visibility.Collapsed;


    /// <summary>
    /// Gets, sets the text of button 1.
    /// </summary>
    public string? Button1Text
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = "Button 1";


    /// <summary>
    /// Gets, sets the text of button 2.
    /// </summary>
    public string? Button2Text
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = "Button 2";


    /// <summary>
    /// Gets, sets the text of button 3.
    /// </summary>
    public string? Button3Text
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = "Button 3";


    /// <summary>
    /// Gets, sets the default button of dialog.
    /// </summary>
    public DialogButton DefaultButton { get; set; } = DialogButton.Button1;


    /// <summary>
    /// Gets, sets the default focus of dialog.
    /// </summary>
    public DialogFocus DefaultFocus { get; set; } = DialogFocus.Button1;


    #endregion // Control Properties



    public DialogWindow_Content()
    {
        InitializeComponent();
    }



    #region Control Events

    protected override void OnIgLoaded(FrameworkElement fe)
    {
        base.OnIgLoaded(fe);

        SetDefaultButton();
        SetDefaultFocus();
    }


    private void PART_Button1_Click(object sender, RoutedEventArgs e)
    {
        Button1Click?.Invoke(PART_Button1, e);
    }


    private void PART_Button2_Click(object sender, RoutedEventArgs e)
    {
        Button2Click?.Invoke(PART_Button2, e);
    }


    private void PART_Button3_Click(object sender, RoutedEventArgs e)
    {
        Button3Click?.Invoke(PART_Button3, e);
    }


    private void FooterPanel_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var isMoveNext = e.Key == VirtualKey.Up || e.Key == VirtualKey.Right;
        var isMoveBack = e.Key == VirtualKey.Down || e.Key == VirtualKey.Left;
        if (!isMoveNext && !isMoveBack) return;
        if (sender is not StackPanel panel) return;


        // get visible footer buttons
        var visibleButtons = panel.Children
            .OfType<Button>()
            .Where(i => i.Visibility == Visibility.Visible)
            .ToList();
        if (visibleButtons.Count == 0) return;

        // get focused button and it's index
        var focusedIndex = -1;
        if (FocusManager.GetFocusedElement(XamlRoot) is Button focusedButton)
        {
            focusedIndex = visibleButtons.IndexOf(focusedButton);
        }


        // if no button has focus yet => focus the first one
        if (focusedIndex == -1)
        {
            visibleButtons[0].Focus(FocusState.Keyboard);
        }
        else
        {
            if (isMoveNext)
            {
                // wrap around
                focusedIndex = (focusedIndex + 1) % visibleButtons.Count;
            }
            else if (isMoveBack)
            {
                // wrap around
                focusedIndex = (focusedIndex - 1 + visibleButtons.Count) % visibleButtons.Count;
            }

            visibleButtons[focusedIndex].Focus(FocusState.Keyboard);
        }

        e.Handled = true;
    }


    #endregion // Control Events



    #region Public methods

    /// <summary>
    /// Updates background according to theme color.
    /// </summary>
    public void ApplyTheme()
    {
        var isDarkMode = AP.Config.Theme.Settings.IsDarkMode;
        var bg = AP.Config.Theme.ComputedColors.BgColor.NoAlpha();

        // content bg
        var contentAlpha = isDarkMode ? 180 : 220;
        var contentBg = bg.WithAlpha(contentAlpha);
        PART_Content.Background = new SolidColorBrush(contentBg);

        // footer bg
        var footerAlpha = isDarkMode ? 100 : 150;
        var footerBg = bg.Blend(AP.Config.Theme.InvertedBaseColor, 0.925f, footerAlpha);
        PART_Footer.Background = new SolidColorBrush(footerBg);
    }


    /// <summary>
    /// Set default button.
    /// </summary>
    public void SetDefaultButton()
    {
        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];

        if (DefaultButton == DialogButton.Button1)
        {
            PART_Button1.Style = accentStyle;
        }
        else if (DefaultButton == DialogButton.Button2)
        {
            PART_Button2.Style = accentStyle;
        }
        else if (DefaultButton == DialogButton.Button3)
        {
            PART_Button3.Style = accentStyle;
        }
    }


    /// <summary>
    /// Set default focused element.
    /// </summary>
    public void SetDefaultFocus()
    {
        if (DefaultFocus == DialogFocus.Button1)
        {
            PART_Button1.Focus(FocusState.Keyboard);
        }
        else if (DefaultFocus == DialogFocus.Button2)
        {
            PART_Button2.Focus(FocusState.Keyboard);
        }
        else if (DefaultFocus == DialogFocus.Button3)
        {
            PART_Button3.Focus(FocusState.Keyboard);
        }
    }

    #endregion // Public methods


}
