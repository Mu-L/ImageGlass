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
using ImageGlass.Common;
using Microsoft.UI.Xaml.Controls;
using System.Drawing;
using Windows.Graphics.Imaging;

namespace ImageGlass.Win64.UI;


public class PopupFormValue
{
    public string InputValue { get; set; } = "";
    public bool IsRememberOptionChecked { get; set; } = false;
}


public partial class PopupWindowViewModel : DisposableImpl
{
    public string? Heading
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = null;

    public string? Description
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = null;

    public string? Details
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = null;

    public string? Note
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = null;

    public InfoBarSeverity NoteStyle
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = InfoBarSeverity.Informational;

    public SoftwareBitmap? Thumbnail
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = null;

    public StockIconId? ThumbnailIcon
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsThumbnailSectionVisible));
            }
        }
    } = null;

    public bool IsThumbnailSectionVisible => Thumbnail != null || ThumbnailIcon != null;

    public bool IsRememberOptionVisible
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

    public bool IsInputVisible
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


    public string InputValue
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = "";


    public TextBoxAcceptValue AcceptValue
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = TextBoxAcceptValue.Any;



    protected override void OnDisposing()
    {
        base.OnDisposing();

        Thumbnail?.Dispose();
        Thumbnail = null;
    }

}

