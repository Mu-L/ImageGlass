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
using System.Drawing;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImageGlass.Win64.UI;

public partial class PopupWindow : DialogWindow
{
    private readonly PopupWindow_Content _contentEl = new();


    public PopupWindow()
    {
        DialogContent = _contentEl;
    }


    protected override void OnButton1Clicked(DialogButtonClickedEventArgs e)
    {
        // don't proceed if value is invalid
        if (!_contentEl.Validate()) return;

        base.OnButton1Clicked(e);
    }


    /// <summary>
    /// Shows modal dialog.
    /// </summary>
    public static async Task<PopupResult> ShowAsync(Window? owner,
        string? title,
        PopupButton buttons,
        PopupWindowViewModel vm,
        DialogFocus defaultFocus = DialogFocus.Button1)
    {
        var popup = new PopupWindow()
        {
            TitlebarText = title,
            DefaultFocus = defaultFocus,
            DialogContentDataContext = vm,
        };

        // TODO: lang
        switch (buttons)
        {
            case PopupButton.OK:
                popup.Button1Text = "OK";
                popup.IsButton1Visible = true;
                popup.IsButton2Visible = popup.IsButton3Visible = false;
                break;

            case PopupButton.Close:
                popup.Button1Text = "Close";
                popup.IsButton1Visible = true;
                popup.IsButton2Visible = popup.IsButton3Visible = false;
                break;

            case PopupButton.Yes_No:
                popup.Button1Text = "Yes";
                popup.Button2Text = "No";
                popup.IsButton1Visible = popup.IsButton2Visible = true;
                popup.IsButton3Visible = false;
                break;

            case PopupButton.OK_Cancel:
                popup.Button1Text = "OK";
                popup.Button2Text = "Cancel";
                popup.IsButton1Visible = popup.IsButton2Visible = true;
                popup.IsButton3Visible = false;
                break;

            case PopupButton.OK_Close:
                popup.Button1Text = "OK";
                popup.Button2Text = "Close";
                popup.IsButton1Visible = popup.IsButton2Visible = true;
                popup.IsButton3Visible = false;
                break;

            case PopupButton.LearnMore_Close:
                popup.Button1Text = "Learn more";
                popup.Button2Text = "Close";
                popup.IsButton1Visible = popup.IsButton2Visible = true;
                popup.IsButton3Visible = false;
                break;

            case PopupButton.Continue_Quit:
                popup.Button1Text = "Continue";
                popup.Button2Text = "Quit";
                popup.IsButton1Visible = popup.IsButton2Visible = true;
                popup.IsButton3Visible = false;
                break;

            default:
                break;
        }


        var exitCode = await popup.ShowAsync(owner);

        // get dialog result
        var formValue = popup._contentEl.GetFormValue();
        var result = new PopupResult()
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
    public static async Task<PopupResult> ShowWarningAsync(Window? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? note = null,
        PopupButton buttons = PopupButton.OK,
        StockIconId? thumbnailIcon = null,
        SoftwareBitmap? thumbnail = null,
        InfoBarSeverity noteStyle = InfoBarSeverity.Warning,
        bool showRememberOption = false)
    {
        heading ??= "Warning"; // TODO: lang

        // use stock icon as thumbnail
        if (thumbnail is null)
        {
            thumbnail = await IconApi.GetSystemIconAsync(thumbnailIcon ?? StockIconId.Warning, 128);
            thumbnailIcon = null;
        }

        using var vm = new PopupWindowViewModel()
        {
            Description = description,
            Heading = heading,
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
    public static async Task<PopupResult> ShowErrorAsync(Window? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? details = null,
        PopupButton buttons = PopupButton.OK)
    {
        heading ??= "Error"; // TODO: lang

        return await ShowWarningAsync(owner, title, description, heading, null, buttons, StockIconId.Error, null, InfoBarSeverity.Error, false);
    }


    /// <summary>
    /// Shows modal dialog for information.
    /// </summary>
    public static async Task<PopupResult> ShowInfoAsync(Window? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? note = null,
        PopupButton buttons = PopupButton.OK,
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
    public static async Task<PopupResult> ShowInputAsync(Window? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? inputValue = null,
        PopupButton buttons = PopupButton.OK_Cancel,
        StockIconId? thumbnailIcon = null,
        SoftwareBitmap? thumbnail = null)
    {
        // use stock icon as thumbnail
        if (thumbnail is null)
        {
            thumbnail = await IconApi.GetSystemIconAsync(thumbnailIcon ?? StockIconId.Warning, 128);
            thumbnailIcon = null;
        }

        using var vm = new PopupWindowViewModel()
        {
            Description = description,
            Heading = heading,
            Thumbnail = thumbnail,
            ThumbnailIcon = thumbnailIcon,
            IsInputVisible = true,
            InputValue = inputValue ?? "",
        };

        return await ShowAsync(owner, title, buttons, vm, DialogFocus.Default);

    }

}
