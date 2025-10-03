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
using D2Phap;
using ImageGlass.Common;
using ImageGlass.Common.FileSystem;
using ImageGlass.Common.Photoing;
using ImageGlass.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace ImageGlass;

public partial class MainWindow : IgWindow
{
    private readonly AppStatusInfo _status;
    private readonly MainWindow_Content _contentEl = new();
    private readonly Progress<FileSearchingEventArgs> _searchProgress;



    public ToolbarControl ToolbarMain => _contentEl.ToolbarMain;
    public GalleryControl Gallery => _contentEl.Gallery;
    public VirtualViewerControl Viewer => _contentEl.Viewer;


    public MainWindow()
    {
        WindowContent = _contentEl;
        _searchProgress = new(Files_Searched);
        _status = new(_contentEl.Viewer);


        // load window bounds from settings
        SetWindowBounds(AP.Config.MainWindowBounds, AP.Config.IsMainWindowMaximized);
    }


    #region Override methods

    protected override void OnIgWindowLoaded(FrameworkElement fe)
    {
        base.OnIgWindowLoaded(fe);

        // load image from command line arguments
        LoadImagesFromCmdArgs();

        // register hotkeys
        RegisterHotkeys();


        _contentEl.ToolbarButtonClicked += Toolbar_ButtonClicked;
        _contentEl.GalleryItemClicked += Gallery_ItemClicked;
        _contentEl.ViewerDrop += Viewer_Drop;
        _contentEl.ViewerZoomChanged += Viewer_ZoomChanged;
        Content.PreviewKeyUp += Content_PreviewKeyUp;

        _status.Changed += Status_Changed;
        Hotkey.Invoked += Hotkey_Invoked;
    }


    protected override void OnIgWindowClosing(AppWindow sender, AppWindowClosingEventArgs e)
    {
        base.OnIgWindowClosing(sender, e);

        // save window maximized state
        AP.Config.IsMainWindowMaximized = WindowState == OverlappedPresenterState.Maximized;

        // save window bounds
        if (WindowState == OverlappedPresenterState.Restored)
        {
            AP.Config.MainWindowBounds = GetWindowBounds();
        }


        Viewer.UnloadPhoto();

        // clear hotkeys
        Content.KeyboardAccelerators.Clear();
        Hotkey.Invoked -= Hotkey_Invoked;


        _contentEl.ToolbarButtonClicked -= Toolbar_ButtonClicked;
        _contentEl.GalleryItemClicked -= Gallery_ItemClicked;
        _contentEl.ViewerDrop -= Viewer_Drop;
        _contentEl.ViewerZoomChanged -= Viewer_ZoomChanged;
        Content.PreviewKeyUp -= Content_PreviewKeyUp;

        _status.Changed -= Status_Changed;
        _status.Dispose();
    }


    protected override void OnIgWindowStateChanged(WindowStateChangedEventArgs e)
    {
        base.OnIgWindowStateChanged(e);

        // save window bounds
        if (e.State == OverlappedPresenterState.Restored)
        {
            AP.Config.MainWindowBounds = e.Bounds;
        }
        else if (e.OldState == OverlappedPresenterState.Restored)
        {
            AP.Config.MainWindowBounds = e.OldBounds;
        }
    }

    #endregion // Override methods


    #region Control events

    private void Status_Changed(object? sender, EventArgs e)
    {
        WindowTitle = _status.Text;
    }


    private async void Toolbar_ButtonClicked(IgToolbarButton sender, ToolbarItemClickedEventArgs e)
    {
        var error = await RunActionAsync(e.VM.OnClick);
        if (error != null)
        {
            _ = await ModalWindow.ShowErrorAsync(this, e.VM.Text, error.Message);
        }
    }


    private void Gallery_ItemClicked(IgGalleryItem sender, EventArgs e)
    {
        var photoIndex = AP.Photos.IndexOf(sender.VM.FilePath);
        IG_ViewByIndex(photoIndex);
    }


