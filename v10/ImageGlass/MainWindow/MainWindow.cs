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
using D2Phap;
using ImageGlass.Common;
using ImageGlass.Common.FileSystem;
using ImageGlass.Common.Photoing;
using ImageGlass.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace ImageGlass;

public partial class MainWindow : IgWindow
{
    private readonly AppStatusInfo _status;
    private readonly MainWindow_Content _contentEl;
    private readonly Progress<FileSearchingEventArgs> _searchProgress;



    public ToolbarControl ToolbarMain => _contentEl.ToolbarMain;
    public GalleryControl Gallery => _contentEl.Gallery;
    public VirtualViewerControl Viewer => _contentEl.Viewer;


    public MainWindow()
    {
        WindowContent = _contentEl = new(this);
        UseBackdropForTransparentWindowOnly = false;
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
        RegisterHotkeys_();


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
        Viewer.UnloadPhoto();
        AP.DisposeClipboardPhoto();
        WicCodec.DisposeResources();

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

        base.OnIgWindowClosing(sender, e);
    }


    protected override async void OnIgWindowClosed(WindowEventArgs e)
    {
        // save user config
        await SaveConfigOnClosingAsync();

        base.OnIgWindowClosed(e);
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


    private void Toolbar_ButtonClicked(IgToolbarButton sender, ToolbarItemClickedEventArgs e)
    {
        _ = RunActionAsync(e.VM.OnClick);
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
            PrepareLoadPhotoList(paths,
                currentFilePath: null, disposeForegroundShell: true, loadInitPhoto: true);
            return;
        }


        // 3. load single file path
        // 3.1 get foreground shell
        if (AP.Config.ShouldUseExplorerSortOrder)
        {
            using var shell = new EggShell();
            AP.ForegroundShell = shell.GetForegroundWindowView();
        }
        AP.UpdateInitImagePath(paths[0]);

        // 3.2 open the path
        IG_OpenPath(paths[0]);
    }


    private void Viewer_ZoomChanged(VirtualViewerControl sender, ZoomEventArgs e)
    {

    }


    private async void Files_Searched(FileSearchingEventArgs e)
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
            _ = ViewPhotoAsync(AP.Photos.InitPhoto, true, false);
        }


        // Gallery: scroll to the selected item
        if (isEmptyList)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                // set gallery items source
                await Gallery.SetSourceAsync(AP.Photos.Items);

                // set photo to the viewer
                Gallery.ScrollToItem(AP.Photos.CurrentIndex, AP.Photos.Count > 1000);
            });
        }
    }


    #endregion // Control events



    private async Task SaveConfigOnClosingAsync()
    {
        // save window maximized state
        AP.Config.IsMainWindowMaximized = WindowState == OverlappedPresenterState.Maximized;

        // save window bounds
        if (WindowState == OverlappedPresenterState.Restored)
        {
            AP.Config.MainWindowBounds = GetWindowBounds();
        }

        AP.Config.LastSeenImagePath = AP.Photos.CurrentFilePath;
        //AP.Config.ZoomLockValue = Viewer.ZoomFactor * 100f;


        // save config to file
        await AP.Config.SaveAsync();


        // dispose the global singleton
        AP.Dispose();


        //// cleaning
        //try
        //{
        //    // delete trash
        //    Directory.Delete(Config.ConfigDir(PathType.Dir, Dir.Temporary), true);
        //}
        //catch { }
    }


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
        PrepareLoadPhotoList([pathToLoad],
            currentFilePath: null, disposeForegroundShell: false, loadInitPhoto: true);
    }


    private void PrepareLoadPhotoList(ICollection<string> inputPaths, string? currentFilePath,
        bool disposeForegroundShell, bool loadInitPhoto)
    {
        // dispose the foreground shell if requested
        if (disposeForegroundShell) AP.ForegroundShell = null;


        // check if we should load images from foreground window
        var useForegroundWindow = AP.CanUseForegroundShell();
        var foregroundShell = useForegroundWindow
            ? AP.ForegroundShell
            : null;


        DispatcherQueue.TryEnqueue(async () =>
        {
            // clear gallery
            await Gallery.ClearSourceAsync();

            // start loading files
            var initPhoto = AP.Photos.StartLoadingFiles(inputPaths, currentFilePath,
                new FileShellSearchOptions()
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


            if (loadInitPhoto)
            {
                _ = ViewPhotoAsync(initPhoto);
            }
        });
    }


    private async Task ViewPhotoAsync(Photo? photo, bool useCache = true, bool scrollToThumbnail = true)
    {
        // clear the current in-app message
        _ = _contentEl.ShowMessageAsync(null);

        AP.DisposeClipboardPhoto();
        AP.ImageTransform.Clear();

        // set read options for photo
        if (photo is not null)
        {
            photo.ReadOptions = new()
            {
                FrameIndex = 0,
                FirstFrameOnly = AP.Config.SingleFrameFormats.Contains(photo.Extension),
            };
        }

        // apply user settings to the viewer
        Viewer.EnableImagePreview = AP.Config.ShowImagePreview;


        if (scrollToThumbnail)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // set photo to the viewer
                Gallery.ScrollToItem(AP.Photos.CurrentIndex);
            });
        }


        await Viewer.SetPhotoAsync(photo, useCache);
    }


}
