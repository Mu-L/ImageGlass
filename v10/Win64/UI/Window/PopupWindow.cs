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
    public static async Task<DialogResult> ShowAsync(Window owner,
        string? title,
        string? heading,
        string? description,
        string? details,
        string? note,
        bool showInput,
        bool showRemember,
        uint buttonCount,
        SoftwareBitmap? thumbnail,
        StockIconId? thumbnailIcon)
    {
        var popup = new PopupWindow()
        {
            TitlebarText = title,
            IsButton1Visible = buttonCount >= 1,
            IsButton2Visible = buttonCount >= 2,
            IsButton3Visible = buttonCount >= 3,
            DefaultFocus = DialogFocus.Button1,
            VM = new PopupWindowViewModel()
            {
                Heading = heading,
                Description = description,
                Details = details,
                Note = note,
                IsRememberOptionVisible = showRemember,
                IsInputVisible = showInput,
                Thumbnail = thumbnail,
                ThumbnailIcon = thumbnailIcon,
            },
        };

        var result = await popup.ShowAsync(owner);

        return result;
    }


    public static async Task<DialogResult> ShowErrorAsync(Window owner,
        string? title = null,
        string? description = null,
        string? heading = null,
        string? details = null)
    {
        heading ??= "Error"; // TODO: lang
        using var thumbnail = await IconApi.GetSystemIconAsync(StockIconId.Error, 128);

        return await ShowAsync(owner,
            title, heading, description, details, null, false, false, 1, thumbnail, null);
    }

}
