
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
using Catel.Collections;
using D2Phap;
using ImageGlass.Common;
using ImageGlass.Common.FileSystem;
using ImageGlass.Win64.Common;
using ImageGlass.Win64.Common.FileSystem;
using ImageGlass.Win64.Common.Photoing;
using ImageGlass.Win64.UI;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using WinRT.Interop;


namespace ImageGlass;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private Progress<FileSearchingEventArgs> _searchProgress;

    public MainWindowViewModel VM;
    public nint Handle => WindowNative.GetWindowHandle(this);


    public MainWindow()
    {
        _searchProgress = new(Files_Searched);
        VM = new MainWindowViewModel(this);

        InitializeComponent();

        // set title bar
        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(WinTitleBar);

        AppWindow.Resize(new SizeInt32(2000, 1500));
    }

    private void WindowContent_Loaded(object sender, RoutedEventArgs e)
    {
        // set title bar
        SetTitleBar();

        // load image from command line arguments
        LoadImagesFromCmdArgs();
    }

    private void Window_Closed(object sender, WindowEventArgs e)
    {
        Viewer.UnloadPhoto();
        VM.Dispose();
    }


    private void Viewer_DragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = "Open with ImageGlass";
    }


    private async void Viewer_Drop(object sender, DragEventArgs e)
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
        var path = WHelper.ResolvePath(paths[0]);
        if (BHelper.CheckPath(path) == PathType.Dir)
        {
            PrepareLoadPhoto([path], true);
            return;
        }


        // 4. load single file path
        var imageIndex = Local.Photos.IndexOf(path);

        // 4.1 get foreground shell
        if (Config.Current.ShouldUseExplorerSortOrder)
        {
            using var shell = new EggShell();
            Local.ForegroundShell = shell.GetForegroundWindowView();
        }

        // 4.2 save init input path
        Local.UpdateInputImagePath(path);


        // 4.3 The file is located another folder, load the entire folder
        if (imageIndex == -1 || Local.CanUseForegroundShell())
        {
            PrepareLoadPhoto([path], false);
        }
        // 4.4 The file is in current folder AND it is the viewing image
        else if (Local.Photos.CurrentIndex == imageIndex)
        {
            //do nothing
        }
        // 4.5 The file is in current folder AND it is NOT the viewing image
        else
        {
            ViewByIndex(imageIndex);
        }
    }


    private async void BtnOpenFile_Clicked(object sender, RoutedEventArgs e)
    {
        var op = new Windows.Storage.Pickers.FileOpenPicker();
        op.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(op, Handle);

        var file = await op.PickSingleFileAsync();
        if (file == null) return;


        PrepareLoadPhoto([file.Path], false);
    }


    private async void BtnOpenFolder_Clicked(object sender, RoutedEventArgs e)
    {
        var fp = new Windows.Storage.Pickers.FolderPicker();
        InitializeWithWindow.Initialize(fp, Handle);

        var dir = await fp.PickSingleFolderAsync();
        if (dir == null) return;


        PrepareLoadPhoto([dir.Path], false);
    }

    private void BtnViewNext_Clicked(object sender, RoutedEventArgs e)
    {
        ViewByStep(1);
    }

    private void BtnViewPrevious_Clicked(object sender, RoutedEventArgs e)
    {
        ViewByStep(-1);
    }

    private void Gallery_ItemClicked(GalleryButtonItem sender, EventArgs args)
    {
        var photoIndex = Local.Photos.IndexOf(sender.FilePath);
        ViewByIndex(photoIndex);
    }


    private void SetTitleBar()
    {
        // update title bar size according to API
        VM.TitleBarHeight = AppWindow.TitleBar.Height;
        VM.TitleBarRightInset = AppWindow.TitleBar.RightInset;

        // set drag area
        var dragRect = new RectInt32(
                0, 0,
                (int)(ToolbarMain.ActualWidth * VM.DpiScale),
                (int)((ToolbarMain.ActualHeight + VM.TitleBarHeight) * VM.DpiScale));

        AppWindow.TitleBar.SetDragRectangles([dragRect]);
    }


    private void LoadImagesFromCmdArgs()
    {
        var pathToLoad = Local.InputImagePathFromArgs;

        // check for last seen image
        if (string.IsNullOrEmpty(pathToLoad)
            && Config.Current.ShouldOpenLastSeenImage
            && BHelper.CheckPath(Config.Current.LastSeenImagePath) == PathType.File)
        {
            pathToLoad = Config.Current.LastSeenImagePath;
        }

        // check for Welcome image
        if (string.IsNullOrEmpty(pathToLoad))
        {
            if (Config.Current.ShowWelcomeImage)
            {
                pathToLoad = Config.StartUpDir("default.webp");
            }
            else
            {
                return;
            }
        }


        // start loading path with the foreground shell
        PrepareLoadPhoto([pathToLoad], false);
    }


    private void PrepareLoadPhoto(string[] inputPaths, bool disposeForegroundShell)
    {
        // dispose the foreground shell if requested
        if (disposeForegroundShell) Local.ForegroundShell = null;


        // check if we should load images from foreground window
        var useForegroundWindow = Local.CanUseForegroundShell();
        var foregroundShell = useForegroundWindow
            ? Local.ForegroundShell
            : null;


        // start loading files
        var initPhoto = Local.Photos.StartLoadingFiles(inputPaths, new FileShellSearchOptions()
        {
            AllowedExtensions = Config.Current.FileFormats,
            UseExplorerSortOrder = Config.Current.ShouldUseExplorerSortOrder,
            ForegroundShell = foregroundShell,
            SearchSubDirectories = Config.Current.EnableRecursiveLoading,
            GroupByDir = Config.Current.ShouldGroupImagesByDirectory,
            IncludeHidden = Config.Current.ShouldLoadHiddenImages,
            OrderBy = Config.Current.ImageLoadingOrder,
            OrderType = Config.Current.ImageLoadingOrderType,
        }, _searchProgress);


        Gallery.ClearThumbnails();

        ViewPhoto(initPhoto);
    }


    private void Files_Searched(FileSearchingEventArgs e)
    {
        var isEmptyList = Local.Photos.Count == 0;
        Local.Photos.Add(e.Results);

        // if we haven't found current index for the init photo yet
        if (Local.Photos.InitPhoto is not null && Local.Photos.CurrentIndex == -1)
        {
            // find index of the init photo and select it
            _ = Local.Photos.Select(Local.Photos.InitPhoto.FilePath);

            // save the init photo to the list
            if (Local.Photos.CurrentIndex >= 0)
            {
                Local.Photos.Items[Local.Photos.CurrentIndex]?.Dispose();
                Local.Photos.Items[Local.Photos.CurrentIndex] = Local.Photos.InitPhoto;
                Local.Photos.Items[Local.Photos.CurrentIndex].IsCurrent = true;
            }
        }
        // display the first file in a folder
        else
        {
            Local.Photos.InitPhoto = Local.Photos.Select(0);
            ViewPhoto(Local.Photos.InitPhoto);
        }


        // Gallery: scroll to the selected item
        if (isEmptyList)
        {
            // make sure gallery is rendered
            Gallery.UpdateLayout();
            Gallery.ScrollToItem(Local.Photos.CurrentIndex);
        }
    }


    private void ViewByIndex(int photoIndex)
    {
        if (photoIndex < 0) return;

        var step = photoIndex - Local.Photos.CurrentIndex;
        ViewByStep(step);
    }


    private void ViewByStep(int step)
    {
        var photo = Local.Photos.GetByStep(step, true);
        ViewPhoto(photo);
    }


    private void ViewPhoto(Photo? photo)
    {
        VM.Title = photo?.FilePath;
        Viewer.SetPhoto(photo);

        Gallery.ScrollToItem(Local.Photos.CurrentIndex);
    }


}
