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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ImageGlass.Common;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Lib.UI.Windowing;

public partial class DialogWindow : IgWindow
{
    internal readonly int MIN_WIDTH = 400;
    internal readonly int MAX_WIDTH = 600;

    protected Grid _contentEl;
    protected StackPanel _footerEl;
    protected Button _btn1;
    protected Button _btn2;
    protected Button _btn3;

    protected TaskCompletionSource<DialogExitCode> _taskSourceExitCode = new(TaskCreationOptions.RunContinuationsAsynchronously);


    #region Control Properties

    /// <summary>
    /// Gets, sets the dialog content.
    /// </summary>
    public object DialogContent
    {
        get => GetValue(DialogContentProperty);
        set => SetValue(DialogContentProperty, value);
    }
    public static readonly StyledProperty<object> DialogContentProperty =
        AvaloniaProperty.Register<DialogWindow, object>(nameof(DialogContent));


    /// <summary>
    /// Gets, sets the button 1 text.
    /// </summary>
    public string Button1Text
    {
        get => GetValue(Button1TextProperty);
        set => SetValue(Button1TextProperty, value);
    }
    public static readonly StyledProperty<string> Button1TextProperty =
        AvaloniaProperty.Register<DialogWindow, string>(nameof(Button1Text), "[Button 1]");


    /// <summary>
    /// Gets, sets the button 2 text.
    /// </summary>
    public string Button2Text
    {
        get => GetValue(Button2TextProperty);
        set => SetValue(Button2TextProperty, value);
    }
    public static readonly StyledProperty<string> Button2TextProperty =
        AvaloniaProperty.Register<DialogWindow, string>(nameof(Button2Text), "[Button 2]");


    /// <summary>
    /// Gets, sets the button 3 text.
    /// </summary>
    public string Button3Text
    {
        get => GetValue(Button3TextProperty);
        set => SetValue(Button3TextProperty, value);
    }
    public static readonly StyledProperty<string> Button3TextProperty =
        AvaloniaProperty.Register<DialogWindow, string>(nameof(Button3Text), "[Button 3]");


    /// <summary>
    /// Gets, sets the visibility of button 1.
    /// </summary>
    public bool IsButton1Visible
    {
        get => GetValue(IsButton1VisibleProperty);
        set => SetValue(IsButton1VisibleProperty, value);
    }
    public static readonly StyledProperty<bool> IsButton1VisibleProperty =
        AvaloniaProperty.Register<DialogWindow, bool>(nameof(IsButton1Visible), true);


    /// <summary>
    /// Gets, sets the visibility of button 2.
    /// </summary>
    public bool IsButton2Visible
    {
        get => GetValue(IsButton2VisibleProperty);
        set => SetValue(IsButton2VisibleProperty, value);
    }
    public static readonly StyledProperty<bool> IsButton2VisibleProperty =
        AvaloniaProperty.Register<DialogWindow, bool>(nameof(IsButton2Visible), false);


    /// <summary>
    /// Gets, sets the visibility of button 3.
    /// </summary>
    public bool IsButton3Visible
    {
        get => GetValue(IsButton3VisibleProperty);
        set => SetValue(IsButton3VisibleProperty, value);
    }
    public static readonly StyledProperty<bool> IsButton3VisibleProperty =
        AvaloniaProperty.Register<DialogWindow, bool>(nameof(IsButton3Visible), false);


    /// <summary>
    /// Gets, sets the default button of dialog.
    /// </summary>
    public DialogButton DefaultButton { get; set; } = DialogButton.Button1;


    /// <summary>
    /// Gets, sets the default focus of dialog.
    /// </summary>
    public DialogFocus DefaultFocus { get; set; } = DialogFocus.Default;


    /// <summary>
    /// Gets or sets the result for the dialog.
    /// </summary>
    public DialogExitCode DialogResult { get; set; } = DialogExitCode.None;

    #endregion // Control Properties



    public DialogWindow()
    {
        CanResize = false;
        ShowInTaskbar = true;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        SizeToContent = SizeToContent.WidthAndHeight;

        TransparencyLevelHint = [WindowTransparencyLevel.Mica, WindowTransparencyLevel.None];
        BackdropStyle = BackdropStyle.Acrylic;

        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CloseWindowHotkeys = [new(Avalonia.Input.Key.Escape)];

        Content = CreateDialogContentElement();
    }



    #region Window Events

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ApplyTheme();
        SetDefaultButton();

