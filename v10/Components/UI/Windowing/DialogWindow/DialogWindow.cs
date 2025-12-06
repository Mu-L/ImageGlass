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
using ImageGlass.Common;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;

namespace ImageGlass.UI;


public partial class DialogWindow : IgWindow
{
    // public events
    public event TypedEventHandler<DialogWindow, DialogEventArgs>? Submitted;
    public event TypedEventHandler<DialogWindow, DialogEventArgs>? Cancelled;
    public event TypedEventHandler<DialogWindow, DialogEventArgs>? Applied;


    protected IgWindow? _owner = null;
    protected readonly DialogWindow_Content _dialogContentEl = new();
    protected readonly TaskCompletionSource<bool> _taskSourceResized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource<DialogExitCode> _taskSourceExitCode = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected readonly KeyboardAccelerator _closeByEscKey = new()
    {
        Key = VirtualKey.Escape,
        Modifiers = VirtualKeyModifiers.None,
        IsEnabled = true,
    };
    protected readonly KeyboardAccelerator _submitByEnterKey = new()
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
        get => (FrameworkElement)_dialogContentEl.DialogContent;
        set => _dialogContentEl.DialogContent = value;
    }


    /// <summary>
    /// Gets or sets the result for the dialog.
    /// </summary>
    public DialogExitCode DialogResult { get; set; } = DialogExitCode.None;


    /// <summary>
    /// Gets, sets the visibility of button 1.
    /// </summary>
    public bool IsButton1Visible
    {
        get => _dialogContentEl.IsButton1Visible;
        set
        {
            if (_dialogContentEl.IsButton1Visible != value)
            {
                _dialogContentEl.IsButton1Visible = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the visibility of button 2.
    /// </summary>
    public bool IsButton2Visible
    {
        get => _dialogContentEl.IsButton2Visible;
        set
        {
            if (_dialogContentEl.IsButton2Visible != value)
            {
                _dialogContentEl.IsButton2Visible = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the visibility of button 3.
    /// </summary>
    public bool IsButton3Visible
    {
        get => _dialogContentEl.IsButton3Visible;
        set
        {
            if (_dialogContentEl.IsButton3Visible != value)
            {
                _dialogContentEl.IsButton3Visible = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the text of button 1.
    /// </summary>
    public string? Button1Text
    {
        get => _dialogContentEl.Button1Text;
        set
        {
            if (_dialogContentEl.Button1Text != value)
            {
                _dialogContentEl.Button1Text = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the text of button 2.
    /// </summary>
    public string? Button2Text
    {
        get => _dialogContentEl.Button2Text;
        set
        {
            if (_dialogContentEl.Button2Text != value)
            {
                _dialogContentEl.Button2Text = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the text of button 3.
    /// </summary>
    public string? Button3Text
    {
        get => _dialogContentEl.Button3Text;
        set
        {
            if (_dialogContentEl.Button3Text != value)
            {
                _dialogContentEl.Button3Text = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the default button of dialog.
    /// </summary>
    public DialogButton DefaultButton
    {
        get => _dialogContentEl.DefaultButton;
        set
        {
            if (_dialogContentEl.DefaultButton != value)
            {
                _dialogContentEl.DefaultButton = value;
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the default focus of dialog.
    /// </summary>
    public DialogFocus DefaultFocus
    {
        get => _dialogContentEl.DefaultFocus;
        set
        {
            if (_dialogContentEl.DefaultFocus != value)
            {
                _dialogContentEl.DefaultFocus = value;
                _ = OnPropertyChanged();
            }
        }
    }


    #endregion // Public Properties



    public DialogWindow()
    {
        WindowContent = _dialogContentEl;
        WindowContent.SizeChanged += WindowContent_SizeChanged;

        UseBackdropForTransparentWindowOnly = false;
        BackdropStyle = BackdropStyle.MicaAlt;
    }



    #region Window Events

    protected override void OnIgWindowLoaded(FrameworkElement fe)
    {
        base.OnIgWindowLoaded(fe);

        _dialogContentEl.Button1Click += ContentEl_Button1Click;
        _dialogContentEl.Button2Click += ContentEl_Button2Click;
        _dialogContentEl.Button3Click += ContentEl_Button3Click;

        // hotkey: ESC to close
        _closeByEscKey.Invoked += CloseByEscKey_Invoked;
        _submitByEnterKey.Invoked += SubmitByEnterKey_Invoked;
        Content.KeyboardAccelerators.Add(_closeByEscKey);
        Content.KeyboardAccelerators.Add(_submitByEnterKey);

        ApplyTheme();
    }


    protected override void OnIgWindowClosed(WindowEventArgs e)
    {
        base.OnIgWindowClosed(e);

        WindowContent.SizeChanged -= WindowContent_SizeChanged;
        _dialogContentEl.Button1Click -= ContentEl_Button1Click;
        _dialogContentEl.Button2Click -= ContentEl_Button2Click;
        _dialogContentEl.Button3Click -= ContentEl_Button3Click;

        // remove hotkeys
        _closeByEscKey.Invoked -= CloseByEscKey_Invoked;
        _submitByEnterKey.Invoked -= SubmitByEnterKey_Invoked;
        Content.KeyboardAccelerators.Remove(_closeByEscKey);
        Content.KeyboardAccelerators.Remove(_submitByEnterKey);

        // if the dialog is closed unexpected, returns Abort code to break the while loop.
        if (DialogResult == DialogExitCode.None) DialogResult = DialogExitCode.Abort;

        // set the result to complete the task
        _ = _taskSourceExitCode.TrySetResult(DialogResult);

        // reactivate the owner window
        _owner?.Activate();
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);

        ApplyTheme();
    }


    private void WindowContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DesiredSize.IsEmpty()) return;

        // only adjust the size once
        _dialogContentEl.SizeChanged -= WindowContent_SizeChanged;

        // calculate the size of the dialog window according to the root content
        var clientHeight = (int)Math.Ceiling(fe.DesiredSize.Height * DpiScale);
        var clientWidth = (int)Math.Ceiling(fe.DesiredSize.Width * DpiScale);

        // set dialog position to center the owner
        ResizeAndMoveCenterParent(clientWidth, clientHeight, true);

        // done resizing
        _ = _taskSourceResized.TrySetResult(true);
    }


    private void ContentEl_Button1Click(Button sender, RoutedEventArgs e)
    {
        OnDialogSubmitted(new DialogEventArgs(DialogAction.Submit));
    }


    private void ContentEl_Button2Click(Button sender, RoutedEventArgs e)
    {
        OnDialogCancelled(new DialogEventArgs(DialogAction.Cancel));
    }


    private void ContentEl_Button3Click(Button sender, RoutedEventArgs e)
    {
        OnDialogApplied(new DialogEventArgs(DialogAction.Apply));
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


    #endregion // Window Events



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



    #region Methods

    /// <summary>
    /// Updates background according to theme color.
    /// </summary>
    private void ApplyTheme()
    {
        _dialogContentEl.ApplyTheme();
        TitleBar.BackgroundColor = ((SolidColorBrush)_dialogContentEl.ContentEl.Background).Color;
    }



    /// <summary>
    /// Moves the dialog to center its parent.
    /// </summary>
    private void ResizeAndMoveCenterParent(int clientWidth, int clientHeight, bool limitWithinWorkarea)
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
        }


        // update the size of dialog window
        AppWindow.ResizeClient(new(clientWidth, clientHeight));

        // update position of dialog window
        AppWindow.Move(new((int)posX, (int)posY));
    }


    /// <summary>
    /// Sets the owner of this window.
    /// </summary>
    private void SetWindowOwner(IgWindow? owner)
    {
        _owner = owner;
        if (owner is null) return;

        var ownerHandle = WindowNative.GetWindowHandle(owner);
        WindowApi.SetWindowOwner(Handle, ownerHandle);
    }


    /// <summary>
    /// Shows dialog.
    /// </summary>
    public async Task<DialogExitCode> ShowAsync(IgWindow? owner = null)
    {
        // set window owner
        SetWindowOwner(owner);
        _taskSourceExitCode = new TaskCompletionSource<DialogExitCode>();

        var maxWidth = (int)(_dialogContentEl.MAX_WIDTH * DpiScale);

        // create a dialog modal
        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsModal = _owner is not null;
        presenter.IsResizable = false;
        presenter.PreferredMaximumWidth = maxWidth;
        presenter.SetBorderAndTitleBar(true, false);
        AppWindow.SetPresenter(presenter);


        // load the window in background
        WindowApi.ShowWindowHidden(Handle);

        // wait for the window size updated
        await _taskSourceResized.Task;

        // show dialog
        AppWindow.Show();


        // wait for exit code
        var exitCode = await _taskSourceExitCode.Task;

        return exitCode;
    }


    #endregion // Methods


}
