/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ImageGlass.Common;
using ImageGlass.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ImageGlass.UI;

public partial class ToolbarControl : UserControl
{
    public ToolbarControlModel VM => (ToolbarControlModel)DataContext!;


    private readonly Dictionary<string, List<int>> _configBindingsMap = [];
    private readonly Dictionary<int, ToolbarItemMetadata> _metadataMap = [];
    public readonly List<ToolbarItemModel> PrimaryItems = [];
    public readonly List<ToolbarItemModel> PrimaryItemsOverflow = [];
    public readonly List<ToolbarItemModel> SecondaryItems = [];

    private readonly List<Control> _allItemEls = [];


    /// <summary>
    /// Gets, sets the value indicates that empty value is not allowed.
    /// </summary>
    public ObservableCollection<ToolbarItemModel> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
    public static readonly StyledProperty<ObservableCollection<ToolbarItemModel>> ItemsProperty =
        AvaloniaProperty.Register<ModalWindow, ObservableCollection<ToolbarItemModel>>(nameof(Items), []);




    public ToolbarControl()
    {
        InitializeComponent();

        Core.ThemeChanged += Core_ThemeChanged;
        Core.Config.PropertyChanged += Config_PropertyChanged;
    }


    private void Core_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        // a new theme just loaded
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.Background));
        }
    }


    private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (nameof(Core.Config.ToolbarIconHeight).Equals(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.ItemSpacing));
            _ = VM.OnPropertyChanged(nameof(VM.ItemPadding));
        }
    }


    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await Task.Delay(100);
        HandleOverflow__();

    }


    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        HandleOverflow__();
    }


    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ItemsProperty)
        {
            LoadItems__();
        }
    }


    /// <summary>
    /// Loads toolbar items.
    /// </summary>
    private void LoadItems__()
    {
        _allItemEls.Clear();
        _metadataMap.Clear();
        _configBindingsMap.Clear();

        PrimaryItems.Clear();
        PrimaryItemsOverflow.Clear();
        SecondaryItems.Clear();

        var primaryList = new List<Control>();
        var secondaryList = new List<Control>();

        int srcIndex = -1;
        int primaryIndex = -1;
        int secondaryIndex = -1;

        foreach (var vm in Items)
        {
            srcIndex++;
            vm.SourceIndex = srcIndex;


            // create toolbar item element
            Control itemEl;
            if (vm.IsSeparator) itemEl = new ToolbarSeparator();
            else itemEl = new ToolbarButton();

            itemEl.DataContext = vm;
            itemEl.PropertyChanged += ItemEl_PropertyChanged;


            // group: secondary
            if (vm.Alignment == ToolbarItemAlignment.Right)
            {
                secondaryIndex++;
                SecondaryItems.Add(vm);
                secondaryList.Add(itemEl);
            }
            // group: primary
            else
            {
                primaryIndex++;
                PrimaryItems.Add(vm);
                primaryList.Add(itemEl);
            }
            _allItemEls.Add(itemEl);


            // save item metadata
            _metadataMap.TryAdd(srcIndex, new ToolbarItemMetadata()
            {
                SourceIndex = srcIndex,
                PrimaryItemIndex = primaryIndex,
                SecondaryItemIndex = secondaryIndex,
                RenderedWidth = 0,
            });

            // save binding map
            var bindingIndice = _configBindingsMap.GetValueOrDefault(vm.ConfigBinding) ?? [];
            if (!string.IsNullOrWhiteSpace(vm.ConfigBinding))
            {
                bindingIndice.Add(srcIndex);
                _configBindingsMap[vm.ConfigBinding] = bindingIndice;
            }


            // set item check state
            if (!string.IsNullOrWhiteSpace(vm.ConfigBinding))
            {
                var configValue = Core.Config.GetAsString(vm.ConfigBinding);
                var isChecked = configValue.Equals(vm.ConfigBindingValue, StringComparison.Ordinal);
                vm.IsChecked = isChecked;
            }
        }



        PART_PrimaryGroup.Children.Clear();
        PART_PrimaryGroup.Children.AddRange(primaryList);

        PART_SecondaryGroup.Children.Clear();
        PART_SecondaryGroup.Children.AddRange(secondaryList);
    }


    private void ItemEl_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Control.BoundsProperty) return;
        if (e.NewValue is not Rect bounds) return;
        if (bounds.Width == 0 || bounds.Height == 0) return;
        if (sender is not IToolbarItem item) return;

        // save toolbar item width
        if (_metadataMap.TryGetValue(item.VM.SourceIndex, out var meta))
        {
            meta.RenderedWidth = bounds.Width;
        }
    }


    private void HandleOverflow__()
    {
        PrimaryItemsOverflow.Clear();

        // 1. calculate how much space can I safely use for center toolbar items
        // before they hit the right-side panel
        var availableSpaceOfCenterToolbar =
            (PART_Root.Bounds.Width / 2) // center line
            - PART_PrimaryGroup.Bounds.Width / 2 // shifts calculation for primary panel
            - PART_RightGroup.Bounds.Width // reserves space
            - Core.Config.ToolbarIconHeight; // safety gap


        // 2. if has no space,
        // align items to the left to have more space
        if (availableSpaceOfCenterToolbar <= 0)
        {
            PART_PrimaryGroup.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        }
        else
        {
            PART_PrimaryGroup.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        }


        // 3. event if after the items aligned to the left
        // it does not have enough space to fit the toolbar,
        // we need to hide the items until preserve enough space

        // 3.1 calculate available width for visible items
        var usedWidth = 0d;
        var availableWidth = PART_Root.Bounds.Width
            - PART_Root.Padding.Left
            - PART_Root.Padding.Right
            - PART_RightGroup.Bounds.Width
            - (PrimaryItems.Count * ToolbarControlModel.ItemSpacing);

        // 3.2 check if we should hide the item
        foreach (var item in PrimaryItems)
        {
            if (!_metadataMap.TryGetValue(item.SourceIndex, out var meta)) continue;
            usedWidth += meta.RenderedWidth;

            // check if the item has enough space to show
            item.IsNotOverflow = availableWidth >= usedWidth;


            // add overflow item
            if (!item.IsNotOverflow)
            {
                PrimaryItemsOverflow.Add(item);
            }
        }

        // 4. show the overflow button if there are hidden icons
        BtnOverflowMenu.IsVisible = usedWidth > availableWidth;
    }


}



public record ToolbarItemMetadata
{
    public int SourceIndex { get; set; } = -1;
    public int PrimaryItemIndex { get; set; } = -1;
    public int SecondaryItemIndex { get; set; } = -1;
    public double RenderedWidth { get; set; } = 0;
}


