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
using ImageGlass.Common.Types;
using ImageGlass.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.UI;

public partial class ToolbarControl : PhControl
{
    public ToolbarControlModel VM => (ToolbarControlModel)DataContext!;


    // events
    public event TEventHandler<ToolbarButton, EventArgs>? ItemClicked;


    private readonly Dictionary<string, List<int>> _configBindingsMap = [];
    private readonly Dictionary<int, ToolbarItemMetadata> _metadataMap = [];
    public readonly List<ToolbarItemModel> _groupPrimaryItemModels = [];
    public readonly List<ToolbarItemModel> _groupSecondaryItemModels = [];
    public readonly List<ToolbarItemModel> _groupOverflowItemModels = [];
    private readonly List<Control> _itemElements = [];


    #region Public Properties

    /// <summary>
    /// Gets, sets the value indicates that empty value is not allowed.
    /// </summary>
    public ObservableCollection<ToolbarItemModel> ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    public static readonly StyledProperty<ObservableCollection<ToolbarItemModel>> ItemsSourceProperty =
        AvaloniaProperty.Register<ModalWindow, ObservableCollection<ToolbarItemModel>>(nameof(ItemsSource), []);


    #endregion // Public Properties



    public ToolbarControl()
    {
        InitializeComponent();
    }



    #region Control Events

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Core.Config.PropertyChanged += Config_PropertyChanged;

        await Task.Delay(100);
        HandleOverflow__();
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        Core.Config.PropertyChanged -= Config_PropertyChanged;
    }


    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        HandleOverflow__();
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);

        // a new theme just loaded
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.Background));
        }
    }


    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ItemsSourceProperty)
        {
            LoadItems__();
        }
    }


    private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // update toolbar spacing
        if (nameof(Core.Config.ToolbarIconHeight).Equals(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.ItemSpacing));
            _ = VM.OnPropertyChanged(nameof(VM.ItemPadding));
        }

        // update toolbar button check state
        else
        {
            UpdateButtonCheckState__(e.PropertyName);
        }
    }


    private void ToolbarButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToolbarButton btn) return;

        ItemClicked?.Invoke(btn, EventArgs.Empty);
    }


    private void ToolbarItem_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
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


    #endregion // Control Events



    #region Methods

    /// <summary>
    /// Clears all items and metadata.
    /// </summary>
    private void ClearItems__()
    {
        // remove item events
        foreach (var item in _itemElements)
        {
            item.PropertyChanged -= ToolbarItem_PropertyChanged;

            if (item is ToolbarButton itemBtn)
            {
                itemBtn.Click -= ToolbarButton_Click;
            }
        }

        _itemElements.Clear();
        _metadataMap.Clear();
        _configBindingsMap.Clear();

        _groupPrimaryItemModels.Clear();
        _groupOverflowItemModels.Clear();
        _groupSecondaryItemModels.Clear();

        PART_PrimaryGroup.Children.Clear();
        PART_SecondaryGroup.Children.Clear();
    }


    /// <summary>
    /// Loads toolbar items.
    /// </summary>
    private void LoadItems__()
    {
        ClearItems__();

        var primaryList = new List<Control>();
        var secondaryList = new List<Control>();

        int srcIndex = -1;
        int primaryIndex = -1;
        int secondaryIndex = -1;

        foreach (var vm in ItemsSource)
        {
            srcIndex++;
            vm.SourceIndex = srcIndex;


            // create toolbar item element
            Control itemEl;
            if (vm.IsSeparator) itemEl = new ToolbarSeparator();
            else
            {
                var itemBtn = new ToolbarButton();
                itemBtn.Click += ToolbarButton_Click;
                itemEl = itemBtn;
            }

            itemEl.DataContext = vm;
            itemEl.PropertyChanged += ToolbarItem_PropertyChanged;


            // group: secondary
            if (vm.Alignment == ToolbarItemAlignment.Right)
            {
                secondaryIndex++;
                _groupSecondaryItemModels.Add(vm);
                secondaryList.Add(itemEl);
            }
            // group: primary
            else
            {
                primaryIndex++;
                _groupPrimaryItemModels.Add(vm);
                primaryList.Add(itemEl);
            }
            _itemElements.Add(itemEl);


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


        // append to visual tree
        PART_PrimaryGroup.Children.AddRange(primaryList);
        PART_SecondaryGroup.Children.AddRange(secondaryList);
    }


    /// <summary>
    /// Updates item position and alignment.
    /// </summary>
    private void HandleOverflow__()
    {
        _groupOverflowItemModels.Clear();

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
            - (_groupPrimaryItemModels.Count * ToolbarControlModel.ItemSpacing);

        // 3.2 check if we should hide the item
        foreach (var item in _groupPrimaryItemModels)
        {
            if (!_metadataMap.TryGetValue(item.SourceIndex, out var meta)) continue;
            usedWidth += meta.RenderedWidth;

            // check if the item has enough space to show
            item.IsNotOverflow = availableWidth >= usedWidth;


            // add overflow item
            if (!item.IsNotOverflow)
            {
                _groupOverflowItemModels.Add(item);
            }
        }

        // 4. show the overflow button if there are hidden icons
        BtnOverflowMenu.IsVisible = usedWidth > availableWidth;
    }


    /// <summary>
    /// Updates check state of toolbar button according to config name.
    /// </summary>
    private void UpdateButtonCheckState__(string? configName)
    {
        if (string.IsNullOrWhiteSpace(configName)) return;
        if (_configBindingsMap.GetValueOrDefault(configName) is not List<int> itemIndice) return;
        if (ItemsSource is not IEnumerable<ToolbarItemModel> allItems) return;

        var items = allItems.ToArray();
        foreach (var srcIndex in itemIndice)
        {
            var configValue = Core.Config.GetAsString(configName);
            var isChecked = configValue.Equals(items[srcIndex].ConfigBindingValue, StringComparison.Ordinal);

            items[srcIndex].IsChecked = isChecked;
        }
    }


    #endregion // Methods


}



public record ToolbarItemMetadata
{
    public int SourceIndex { get; set; } = -1;
    public int PrimaryItemIndex { get; set; } = -1;
    public int SecondaryItemIndex { get; set; } = -1;
    public double RenderedWidth { get; set; } = 0;
}


