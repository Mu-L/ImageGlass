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
using ImageGlass.Win64.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

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


    #region Public Properties

    public PopupWindowViewModel VM
    {
        get; set
        {
            if (field != value)
            {
                field?.Dispose();
                field = value;
                OnPropertyChanged();
            }
        }
    } = new PopupWindowViewModel();

    #endregion Public Properties



    public PopupWindow_Content()
    {
        InitializeComponent();

        DataContextChanged += PopupWindow_Content_DataContextChanged;
        Root.Loaded += Root_Loaded;
        Root.Unloaded += Root_Unloaded;
    }

    private void PopupWindow_Content_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
    {
        if (e.NewValue is PopupWindowViewModel vm) VM = vm;
        else VM = new();
    }

    private void Root_Unloaded(object sender, RoutedEventArgs e)
    {
        DataContextChanged -= PopupWindow_Content_DataContextChanged;
        Root.Loaded -= Root_Loaded;
        Root.Unloaded -= Root_Unloaded;
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        _ = LoadThumbnailSourceAsync();
        _ = LoadThumbnailIconSourceAsync();
    }


    /// <summary>
    /// Loads thumbnail.
    /// </summary>
    public async Task LoadThumbnailSourceAsync()
    {
        if (VM.Thumbnail is null) return;
        if (PART_Thumbnail.Source is not null) return;


        // create software bitmap source
        var sbSrc = new SoftwareBitmapSource();
        await sbSrc.SetBitmapAsync(VM.Thumbnail);

        // set the icon
        PART_Thumbnail.Source = sbSrc;

    }


    /// <summary>
    /// Loads thumbnail icon.
    /// </summary>
    public async Task LoadThumbnailIconSourceAsync()
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    } = null;

    public string? RememberOptionText
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = string.Empty;

    public SoftwareBitmap? Thumbnail
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsThumbnailSectionVisible));
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    } = false;


    protected override void OnDisposing()
    {
        base.OnDisposing();

        Thumbnail?.Dispose();
        Thumbnail = null;
    }

}
