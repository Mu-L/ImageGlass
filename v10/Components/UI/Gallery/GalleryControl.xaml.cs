/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
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
using Catel.Collections;
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ImageGlass.UI;

public sealed partial class GalleryControl : IgControl
{
    public static double ItemSpacing => 1;
    public event TypedEventHandler<IgGalleryItem, EventArgs>? ItemClicked;

    private InterlockedBool _isLoadingFirstItem = new();
    private Progress<ThumbnailLoadedEventArgs> _progressThumbnailLoader;


    #region Public Properties

    /// <summary>
    /// Gets the <see cref="ItemsRepeater"/> element of gallery control.
    /// </summary>
    public ItemsRepeater ListEl => PART_GalleryItemRepeater;


    /// <summary>
    /// Gets the items source.
    /// </summary>
    public FastObservableCollection<Photo> Items
    {
        get; private set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
            _ = OnPropertyChanged(nameof(GalleryVisibility));
        }
    } = [];


    /// <summary>
    /// Gets the visibility of gallery list.
    /// </summary>
    public Visibility GalleryVisibility => Items.Count > 0 && IsContentVisible
        ? Visibility.Visible
        : Visibility.Collapsed;


    #endregion // Public Properties



    public GalleryControl()
    {
        InitializeComponent();

        _progressThumbnailLoader = new Progress<ThumbnailLoadedEventArgs>(GalleryItemThumbnail_Loaded);
    }



    #region Control Events

    protected override void OnIgSizeChanged(FrameworkElement fe, SizeChangedEventArgs e)
    {
        base.OnIgSizeChanged(fe, e);
        UpdateControlPadding();
    }


    private void PART_ItemRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        LoadThumbnail(e.Index, true);
    }


    private async void GalleryItemThumbnail_Loaded(ThumbnailLoadedEventArgs e)
    {
        if (e.Bitmap == null)
        {
            e.Sender.ThumbnailBitmap = null;
            e.Sender.GalleryThumbnail = null;
            return;
        }

        try
        {
            e.Sender.ThumbnailBitmap = e.Bitmap;

            // load bitmap source to the UI
            var bmpSource = new SoftwareBitmapSource();
            await bmpSource.SetBitmapAsync(e.Bitmap);

            e.Sender.GalleryThumbnail = bmpSource;
        }
        catch
        {
            e.Sender.ThumbnailBitmap = null;
            e.Sender.GalleryThumbnail = null;
        }
    }


    private void GalleryItem_Clicked(object sender, RoutedEventArgs e)
    {
        if (sender is not IgGalleryItem btnItem) return;
        if (btnItem.VM.IsCurrent) return;

        // scroll the clicked item into the view
        if (PART_ScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible
            || PART_ScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
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

    #endregion // Control Events



    #region Public Methods

    /// <summary>
    /// Removes the source binding.
    /// </summary>
    public async Task ClearSourceAsync()
    {
        PART_GalleryItemRepeater.ClearValue(ItemsRepeater.ItemsSourceProperty);
        Items = [];
        _isLoadingFirstItem.Clear();

        await WaitForLayoutAsync();
    }


    /// <summary>
    /// Sets source binding.
    /// </summary>
    public async Task SetSourceAsync(FastObservableCollection<Photo> items)
    {
        Items = items;

        PART_GalleryItemRepeater.SetBinding(ItemsRepeater.ItemsSourceProperty, new Binding()
        {
            Source = Items,
            Mode = BindingMode.OneWay,
        });

        await WaitForLayoutAsync();
    }


    /// <summary>
    /// Waits for layout updated.
    /// </summary>
    public Task WaitForLayoutAsync()
    {
        var tcs = new TaskCompletionSource();

        void handler(object? s, object e)
        {
            ListEl.LayoutUpdated -= handler;
            tcs.TrySetResult();
        }

        ListEl.LayoutUpdated += handler;
        return tcs.Task;
    }


    /// <summary>
    /// Update the padding of gallery according to scrollbar visibility.
    /// </summary>
    private void UpdateControlPadding()
    {
        if (PART_ScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
        {
            var padding = PART_ScrollViewer.Padding;

            PART_ScrollViewer.Padding = new Thickness(padding.Left, padding.Top, padding.Right, padding.Bottom)
            {
                Bottom = padding.Top * 2.5,
            };
        }
        else
        {
            var padding = PART_ScrollViewer.Padding;

            PART_ScrollViewer.Padding = new Thickness(padding.Left, padding.Top, padding.Right, padding.Bottom)
            {
                Bottom = padding.Top,
            };
        }
    }


    /// <summary>
    /// Scrolls the gallery to bring the specified item into view.
    /// </summary>
    public void ScrollToItem(int index, bool disableAnimation = true)
    {
        if (index < 0 || index >= Items.Count) return;

        var centerItemIndex = index + 1;
        var itemCenterX = (AP.Config.ThumbnailSize * centerItemIndex)
            + (ItemSpacing * centerItemIndex)
            - (PART_ScrollViewer.ViewportWidth / 2)
            - (AP.Config.ThumbnailSize / 2);

        PART_ScrollViewer.ChangeView(itemCenterX, null, null, disableAnimation);
    }


    /// <summary>
    /// Loads the thumbnail.
    /// </summary>
    public void LoadThumbnail(int index, bool useCache)
    {
        var el = PART_GalleryItemRepeater.TryGetElement(index);
        if (el is not IgGalleryItem item) return;

        // HACK: to make sure the ItemsRepeater does not load the index-0 item twice!
        if (index == 0 && _isLoadingFirstItem.Value) return;
        _isLoadingFirstItem.Set();

        // start loading thumbnail
        var thumbSize = AP.Config.ThumbnailSize * DpiScale;
        _ = item.VM.StartLoadingGalleryThumbnail(thumbSize, useCache, _progressThumbnailLoader);
    }


    #endregion // Public Methods


}

