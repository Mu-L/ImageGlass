using ImageGlass.WinNT;
using Microsoft.Graphics.Display;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
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

        _ = Viewer.LoadImageAsync(file.Path);

    }


    private void Viewer_Loaded(object sender, RoutedEventArgs e)
    {
    }

}
