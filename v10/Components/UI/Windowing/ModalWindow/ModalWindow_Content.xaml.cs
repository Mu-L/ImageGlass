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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace ImageGlass.UI;


internal sealed partial class ModalWindow_Content : IgControl
{
    private readonly double THUMBNAIL_SIZE = 80;


    #region Public Properties

    /// <summary>
    /// Gets view model from data context.
    /// </summary>
    public ModalWindowViewModel VM => ((ModalWindowViewModel)DataContext) ?? new();


    internal string RememberOptionText
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "[Don't show this message again]";


    internal bool IsInputInvalid
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


    #endregion // Public Properties



    public ModalWindow_Content()
    {
        InitializeComponent();
    }


    #region Control Events

    protected override void OnIgLoaded(FrameworkElement fe)
    {
        base.OnIgLoaded(fe);

        // set focus to textbox
        if (VM.IsInputVisible)
        {
            PART_Input.Focus(FocusState.Keyboard);
            PART_Input.SelectAll();
        }

        _ = LoadThumbnailSourceAsync();
        _ = LoadThumbnailIconSourceAsync();
    }

    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        RememberOptionText = AP.Config.Lang[LangId._DoNotShowThisMessageAgain];
    }

    protected override void OnIgDataContextChanged(FrameworkElement fe, DataContextChangedEventArgs e)
    {
        base.OnIgDataContextChanged(fe, e);

        // notify the change of view model
        OnPropertyChanged(nameof(VM));
    }

    #endregion // Control Events


    #region Private Methods

    /// <summary>
    /// Loads thumbnail.
    /// </summary>
    private async Task LoadThumbnailSourceAsync()
    {
        if (VM.Thumbnail is null) return;
        if (PART_Thumbnail.Source is not null) return;

        // get the max size after DPI
        var maxSize = THUMBNAIL_SIZE * DpiScale;

        var imgWidth = VM.Thumbnail.PixelWidth;
        var imgHeight = VM.Thumbnail.PixelHeight;

        // check if the thumbnail is smaller than max siE
        if (imgWidth < maxSize || imgHeight < maxSize)
        {
            // render as original size
            PART_Thumbnail.Width = imgWidth / DpiScale;
            PART_Thumbnail.Height = imgHeight / DpiScale;

            PART_ThumbnailViewbox.Stretch = Stretch.None;
        }


        // create software bitmap source
        var sbSrc = new SoftwareBitmapSource();
        await sbSrc.SetBitmapAsync(VM.Thumbnail);

        // set the icon
        PART_Thumbnail.Source = sbSrc;
    }


    /// <summary>
    /// Loads thumbnail icon.
    /// </summary>
    private async Task LoadThumbnailIconSourceAsync()
    {
        if (PART_ThumbnailIcon.Source is not null) return;

        // get system icon
        using var sb = await IconApi.GetSystemIconAsync(VM.ThumbnailIcon, (int)(40 * DpiScale));
        if (sb is null) return;

        // create software bitmap source
        var sbSrc = new SoftwareBitmapSource();
        await sbSrc.SetBitmapAsync(sb);

        // set the icon
        PART_ThumbnailIcon.Source = sbSrc;
    }

    #endregion // Private Methods


    #region Public Methods

    /// <summary>
    /// Validates the form.
    /// </summary>
    public bool Validate()
    {
        var isValid = true;

        // validate the input
        if (VM.IsInputVisible)
        {
            isValid = PART_Input.Validate();
        }


        // animate error icon
        if (!isValid) PART_Input.AnimateErrorIcon();

        IsInputInvalid = !isValid;
        return isValid;
    }


    /// <summary>
    /// Gets form value.
    /// </summary>
    public ModalWindowData GetFormValue()
    {
        var value = new ModalWindowData()
        {
            InputValue = PART_Input.Text,
            IsRememberOptionChecked = PART_Checkbox.IsChecked ?? false,
        };

        return value;
    }

    #endregion // Public Methods


}

