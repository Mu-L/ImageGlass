using ImageGlass.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass.Win64.UI;
public sealed partial class ToolbarCustomControl : UserControl
{
    private Dictionary<string, double> _itemsDict = [];
    private double ItemSpacing => 5;
    private Thickness ToolbarPadding => new Thickness(5);

    public ObservableCollection<ToolbarItemModel> PrimaryVisibleItems { get; } = [];
    public ObservableCollection<ToolbarItemModel> PrimaryOverflowItems { get; } = [];


    #region Dependency Properties

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
        if (d is ToolbarCustomControl toolbar)
            toolbar.UpdateLayoutItems();
    }

    #endregion


    public ToolbarCustomControl()
    {
        this.InitializeComponent();
        this.SizeChanged += Toolbar_SizeChanged;
    }


    private void Toolbar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        HandleOverflow();
    }


    private void ToolbarItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;

        if (_itemsDict.ContainsKey(fe.Name))
        {
            _itemsDict[fe.Name] = fe.ActualWidth;
        }
    }

    private void MnuOverflow_Opening(object sender, object e)
    {
        if (sender is not MenuFlyout mnu) return;
        mnu.Items.Clear();


        foreach (var item in PrimaryOverflowItems)
        {
            MenuFlyoutItemBase mnuItem;

            if (!string.IsNullOrWhiteSpace(item.CheckableConfigBinding))
            {
                mnuItem = new ToggleMenuFlyoutItem()
                {
                    Text = item.Text,
                    Icon = new SymbolIcon(Symbol.Placeholder),
                };
            }
            else
            {
                mnuItem = new MenuFlyoutItem()
                {
                    Text = item.Text,
                    Icon = new SymbolIcon(Symbol.Placeholder),
                };
            }

            mnu.Items.Add(mnuItem);
        }
    }


    private void UpdateLayoutItems()
    {
        _itemsDict.Clear();
        PrimaryVisibleItems.Clear();
        PrimaryOverflowItems.Clear();

        if (ItemsSource == null) return;

        foreach (var item in ItemsSource)
        {
            if (item is not ToolbarItemModel itemModel) continue;

            _itemsDict.TryAdd(itemModel.Id, 0);
            PrimaryVisibleItems.Add(itemModel);
        }

        HandleOverflow();
    }


    private void HandleOverflow()
    {
        PrimaryOverflowItems.Clear();

        var availableWidth = ActualWidth - BtnOverflow.ActualWidth - BtnMenu.ActualWidth;
        var usedWidth = 0d;


        if (ItemsSource is IEnumerable<ToolbarItemModel> allItems)
        {
            availableWidth -= allItems.Count() * ItemSpacing;
            PrimaryVisibleItems.Clear();

            foreach (var item in allItems)
            {
                if (!_itemsDict.TryGetValue(item.Id, out var itemWidth)) continue;


                if (usedWidth + itemWidth <= availableWidth)
                {
                    PrimaryVisibleItems.Add(item);
                }
                else
                {
                    PrimaryOverflowItems.Add(item);
                }

                usedWidth += itemWidth;
            }
        }


        BtnOverflow.Visibility = PrimaryOverflowItems.Any()
            ? Visibility.Visible
            : Visibility.Collapsed;

    }


}
