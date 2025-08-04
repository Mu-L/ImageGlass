
using ImageGlass.Common;
using ImageGlass.Common.FileSystem;
using ImageGlass.WinNT.Common.FileSystem;
using Microsoft.UI.Xaml;
using System;
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

        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode;

        WinMainTitleBarText.Text = $".NET {Environment.Version} - {Environment.OSVersion}";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(WinMainTitleBar);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(2000, 1500));
    }

    public GridLength TitleBarLeftInset => new(AppWindow.TitleBar.LeftInset);
    public GridLength TitleBarRightInset => new(AppWindow.TitleBar.RightInset);
    public GridLength TitleBarHeight => new(AppWindow.TitleBar.Height / 2.5f);
    public Thickness TitleBarMargin => new Thickness(0, 0, AppWindow.TitleBar.RightInset / 2.5f, 0);

    public nint Handle => WindowNative.GetWindowHandle(this);


    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Viewer.UnloadPhoto();
    }


    private void Viewer_Loaded(object sender, RoutedEventArgs e)
    {
        LoadImagesFromCmdArgs();
    }


    private async void BtnOpenFile_Clicked(object sender, RoutedEventArgs e)
    {
        var op = new Windows.Storage.Pickers.FileOpenPicker();
        op.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(op, Handle);

        var file = await op.PickSingleFileAsync();
        if (file == null) return;


        PrepareLoadPhoto(file.Path, false);
    }


    private async void BtnOpenFolder_Clicked(object sender, RoutedEventArgs e)
    {
        var fp = new Windows.Storage.Pickers.FolderPicker();
        InitializeWithWindow.Initialize(fp, Handle);

        var dir = await fp.PickSingleFolderAsync();
        if (dir == null) return;


        PrepareLoadPhoto(dir.Path, false);
    }

    private void BtnViewNext_Clicked(object sender, RoutedEventArgs e)
    {
        ViewNext(1);
    }

    private void BtnViewPrevious_Clicked(object sender, RoutedEventArgs e)
    {
        ViewNext(-1);
    }

    private void Gallery_ItemClicked(WinNT.GalleryButtonItem sender, EventArgs args)
    {
        var photoIndex = Local.Photos.IndexOf(sender.FilePath);
        if (photoIndex < 0) return;

        var step = photoIndex - Local.Photos.CurrentIndex;
        ViewNext(step);
    }



    private void LoadImagesFromCmdArgs()
    {
        var pathToLoad = Local.InputImagePathFromArgs;

        //if (string.IsNullOrEmpty(pathToLoad)
        //    && Config.ShouldOpenLastSeenImage
        //    && BHelper.CheckPath(Config.LastSeenImagePath) == PathType.File)
        //{
        //    pathToLoad = Config.LastSeenImagePath;
        //}


        //if (string.IsNullOrEmpty(pathToLoad))
        //{
        //    if (Config.ShowWelcomeImage)
        //    {
        //        pathToLoad = App.StartUpDir("default.webp");
        //    }
        //    else
        //    {
        //        return;
        //    }
        //}


        // start loading path with the foreground shell
        PrepareLoadPhoto(pathToLoad, false);
    }


    private async void PrepareLoadPhoto(string path, bool disposeForegroundShell)
    {
        WinMainTitleBarText.Text = path;

        // dispose the foreground shell if requested
        if (disposeForegroundShell) Local.ForegroundShell = null;


        // check if we should load images from foreground window
        var useForegroundWindow = Local.CanUseForegroundShell();
        var foregroundShell = useForegroundWindow
            ? Local.ForegroundShell
            : null;


        // start loading files
        var photo = await Local.Photos.LoadFolderAsync(path, new FileShellSearchOptions()
        {
            AllowedExtensions = Const.FileFormats,
            UseExplorerSortOrder = true, // TODO: from setting
            ForegroundShell = foregroundShell,
        }, _searchProgress);



        Viewer.SetPhoto(photo);
        Gallery.ClearThumbnails();
    }

    private void Files_Searched(FileSearchingEventArgs e)
    {
        Local.Photos.Add(e.Results);

        // if we haven't found current index for the init photo yet
        if (Local.Photos.InitPhoto is not null && Local.Photos.CurrentIndex == -1)
        {
            _ = Local.Photos.Select(Local.Photos.InitPhoto.FilePath);

            //// save the init photo to the list
            //if (CurrentIndex >= 0)
            //{
            //    _list[CurrentIndex]?.Dispose();
            //    _list[CurrentIndex] = InitPhoto;
            //}
        }
    }


    private void ViewNext(int step)
    {
        var photo = Local.Photos.GetByStep(step, true);

        WinMainTitleBarText.Text = photo?.FilePath;
        Viewer.SetPhoto(photo);

        if (photo != null)
        {
            Gallery.SelectItem(photo.FilePath);
        }
    }


}
