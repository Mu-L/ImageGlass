// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using ImageGlass.Common;
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
using Windows.Graphics.Imaging;

namespace ImageGlass.WinNT;

public sealed partial class GalleryControl : UserControl
{
    private ConcurrentDictionary<string, SoftwareBitmap?> _thumbMap = new(StringComparer.OrdinalIgnoreCase);

    public double ItemSize { get; set; } = 70;


    public PhotoManager PhotoManager { get; set; } = new();


    public List<Photo> ItemsSource
    {
        get => GalleryItemRepeater.ItemsSource as List<Photo> ?? [];
        set
        {
            GalleryItemRepeater.ItemsSource = null;
            ClearThumbnails();

            GalleryItemRepeater.ItemsSource = value;
        }
    }


    public GalleryControl()
    {
        InitializeComponent();
    }


    public void SelectItem()
    {

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
            ItemSize, default, ShellThumbnailOptions.BiggerSizeOk);

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


    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        ClearThumbnails();
    }


    private void GalleryItem_Clicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;

        // When the clicked item has been received, bring it to the middle of the viewport.
        fe.StartBringIntoView(new BringIntoViewOptions()
        {
            VerticalAlignmentRatio = 0.5,
            HorizontalAlignmentRatio = 0.5,
            AnimationDesired = true,
        });
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
        if (e.Element is not Button item) return;
        if (PhotoManager.Get(e.Index) is not Photo photo) return;

        // get the image element
        if (item.FindName("GalleryItem_Thumbnail") is not Image img) return;

        // get the thumbnail
        var softwareBmp = await GetThumbnailAsync(photo.FilePath);
        var softwareBmpSrc = new SoftwareBitmapSource();
        await softwareBmpSrc.SetBitmapAsync(softwareBmp);

        // render the thumbnail
        img.Source = softwareBmpSrc;
    }


}
