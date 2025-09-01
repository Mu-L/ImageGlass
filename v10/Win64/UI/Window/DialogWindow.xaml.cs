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
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace ImageGlass.Win64.UI;


public partial class DialogWindow : Window
{
    private IgWindowHook _winHook;
    private Window? _owner = null;
    private TaskCompletionSource<DialogResult> _resultCompletionSource = new();
    private readonly KeyboardAccelerator _closeByEscKey = new KeyboardAccelerator()
    {
        Key = VirtualKey.Escape,
        Modifiers = VirtualKeyModifiers.None,
        IsEnabled = true,
    };


    /// <summary>
    /// Gets or sets the result for the dialog.
    /// </summary>
    public DialogResult DialogResult { get; set; } = DialogResult.None;


    public DialogWindow()
    {
        InitializeComponent();

        _winHook = new(this);
        Closed += DialogWindow_Closed;
        Root.Loaded += Root_Loaded;
        Root.SizeChanged += Root_SizeChanged;

        // hotkey: ESC to close
        _closeByEscKey.Invoked += CloseByEscKey_Invoked;
        Content.KeyboardAccelerators.Add(_closeByEscKey);
    }


    private void DialogWindow_Closed(object sender, WindowEventArgs args)
    {
        Closed -= DialogWindow_Closed;
        Root.Loaded -= Root_Loaded;
        _closeByEscKey.Invoked -= CloseByEscKey_Invoked;
        Content.KeyboardAccelerators.Remove(_closeByEscKey);

        _winHook.Dispose();

        // if the dialog is closed unexpected, returns Abort code to break the while loop.
        if (DialogResult == DialogResult.None) DialogResult = DialogResult.Abort;

        // set the result to complete the task
        _resultCompletionSource.SetResult(DialogResult);

        // reactivate the owner window
        _owner?.Activate();
    }


    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_winHook.Titlebar == null)
        {
            _winHook.SetTitlebar(Root.Titlebar);
        }
    }


    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_owner is null || sender is not DialogContent fe) return;

        // only adjust the size once
        fe.SizeChanged -= Root_SizeChanged;

        // calculate the size of the dialog window according to the root content
        var dpiScale = fe.XamlRoot.RasterizationScale;
        var titlebarHeight = fe.Titlebar.DesiredSize.Height - 1;
        var windowHeight = (int)Math.Ceiling((fe.DesiredSize.Height - titlebarHeight) * dpiScale);
        var windowWidth = (int)Math.Ceiling(fe.DesiredSize.Width * dpiScale);

        // set dialog position to center the owner
        var ownerSize = _owner.AppWindow.Size;
        var posX = _owner.AppWindow.Position.X + ownerSize.Width / 2 - windowWidth / 2;
        var posY = _owner.AppWindow.Position.Y + ownerSize.Height / 2 - windowHeight / 2;

        // resize the dialog
        AppWindow.ResizeClient(new(windowWidth, windowHeight));
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
    protected virtual void OnAccepted()
    {
        DialogResult = DialogResult.OK;
        Close();
    }


    /// <summary>
    /// Closes the form and returns <see cref="DialogResult.Cancel"/> code.
    /// </summary>
    protected virtual void OnCancelled()
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }


    /// <summary>
    /// Sets <see cref="DialogResult"/> to <see cref="DialogResult.None"/>
    /// and does nothing.
    /// </summary>
    protected virtual void OnApplied()
    {
        DialogResult = DialogResult.None;
    }


    private void CloseByEscKey_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        OnAborted();
    }


    private void PART_Dialog_BtnAccept_Click(object sender, RoutedEventArgs e)
    {
        OnAccepted();
    }


    private void PART_Dialog_BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        OnCancelled();
    }


    private void PART_Dialog_BtnApply_Click(object sender, RoutedEventArgs e)
    {
        OnApplied();
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


        // create a dialog modal
        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsModal = true;
        presenter.IsResizable = false;
        AppWindow.SetPresenter(presenter);


        // show the dialog
        AppWindow.Show();


        // wait for dialog result
        return await _resultCompletionSource.Task;
    }


}
