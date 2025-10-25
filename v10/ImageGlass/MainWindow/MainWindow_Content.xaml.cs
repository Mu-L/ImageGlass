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
using Cysharp.Text;
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace ImageGlass;

public sealed partial class MainWindow_Content : IgControl
{
    private readonly MainWindow _owner;
    private readonly MenuFlyout _mnuMain = new();
    private bool _shouldUpdateMenuText = false;

    // in-app message
    private CancellationTokenSource? _cancelMessage;
    private readonly Lock _lockCancelMessage = new();

    public event TypedEventHandler<IgToolbarButton, ToolbarItemClickedEventArgs>? ToolbarButtonClicked;
    public event TypedEventHandler<IgGalleryItem, EventArgs>? GalleryItemClicked;
    public event TypedEventHandler<VirtualViewerControl, DragEventArgs>? ViewerDrop;
    public event TypedEventHandler<VirtualViewerControl, ZoomEventArgs>? ViewerZoomChanged;
    public event TypedEventHandler<VirtualViewerControl, SelectionEventArgs>? ViewerSelectionChanged;
    public event TypedEventHandler<VirtualViewerControl, PanningEventArgs>? ViewerPanning;
    public event TypedEventHandler<VirtualViewerControl, PhotoLoadingEventArgs>? PhotoLoading;


    public ToolbarControl ToolbarMain => PART_ToolbarMain;
    public GalleryControl Gallery => PART_Gallery;
    public VirtualViewerControl Viewer => PART_Viewer;
    public MenuFlyout MainMenu => PART_MainMenu;
    public IgToolbarButton MainMenuButton => PART_ToolbarMain.MainMenuButton;


