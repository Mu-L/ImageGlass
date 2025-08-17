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
using CommunityToolkit.WinUI.Controls;
using ImageGlass.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace ImageGlass.Win64.UI;

public sealed partial class ToolbarControl : UserControl
{

    private Dictionary<int, ToolbarItemMetadata> _itemsMetadata = [];
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
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ToolbarControl),
            new PropertyMetadata(null, OnItemsSourceChanged));

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ToolbarControl toolbar) return;
        toolbar.UpdateLayoutItems();
    }


    #endregion // Dependency Properties


    public ToolbarControl()
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
        if (sender is not IIgToolbarItem item) return;
        if (sender is not FrameworkElement fe) return;

        // save toolbar item width
        if (_itemsMetadata.TryGetValue(item.ViewModel.SourceIndex, out ToolbarItemMetadata? value))
        {
            value.RenderedWidth = fe.ActualWidth;
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

            // 1. Separator
            if (item.Type == ToolbarItemType.Separator)
            {
                mnu.Items.Add(new MenuFlyoutSeparator());
                continue;
            }

            // 2. Button item
            // get toolbar item metadata
            if (!_itemsMetadata.TryGetValue(item.SourceIndex, out var meta)) continue;
            if (RepeaterPrimaryItems.TryGetElement(meta.PrimaryItemIndex) is not SwitchPresenter sp) continue;
            if (sp.FindName("ToolbarItem_Button") is not IgToolbarItemButton btnEl) continue;


            // get image source from toolbar item
            if (btnEl.FindName("ToolbarItem_Button_Icon") is ImageIcon iconEl)
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


            if (btnEl.ViewModel.IsToggle)
            {
                mnuItem = new ToggleMenuFlyoutItem()
                {
                    Text = item.Text,
                    Icon = iconFe,
                    //IsChecked = btnEl.IsChecked,
                    //Command = btnEl.Command,
                    //CommandParameter = btnEl.CommandParameter,
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

    private void MnuOverflow_Opened(object sender, object e)
    {
        BtnOverflowMenu.IsChecked = true;
    }

    private void MnuOverflow_Closed(object sender, object e)
    {
        BtnOverflowMenu.IsChecked = false;
    }

    private void MnuMain_Opened(object sender, object e)
    {
        BtnMainMenu.IsChecked = true;
    }

    private void MnuMain_Closed(object sender, object e)
    {
        BtnMainMenu.IsChecked = false;
    }


    public void UpdateLayoutItems()
    {
        _itemsMetadata.Clear();
        PrimaryItems.Clear();
        PrimaryItemsOverflow.Clear();
        SecondaryItems.Clear();

        if (ItemsSource is not IEnumerable<ToolbarItemModel> allItems) return;


        int srcIndex = -1;
        int primaryIndex = -1;
        int secondaryIndex = -1;

        foreach (var item in allItems)
        {
            srcIndex++;
            item.SourceIndex = srcIndex;

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
            _itemsMetadata.TryAdd(srcIndex, new ToolbarItemMetadata()
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
        if (ItemsSource is not IEnumerable<ToolbarItemModel> allItems) return;

        PrimaryItemsOverflow.Clear();

        // calculate available width for visible items
        var usedWidth = 0d;
        var availableWidth = GridToolbar.ActualWidth
            - GridToolbar.Padding.Left
            - GridToolbar.Padding.Right
            - PanelRight.ActualWidth
            - (PrimaryItems.Count * ItemSpacing);


        foreach (var item in PrimaryItems)
        {
            if (!_itemsMetadata.TryGetValue(item.SourceIndex, out var meta)) continue;

            usedWidth += meta.RenderedWidth;

            // check if item is overflow
            var hasEnoughSpace = availableWidth >= usedWidth;
            item.IsOverflow = !hasEnoughSpace;


            // add overflow item
            if (item.IsOverflow)
            {
                PrimaryItemsOverflow.Add(item);
            }

        }

        // set visibility of overflow button
        BtnOverflowMenu.Visibility = usedWidth > availableWidth
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

