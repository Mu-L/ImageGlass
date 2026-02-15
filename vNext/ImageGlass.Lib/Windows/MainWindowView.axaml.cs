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
using ImageGlass.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Common.Windows;

public partial class MainWindowView : PhControl
{
    public MainWindowViewModel VM => (MainWindowViewModel)DataContext!;


    public MainWindowView()
    {
        InitializeComponent();

        // apply app layout from settings
        ApplyLayout();
    }


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // drag-drop events
        DragDrop.SetAllowDrop(PART_Viewer, true);
        DragDrop.AddDragOverHandler(PART_Viewer, PART_Viewer_DragOver);
        DragDrop.AddDropHandler(PART_Viewer, PART_Viewer_Drop);

        Core.Config.PropertyChanged += Config_PropertyChanged;
        PART_Viewer.PhotoLoading += PART_Viewer_PhotoLoading;


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

    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);

        if (string.IsNullOrEmpty(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.ViewerBackground));
            _ = VM.OnPropertyChanged(nameof(VM.GalleryBackground));
        }
    }


    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Config.Layout))
        {
            ApplyLayout();
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

        if (!File.Exists(pathToLoad)) return;

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
        Core.Photos.Add(e.Results);

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
    private void ApplyLayout()
    {
        // 1. read control's layouts from setting
        var toolbarPos = Core.Config.Layout.GetValueOrDefault(LayoutControl.Toolbar, LayoutPosition.Top);
        var galleryPos = Core.Config.Layout.GetValueOrDefault(LayoutControl.Gallery, LayoutPosition.Bottom);


        // 2. standardize toolbar position
        if (toolbarPos is LayoutPosition.Left or LayoutPosition.Right)
        {
            toolbarPos = LayoutPosition.Top;
        }


        // 3. create layout
        if (toolbarPos == LayoutPosition.Top)
        {
            PART_Toolbar.ItemTooltipPlacement = PlacementMode.Bottom;

            if (galleryPos == LayoutPosition.Top)
            {
                PART_Layout.RowDefinitions = new("Auto, Auto, *");
                PART_Layout.ColumnDefinitions = new();

                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
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
                PART_Layout.ColumnDefinitions = new();

                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
                Grid.SetColumnSpan(PART_Toolbar, 1);

                Grid.SetRow(PART_Toolbar, 0);
                Grid.SetRow(PART_Gallery, 2);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
            else if (galleryPos == LayoutPosition.Left)
            {
                PART_Layout.RowDefinitions = new("Auto, *");
                PART_Layout.ColumnDefinitions = new("Auto, *");

                PART_Gallery.MaxWidth = 300;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                Grid.SetColumnSpan(PART_Toolbar, 2);

                Grid.SetRow(PART_Toolbar, 0);
                Grid.SetRow(PART_Gallery, 1);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_ViewerWrapper, 1);
            }
            else if (galleryPos == LayoutPosition.Right)
            {
                PART_Layout.RowDefinitions = new("Auto, *");
                PART_Layout.ColumnDefinitions = new("*, Auto");

                PART_Gallery.MaxWidth = 300;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                Grid.SetColumnSpan(PART_Toolbar, 2);

                Grid.SetRow(PART_Toolbar, 0);
                Grid.SetRow(PART_Gallery, 1);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 1);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
        }
        else if (toolbarPos == LayoutPosition.Bottom)
        {
            PART_Toolbar.ItemTooltipPlacement = PlacementMode.Top;

            if (galleryPos == LayoutPosition.Top)
            {
                PART_Layout.RowDefinitions = new("Auto, *, Auto");
                PART_Layout.ColumnDefinitions = new();

                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
                Grid.SetColumnSpan(PART_Toolbar, 1);

                Grid.SetRow(PART_Toolbar, 2);
                Grid.SetRow(PART_Gallery, 0);
                Grid.SetRow(PART_ViewerWrapper, 1);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
            else if (galleryPos == LayoutPosition.Bottom)
            {
                PART_Layout.RowDefinitions = new("*, Auto, Auto");
                PART_Layout.ColumnDefinitions = new();

                PART_Gallery.MaxWidth = double.PositiveInfinity;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.FilmStrip;
                Grid.SetColumnSpan(PART_Toolbar, 1);

                Grid.SetRow(PART_Toolbar, 2);
                Grid.SetRow(PART_Gallery, 1);
                Grid.SetRow(PART_ViewerWrapper, 0);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
            else if (galleryPos == LayoutPosition.Left)
            {
                PART_Layout.RowDefinitions = new("*, Auto");
                PART_Layout.ColumnDefinitions = new("Auto, *");

                PART_Gallery.MaxWidth = 300;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                Grid.SetColumnSpan(PART_Toolbar, 2);

                Grid.SetRow(PART_Toolbar, 1);
                Grid.SetRow(PART_Gallery, 0);
                Grid.SetRow(PART_ViewerWrapper, 0);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 0);
                Grid.SetColumn(PART_ViewerWrapper, 1);
            }
            else if (galleryPos == LayoutPosition.Right)
            {
                PART_Layout.RowDefinitions = new("*, Auto");
                PART_Layout.ColumnDefinitions = new("*, Auto");

                PART_Gallery.MaxWidth = 300;
                PART_Gallery.ViewMode = PhVirtualizingUniformPanelViewMode.Gallery;
                Grid.SetColumnSpan(PART_Toolbar, 2);

                Grid.SetRow(PART_Toolbar, 1);
                Grid.SetRow(PART_Gallery, 0);
                Grid.SetRow(PART_ViewerWrapper, 0);
                Grid.SetColumn(PART_Toolbar, 0);
                Grid.SetColumn(PART_Gallery, 1);
                Grid.SetColumn(PART_ViewerWrapper, 0);
            }
        }
    }


}