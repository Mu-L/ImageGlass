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
using Windows.Foundation;
using Windows.Graphics;
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


    // public events
    public event TypedEventHandler<DialogWindow, DialogEventArgs>? Submitted;
    public event TypedEventHandler<DialogWindow, DialogEventArgs>? Cancelled;
    public event TypedEventHandler<DialogWindow, DialogEventArgs>? Applied;


    protected IgWindowHook _winHook;
    protected Window? _owner = null;
    protected readonly int MAX_WIDTH = 600;
    protected TaskCompletionSource<DialogExitCode> _resultCompletionSource = new();
    protected readonly KeyboardAccelerator _closeByEscKey = new KeyboardAccelerator()
    {
        Key = VirtualKey.Escape,
        Modifiers = VirtualKeyModifiers.None,
        IsEnabled = true,
    };
    protected readonly KeyboardAccelerator _submitByEnterKey = new KeyboardAccelerator()
    {
        Key = VirtualKey.Enter,
        Modifiers = VirtualKeyModifiers.None,
        IsEnabled = true,
    };


    #region Public Properties

    /// <summary>
    /// Gets, sets the content of dialog window.
    /// </summary>
    public FrameworkElement DialogContent
    {
        get => (FrameworkElement)PART_DialogContent.Content;
        set => PART_DialogContent.Content = value;
    }


    /// <summary>
    /// Gets, sets the data context for <see cref="DialogContent"/>.
    /// </summary>
    public object DialogContentDataContext
    {
        get => DialogContent.DataContext;
        set
        {
            if (DialogContent.DataContext != value)
            {
                // update view model in content dialog
                DialogContent.DataContext = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets or sets the result for the dialog.
    /// </summary>
    public DialogExitCode DialogResult { get; set; } = DialogExitCode.None;


    /// <summary>
    /// Gets, sets the title of dialog.
    /// </summary>
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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


    /// <summary>
    /// Gets, sets the background for dialog footer.
    /// </summary>
    protected SolidColorBrush? FooterBackground
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



    #region Virtual methods

    /// <summary>
    /// Closes the form and returns <see cref="DialogExitCode.Abort"/> code.
    /// </summary>
    protected virtual void OnDialogAborted()
    {
        DialogResult = DialogExitCode.Abort;
        Close();
    }


    /// <summary>
    /// Closes the form and returns <see cref="DialogExitCode.OK"/> code.
    /// </summary>
    protected virtual void OnDialogSubmitted(DialogEventArgs e)
    {
        Submitted?.Invoke(this, e);

        if (!e.CanProceed) return;
        DialogResult = DialogExitCode.OK;
        Close();
    }


    /// <summary>
    /// Closes the form and returns <see cref="DialogExitCode.Cancel"/> code.
    /// </summary>
    protected virtual void OnDialogCancelled(DialogEventArgs e)
    {
        Cancelled?.Invoke(this, e);

        if (!e.CanProceed) return;
        DialogResult = DialogExitCode.Cancel;
        Close();
    }


    /// <summary>
    /// Sets <see cref="DialogResult"/> to <see cref="DialogExitCode.None"/>
    /// and does nothing.
    /// </summary>
    protected virtual void OnDialogApplied(DialogEventArgs e)
    {
        Applied?.Invoke(this, e);

        if (!e.CanProceed) return;
        DialogResult = DialogExitCode.None;
    }


    #endregion // Virtual methods



    #region Window Events

    private void AP_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        FooterBackground = GetThemeFooterBackground();
    }


    private void WinHook_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }


    private void CloseByEscKey_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        OnDialogAborted();
    }


    private void SubmitByEnterKey_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        OnDialogSubmitted(new DialogEventArgs(DialogAction.Submit));
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
        if (DialogContentDataContext is IDisposable dcdc) dcdc.Dispose();

        // if the dialog is closed unexpected, returns Abort code to break the while loop.
        if (DialogResult == DialogExitCode.None) DialogResult = DialogExitCode.Abort;

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

        Root.SizeChanged += Root_SizeChanged;
    }


    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DesiredSize.IsEmpty()) return;

        // only adjust the size once
        fe.SizeChanged -= Root_SizeChanged;


        // calculate the size of the dialog window according to the root content
        var clientHeight = (int)Math.Ceiling(fe.DesiredSize.Height * _winHook.DpiScale);
        var clientWidth = (int)Math.Ceiling(fe.DesiredSize.Width * _winHook.DpiScale);

        // set dialog position to center the owner
        MoveCenterParent(clientWidth, clientHeight, true);
    }


    // change the focused button using arrow keys
    private void FooterButtonsPanel_KeyDown(object sender, KeyRoutedEventArgs e)
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
        if (FocusManager.GetFocusedElement(Content.XamlRoot) is Button focusedButton)
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


    private void PART_Dialog_Button1_Click(object sender, RoutedEventArgs e)
    {
        OnDialogSubmitted(new DialogEventArgs(DialogAction.Submit));
    }


    private void PART_Dialog_Button2_Click(object sender, RoutedEventArgs e)
    {
        OnDialogCancelled(new DialogEventArgs(DialogAction.Cancel));
    }


    private void PART_Dialog_Button3_Click(object sender, RoutedEventArgs e)
    {
        OnDialogApplied(new DialogEventArgs(DialogAction.Apply));
    }

    #endregion // Window Events



    #region Private methods

    /// <summary>
    /// Moves the dialog to center its parent.
    /// </summary>
    private void MoveCenterParent(int clientWidth, int clientHeight, bool limitWithinWorkarea)
    {
        RectInt32? workarea = null;
        Rect parentBounds;

        // get the parent bounds
        if (_owner is null)
        {
            // get workarea of current window
            workarea ??= DisplayArea
                .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)
                .WorkArea;

            parentBounds = workarea.Value.ToRect();
        }
        else
        {
            parentBounds = new Rect(
                _owner.AppWindow.Position.X,
                _owner.AppWindow.Position.Y,
                _owner.AppWindow.Size.Width,
                _owner.AppWindow.Size.Height);
        }

        // get title bar height
        var titlebarHeight = (int)((PART_Titlebar.DesiredSize.Height - 1) * _winHook.DpiScale);

        // set dialog position to center the owner
        var posX = parentBounds.X + parentBounds.Width / 2 - clientWidth / 2;
        var posY = parentBounds.Y + parentBounds.Height / 2 - clientHeight / 2;


        if (limitWithinWorkarea)
        {
            // get workarea of current window
            var workareBounds = workarea ??= DisplayArea
                .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)
                .WorkArea;

            // check if the dialog is within workarea
            var isWithinWorkarea = workareBounds
                .ToRect()
                .Contains(new Rect(posX, posY, clientWidth, clientHeight));

            // reposition dialog if it's not
            if (!isWithinWorkarea)
            {
                posX = workareBounds.X + workareBounds.Width / 2 - clientWidth / 2;
                posY = workareBounds.Y + workareBounds.Height / 2 - clientHeight / 2;
            }


            //// make sure the window position is within the workarea
            //var gap = 10;
            //var posRight = posX + clientWidth;
            //var posBottom = posY + clientHeight;

            //if (posX > workarea.X + workarea.Width - gap) posX -= clientWidth;
            //if (posY > workarea.Y + workarea.Height - gap) posY -= clientHeight;
            //if (posRight < workarea.X + gap) posX = 0;
            //if (posBottom < workarea.Y + gap) posY = 0;
        }


        // update the size of dialog window
        AppWindow.ResizeClient(new(clientWidth, clientHeight - titlebarHeight));

        // update position of dialog window
        AppWindow.Move(new((int)posX, (int)posY));
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
    private void SetDefaultFocus()
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

    #endregion // Private methods



    /// <summary>
    /// Shows dialog.
    /// </summary>
    public async Task<DialogExitCode> ShowAsync(Window? owner = null)
    {
        // set window owner
        _owner = owner;
        _winHook.SetWindowOwner(_owner);
        _resultCompletionSource = new TaskCompletionSource<DialogExitCode>();

        var maxWidth = (int)(MAX_WIDTH * _winHook.DpiScale);

        // create a dialog modal
        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsModal = _owner is not null;
        presenter.IsResizable = false;
        presenter.PreferredMaximumWidth = maxWidth;
        presenter.SetBorderAndTitleBar(true, false);
        AppWindow.SetPresenter(presenter);

        // set initial size
        MoveCenterParent(maxWidth, maxWidth, false);

        // show dialog
        AppWindow.Show();


        // wait for exit code
        var exitCode = await _resultCompletionSource.Task;

        return exitCode;
    }


}
