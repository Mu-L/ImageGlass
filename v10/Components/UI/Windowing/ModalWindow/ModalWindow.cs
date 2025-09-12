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
using Microsoft.UI.Xaml.Controls;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImageGlass.UI;

public partial class ModalWindow : DialogWindow
{
    private readonly ModalWindow_Content _contentEl = new();


    public ModalWindow()
    {
        DialogContent = _contentEl;
    }


    protected override void OnDialogSubmitted(DialogEventArgs e)
    {
        // don't proceed if value is invalid
        if (!_contentEl.Validate()) return;

        base.OnDialogSubmitted(e);
    }


    /// <summary>
    /// Shows modal dialog.
    /// </summary>
    public static async Task<ModalWindowResult> ShowAsync(IgWindow? owner,
        string? title,
        ModalWindowButton buttons,
        ModalWindowViewModel vm,
        DialogFocus defaultFocus = DialogFocus.Button1)
    {
        var modal = new ModalWindow()
        {
            WindowTitle = title,
            DefaultFocus = defaultFocus,
            WindowContentDataContext = vm,
        };

        // TODO: lang
        switch (buttons)
        {
            case ModalWindowButton.OK:
                modal.Button1Text = "OK";
                modal.IsButton1Visible = true;
                modal.IsButton2Visible = modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.Close:
                modal.Button1Text = "Close";
                modal.IsButton1Visible = true;
                modal.IsButton2Visible = modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.Yes_No:
                modal.Button1Text = "Yes";
                modal.Button2Text = "No";
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.OK_Cancel:
                modal.Button1Text = "OK";
                modal.Button2Text = "Cancel";
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.OK_Close:
                modal.Button1Text = "OK";
                modal.Button2Text = "Close";
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.LearnMore_Close:
                modal.Button1Text = "Learn more";
                modal.Button2Text = "Close";
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.Continue_Quit:
                modal.Button1Text = "Continue";
                modal.Button2Text = "Quit";
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            default:
                break;
        }


        var exitCode = await modal.ShowAsync(owner);

        // get dialog result
        var formValue = modal._contentEl.GetFormValue();
        var result = new ModalWindowResult()
        {
            ExitCode = exitCode,
            InputValue = formValue.InputValue,
            IsRememberOptionChecked = formValue.IsRememberOptionChecked,
        };

        return result;
    }


    /// <summary>
    /// Shows modal dialog for warning.
    /// </summary>
    public static async Task<ModalWindowResult> ShowWarningAsync(IgWindow? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? note = null,
        ModalWindowButton buttons = ModalWindowButton.OK,
        StockIconId? thumbnailIcon = null,
        SoftwareBitmap? thumbnail = null,
        InfoBarSeverity noteStyle = InfoBarSeverity.Warning,
        bool showRememberOption = false,
        string? details = null)
    {
        heading ??= "Warning"; // TODO: lang

        // use stock icon as thumbnail
        if (thumbnail is null)
        {
            thumbnail = await IconApi.GetSystemIconAsync(thumbnailIcon ?? StockIconId.Warning, 128);
            thumbnailIcon = null;
        }

        using var vm = new ModalWindowViewModel()
        {
            Description = description,
            Heading = heading,
            Details = details,
            Note = note,
            NoteStyle = noteStyle,
            Thumbnail = thumbnail,
            ThumbnailIcon = thumbnailIcon,
            IsRememberOptionVisible = showRememberOption,
        };

        return await ShowAsync(owner, title, buttons, vm);
    }


    /// <summary>
    /// Shows modal dialog for error.
    /// </summary>
    public static async Task<ModalWindowResult> ShowErrorAsync(IgWindow? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? details = null,
        ModalWindowButton buttons = ModalWindowButton.OK)
    {
        heading ??= "Error"; // TODO: lang

        return await ShowWarningAsync(owner, title, description, heading, null, buttons, StockIconId.Error, null, InfoBarSeverity.Error, false, details);
    }


    /// <summary>
    /// Shows modal dialog for information.
    /// </summary>
    public static async Task<ModalWindowResult> ShowInfoAsync(IgWindow? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? note = null,
        ModalWindowButton buttons = ModalWindowButton.OK,
        StockIconId? thumbnailIcon = null,
        SoftwareBitmap? thumbnail = null,
        InfoBarSeverity noteStyle = InfoBarSeverity.Informational)
    {
        heading ??= "";
        thumbnailIcon ??= StockIconId.Info;

        return await ShowWarningAsync(owner, title, description, heading, note, buttons, thumbnailIcon, thumbnail, noteStyle, false);
    }


    /// <summary>
    /// Shows modal dialog for input.
    /// </summary>
    public static async Task<ModalWindowResult> ShowInputAsync(IgWindow? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? inputValue = null,
        ModalWindowButton buttons = ModalWindowButton.OK_Cancel,
        StockIconId? thumbnailIcon = null,
        SoftwareBitmap? thumbnail = null,
        TextBoxAcceptValue acceptValue = TextBoxAcceptValue.Any)
    {
        // use stock icon as thumbnail
        if (thumbnail is null)
        {
            thumbnail = await IconApi.GetSystemIconAsync(thumbnailIcon ?? StockIconId.Warning, 128);
            thumbnailIcon = null;
        }

        using var vm = new ModalWindowViewModel()
        {
            Description = description,
            Heading = heading,
            Thumbnail = thumbnail,
            ThumbnailIcon = thumbnailIcon,
            IsInputVisible = true,
            InputValue = inputValue ?? "",
            AcceptValue = acceptValue,
        };

        return await ShowAsync(owner, title, buttons, vm, DialogFocus.Default);

    }

}
