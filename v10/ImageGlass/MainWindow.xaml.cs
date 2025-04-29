using ImageGlass.WinNT.Common.Photoing;
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

        Title = $".NET {Environment.Version} - {Environment.OSVersion}";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(WinMainTitleBar);
    }

    public GridLength TitleBarLeftInset => new(AppWindow.TitleBar.LeftInset);
    public GridLength TitleBarRightInset => new(AppWindow.TitleBar.RightInset);
    public GridLength TitleBarHeight => new(AppWindow.TitleBar.Height / 2.5f);
    public Thickness TitleBarMargin => new Thickness(0, 0, AppWindow.TitleBar.RightInset / 2.5f, 0);


    private void Window_Closed(object sender, WindowEventArgs args)
    {

    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
        openPicker.FileTypeFilter.Add("*");
        var hWnd = WindowNative.GetWindowHandle(this);

        InitializeWithWindow.Initialize(openPicker, hWnd);

        var file = await openPicker.PickSingleFileAsync();
        if (file == null) return;


        Title = file.Path;

        var photo = new Photo(file.Path);

        Viewer.SetPhoto(photo);
    }


    private void Viewer_Loaded(object sender, RoutedEventArgs e)
    {

        var photo = new Photo(path);

        Viewer.SetPhoto(photo);

    }

}
