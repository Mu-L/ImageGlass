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
using Windows.Foundation;

namespace ImageGlass.Win64.UI;

public sealed partial class GalleryControl : UserControl
{
    private Progress<ThumbnailLoadedEventArgs> _progressThumbnailLoader;

    public event TypedEventHandler<IgGalleryItem, EventArgs>? ItemClicked;


    public static double GalleryThumbnailSize => (double)Application.Current.Resources[nameof(GalleryThumbnailSize)];

    public static double ItemSpacing => 1;


    public PhotoManager PhotoManager
    {
        get => (PhotoManager)GetValue(PhotoManagerProperty);
        set => SetValue(PhotoManagerProperty, value);
    }
    public static readonly DependencyProperty PhotoManagerProperty =
        DependencyProperty.Register(
            nameof(PhotoManager),
            typeof(PhotoManager),
            typeof(GalleryControl),
            new PropertyMetadata(new PhotoManager()));



    public GalleryControl()
    {
        InitializeComponent();

        _progressThumbnailLoader = new Progress<ThumbnailLoadedEventArgs>(Thumbnail_Loaded);
    }


    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {

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


    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
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


    private async void Thumbnail_Loaded(ThumbnailLoadedEventArgs e)
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


    private void GalleryItemRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is not IgGalleryItem btnItem) return;

        // start loading thumbnail
        btnItem.ViewModel.LoadGalleryThumbnail(GalleryThumbnailSize, _progressThumbnailLoader);
    }


    private void GalleryItemRepeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element is not IgGalleryItem btnItem) return;

        // cancel loading thumbnail
        btnItem.ViewModel.CancelLoadingGalleryThumbnail();
    }





    /// <summary>
    /// Scrolls the gallery to bring the specified item into view.
    /// </summary>
    public void ScrollToItem(int index, bool disableAnimation = true)
    {
        if (index < 0 || index >= PhotoManager.Count) return;

        var centerItemIndex = index + 1;
        var itemCenterX = (GalleryThumbnailSize * centerItemIndex)
            + (ItemSpacing * centerItemIndex)
            - (GalleryScrollViewer.ViewportWidth / 2)
            - (GalleryThumbnailSize / 2);

        GalleryScrollViewer.ChangeView(itemCenterX, null, null, disableAnimation);
    }


}




