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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.ServiceProviders.FileSearchService;
using ImageGlass.Common.Types;
using ImageGlass.UI;
using ImageGlass.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Views;

public partial class MainView : PhControl
{
    public MainViewModel VM => (MainViewModel)DataContext!;


    public MainView()
    {
        InitializeComponent();
    }



    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // drag-drop events
        DragDrop.SetAllowDrop(PART_Viewer, true);
        DragDrop.AddDragOverHandler(PART_Viewer, PART_Viewer_DragOver);
        DragDrop.AddDropHandler(PART_Viewer, PART_Viewer_Drop);


        // load image from command line arguments
        LoadImagesFromCmdArgs();
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // drag-drop events
        DragDrop.RemoveDragOverHandler(PART_Viewer, PART_Viewer_DragOver);
        DragDrop.RemoveDropHandler(PART_Viewer, PART_Viewer_Drop);
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



    public void PrepareLoadPhotoList(ICollection<string> inputPaths, string? currentFilePath,
        bool disposeForegroundShell, bool loadInitPhoto)
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
            // TODO:
            //// clear gallery
            //await Gallery.ClearSourceAsync();

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


            if (loadInitPhoto)
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
                // TODO:
                //// set gallery items source
                //await Gallery.SetSourceAsync(AP.Photos.Items);

                //// set photo to the viewer
                //Gallery.ScrollToItem(AP.Photos.CurrentIndex, AP.Photos.Count > 100);
            });
        }
    }




    public async Task ViewPhotoAsync(Photo? photo, bool useCache = true, bool scrollToThumbnail = true)
    {
        //// clear the current in-app message
        //_ = _contentEl.ShowMessageAsync(null);

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


        //if (scrollToThumbnail)
        //{
        //    Dispatcher.UIThread.Post(() =>
        //    {
        //        // set photo to the viewer
        //        PART_Gallery.ScrollToItem(Core.Photos.CurrentIndex);
        //    });
        //}


        await PART_Viewer.SetPhotoAsync(photo, useCache);
    }






}