        // Note: need a delay so that pressing Space key won't hit the focused button
        await Task.Delay(200);
        SetDefaultFocus();
    }


    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // if the dialog is closed unexpected, returns Abort code to break the while loop.
        if (DialogResult == DialogExitCode.None) DialogResult = DialogExitCode.Abort;

        // set the result to complete the task
        _ = _taskSourceExitCode.TrySetResult(DialogResult);

        // reactivate the owner window
        Owner?.Activate();
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);

        ApplyTheme();
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var hk = new Hotkey(e.KeyModifiers, e.Key);
        if (hk.IsSame(Key.Enter))
        {
            e.Handled = true;

            // execute the action of the current default button
            if (DefaultButton == DialogButton.Button1)
            {
                Button1_Click(_btn1, e);
            }
            else if (DefaultButton == DialogButton.Button2)
            {
                Button2_Click(_btn2, e);
            }
            else if (DefaultButton == DialogButton.Button3)
            {
                Button3_Click(_btn3, e);
            }
            else
            {
                OnDialogSubmitted(new DialogEventArgs(DialogAction.Submit));
            }
        }
    }


    protected override void OnIgCloseWindowHotkeyPressed(KeyEventArgs e)
    {
        base.OnIgCloseWindowHotkeyPressed(e);

        e.Handled = true;
        OnDialogAborted();
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Source is TextBox
            or Button
            or CheckBox
            or SelectableTextBlock
            or ScrollContentPresenter) return;

        var p = e.GetCurrentPoint(this);
        if (p.Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }



    /// <summary>
    /// Creates layout and content for dialog window.
    /// </summary>
    [MemberNotNull(nameof(_contentEl), nameof(_footerEl), nameof(_btn1), nameof(_btn2), nameof(_btn3))]
    protected Grid CreateDialogContentElement()
    {
        // 1. create content slot
        var slot = new ContentControl
        {
            Padding = new Thickness(24),
            [!ContentControl.ContentProperty] = this[!DialogContentProperty],
        };

        _contentEl = new Grid();
        _contentEl.Children.Add(slot);
        Grid.SetRow(_contentEl, 0);


        // 2. create footer
        _btn1 = new Button
        {
            MinWidth = 80,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            [!Button.ContentProperty] = this[!Button1TextProperty],
            [!Button.IsVisibleProperty] = this[!IsButton1VisibleProperty],
        };
        _btn1.Click += Button1_Click;

        _btn2 = new Button
        {
            MinWidth = 80,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            [!Button.ContentProperty] = this[!Button2TextProperty],
            [!Button.IsVisibleProperty] = this[!IsButton2VisibleProperty],
        };
        _btn2.Click += Button2_Click;

        _btn3 = new Button
        {
            MinWidth = 80,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            [!Button.ContentProperty] = this[!Button3TextProperty],
            [!Button.IsVisibleProperty] = this[!IsButton3VisibleProperty],
        };
        _btn3.Click += Button3_Click;

        var footerContent = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(24),
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };
        footerContent.KeyDown += FooterContent_KeyDown;
        footerContent.Children.AddRange([_btn1, _btn2, _btn3]);

        _footerEl = new StackPanel();
        _footerEl.Children.Add(footerContent);
        Grid.SetRow(_footerEl, 1);


        // 3. create root content
        var root = new Grid
        {
            MinWidth = MIN_WIDTH,
            MaxWidth = MAX_WIDTH,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            RowDefinitions = new RowDefinitions("*, Auto"),
        };
        root.Children.Add(_contentEl);
        root.Children.Add(_footerEl);

        return root;
    }


    private void FooterContent_KeyDown(object? sender, KeyEventArgs e)
    {
        var isMoveNext = e.Key == Key.Up || e.Key == Key.Right;
        var isMoveBack = e.Key == Key.Down || e.Key == Key.Left;
        if (!isMoveNext && !isMoveBack) return;
        if (sender is not StackPanel panel) return;


        // get visible footer buttons
        var visibleButtons = panel.Children
            .OfType<Button>()
            .Where(i => i.IsVisible)
            .ToList();
        if (visibleButtons.Count == 0) return;

        // get focused button and it's index
        var focusedIndex = -1;
        if (FocusManager?.GetFocusedElement() is Button focusedButton)
        {
            focusedIndex = visibleButtons.IndexOf(focusedButton);
        }


        // if no button has focus yet => focus the first one
        if (focusedIndex == -1)
        {
            visibleButtons[0].Focus(NavigationMethod.Tab);
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

            visibleButtons[focusedIndex].Focus(NavigationMethod.Tab);
        }

        e.Handled = true;
    }


    private void Button1_Click(object? sender, RoutedEventArgs e)
    {
        OnDialogSubmitted(new DialogEventArgs(DialogAction.Submit));
    }


    private void Button2_Click(object? sender, RoutedEventArgs e)
    {
        OnDialogCancelled(new DialogEventArgs(DialogAction.Cancel));
    }


    private void Button3_Click(object? sender, RoutedEventArgs e)
    {
        OnDialogApplied(new DialogEventArgs(DialogAction.Apply));
    }


    #endregion // Window Events



    #region Virtual methods

    /// <summary>
    /// Closes the form and returns <see cref="DialogExitCode.Abort"/> code.
    /// </summary>
    protected virtual void OnDialogAborted()
    {
        DialogResult = DialogExitCode.Abort;
        Close(DialogResult);
    }


    /// <summary>
    /// Closes the form and returns <see cref="DialogExitCode.OK"/> code.
    /// </summary>
    protected virtual void OnDialogSubmitted(DialogEventArgs e)
    {
        if (!e.CanProceed) return;
        DialogResult = DialogExitCode.OK;
        Close(DialogResult);
    }


    /// <summary>
    /// Closes the form and returns <see cref="DialogExitCode.Cancel"/> code.
    /// </summary>
    protected virtual void OnDialogCancelled(DialogEventArgs e)
    {
        if (!e.CanProceed) return;
        DialogResult = DialogExitCode.Cancel;
        Close(DialogResult);
    }


    /// <summary>
    /// Sets the dialog result to <see cref="DialogExitCode.None"/>
    /// and does nothing.
    /// </summary>
    protected virtual void OnDialogApplied(DialogEventArgs e)
    {
        if (!e.CanProceed) return;
        DialogResult = DialogExitCode.None;
    }


    #endregion // Virtual methods



    #region Methods

    /// <summary>
    /// Updates background according to theme color.
    /// </summary>
    protected void ApplyTheme()
    {
        var isDarkMode = Core.Config.Theme.Settings.IsDarkMode;
        var bg = Core.Config.Theme.ComputedColors.BgColor.NoAlpha();

        // content bg
        var contentAlpha = isDarkMode ? 180 : 220;
        var contentBg = bg.WithAlpha(contentAlpha);
        _contentEl.Background = new SolidColorBrush(contentBg);

        // footer bg
        var footerAlpha = isDarkMode ? 100 : 150;
        var footerBg = bg.Blend(Core.Config.Theme.InvertedBaseColor, 0.925f, footerAlpha);
        _footerEl.Background = new SolidColorBrush(footerBg);
    }


    /// <summary>
    /// Sets the default button style.
    /// </summary>
    protected void SetDefaultButton()
    {
        if (DefaultButton == DialogButton.Button1)
        {
            _btn1.IsDefault = true;
            _btn1.Classes.Add("accent");
        }
        else if (DefaultButton == DialogButton.Button2)
        {
            _btn2.IsDefault = true;
            _btn2.Classes.Add("accent");
        }
        else if (DefaultButton == DialogButton.Button3)
        {
            _btn3.IsDefault = true;
            _btn3.Classes.Add("accent");
        }
    }


    /// <summary>
    /// Sets the default focused button.
    /// </summary>
    protected void SetDefaultFocus()
    {
        if (DefaultFocus == DialogFocus.Button1)
        {
            _btn1.Focus(Avalonia.Input.NavigationMethod.Directional);
        }
        else if (DefaultFocus == DialogFocus.Button2)
        {
            _btn2.Focus(Avalonia.Input.NavigationMethod.Tab);
        }
        else if (DefaultFocus == DialogFocus.Button3)
        {
            _btn3.Focus(Avalonia.Input.NavigationMethod.Tab);
        }
    }


    /// <summary>
    /// Shows dialog.
    /// </summary>
    public async Task<DialogExitCode> ShowAsync(IgWindow? owner)
    {
        _taskSourceExitCode = new TaskCompletionSource<DialogExitCode>();

        if (owner is not null)
        {
            await ShowDialog(owner);
        }
        else
        {
            Show();
        }


        // wait for exit code
        var exitCode = await _taskSourceExitCode.Task;

        return exitCode;
    }

    #endregion // Methods


}
