// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ImageGlass.WinNT;

public sealed partial class GalleryControl : UserControl
{
    public int ItemSize { get; set; } = 70;

    public object ItemsSource
    {
        get => GalleryItemRepeater.ItemsSource;
        set
        {
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


    private void GalleryItem_Selected(object sender, RoutedEventArgs e)
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
}
