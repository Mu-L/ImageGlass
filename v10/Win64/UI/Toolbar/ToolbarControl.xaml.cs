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
using ImageGlass.Win64.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageGlass.Win64.UI;


public sealed partial class ToolbarControl : UserControl, INotifyPropertyChanged
{
    #region INotifyPropertyChanged Implementation

    // to manage PropertyChanged events
    private List<PropertyChangedEventHandler> _propertyChangedEvent = new();
    private event PropertyChangedEventHandler? _propertyChangedHandler;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            if (value != null)
            {
                _propertyChangedHandler += value;
                _propertyChangedEvent.Add(value);
            }
        }

        remove
        {
            if (value != null)
            {
                _propertyChangedHandler -= value;
                _propertyChangedEvent.Remove(value);
            }
        }
    }


    /// <summary>
    /// Emits event <see cref="PropertyChanged"/>.
    /// </summary>
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        _propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    /// Clears event handlers list of <see cref="PropertyChanged"/>.
    /// </summary>
    public void ClearPropertyChangedEvents()
    {
        // remove PropertyChanged events
        foreach (var eventHandler in _propertyChangedEvent)
        {
            _propertyChangedHandler -= eventHandler;
        }
        _propertyChangedEvent.Clear();
    }

    #endregion // INotifyPropertyChanged Implementation


    private Dictionary<int, ToolbarItemMetadata> _itemsMetadata = [];
    public static double ItemSpacing => 4;
    public static string _PART_ItemButton => "PART_ItemButton";
    public static string _PART_ItemSeparator => "PART_ItemSeparator";
    public static string MainMenuIconName => nameof(IgThemeIcon.MainMenu);

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
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ToolbarControl), new PropertyMetadata(null, OnItemsSourceChanged));
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ToolbarControl toolbar) return;
        toolbar.UpdateLayoutItems();
    }


    #endregion // Dependency Properties


    public ToolbarControl()
    {
        InitializeComponent();

        SizeChanged += ToolbarControl_SizeChanged;
        Unloaded += ToolbarControl_Unloaded;
    }


    private void ToolbarControl_Unloaded(object sender, RoutedEventArgs e)
    {
        SizeChanged -= ToolbarControl_SizeChanged;
        Unloaded -= ToolbarControl_Unloaded;

        ClearPropertyChangedEvents();
    }


    private void ToolbarControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        HandleOverflow();
    }


    private void ToolbarItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not IIgToolbarItem item) return;
        if (sender is not FrameworkElement fe) return;

        // save toolbar item width
        if (_itemsMetadata.TryGetValue(item.VM.SourceIndex, out ToolbarItemMetadata? value))
        {
            value.RenderedWidth = fe.ActualWidth;
        }
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
            if (sp.FindName(_PART_ItemButton) is not IgToolbarItemButton btnEl) continue;


            // get image source from toolbar item
            if (btnEl.FindName(IgToolbarItemButton._PART_ButtonIcon) is ImageIcon iconEl)
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


}



public record ToolbarItemMetadata
{
    public int SourceIndex { get; set; } = -1;
    public int PrimaryItemIndex { get; set; } = -1;
    public int SecondaryItemIndex { get; set; } = -1;
    public double RenderedWidth { get; set; } = 0;
}

