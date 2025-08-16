using ImageGlass.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass.Win64.UI;
public sealed partial class ToolbarCustomControl : UserControl
{
    private Dictionary<string, ToolbarItemMetadata> _itemsMetadata = [];
    public static double ItemSpacing => 4;

    public ObservableCollection<ToolbarItemModel> PrimaryItems { get; } = [];
    public ObservableCollection<ToolbarItemModel> PrimaryItemsOverflow { get; } = [];
    public ObservableCollection<ToolbarItemModel> SecondaryItems { get; } = [];


    // Dependency Properties
    #region Dependency Properties

    /// <summary>
    /// Gets, sets the items source of toolbar.
    /// </summary>
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ToolbarCustomControl),
            new PropertyMetadata(null, OnItemsSourceChanged));

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ToolbarCustomControl toolbar) return;
        toolbar.UpdateLayoutItems();
    }

    #endregion // Dependency Properties


    public ToolbarCustomControl()
    {
        this.InitializeComponent();
        this.SizeChanged += UserControl_SizeChanged;
    }


    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        this.SizeChanged -= UserControl_SizeChanged;
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        HandleOverflow();
    }


    private void ToolbarItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not IgButton btn) return;

        // save button size
        if (_itemsMetadata.TryGetValue(btn.Name, out ToolbarItemMetadata? value))
        {
            value.RenderedWidth = btn.ActualWidth;
        }


        //if (btn.FindName("ToolbarButtonIcon") is ImageIcon iconEl)
        //{
        //    // set new icon
        //    if (iconEl.Source == null)
        //    {
        //        var model = (ItemsSource as IEnumerable<ToolbarItemModel>).FirstOrDefault(i => i.Id == btn.Name);
        //        iconEl.Source = new SvgImageSource(new Uri(model.Image));
        //    }
        //}
    }

    private void MnuOverflow_Opening(object sender, object e)
    {
        if (sender is not MenuFlyout mnu) return;
        mnu.Items.Clear();


        foreach (var item in PrimaryItemsOverflow)
        {
            MenuFlyoutItemBase mnuItem;
            IconElement iconFe;
            ImageSource? imgSrc = null;

            // get toolbar item metadata
            if (!_itemsMetadata.TryGetValue(item.Id, out var meta)) continue;
            if (RepeaterPrimaryItems.TryGetElement(meta.PrimaryItemIndex) is not IgButton btnEl) continue;


            // get image source from toolbar item
            if (btnEl.FindName("ToolbarButtonIcon") is ImageIcon iconEl)
            {
                imgSrc = iconEl.Source;
            }

            if (imgSrc == null)
            {
                iconFe = new SymbolIcon(Symbol.Placeholder);
            }
            else
            {
                iconFe = new ImageIcon()
                {
                    Source = imgSrc,
                };
            }


            if (!string.IsNullOrWhiteSpace(item.CheckableConfigBinding))
            {
                mnuItem = new ToggleMenuFlyoutItem()
                {
                    Text = item.Text,
                    Icon = iconFe,
                    IsChecked = btnEl.IsChecked,
                    Command = btnEl.Command,
                    CommandParameter = btnEl.CommandParameter,
                };
            }
            else
            {
                mnuItem = new MenuFlyoutItem()
                {
                    Text = item.Text,
                    Icon = iconFe,
                };
            }

            mnu.Items.Add(mnuItem);
        }

    }


    public void UpdateLayoutItems()
    {
        _itemsMetadata.Clear();
        PrimaryItems.Clear();
        PrimaryItemsOverflow.Clear();
        SecondaryItems.Clear();

        if (ItemsSource is not IEnumerable<ToolbarItemModel> btnItems) return;


        int srcIndex = -1;
        int primaryIndex = -1;
        int secondaryIndex = -1;

        foreach (var item in btnItems)
        {
            // group: secondary
            if (item.Alignment == ToolbarItemAlignment.Right)
            {
                secondaryIndex++;
                SecondaryItems.Add(item);
            }
            // group: primary
            else
            {
                primaryIndex++;
                PrimaryItems.Add(item);
            }

            // save item metadata
            _itemsMetadata.TryAdd(item.Id, new ToolbarItemMetadata()
            {
                SourceIndex = srcIndex,
                PrimaryItemIndex = primaryIndex,
                SecondaryItemIndex = secondaryIndex,
                RenderedWidth = 0,
            });
        }
    }


    private void HandleOverflow()
    {
        PrimaryItemsOverflow.Clear();

        // calculate available width for visible items
        var usedWidth = 0d;
        var availableWidth = GridToolbar.ActualWidth
            - PanelRight.ActualWidth
            - (PrimaryItems.Count * ItemSpacing);


        foreach (var item in PrimaryItems)
        {
            if (!_itemsMetadata.TryGetValue(item.Id, out var meta)) continue;

            // check if item is overflow
            if (meta.RenderedWidth > 0)
            {
                var hasEnoughSpace = availableWidth >= usedWidth + meta.RenderedWidth;
                item.IsOverflow = !hasEnoughSpace;
            }

            // add overflow item
            if (item.IsOverflow)
            {
                PrimaryItemsOverflow.Add(item);
            }

            usedWidth += meta.RenderedWidth;
        }

        // set visibility of overflow button
        BtnOverflow.Visibility = usedWidth > availableWidth
            ? Visibility.Visible
            : Visibility.Collapsed;
    }


}



public record ToolbarItemMetadata
{
    public int SourceIndex { get; set; } = -1;
    public int PrimaryItemIndex { get; set; } = -1;
    public int SecondaryItemIndex { get; set; } = -1;
    public double RenderedWidth { get; set; } = 0;
}