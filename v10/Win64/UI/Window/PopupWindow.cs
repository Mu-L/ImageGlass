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
    public PopupWindow()
    {
        DialogContent = new PopupWindow_Content();
    }


    /// <summary>
    /// Shows modal dialog window.
    /// </summary>
    public static async Task<DialogResult> ShowAsync(Window? owner,
        string? title,
        uint buttonCount,
        PopupWindowViewModel vm)
    {
        var popup = new PopupWindow()
        {
            TitlebarText = title,
            IsButton1Visible = buttonCount >= 1,
            IsButton2Visible = buttonCount >= 2,
            IsButton3Visible = buttonCount >= 3,
            DefaultFocus = DialogFocus.Button1,
            VM = vm,
        };

        var result = await popup.ShowAsync(owner);

        return result;
    }


    /// <summary>
    /// Shows modal dialog window for warning.
    /// </summary>
    public static async Task<DialogResult> ShowWarningAsync(Window? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? note = null,
        bool showRememberOption = false,
        StockIconId? thumbnailIcon = null,
        SoftwareBitmap? thumbnail = null,
        InfoBarSeverity noteStyle = InfoBarSeverity.Warning)
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

        return await ShowAsync(owner, title, 2, vm);
    }


    /// <summary>
    /// Shows modal dialog window for error.
    /// </summary>
    public static async Task<DialogResult> ShowErrorAsync(Window? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? details = null)
    {
        heading ??= "Error"; // TODO: lang

        return await ShowWarningAsync(owner, title, description, heading, null, false, StockIconId.Error, null, InfoBarSeverity.Error);
    }


    /// <summary>
    /// Shows modal dialog window for information.
    /// </summary>
    public static async Task<DialogResult> ShowInfoAsync(Window? owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? note = null,
        StockIconId? thumbnailIcon = null,
        SoftwareBitmap? thumbnail = null,
        InfoBarSeverity noteStyle = InfoBarSeverity.Informational)
    {
        heading ??= "";
        thumbnailIcon ??= StockIconId.Info;

        return await ShowWarningAsync(owner, title, description, heading, note, false, thumbnailIcon, thumbnail, noteStyle);
    }

}
