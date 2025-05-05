using ImageGlass.Common;
using ImageGlass.Common.FileSystem;
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
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode;

        WinMainTitleBarText.Text = $".NET {Environment.Version} - {Environment.OSVersion}";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(WinMainTitleBar);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(2000, 800));
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


        OpenPhotoFromPath(path);
    }


    private async void BtnOpenFile_Clicked(object sender, RoutedEventArgs e)
    {
        var op = new Windows.Storage.Pickers.FileOpenPicker();
        op.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(op, Handle);

        var file = await op.PickSingleFileAsync();
        if (file == null) return;


        OpenPhotoFromPath(file.Path);
    }


    private async void BtnOpenFolder_Clicked(object sender, RoutedEventArgs e)
    {
        var fp = new Windows.Storage.Pickers.FolderPicker();
        InitializeWithWindow.Initialize(fp, Handle);

        var dir = await fp.PickSingleFolderAsync();
        if (dir == null) return;


        OpenPhotoFromPath(dir.Path);
    }

    private void BtnViewNext_Clicked(object sender, RoutedEventArgs e)
    {
        ViewNext(1);
    }

    private void BtnViewPrevious_Clicked(object sender, RoutedEventArgs e)
    {
        ViewNext(-1);
    }


    private async void OpenPhotoFromPath(string path)
    {
        WinMainTitleBarText.Text = path;

        var photo = await Local.Photos.LoadFolderAsync(path, new FilesSearchOptions()
        {
            AllowedExtensions = Const.FileFormats,
        });

        Viewer.SetPhoto(photo);
    }


    private void ViewNext(int step)
    {
        var photo = Local.Photos.GetByStep(step, true);

        WinMainTitleBarText.Text = photo?.FilePath;
        Viewer.SetPhoto(photo);
    }

}
