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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ImageGlass.Win64.UI;


public sealed partial class PopupWindow_Content : UserControl, INotifyPropertyChanged
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


    private readonly double THUMBNAIL_SIZE = 80;


    #region Public Properties

    /// <summary>
    /// Gets view model from data context.
    /// </summary>
    public PopupWindowViewModel VM => ((PopupWindowViewModel)DataContext) ?? new();


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
    } = "Don't show this message again";


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



    public PopupWindow_Content()
    {
        InitializeComponent();

        Root.Loaded += Root_Loaded;
        Root.Unloaded += Root_Unloaded;
        DataContextChanged += PopupWindow_Content_DataContextChanged;
    }


    #region Control Events

    private void Root_Unloaded(object sender, RoutedEventArgs e)
    {
        Root.Loaded -= Root_Loaded;
        Root.Unloaded -= Root_Unloaded;
        DataContextChanged -= PopupWindow_Content_DataContextChanged;
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        _ = LoadThumbnailSourceAsync();
        _ = LoadThumbnailIconSourceAsync();
    }

    private void PopupWindow_Content_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
    {
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
        var dpiScale = Content.XamlRoot.RasterizationScale;
        var maxSize = THUMBNAIL_SIZE * dpiScale;

        var imgWidth = VM.Thumbnail.PixelWidth;
        var imgHeight = VM.Thumbnail.PixelHeight;

        // check if the thumbnail is smaller than max siE
        if (imgWidth < maxSize || imgHeight < maxSize)
        {
            // render as original size
            PART_Thumbnail.Width = imgWidth / dpiScale;
            PART_Thumbnail.Height = imgHeight / dpiScale;

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
        var dpiScale = XamlRoot.RasterizationScale;
        using var sb = await IconApi.GetSystemIconAsync(VM.ThumbnailIcon, (int)(40 * dpiScale));
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
    public PopupFormValue GetFormValue()
    {
        var value = new PopupFormValue()
        {
            InputValue = PART_Input.Text,
            IsRememberOptionChecked = PART_Checkbox.IsChecked ?? false,
        };

        return value;
    }

    #endregion // Public Methods


}



public class PopupFormValue
{
    public string InputValue { get; set; } = "";
    public bool IsRememberOptionChecked { get; set; } = false;
}