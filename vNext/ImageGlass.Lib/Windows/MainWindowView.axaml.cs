/*
ImageGlass - A lightweight, versatile image viewer
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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.ServiceProviders.FileSearchService;
using ImageGlass.Common.Types;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using ImageGlass.UI.Viewer.ZoomAndPan;
using ImageGlass.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Common.Windows;

public partial class MainWindowView : PhControl
{
    public MainWindowViewModel VM => (MainWindowViewModel)DataContext!;


    public MainWindowView()
    {
        InitializeComponent();
    }



    #region Control Events

    protected override void OnLoaded(RoutedEventArgs e)
    {
        // apply app layout from settings
        ApplyAppLayout();

        base.OnLoaded(e);

        // drag-drop events
        DragDrop.SetAllowDrop(PART_Viewer, true);
        DragDrop.AddDragOverHandler(PART_Viewer, PART_Viewer_DragOver);
        DragDrop.AddDropHandler(PART_Viewer, PART_Viewer_Drop);

        Core.Config.PropertyChanged += Config_PropertyChanged;
        PART_Viewer.PhotoLoading += PART_Viewer_PhotoLoading;
        PART_Viewer.ZoomChanged += PART_Viewer_ZoomChanged;


        // load image from command line arguments
        LoadImagesFromCmdArgs();
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // drag-drop events
        DragDrop.RemoveDragOverHandler(PART_Viewer, PART_Viewer_DragOver);
        DragDrop.RemoveDropHandler(PART_Viewer, PART_Viewer_Drop);

        Core.Config.PropertyChanged -= Config_PropertyChanged;
        PART_Viewer.PhotoLoading -= PART_Viewer_PhotoLoading;
        PART_Viewer.ZoomChanged -= PART_Viewer_ZoomChanged;

    }


    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        UpdateGalleryWidth();
    }


    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Config.Layout))
        {
            ApplyAppLayout();
        }
    }


    private void PART_Viewer_DragOver(object? sender, DragEventArgs e)
    {
        // if drag data contains files or folders
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy | DragDropEffects.Link;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }


    private void PART_Viewer_Drop(object? sender, DragEventArgs e)
    {
        // if drag data contains files or folders
        if (e.DataTransfer.TryGetFiles() is not IStorageItem[] sItems) return;

        // 1. get dropped paths
        var paths = sItems.Select(i => i.Path.LocalPath).ToArray();


        // 2. load multiple paths
        if (paths.Length > 1)
        {
            PrepareLoadPhotoList(paths,
                currentFilePath: null, disposeForegroundShell: true, loadInitPhoto: true);
            return;
        }


        // 3. load single file path
        // 3.1 get foreground shell
        if (Core.Config.ShouldUseExplorerSortOrder)
        {
            Core.ShellProvider?.ForegroundShell = Core.ShellProvider?.GetForegroundWindowView();
        }
        Core.UpdateInitImagePath(paths[0]);

        // 3.2 open the path
        Core.API?.IG_OpenPath(paths[0]);
    }


    private void PART_Viewer_PhotoLoading(ViewerControl sender, PhotoLoadingEventArgs e)
    {
        // Note: events are not fired in order

        // 1. handle loading error first
        if (e.Photo.Error is not null)
        {
            var heading = $"{Core.Lang[LangId.FrmMain_PicMain_ErrorText]} 😶";
            var err = BHelper.GetInAppError(e.Photo.Error);

            // show error message
            _ = PART_Message.ShowAsync(err.DebugInfo, heading, err.Details, 0);
        }

        // 2. handle photo loading
        else if (e.State == PhotoLoadingState.Loading)
        {
            // show loading message after 2s
            _ = PART_Message.ShowAsync(Core.Lang[LangId.FrmMain_Loading], durationMs: 0, delayMs: 2000);
        }

        // 3. handle photo loaded
        else if (e.State == PhotoLoadingState.Loaded)
        {
            // clear in-app message
            _ = PART_Message.ShowAsync(null);
        }
    }


    private void PART_Viewer_ZoomChanged(ViewerControl sender, ViewerZoomEventArgs e)
    {
        // update window fit
        if (Core.Config.EnableWindowFit
            && e.ChangeSource != ZoomChangeSource.SizeChanged
            && (e.IsManualZoom || e.IsZoomModeChange))
        {
            Core.API?.ApplyWindowFitMode(e.ChangeSource == ZoomChangeSource.ZoomMode);
        }
    }


    private void PART_GalleryResizer_DragCompleted(object? sender, VectorEventArgs e)
    {
        var panelEl = PART_Gallery.FindVirtualPanel();
        if (panelEl is null) return;

        // save number of columns
        Core.Config.GalleryColumns = panelEl.ColumnsPerRow;
    }

    #endregion // Control Events



    #region Control Methods

    private void LoadImagesFromCmdArgs()
    {
        var pathToLoad = Core.InputImagePathFromArgs;

        // check for last seen image
        if (string.IsNullOrEmpty(pathToLoad)
            && Core.Config.ShouldOpenLastSeenImage
            && BHelper.CheckPath(Core.Config.LastSeenImagePath) == PathType.File)
        {
            pathToLoad = Core.Config.LastSeenImagePath;
        }

        // check for Welcome image
        if (string.IsNullOrEmpty(pathToLoad))
        {
            if (Core.Config.ShowWelcomeImage)
            {
                pathToLoad = BHelper.BaseDir("default.webp");
            }
            else
            {
                return;
            }
        }

        // start loading path with the foreground shell
        PrepareLoadPhotoList([pathToLoad],
            currentFilePath: null, disposeForegroundShell: false, loadInitPhoto: true);
    }


    public void PrepareLoadPhotoList(ICollection<string> inputPaths, string? currentFilePath, bool disposeForegroundShell, bool loadInitPhoto)
    {
        // dispose the foreground shell if requested
        if (disposeForegroundShell) Core.ShellProvider?.ForegroundShell = null;


        // check if we should load images from foreground window
        var useForegroundWindow = Core.ShellProvider?.CanUseForegroundShell() ?? false;
        var foregroundShell = useForegroundWindow
            ? Core.ShellProvider?.ForegroundShell
            : null;


        Dispatcher.UIThread.Post(async () =>
        {
            // start loading files
            var initPhoto = Core.Photos.StartLoadingFiles(inputPaths, currentFilePath,
                new FileSearchOptions()
                {
                    AllowedExtensions = Core.Config.FileFormats,
                    UseExplorerSortOrder = Core.Config.ShouldUseExplorerSortOrder,
                    ForegroundShell = foregroundShell,
                    SearchSubDirectories = Core.Config.EnableRecursiveLoading,
                    GroupByDir = Core.Config.ShouldGroupImagesByDirectory,
                    IncludeHidden = Core.Config.ShouldLoadHiddenImages,
                    OrderBy = Core.Config.ImageLoadingOrder,
                    OrderType = Core.Config.ImageLoadingOrderType,
                }, Files_Searched);


            if (loadInitPhoto && initPhoto is not null)
            {
                _ = ViewPhotoAsync(initPhoto);
            }
        });
    }


    private async void Files_Searched(FileSearchingEventArgs e)
    {
        var isEmptyList = Core.Photos.Count == 0;

        // add files to list on UI thread
        Dispatcher.UIThread.Invoke(() => Core.Photos.Add(e.Results));


        // if we haven't found current index for the init photo yet
        if (Core.Photos.InitPhoto is not null && Core.Photos.CurrentIndex == -1)
        {
            // find index of the init photo and select it
            _ = Core.Photos.Select(Core.Photos.InitPhoto.FilePath);

            // save the init photo to the list
            if (Core.Photos.CurrentIndex >= 0)
            {
                Core.Photos.Items[Core.Photos.CurrentIndex]?.Dispose();
                Core.Photos.Items[Core.Photos.CurrentIndex] = Core.Photos.InitPhoto;
                Core.Photos.Items[Core.Photos.CurrentIndex].IsCurrent = true;
            }
        }
        // display the first file in a folder
        else
        {
            Core.Photos.InitPhoto = Core.Photos.Select(0);
            _ = ViewPhotoAsync(Core.Photos.InitPhoto, true, false);
        }


        // Gallery: scroll to the selected item
        if (isEmptyList)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                // set photo to the viewer
                PART_Gallery.ScrollToItem(Core.Photos.CurrentIndex);
            });
        }
    }


    public async Task ViewPhotoAsync(Photo? photo, bool useCache = true, bool scrollToThumbnail = true)
    {
        // clear the current in-app message
        _ = PART_Message.ClearAsync();

        Core.DisposeClipboardPhoto();
        Core.ImageTransform.Clear();

        // set read options for photo
        if (photo is not null)
        {
            photo.ReadOptions = new()
            {
                FrameIndex = 0,
                FirstFrameOnly = Core.Config.SingleFrameFormats.Contains(photo.Extension),
            };
        }

        // apply user settings to the viewer
        PART_Viewer.EnableImagePreview = Core.Config.ShowImagePreview;


        Dispatcher.UIThread.Post(async () =>
        {
            if (scrollToThumbnail)
            {
                // set photo to the viewer
                PART_Gallery.ScrollToItem(Core.Photos.CurrentIndex);
            }

            await PART_Viewer.SetPhotoAsync(photo, useCache);
        });
    }



    /// <summary>
    /// Updates app layout.
    /// </summary>
    public void ApplyAppLayout()
    {
        // 1. read control's layouts from setting
        var toolbarPos = Config.GetControlLayout(LayoutControl.Toolbar);
        var galleryPos = Config.GetControlLayout(LayoutControl.Gallery);


        // 2. create layout
        if (toolbarPos == LayoutPosition.Top)
        {
            PART_Toolbar.ItemTooltipPlacement = PlacementMode.Bottom;

            if (galleryPos == LayoutPosition.Top)
            {
                PART_Layout.RowDefinitions = new("Auto, Auto, *");
                PART_Layout.ColumnDefinitions = new("*");

                PART_GalleryResizer.IsVisible = false;
                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Bottom;
                Grid.SetColumnSpan(PART_Toolbar, 1);

                Grid.SetRow(PART_Toolbar, 0);
                Grid.SetRow(PART_Gallery, 1);
                Grid.SetRow(PART_ViewerWrapper, 2);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
            else if (galleryPos == LayoutPosition.Bottom)
            {
                PART_Layout.RowDefinitions = new("Auto, *, Auto");
                PART_Layout.ColumnDefinitions = new("*");

                PART_GalleryResizer.IsVisible = false;
                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Top;
                Grid.SetColumnSpan(PART_Toolbar, 1);

                Grid.SetRow(PART_Toolbar, 0);
                Grid.SetRow(PART_Gallery, 2);
                Grid.SetRow(PART_GalleryResizer, 0);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_GalleryResizer, 0);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
            else if (galleryPos == LayoutPosition.Left)
            {
                PART_Layout.RowDefinitions = new("Auto, *");
                PART_Layout.ColumnDefinitions = new("Auto, Auto, *");

                PART_GalleryResizer.IsVisible = true;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Pointer;
                Grid.SetColumnSpan(PART_Toolbar, 3);

                Grid.SetRow(PART_Toolbar, 0);
                Grid.SetRow(PART_Gallery, 1);
                Grid.SetRow(PART_GalleryResizer, 1);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_GalleryResizer, 1);
                Grid.SetColumn(PART_ViewerWrapper, 2);
            }
            else if (galleryPos == LayoutPosition.Right)
            {
                PART_Layout.RowDefinitions = new("Auto, *");
                PART_Layout.ColumnDefinitions = new("*, Auto, Auto");

                PART_GalleryResizer.IsVisible = true;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Pointer;
                Grid.SetColumnSpan(PART_Toolbar, 3);

                Grid.SetRow(PART_Toolbar, 0);
                Grid.SetRow(PART_Gallery, 1);
                Grid.SetRow(PART_GalleryResizer, 1);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 2);
                Grid.SetColumn(PART_GalleryResizer, 1);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
        }
        else if (toolbarPos == LayoutPosition.Bottom)
        {
            PART_Toolbar.ItemTooltipPlacement = PlacementMode.Top;

            if (galleryPos == LayoutPosition.Top)
            {
                PART_Layout.RowDefinitions = new("Auto, *, Auto");
                PART_Layout.ColumnDefinitions = new("*");

                PART_GalleryResizer.IsVisible = false;
                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Bottom;
                Grid.SetColumnSpan(PART_Toolbar, 1);

                Grid.SetRow(PART_Toolbar, 2);
                Grid.SetRow(PART_Gallery, 0);
                Grid.SetRow(PART_GalleryResizer, 0);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_GalleryResizer, 0);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
            else if (galleryPos == LayoutPosition.Bottom)
            {
                PART_Layout.RowDefinitions = new("*, Auto, Auto");
                PART_Layout.ColumnDefinitions = new("*");

                PART_GalleryResizer.IsVisible = false;
                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Top;
                Grid.SetColumnSpan(PART_Toolbar, 1);

                Grid.SetRow(PART_Toolbar, 2);
                Grid.SetRow(PART_Gallery, 1);
                Grid.SetRow(PART_GalleryResizer, 0);
                Grid.SetRow(PART_ViewerWrapper, 0);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_GalleryResizer, 0);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
            else if (galleryPos == LayoutPosition.Left)
            {
                PART_Layout.RowDefinitions = new("*, Auto");
                PART_Layout.ColumnDefinitions = new("Auto, Auto, *");

                PART_GalleryResizer.IsVisible = true;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Pointer;
                Grid.SetColumnSpan(PART_Toolbar, 3);

                Grid.SetRow(PART_Toolbar, 1);
                Grid.SetRow(PART_Gallery, 0);
                Grid.SetRow(PART_GalleryResizer, 0);
                Grid.SetRow(PART_ViewerWrapper, 0);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_GalleryResizer, 1);
                Grid.SetColumn(PART_ViewerWrapper, 2);
            }
            else if (galleryPos == LayoutPosition.Right)
            {
                PART_Layout.RowDefinitions = new("*, Auto");
                PART_Layout.ColumnDefinitions = new("*, Auto, Auto");

                PART_GalleryResizer.IsVisible = true;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                PART_Gallery.ItemTooltipPlacement = PlacementMode.Pointer;
                Grid.SetColumnSpan(PART_Toolbar, 3);

                Grid.SetRow(PART_Toolbar, 1);
                Grid.SetRow(PART_Gallery, 0);
                Grid.SetRow(PART_GalleryResizer, 0);
                Grid.SetRow(PART_ViewerWrapper, 0);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 2);
                Grid.SetColumn(PART_GalleryResizer, 1);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
        }


        // 3. update gallery initial width
        UpdateGalleryWidth();
    }


    /// <summary>
    /// Updates the width of the gallery and its resizer columns based on the current layout.
    /// </summary>
    private void UpdateGalleryWidth()
    {
        if (PART_Layout.ColumnDefinitions.Count == 0) return;

        // 1. get gallery position
        var galleryPos = Core.Config.Layout.GetValueOrDefault(LayoutControl.Gallery, LayoutPosition.Bottom);
        if (galleryPos is LayoutPosition.Top or LayoutPosition.Bottom) return;

        // 2. get column indexes
        var galleryColIndex = Grid.GetColumn(PART_Gallery);
        var galleryResizerColIndex = Grid.GetColumn(PART_GalleryResizer);


        // 3. hide gallery space
        if (!Core.Config.ShowGallery)
        {
            PART_Layout.ColumnDefinitions[galleryColIndex].Width = new(0);
            PART_Layout.ColumnDefinitions[galleryResizerColIndex].Width = new(0);
            PART_GalleryResizer.IsVisible = false;
        }

        // 4. update gallery size
        else
        {
            var galleryViewMinWidth = GalleryControl.CalculateWidthForGalleryView(1);
            var galleryViewWidth = GalleryControl.CalculateWidthForGalleryView(Core.Config.GalleryColumns);
            galleryViewWidth = Math.Min(galleryViewWidth, Bounds.Width * 0.8); // max width = 80% window width

            PART_Layout.ColumnDefinitions[galleryColIndex].Width = new(galleryViewWidth);
            PART_Layout.ColumnDefinitions[galleryColIndex].MinWidth = galleryViewMinWidth;
            PART_Layout.ColumnDefinitions[galleryResizerColIndex].Width = new(5);
            PART_GalleryResizer.IsVisible = true;
        }
    }


    #endregion // Control Methods


}