    private async void Viewer_Drop(VirtualViewerControl sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        // 1. get dropped paths
        var droppedItems = await e.DataView.GetStorageItemsAsync();
        var paths = droppedItems.Select(i => i.Path).ToArray();


        // 2. load multiple paths
        if (paths.Length > 1)
        {
            PrepareLoadPhoto(paths, true);
            return;
        }


        // 3. load single directory path
        var path = BHelper.ResolvePath(paths[0]);
        if (BHelper.CheckPath(path) == PathType.Dir)
        {
            PrepareLoadPhoto([path], true);
            return;
        }


        // 4. load single file path
        // 4.1 get foreground shell
        if (AP.Config.ShouldUseExplorerSortOrder)
        {
            using var shell = new EggShell();
            AP.ForegroundShell = shell.GetForegroundWindowView();
        }

        // 4.2 save init input path
        AP.UpdateInputImagePath(path);

        // 4.3 open the path
        IG_OpenPath(path);
    }


    private void Viewer_ZoomChanged(VirtualViewerControl sender, ZoomEventArgs e)
    {

    }


    private void Files_Searched(FileSearchingEventArgs e)
    {
        var isEmptyList = AP.Photos.Count == 0;
        AP.Photos.Add(e.Results);

        // if we haven't found current index for the init photo yet
        if (AP.Photos.InitPhoto is not null && AP.Photos.CurrentIndex == -1)
        {
            // find index of the init photo and select it
            _ = AP.Photos.Select(AP.Photos.InitPhoto.FilePath);

            // save the init photo to the list
            if (AP.Photos.CurrentIndex >= 0)
            {
                AP.Photos.Items[AP.Photos.CurrentIndex]?.Dispose();
                AP.Photos.Items[AP.Photos.CurrentIndex] = AP.Photos.InitPhoto;
                AP.Photos.Items[AP.Photos.CurrentIndex].IsCurrent = true;
            }
        }
        // display the first file in a folder
        else
        {
            AP.Photos.InitPhoto = AP.Photos.Select(0);
            ViewPhoto(AP.Photos.InitPhoto);
        }


        // Gallery: scroll to the selected item
        if (isEmptyList)
        {
            // make sure gallery is rendered
            Gallery.UpdateLayout();
            Gallery.ScrollToItem(AP.Photos.CurrentIndex);
        }
    }


    #endregion // Control events





    private void LoadImagesFromCmdArgs()
    {
        var pathToLoad = AP.InputImagePathFromArgs;

        // check for last seen image
        if (string.IsNullOrEmpty(pathToLoad)
            && AP.Config.ShouldOpenLastSeenImage
            && BHelper.CheckPath(AP.Config.LastSeenImagePath) == PathType.File)
        {
            pathToLoad = AP.Config.LastSeenImagePath;
        }

        // check for Welcome image
        if (string.IsNullOrEmpty(pathToLoad))
        {
            if (AP.Config.ShowWelcomeImage)
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
        PrepareLoadPhoto([pathToLoad], false);
    }


    private void PrepareLoadPhoto(string[] inputPaths, bool disposeForegroundShell)
    {
        // dispose the foreground shell if requested
        if (disposeForegroundShell) AP.ForegroundShell = null;


        // check if we should load images from foreground window
        var useForegroundWindow = AP.CanUseForegroundShell();
        var foregroundShell = useForegroundWindow
            ? AP.ForegroundShell
            : null;


        // start loading files
        var initPhoto = AP.Photos.StartLoadingFiles(inputPaths, new FileShellSearchOptions()
        {
            AllowedExtensions = AP.Config.FileFormats,
            UseExplorerSortOrder = AP.Config.ShouldUseExplorerSortOrder,
            ForegroundShell = foregroundShell,
            SearchSubDirectories = AP.Config.EnableRecursiveLoading,
            GroupByDir = AP.Config.ShouldGroupImagesByDirectory,
            IncludeHidden = AP.Config.ShouldLoadHiddenImages,
            OrderBy = AP.Config.ImageLoadingOrder,
            OrderType = AP.Config.ImageLoadingOrderType,
        }, _searchProgress);


        ViewPhoto(initPhoto);
    }


    private void ViewPhoto(Photo? photo)
    {
        // clear the current in-app message
        _ = _contentEl.ShowMessageAsync(null);


        // set read options for photo
        if (photo is not null)
        {
            photo.ReadOptions = new()
            {
                FrameIndex = 0,
                FirstFrameOnly = AP.Config.SingleFrameFormats.Contains(photo.Extension),
            };
        }


        // set photo to the viewer
        Viewer.SetPhoto(photo);
        Gallery.ScrollToItem(AP.Photos.CurrentIndex);
    }


}
