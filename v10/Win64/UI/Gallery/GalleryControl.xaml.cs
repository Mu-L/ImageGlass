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
using ImageGlass.Win64.Common.Photoing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Foundation;

namespace ImageGlass.Win64.UI;

public sealed partial class GalleryControl : IgControl
{
    public static double ItemSpacing => 1;
    public event TypedEventHandler<IgGalleryItem, EventArgs>? ItemClicked;
    private Progress<ThumbnailLoadedEventArgs> _progressThumbnailLoader;


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets, sets view model for this control.
    /// </summary>
    public PhotoManager VM
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = new();

    #endregion // Public Properties



    public GalleryControl()
    {
        InitializeComponent();

        _progressThumbnailLoader = new Progress<ThumbnailLoadedEventArgs>(GalleryItemThumbnail_Loaded);
    }


    protected override void OnIgSizeChanged(FrameworkElement fe, SizeChangedEventArgs e)
    {
        base.OnIgSizeChanged(fe, e);

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
        item.VM.LoadGalleryThumbnail(AP.Config.ThumbnailSize, _progressThumbnailLoader);
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
        var itemCenterX = (AP.Config.ThumbnailSize * centerItemIndex)
            + (ItemSpacing * centerItemIndex)
            - (GalleryScrollViewer.ViewportWidth / 2)
            - (AP.Config.ThumbnailSize / 2);

        GalleryScrollViewer.ChangeView(itemCenterX, null, null, disableAnimation);
    }


}




