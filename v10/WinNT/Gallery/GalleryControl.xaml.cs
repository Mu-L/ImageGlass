// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Catel.Collections;
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.WinNT.Common;
using ImageGlass.WinNT.Common.Photoing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vortice.WIC;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace ImageGlass.WinNT;

public sealed partial class GalleryControl : UserControl
{
    private ConcurrentDictionary<string, SoftwareBitmap?> _thumbMap = new(StringComparer.OrdinalIgnoreCase);

    public event TypedEventHandler<GalleryButtonItem, EventArgs>? ItemClicked;


    public static double GalleryThumbnailSize => (double)Application.Current.Resources["GalleryThumbnailSize"];

    public static double ItemSpacing => 1;


    public PhotoManager PhotoManager
    {
        get => (PhotoManager)GetValue(PhotoManagerProperty);
        set
        {
            SetValue(PhotoManagerProperty, (PhotoManager)value);
        }
    }
    public static readonly DependencyProperty PhotoManagerProperty =
        DependencyProperty.Register(
            nameof(PhotoManager),
            typeof(PhotoManager),
            typeof(GalleryControl),
            new PropertyMetadata(new PhotoManager()));


    public FastObservableCollection<PhotoPath> FileList
    {
        get => (FastObservableCollection<PhotoPath>)GetValue(FileListProperty);
        set
        {
            SetValue(FileListProperty, (FastObservableCollection<PhotoPath>)value);
        }
    }
    public static readonly DependencyProperty FileListProperty =
        DependencyProperty.Register(
            nameof(FileList),
            typeof(FastObservableCollection<PhotoPath>),
            typeof(GalleryControl),
            new PropertyMetadata(new FastObservableCollection<PhotoPath>()));


    public GalleryControl()
    {
        InitializeComponent();
    }


    public async Task<SoftwareBitmap?> GetThumbnailAsync(string filePath)
    {
        var photo = PhotoManager.Get(filePath);
        if (photo is null) return null;


        // 1. try get from cache if size is big enough
        var cachedThumb = _thumbMap.GetValueOrDefault(photo.FilePath);
        if (cachedThumb is not null)
        {
            return cachedThumb;
        }


        // 2. get a fresh thumbnail
        Log.Info($"Loading thumbnail for index={PhotoManager.IndexOf(filePath)}: {filePath}",
            nameof(GetThumbnailAsync), nameof(GalleryControl));


        await photo.LoadMetadataAsync();
        using var wicBmp = await photo.Metadata.GetPreviewAsync(
            GalleryThumbnailSize, default, ShellThumbnailOptions.BiggerSizeOk);

        // set thumbnail
        var softwareBmp = await SetThumbnailAsync(filePath, wicBmp);

        return softwareBmp;
    }


    public async Task<SoftwareBitmap?> SetThumbnailAsync(string filePath, IWICBitmapSource? wicBmp)
    {
        // remove the old thumbnail
        RemoveThumbnail(filePath);

        if (wicBmp is null) return null;


        // load new thumbnail
        var softwareBmp = await PhotoWIC.ConvertToSoftwareBitmap(wicBmp);

        // save to cache
        _thumbMap.TryAdd(filePath, softwareBmp);

        return softwareBmp;
    }


    public void RemoveThumbnail(string filePath)
    {
        _thumbMap.TryRemove(filePath, out var removedThumb);
        removedThumb?.Dispose();
    }


    public void ClearThumbnails()
    {
        // delete thumbnails cache
        foreach (var item in _thumbMap)
        {
            item.Value?.Dispose();
        }

        _thumbMap.Clear();
    }


    public void SelectItem(string filePath, bool disableAnimation = true)
    {
        var photo = PhotoManager.Select(filePath);
        if (photo is null) return;

        var itemsCount = photo.Index + 1;
        var itemCenterX = (GalleryThumbnailSize * itemsCount)
            + (ItemSpacing * itemsCount)
            - (GalleryScrollViewer.ViewportWidth / 2)
            - (GalleryThumbnailSize / 2);

        GalleryScrollViewer.ChangeView(itemCenterX, null, null, disableAnimation);
    }


    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        ClearThumbnails();
    }


    private void GalleryItem_Clicked(object sender, RoutedEventArgs e)
    {
        if (sender is not GalleryButtonItem btnItem) return;


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


    private async void GalleryItemRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is not GalleryButtonItem btnItem) return;
        if (PhotoManager.Get(e.Index) is not Photo photo) return;


        // 1. get the image element
        if (btnItem.FindName("GalleryItem_Thumbnail") is Image imgThumbnail)
        {
            // get the thumbnail
            var softwareBmp = await GetThumbnailAsync(photo.FilePath);
            var softwareBmpSrc = new SoftwareBitmapSource();
            await softwareBmpSrc.SetBitmapAsync(softwareBmp);

            // render the thumbnail
            imgThumbnail.Source = softwareBmpSrc;
        }

    }

}




