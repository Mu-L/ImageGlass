// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace ImageGlass.WinNT;

public sealed partial class GalleryControl : UserControl
{
    private double ItemWidth = 0;
    private Thickness ItemMargin = new();


    public GalleryControl()
    {
        InitializeComponent();

        InitializeData();
    }


    private void GalleryScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
    {
        var selectedItem = GetSelectedItemFromViewport();
        if (selectedItem is null) return;

        // update corresponding rectangle with selected color
        GalleryScrollViewer.Background = selectedItem.Background;
    }


    private void GalleryItem_Selected(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            // When the clicked item has been received, bring it to the middle of the viewport.
            fe.StartBringIntoView(new BringIntoViewOptions()
            {
                VerticalAlignmentRatio = 0.5,
                AnimationDesired = true,
            });
        }


        if (sender is Button item)
        {
            // Update corresponding rectangle with selected color
            GalleryScrollViewer.Background = item.Background;
        }
    }



    private void InitializeData()
    {
        var colors = new List<string>()
        {
            "Blue",
            //"BlueViolet",
            //"Crimson",
            //"DarkCyan",
            //"DarkGoldenrod",
            //"DarkMagenta",
            //"DarkOliveGreen",
            //"DarkRed",
            //"DarkSlateBlue",
            //"DeepPink",
            "IndianRed",
            "MediumSlateBlue",
            "Maroon",
            "MidnightBlue",
            "Peru",
            "SaddleBrown",
            "SteelBlue",
            "OrangeRed",
            "Firebrick",
            "DarkKhaki"
        };

    }


    public object ItemsSource
    {
        get => GalleryItemRepeater.ItemsSource;
        set
        {
            GalleryItemRepeater.ItemsSource = value;
        }
    }


    // Return item that's at the center of the viewport.
    private Button? GetSelectedItemFromViewport()
    {
        var selectedIndex = GetSelectedIndexFromViewport();
        var selectedElement = GalleryItemRepeater.TryGetElement(selectedIndex);

        return selectedElement as Button;
    }


    // Find index of the item that's at the center of the viewport
    private int GetSelectedIndexFromViewport()
    {
        // get horizontal center point of ScrollViewer
        var centerPoint = GalleryScrollViewer.HorizontalOffset + GalleryScrollViewer.ViewportWidth / 2;

        var selectedIndex = (int)Math.Floor(centerPoint / (ItemMargin.Left + ItemWidth));
        selectedIndex %= GalleryItemRepeater.ItemsSourceView.Count;

        return selectedIndex;
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
}
