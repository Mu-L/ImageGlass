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
using ImageGlass.UI;
using Microsoft.UI.Xaml;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace ImageGlass;

public sealed partial class MainWindow_Content : IgControl
{
    public event TypedEventHandler<IgToolbarButton, ToolbarItemClickedEventArgs>? ToolbarButtonClicked;
    public event TypedEventHandler<IgGalleryItem, EventArgs>? GalleryItemClicked;
    public event TypedEventHandler<VirtualViewerControl, DragEventArgs>? ViewerDrop;
    public event TypedEventHandler<VirtualViewerControl, ZoomEventArgs>? ViewerZoomChanged;
    public event TypedEventHandler<VirtualViewerControl, SelectionEventArgs>? ViewerSelectionChanged;
    public event TypedEventHandler<VirtualViewerControl, PanningEventArgs>? ViewerPanning;


    public ToolbarControl ToolbarMain => PART_ToolbarMain;
    public GalleryControl Gallery => PART_Gallery;
    public VirtualViewerControl Viewer => PART_Viewer;


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
        && !string.IsNullOrWhiteSpace(MessageDescription)
        && !string.IsNullOrWhiteSpace(MessageDetails);


    public MainWindow_Content()
    {
        InitializeComponent();
    }



    #region Override methods

    protected override void OnIgLoaded(FrameworkElement fe)
    {
        base.OnIgLoaded(fe);

        UpdateMessageBoxStyle();

        PART_ToolbarMain.ItemClicked += PART_ToolbarMain_ItemClicked;
        PART_Gallery.ItemClicked += PART_Gallery_ItemClicked;

        PART_Viewer.DragOver += PART_Viewer_DragOver;
        PART_Viewer.Drop += PART_Viewer_Drop;
        PART_Viewer.ZoomChanged += PART_Viewer_ZoomChanged;
        PART_Viewer.Panning += PART_Viewer_Panning;
        PART_Viewer.SelectionChanged += PART_Viewer_SelectionChanged;
        PART_Viewer.Error += PART_Viewer_Error;
    }


    protected override void OnIgUnloaded(FrameworkElement fe)
    {
        base.OnIgUnloaded(fe);

        PART_ToolbarMain.ItemClicked -= PART_ToolbarMain_ItemClicked;
        PART_Gallery.ItemClicked -= PART_Gallery_ItemClicked;

        PART_Viewer.DragOver -= PART_Viewer_DragOver;
        PART_Viewer.Drop -= PART_Viewer_Drop;
        PART_Viewer.ZoomChanged -= PART_Viewer_ZoomChanged;
        PART_Viewer.Panning -= PART_Viewer_Panning;
        PART_Viewer.SelectionChanged -= PART_Viewer_SelectionChanged;
        PART_Viewer.Error -= PART_Viewer_Error;
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);

        UpdateMessageBoxStyle();
    }


    #endregion // Override methods



    #region Control Events

    private void PART_ToolbarMain_ItemClicked(IgToolbarButton sender, ToolbarItemClickedEventArgs e)
    {
        ToolbarButtonClicked?.Invoke(sender, e);
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
        e.DragUIOverride.Caption = AP.Config.Lang["FrmMain._OpenWith", BHelper.AppName];
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

    private void PART_Viewer_Error(VirtualViewerControl sender, ViewerErrorEventArgs e)

    {
        var emoji = BHelper.IsOS(WindowsOS.Win11OrLater) ? "🥲" : "🙄";
        var heading = AP.Config.Lang["FrmMain.PicMain._ErrorText"] + $" {emoji}";
        var err = BHelper.GetInAppError(e.Error);

        SetInAppMessage(err.DebugInfo, heading, err.Details);
    }


    #endregion // Control Events


    /// <summary>
    /// Update message box style according to current theme.
    /// </summary>
    private void UpdateMessageBoxStyle()
    {
        PART_ViewerMessage.Background = AP.Config.Theme.ComputedColors.BgColor.WithAlpha(180).ToBrush();
    }


    /// <summary>
    /// Sets in-app message. Sets all params to <c>null</c> to hide the message.
    /// </summary>
    public void SetInAppMessage(string? description, string? heading = null, string? details = null)
    {
        MessageHeading = heading;
        MessageDescription = description;
        MessageDetails = details;
    }



}