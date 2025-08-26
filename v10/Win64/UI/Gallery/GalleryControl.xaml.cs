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
using ImageGlass.Win64.Common.Photoing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace ImageGlass.Win64.UI;

public sealed partial class GalleryControl : UserControl, INotifyPropertyChanged
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


    public static double GalleryThumbnailSize => (double)Application.Current.Resources[nameof(GalleryThumbnailSize)];
    public static double ItemSpacing => 1;
    public event TypedEventHandler<IgGalleryItem, EventArgs>? ItemClicked;

    private PhotoManager _vm = new();
    private Progress<ThumbnailLoadedEventArgs> _progressThumbnailLoader;


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets, sets view model for this control.
    /// </summary>
    public PhotoManager VM
    {
        get => _vm;
        set
        {
            if (_vm != value)
            {
                _vm = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion // Public Properties



    public GalleryControl()
    {
        InitializeComponent();

        _progressThumbnailLoader = new Progress<ThumbnailLoadedEventArgs>(GalleryItemThumbnail_Loaded);
        Unloaded += GalleryControl_Unloaded;
        SizeChanged += GalleryControl_SizeChanged;
    }

    private void GalleryControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= GalleryControl_Unloaded;
        SizeChanged -= GalleryControl_SizeChanged;

        ClearPropertyChangedEvents();
    }


    private void GalleryControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (GalleryScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
        {
            var padding = GalleryScrollViewer.Padding;

            GalleryScrollViewer.Padding = new Thickness(padding.Left, padding.Top, padding.Right, padding.Bottom)
            {
                Bottom = padding.Top * 2.5,
            };
        }
        else
        {
            var padding = GalleryScrollViewer.Padding;

            GalleryScrollViewer.Padding = new Thickness(padding.Left, padding.Top, padding.Right, padding.Bottom)
            {
                Bottom = padding.Top,
            };
        }
    }


    private void GalleryItemRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is not IgGalleryItem item) return;

        // start loading thumbnail
        item.VM.LoadGalleryThumbnail(GalleryThumbnailSize, _progressThumbnailLoader);
    }


    private void GalleryItemRepeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element is not IgGalleryItem item) return;

        // cancel loading thumbnail
        item.VM.CancelLoadingGalleryThumbnail();
    }


    private async void GalleryItemThumbnail_Loaded(ThumbnailLoadedEventArgs e)
    {
        if (e.Bitmap == null)
        {
            e.Sender.GalleryThumbnail = null;
            return;
        }

        try
        {
            // load bitmap source to the UI
            var bmpSource = new SoftwareBitmapSource();
            await bmpSource.SetBitmapAsync(e.Bitmap);

            e.Sender.GalleryThumbnail = bmpSource;
        }
        catch
        {
            e.Sender.GalleryThumbnail = null;
        }
    }


    private void GalleryItem_Clicked(object sender, RoutedEventArgs e)
    {
        if (sender is not IgGalleryItem btnItem) return;


        // scroll the clicked item into the view
        if (GalleryScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible
            || GalleryScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
        {
            btnItem.StartBringIntoView(new BringIntoViewOptions()
            {
                VerticalAlignmentRatio = 0.5,
                HorizontalAlignmentRatio = 0.5,
                AnimationDesired = true,
            });
        }

        ItemClicked?.Invoke(btnItem, EventArgs.Empty);
    }


    /// <summary>
    /// Scrolls the gallery to bring the specified item into view.
    /// </summary>
    public void ScrollToItem(int index, bool disableAnimation = true)
    {
        if (index < 0 || index >= VM.Count) return;

        var centerItemIndex = index + 1;
        var itemCenterX = (GalleryThumbnailSize * centerItemIndex)
            + (ItemSpacing * centerItemIndex)
            - (GalleryScrollViewer.ViewportWidth / 2)
            - (GalleryThumbnailSize / 2);

        GalleryScrollViewer.ChangeView(itemCenterX, null, null, disableAnimation);
    }


}




