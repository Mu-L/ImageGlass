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
using ImageGlass.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;

namespace ImageGlass.UI;

public partial class ToolbarControl : IgControl
{
    public static string _PART_ItemButton => "PART_ItemButton";
    public static string _PART_ItemSeparator => "PART_ItemSeparator";
    public static double OverflowIconHeight => AP.Config.ToolbarIconHeight / 1.5f; // 16
    public static double ItemSpacing => AP.Config.ToolbarIconHeight / 6f; // 4
    public static string MainMenuIconName => nameof(IgThemeIcon.MainMenu);


    // events
    public event TypedEventHandler<IgToolbarButton, ToolbarItemClickedEventArgs>? ItemClicked;

    private readonly Dictionary<int, ToolbarItemMetadata> _itemsMetadata = [];
    public readonly ObservableCollection<ToolbarItemModel> PrimaryItems = [];
    public readonly ObservableCollection<ToolbarItemModel> PrimaryItemsOverflow = [];
    public readonly ObservableCollection<ToolbarItemModel> SecondaryItems = [];


    #region Public Properties

    #region ItemsSource
    /// <summary>
    /// Gets, sets the items source of toolbar.
    /// </summary>
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ToolbarControl), new PropertyMetadata(null, OnItemsSourceChanged));
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ToolbarControl toolbar) return;
        toolbar.UpdateLayoutItems();
    }
    #endregion // ItemsSource


    /// <summary>
    /// Gets or sets the main menu.
    /// </summary>
    public MenuFlyout? MainMenu
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    }

    #endregion // Public Properties



    public ToolbarControl()
    {
        InitializeComponent();
    }


    protected override void OnIgSizeChanged(FrameworkElement fe, SizeChangedEventArgs e)
    {
        base.OnIgSizeChanged(fe, e);
        HandleOverflow_();
    }


    private void ToolbarItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not IgToolbarItem item) return;
        if (sender is not FrameworkElement fe) return;

        // save toolbar item width
        if (_itemsMetadata.TryGetValue(item.VM.SourceIndex, out var meta))
        {
            meta.RenderedWidth = fe.ActualWidth;
        }
    }


    private void PART_ItemButton_Clicked(IgToolbarButton sender, ToolbarItemClickedEventArgs e)
    {
        OnItemClicked(sender, e);
    }


    /// <summary>
    /// Raises event <see cref="ItemClicked"/>.
    /// </summary>
    protected virtual void OnItemClicked(IgToolbarButton sender, ToolbarItemClickedEventArgs e)
    {
        ItemClicked?.Invoke(sender, e);
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
            if (item.IsSeparator)
            {
                mnu.Items.Add(new MenuFlyoutSeparator());
                continue;
            }

            // 2. Button item
            // get toolbar item metadata
            if (!_itemsMetadata.TryGetValue(item.SourceIndex, out var meta)) continue;
            if (RepeaterPrimaryItems.TryGetElement(meta.PrimaryItemIndex) is not FrameworkElement fe) continue;
            if (fe.FindName(_PART_ItemButton) is not IgToolbarButton btnEl) continue;


            // get image source from toolbar item
            if (btnEl.FindName(IgToolbarButton._PART_ButtonIcon) is ImageIcon iconEl)
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


            if (btnEl.VM.IsToggle)
            {
                mnuItem = new ToggleMenuFlyoutItem()
                {
                    Text = item.Text,
                    Icon = iconFe,
                    IsChecked = item.IsChecked,
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


    private void HandleOverflow_()
    {
        if (ItemsSource is not IEnumerable<ToolbarItemModel> allItems) return;

        PrimaryItemsOverflow.Clear();


        // 1. calculate how much space can I safely use for center toolbar items
        // before they hit the right-side panel
        var availableSpaceOfCenterToolbar =
            (GridToolbar.ActualWidth / 2) // center line
            - PanelPrimary.ActualWidth / 2 // shifts calculation for primary panel
            - PanelRight.ActualWidth // reserves space
            - AP.Config.ToolbarIconHeight; // safety gap


        // 2. if has no space,
        // align items to the left to have more space
        if (availableSpaceOfCenterToolbar <= 0)
        {
            PanelPrimary.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else
        {
            PanelPrimary.HorizontalAlignment = HorizontalAlignment.Center;
        }


        // 3. event if after the items aligned to the left
        // it does not have enough space to fit the toolbar,
        // we need to hide the items until preserve enough space

        // 3.1 calculate available width for visible items
        var usedWidth = 0d;
        var availableWidth = GridToolbar.ActualWidth
            - GridToolbar.Padding.Left
            - GridToolbar.Padding.Right
            - PanelRight.ActualWidth
            - (PrimaryItems.Count * ItemSpacing);

        // 3.2 check if we should hide the item
        foreach (var item in PrimaryItems)
        {
            if (!_itemsMetadata.TryGetValue(item.SourceIndex, out var meta)) continue;
            usedWidth += meta.RenderedWidth;

            // check if the item has enough space to show
            var hasEnoughSpace = availableWidth >= usedWidth;
            item.IsOverflow = !hasEnoughSpace;


            // add overflow item
            if (item.IsOverflow)
            {
                PrimaryItemsOverflow.Add(item);
            }

        }

        // 4. show the overflow button if there are hidden icons
        BtnOverflowMenu.Visibility = usedWidth > availableWidth
            ? Visibility.Visible
            : Visibility.Collapsed;
    }


    /// <summary>
    /// Updates layout of items
    /// </summary>
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


    /// <summary>
    /// Opens the main menu.
    /// </summary>
    public void OpenMainMenu(FlyoutPlacementMode? placement = null)
    {
        BtnMainMenu.OpenFlyoutMenu(placement);
    }

}



public record ToolbarItemMetadata
{
    public int SourceIndex { get; set; } = -1;
    public int PrimaryItemIndex { get; set; } = -1;
    public int SecondaryItemIndex { get; set; } = -1;
    public double RenderedWidth { get; set; } = 0;
}

