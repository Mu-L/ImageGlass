
using D2Phap;
using ImageGlass.Common;
using ImageGlass.Common.FileSystem;
using ImageGlass.Win64.Common;
using ImageGlass.Win64.Common.FileSystem;
using ImageGlass.Win64.Common.Photoing;
using ImageGlass.Win64.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageGlass;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private Progress<FileSearchingEventArgs> _searchProgress;


    public MainWindow()
    {
        InitializeComponent();

        _searchProgress = new(Files_Searched);

        // set title bar
        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(WinMainTitleBar);

        // load toolbar buttons
        LoadToolbarButtons();

        AppWindow.Resize(new Windows.Graphics.SizeInt32(2000, 1500));
    }


    public GridLength TitleBarLeftInset => new(AppWindow.TitleBar.LeftInset);
    public GridLength TitleBarRightInset => new(AppWindow.TitleBar.RightInset);
    public GridLength TitleBarHeight => new(AppWindow.TitleBar.Height / 2.5f);
    public Thickness TitleBarMargin => new Thickness(0, 0, AppWindow.TitleBar.RightInset / 2.5f, 0);

    public nint Handle => WindowNative.GetWindowHandle(this);

    public SystemBackdrop? WindowBackdrop
    {
        get
        {
            if (Config.Current.WindowBackdrop == BackdropStyle.None) return null;
            if (Config.Current.WindowBackdrop == BackdropStyle.Acrylic)
            {
                return new DesktopAcrylicBackdrop();
            }
            else
            {
                return new MicaBackdrop()
                {
                    Kind = Config.Current.WindowBackdrop == BackdropStyle.MicaAlt
                        ? MicaKind.BaseAlt
                        : MicaKind.Base
                };
            }
        }
    }


    private void LoadToolbarButtons()
    {
        ToolbarMain.AddButtons(Config.Current.ToolbarButtons);
    }


    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Viewer.UnloadPhoto();
    }


    private void Viewer_Loaded(object sender, RoutedEventArgs e)
    {
        LoadImagesFromCmdArgs();
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


        // 3. load single path
        var filePath = WHelper.ResolvePath(paths[0]);
        var imageIndex = Local.Photos.IndexOf(filePath);


        // 3.1 get foreground shell
        if (Config.Current.ShouldUseExplorerSortOrder)
        {
            using var shell = new EggShell();
            Local.ForegroundShell = shell.GetForegroundWindowView();
        }

        // 3.2 save init input path
        Local.UpdateInputImagePath(filePath);


        // 3.3 The file is located another folder, load the entire folder
        if (imageIndex == -1 || Local.CanUseForegroundShell())
        {
            PrepareLoadPhoto([filePath], false);
        }
        // 3.4 The file is in current folder AND it is the viewing image
        else if (Local.Photos.CurrentIndex == imageIndex)
        {
            //do nothing
        }
        // 3.5 The file is in current folder AND it is NOT the viewing image
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
                Local.Photos.Items[Local.Photos.CurrentIndex].IsSelected = true;
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
        WinMainTitleBarText.Text = photo?.FilePath;
        Viewer.SetPhoto(photo);

        Gallery.ScrollToItem(Local.Photos.CurrentIndex);
    }


}
