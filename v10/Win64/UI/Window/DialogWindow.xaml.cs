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
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System;

namespace ImageGlass.Win64.UI;

public partial class DialogWindow : Window, INotifyPropertyChanged
{
    #region INotifyPropertyChanged Implementation

    // to manage PropertyChanged events
    private List<PropertyChangedEventHandler> _propertyChangedEvent = new();
    private event PropertyChangedEventHandler? _propertyChangedHandler;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            if (value != null)
            {
                _propertyChangedHandler += value;
                _propertyChangedEvent.Add(value);
            }
        }

        remove
        {
            if (value != null)
            {
                _propertyChangedHandler -= value;
                _propertyChangedEvent.Remove(value);
            }
        }
    }


    /// <summary>
    /// Emits event <see cref="PropertyChanged"/>.
    /// </summary>
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        _propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    /// Clears event handlers list of <see cref="PropertyChanged"/>.
    /// </summary>
    public void ClearPropertyChangedEvents()
    {
        // remove PropertyChanged events
        foreach (var eventHandler in _propertyChangedEvent)
        {
            _propertyChangedHandler -= eventHandler;
        }
        _propertyChangedEvent.Clear();
    }

    #endregion // INotifyPropertyChanged Implementation


    private IgWindowHook _winHook;
    private Window? _owner = null;
    private TaskCompletionSource<DialogResult> _resultCompletionSource = new();
    private readonly KeyboardAccelerator _closeByEscKey = new KeyboardAccelerator()
    {
        Key = VirtualKey.Escape,
        Modifiers = VirtualKeyModifiers.None,
        IsEnabled = true,
    };
    private readonly KeyboardAccelerator _submitByEnterKey = new KeyboardAccelerator()
    {
        Key = VirtualKey.Enter,
        Modifiers = VirtualKeyModifiers.None,
        IsEnabled = true,
    };


    #region Public Properties

    public FrameworkElement DialogContent
    {
        get => (FrameworkElement)PART_DialogContent.Content;
        set => PART_DialogContent.Content = value;
    }


    /// <summary>
    /// Gets or sets the result for the dialog.
    /// </summary>
    public DialogResult DialogResult { get; set; } = DialogResult.None;

    public PopupWindowViewModel VM
    {
        get; set
        {
            if (field != value)
            {
                field?.Dispose();
                field = value;
                OnPropertyChanged();

                // update view model in content dialog
                DialogContent.DataContext = value;
            }
        }
    } = new PopupWindowViewModel();

    public string? TitlebarText
    {
        get => _winHook.TitlebarText;
        set
        {
            if (_winHook.TitlebarText != value)
            {
                _winHook.TitlebarText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsButton1Visible
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = true;
    private Visibility Button1Visibility => IsButton1Visible ? Visibility.Visible : Visibility.Collapsed;

    public bool IsButton2Visible
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = true;
    private Visibility Button2Visibility => IsButton2Visible ? Visibility.Visible : Visibility.Collapsed;

    public bool IsButton3Visible
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = false;
    private Visibility Button3Visibility => IsButton3Visible ? Visibility.Visible : Visibility.Collapsed;

    public string? Button1Text
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "Button 1";

    public string? Button2Text
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "Button 2";

    public string? Button3Text
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "Button 3";

    public DialogDefaultButton DefaultButton { get; set; } = DialogDefaultButton.Button1;

    public DialogDefaultFocus DefaultFocus { get; set; } = DialogDefaultFocus.Button1;


    public SolidColorBrush? FooterBackground
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = null;

    #endregion // Public Properties


    public DialogWindow()
    {
        InitializeComponent();

        _winHook = new(this, PART_Titlebar);
        _winHook.PropertyChanged += WinHook_PropertyChanged;
        Closed += DialogWindow_Closed;
        Root.Loaded += Root_Loaded;
        Root.SizeChanged += Root_SizeChanged;
        AP.ThemeChanged += AP_ThemeChanged;

        // hotkey: ESC to close
        _closeByEscKey.Invoked += CloseByEscKey_Invoked;
        _submitByEnterKey.Invoked += SubmitByEnterKey_Invoked;
        Content.KeyboardAccelerators.Add(_closeByEscKey);
        Content.KeyboardAccelerators.Add(_submitByEnterKey);
    }

    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        FooterBackground = GetThemeFooterBackground();
    }


    private void WinHook_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }


    private void DialogWindow_Closed(object sender, WindowEventArgs args)
    {
        Closed -= DialogWindow_Closed;
        Root.Loaded -= Root_Loaded;
        Root.SizeChanged -= Root_SizeChanged;
        AP.ThemeChanged -= AP_ThemeChanged;

        // remove hotkeys
        _closeByEscKey.Invoked -= CloseByEscKey_Invoked;
        Content.KeyboardAccelerators.Remove(_closeByEscKey);
        Content.KeyboardAccelerators.Remove(_submitByEnterKey);

        _winHook.Dispose();
        VM.Dispose();

        // if the dialog is closed unexpected, returns Abort code to break the while loop.
        if (DialogResult == DialogResult.None) DialogResult = DialogResult.Abort;

        // set the result to complete the task
        _resultCompletionSource.SetResult(DialogResult);

        // reactivate the owner window
        _owner?.Activate();
    }


    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        FooterBackground = GetThemeFooterBackground();

        SetDefaultButton();
        SetDefaultFocus();
    }


    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_owner is null || sender is not FrameworkElement fe) return;

        // only adjust the size once
        fe.SizeChanged -= Root_SizeChanged;


        // calculate the size of the dialog window according to the root content
        var dpiScale = fe.XamlRoot.RasterizationScale;
        var titlebarHeight = PART_Titlebar.DesiredSize.Height - 1;
        var clientHeight = (int)Math.Ceiling((fe.DesiredSize.Height - titlebarHeight) * dpiScale);
        var clientWidth = (int)Math.Ceiling(fe.DesiredSize.Width * dpiScale);

        // set dialog position to center the owner
        var ownerSize = _owner.AppWindow.Size;
        var posX = _owner.AppWindow.Position.X + ownerSize.Width / 2 - clientWidth / 2;
        var posY = _owner.AppWindow.Position.Y + ownerSize.Height / 2 - clientHeight / 2;

        // update size and position of dialog
        AppWindow.ResizeClient(new(clientWidth, clientHeight));
        AppWindow.Move(new(posX, posY));
    }



    /// <summary>
    /// Closes the form and returns <see cref="DialogResult.Abort"/> code.
    /// </summary>
    protected virtual void OnAborted()
    {
        DialogResult = DialogResult.Abort;
        Close();
    }


    /// <summary>
    /// Closes the form and returns <see cref="DialogResult.OK"/> code.
    /// </summary>
    protected virtual void OnButton1Clicked()
    {
        DialogResult = DialogResult.OK;
        Close();
    }


    /// <summary>
    /// Closes the form and returns <see cref="DialogResult.Cancel"/> code.
    /// </summary>
    protected virtual void OnButton2Clicked()
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }


    /// <summary>
    /// Sets <see cref="DialogResult"/> to <see cref="DialogResult.None"/>
    /// and does nothing.
    /// </summary>
    protected virtual void OnButton3Clicked()
    {
        DialogResult = DialogResult.None;
    }


    private void CloseByEscKey_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        OnAborted();
    }


    private void SubmitByEnterKey_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        OnButton1Clicked();
    }


    // change the focused button using arrow keys
    private void FooterButtonsPanel_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var isMoveNext = e.Key == VirtualKey.Up || e.Key == VirtualKey.Right;
        var isMoveBack = e.Key == VirtualKey.Down || e.Key == VirtualKey.Left;
        if (!isMoveNext && !isMoveBack) return;


        var panel = (StackPanel)sender;
        var visibleButtons = panel.Children
            .OfType<Button>()
            .Where(i => i.Visibility == Visibility.Visible)
            .ToList();
        var focusedButton = (Button)FocusManager.GetFocusedElement(Content.XamlRoot);
        int focusedIndex = visibleButtons.IndexOf(focusedButton);

        if (focusedIndex == -1)
        {
            // No button has focus yet → focus the first one
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


    private void PART_Dialog_Button1_Click(object sender, RoutedEventArgs e)
    {
        OnButton1Clicked();
    }


    private void PART_Dialog_Button2_Click(object sender, RoutedEventArgs e)
    {
        OnButton2Clicked();
    }


    private void PART_Dialog_Button3_Click(object sender, RoutedEventArgs e)
    {
        OnButton3Clicked();
    }


    /// <summary>
    /// Shows dialog.
    /// </summary>
    public async Task<DialogResult> ShowAsync(Window owner)
    {
        // set window owner
        _owner = owner;
        _winHook.SetWindowOwner(_owner);
        _resultCompletionSource = new TaskCompletionSource<DialogResult>();

        var dpiScale = _owner.Content.XamlRoot.RasterizationScale;
        var maxWidth = (int)(Root.MaxWidth * dpiScale);
        var maxHeight = (int)(Root.MaxHeight * dpiScale);

        // create a dialog modal
        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsModal = true;
        presenter.IsResizable = false;
        presenter.PreferredMaximumWidth = maxWidth;
        presenter.PreferredMaximumHeight = maxHeight;
        presenter.SetBorderAndTitleBar(true, false);
        AppWindow.SetPresenter(presenter);

        // show the dialog
        AppWindow.Resize(new(maxWidth, maxHeight));
        AppWindow.Show();


        // wait for dialog result
        return await _resultCompletionSource.Task;
    }


    /// <summary>
    /// Get the footer background according to current theme.
    /// </summary>
    private static SolidColorBrush GetThemeFooterBackground()
    {
        var color = AP.Config.Theme.ComputedColors.GalleryBgColor;
        var maxAlpha = AP.Config.Theme.Settings.IsDarkMode ? 100 : 150;

        var alpha = Math.Min(color.A, (byte)maxAlpha);
        var bgColor = color.WithAlpha(alpha);

        return new SolidColorBrush(bgColor);
    }


    /// <summary>
    /// Set default button.
    /// </summary>
    private void SetDefaultButton()
    {
        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];

        if (DefaultButton == DialogDefaultButton.Button1)
        {
            PART_Button1.Style = accentStyle;
        }
        else if (DefaultButton == DialogDefaultButton.Button2)
        {
            PART_Button2.Style = accentStyle;
        }
        else if (DefaultButton == DialogDefaultButton.Button3)
        {
            PART_Button3.Style = accentStyle;
        }
    }


    /// <summary>
    /// Set default focused element.
    /// </summary>
    private void SetDefaultFocus()
    {
        if (DefaultFocus == DialogDefaultFocus.Button1)
        {
            PART_Button1.Focus(FocusState.Keyboard);
        }
        else if (DefaultFocus == DialogDefaultFocus.Button2)
        {
            PART_Button2.Focus(FocusState.Keyboard);
        }
        else if (DefaultFocus == DialogDefaultFocus.Button3)
        {
            PART_Button3.Focus(FocusState.Keyboard);
        }
    }

}