    private string? MessageHeading
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsMessageVisible));
            }
        }
    }
    private string? MessageDescription
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsMessageVisible));
            }
        }
    }
    private string? MessageDetails
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsMessageVisible));
            }
        }
    }
    private bool IsMessageVisible => !string.IsNullOrWhiteSpace(MessageHeading)
        || !string.IsNullOrWhiteSpace(MessageDescription)
        || !string.IsNullOrWhiteSpace(MessageDetails);




    public MainWindow_Content(MainWindow mainWindow)
    {
        InitializeComponent();

        _owner = mainWindow;
    }



    #region Override methods

    protected override void OnIgLoaded(FrameworkElement fe)
    {
        base.OnIgLoaded(fe);

        UpdateStyle_();
        UpdateZoomModeMenuGroup_();


        AP.Config.PropertyChanged += Config_PropertyChanged;

        PART_MainMenu.Opening += PART_MainMenu_Opening;
        PART_MainMenu.Opened += PART_MainMenu_Opened;
        PART_MainMenu.Closed += PART_MainMenu_Closed;
        MenuItemHelper.Clicked += MenuItem_Clicked;

        PART_ToolbarMain.ItemClicked += PART_ToolbarMain_ItemClicked;
        PART_ToolbarMain.PropertyChanged += PART_ToolbarMain_PropertyChanged;
        PART_Gallery.ItemClicked += PART_Gallery_ItemClicked;
        PART_Gallery.PropertyChanged += PART_Gallery_PropertyChanged;

        PART_Viewer.DragOver += PART_Viewer_DragOver;
        PART_Viewer.Drop += PART_Viewer_Drop;
        PART_Viewer.ZoomChanged += PART_Viewer_ZoomChanged;
        PART_Viewer.Panning += PART_Viewer_Panning;
        PART_Viewer.SelectionChanged += PART_Viewer_SelectionChanged;
        PART_Viewer.PhotoLoading += PART_Viewer_PhotoLoading;

        _owner.IgWindowStateChanged += Owner_IgWindowStateChanged;
    }


    protected override void OnIgUnloaded(FrameworkElement fe)
    {
        base.OnIgUnloaded(fe);

        AP.Config.PropertyChanged -= Config_PropertyChanged;

        PART_MainMenu.Opening -= PART_MainMenu_Opening;
        PART_MainMenu.Opened -= PART_MainMenu_Opened;
        PART_MainMenu.Closed -= PART_MainMenu_Closed;
        MenuItemHelper.Clicked -= MenuItem_Clicked;

        PART_ToolbarMain.ItemClicked -= PART_ToolbarMain_ItemClicked;
        PART_ToolbarMain.PropertyChanged -= PART_ToolbarMain_PropertyChanged;
        PART_Gallery.ItemClicked -= PART_Gallery_ItemClicked;
        PART_Gallery.PropertyChanged -= PART_Gallery_PropertyChanged;

        PART_Viewer.DragOver -= PART_Viewer_DragOver;
        PART_Viewer.Drop -= PART_Viewer_Drop;
        PART_Viewer.ZoomChanged -= PART_Viewer_ZoomChanged;
        PART_Viewer.Panning -= PART_Viewer_Panning;
        PART_Viewer.SelectionChanged -= PART_Viewer_SelectionChanged;
        PART_Viewer.PhotoLoading -= PART_Viewer_PhotoLoading;

        _owner.IgWindowStateChanged -= Owner_IgWindowStateChanged;
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);

        UpdateStyle_();
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        _shouldUpdateMenuText = true;
    }


    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Zoom mode is changed
        if (nameof(Config.ZoomMode).Equals(e.PropertyName, StringComparison.Ordinal))
        {
            // update menu items state
            UpdateZoomModeMenuGroup_();
        }
    }


    #endregion // Override methods



    #region Control Events

    private void Owner_IgWindowStateChanged(IgWindow sender, WindowStateChangedEventArgs e)
    {
        if (e.State == OverlappedPresenterState.Minimized) return;
        UpdateStyle_();
    }


    private void PART_MainMenu_Opening(object? sender, object e)
    {
        UpdateMenuTextIfNeeded_();
    }


    private void PART_MainMenu_Opened(object? sender, object e)
    {
        Hotkey.IsEnabled = false;
    }


    private void PART_MainMenu_Closed(object? sender, object e)
    {
        Hotkey.IsEnabled = true;
    }


    private void MenuItem_Clicked(MenuFlyoutItem sender, MenuItemClickedEventArgs e)
    {
        var action = MainWindow.GetMenuAction(e.Item.LangKey);

        _ = _owner.RunActionAsync(action, true);
    }


    private void PART_ToolbarMain_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (nameof(ToolbarMain.IsContentVisible).Equals(e.PropertyName))
        {
            UpdateStyle_();
        }
    }


    private void PART_ToolbarMain_ItemClicked(IgToolbarButton sender, ToolbarItemClickedEventArgs e)
    {
        ToolbarButtonClicked?.Invoke(sender, e);
    }


    private void PART_Gallery_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (nameof(Gallery.IsContentVisible).Equals(e.PropertyName))
        {
            UpdateStyle_();
        }
    }


    private void PART_Gallery_ItemClicked(IgGalleryItem sender, EventArgs e)
    {
        GalleryItemClicked?.Invoke(sender, e);
    }


    private void PART_Viewer_DragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = AP.Config.Lang[LangId.FrmMain_OpenWith, BHelper.AppName];
    }


    private void PART_Viewer_Drop(object sender, DragEventArgs e)
    {
        ViewerDrop?.Invoke((VirtualViewerControl)sender, e);
    }


    private void PART_Viewer_ZoomChanged(VirtualViewerControl sender, ZoomEventArgs e)
    {
        ViewerZoomChanged?.Invoke(sender, e);
    }


    private void PART_Viewer_SelectionChanged(VirtualViewerControl sender, SelectionEventArgs e)
    {
        ViewerSelectionChanged?.Invoke(sender, e);
    }


    private void PART_Viewer_Panning(VirtualViewerControl sender, PanningEventArgs e)
    {
        ViewerPanning?.Invoke(sender, e);
    }


    private void PART_Viewer_PhotoLoading(VirtualViewerControl sender, PhotoLoadingEventArgs e)
    {
        HandlePhotoLoading_(sender, e);
    }


    #endregion // Control Events



    /// <summary>
    /// Updates style according to current theme.
    /// </summary>
    private void UpdateStyle_()
    {
        // 1. style for in-app message
        PART_ViewerMessage.Background = AP.Config.Theme.ComputedColors.BgColor
            .WithAlpha(200)
            .ToBrush();


        // 2. window style: no custom frame
        var frameSize = AP.Config.Theme.Settings.FrameThickness;
        if (frameSize <= 0)
        {
            PART_ContentRoot.Margin = new(0);
            PART_ContentRoot.BorderThickness = new(0, 1, 0, 0);
            PART_Viewer.CornerRadius = new(0);
            return;
        }


        // 3. window style: with custom frame
        var isMaximized = _owner.WindowState == OverlappedPresenterState.Maximized;
        PART_ContentRoot.BorderBrush = AP.Config.Theme.InvertedBaseColor
            .WithAlpha(15)
            .ToBrush();

        // 3.1 maximize state
        if (isMaximized)
        {
            PART_ContentRoot.Margin = new(0);
            PART_Viewer.CornerRadius = new(0);

            if (PART_ToolbarMain.IsContentVisible)
            {
                PART_ContentRoot.BorderThickness = new(0, 1, 0, 0);
                PART_ContentRoot.CornerRadius = new(0)
                {
                    TopLeft = Const.WIN_BORDER_RADIUS.TopLeft,
                    TopRight = Const.WIN_BORDER_RADIUS.TopRight,
                };
            }
            else
            {
                PART_ContentRoot.BorderThickness = new(0, 0, 0, 0);
                PART_ContentRoot.CornerRadius = new(0);
            }
        }
        // 3.2 normal state
        else
        {
            PART_ContentRoot.Margin = new(
                frameSize,
                Math.Clamp(frameSize, 1, frameSize - 4),
                frameSize,
                frameSize);
            PART_ContentRoot.BorderThickness = new(1);


            // Viewer control: set border radius
            var viewerRadius = new CornerRadius();
            if (!PART_ToolbarMain.IsContentVisible)
            {
                viewerRadius.TopLeft = Const.WIN_BORDER_RADIUS.TopLeft;
                viewerRadius.TopRight = Const.WIN_BORDER_RADIUS.TopRight;
            }
            if (!PART_Gallery.IsContentVisible)
            {
                viewerRadius.BottomRight = Const.WIN_BORDER_RADIUS.BottomRight;
                viewerRadius.BottomLeft = Const.WIN_BORDER_RADIUS.BottomLeft;
            }

            PART_Viewer.CornerRadius = viewerRadius;
            PART_ContentRoot.CornerRadius = Const.WIN_BORDER_RADIUS;
        }
    }


    /// <summary>
    /// Updates the check state for zoom mode menu group.
    /// </summary>
    private void UpdateZoomModeMenuGroup_()
    {
        MnuAutoZoom.IsChecked = AP.Config.ZoomMode == Common.ZoomMode.AutoZoom;
        MnuLockZoom.IsChecked = AP.Config.ZoomMode == Common.ZoomMode.LockZoom;
        MnuScaleToWidth.IsChecked = AP.Config.ZoomMode == Common.ZoomMode.ScaleToWidth;
        MnuScaleToHeight.IsChecked = AP.Config.ZoomMode == Common.ZoomMode.ScaleToHeight;
        MnuScaleToFill.IsChecked = AP.Config.ZoomMode == Common.ZoomMode.ScaleToFill;
        MnuScaleToFit.IsChecked = AP.Config.ZoomMode == Common.ZoomMode.ScaleToFit;
    }


    /// <summary>
    /// Updates the text and hotkey text of main menu if needed.
    /// </summary>
    private void UpdateMenuTextIfNeeded_()
    {
        if (!_shouldUpdateMenuText) return;

        LoadMenuText_(MainMenu.Items);
        _shouldUpdateMenuText = false;
    }


    /// <summary>
    /// Loads menu text.
    /// </summary>.
    private static void LoadMenuText_(IList<MenuFlyoutItemBase> items)
    {
        foreach (var item in items)
        {
            // 1. NOTE: only localize subitem menu because it's sealed!
            if (item is MenuFlyoutSubItem submenu)
            {
                submenu.Text = AP.Config.Lang[$"FrmMain_{submenu.Name}"];

                // jump into submenu items
                LoadMenuText_(submenu.Items);
            }


            // 2. update hotkey text for menu items
            if (item is IMenuItem mnuItem)
            {
                var action = MainWindow.GetMenuAction(mnuItem.LangKey);
                if (action is null || action.Hotkeys.Length == 0) continue;

                var hotkeyText = ZString.Join(", ", action.Hotkeys);
                mnuItem.KeyboardAcceleratorTextOverride = hotkeyText;
            }
        }
    }


    /// <summary>
    /// Handles photo loading event.
    /// </summary>
    private void HandlePhotoLoading_(VirtualViewerControl sender, PhotoLoadingEventArgs e)
    {
        // Note: events are not fired in order

        // 1. handle loading error first
        if (e.Photo.Error is not null)
        {
            var emoji = BHelper.IsOS(WindowsOS.Win11OrLater) ? "🥲" : "🙄";
            var heading = AP.Config.Lang[LangId.FrmMain_PicMain_ErrorText] + $" {emoji}";
            var err = BHelper.GetInAppError(e.Photo.Error);

            // show error message
            _ = ShowMessageAsync(err.DebugInfo, heading, err.Details, 0);
        }

        // 2. handle photo loading
        else if (e.State == PhotoLoadingState.Loading)
        {
            // show loading message after 2s
            _ = ShowMessageAsync(AP.Config.Lang[LangId.FrmMain_Loading], durationMs: 0, delayMs: 2000);
        }

        // 3. handle photo loaded
        else if (e.State == PhotoLoadingState.Loaded)
        {
            // clear in-app message
            _ = ShowMessageAsync(null);
        }


        // raise event
        PhotoLoading?.Invoke(sender, e);
    }


    /// <summary>
    /// Sets the in-app message.
    /// </summary>
    private void SetMessage_(string? message, string? heading = null, string? details = null)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            MessageHeading = heading;
            MessageDescription = message;
            MessageDetails = details;
        });
    }


    /// <summary>
    /// Shows in-app message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="heading"></param>
    /// <param name="details"></param>
    /// <param name="durationMs">The duration to display (ms). <c>0</c> = permanent.</param>
    /// <param name="delayMs">The delay time before showing (ms). Default = <c>0</c>.</param>
    public async Task ShowMessageAsync(
        string? message,
        string? heading = null,
        string? details = null,
        int? durationMs = null,
        int delayMs = 0)
    {
        // 1. if intent to clear message
        var clearMessage = message == null && heading == null && details == null;
        if (clearMessage)
        {
            lock (_lockCancelMessage)
            {
                _cancelMessage?.Cancel();
                _cancelMessage?.Dispose();
                _cancelMessage = null; // do not allocate new CTS
            }

            SetMessage_(null);
            return;
        }


        // 2. if intent to show message
        CancellationTokenSource localCancelMessage;
        lock (_lockCancelMessage)
        {
            _cancelMessage?.Cancel();
            _cancelMessage?.Dispose();

            _cancelMessage = new CancellationTokenSource();
            localCancelMessage = _cancelMessage;
        }

        var token = localCancelMessage.Token;


        try
        {
            // wait for the delay
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, token);
            }
            SetMessage_(message, heading, details);


            // clear text after duration
            if (!string.IsNullOrWhiteSpace(message))
            {
                durationMs ??= AP.Config.InAppMessageDuration;

                if (durationMs > 0)
                {
                    await Task.Delay(durationMs.Value, token);
                    SetMessage_(null);
                }
            }
        }
        catch { }
    }





}