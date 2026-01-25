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
using Avalonia.Media.Imaging;
using ImageGlass.Common;
using ImageGlass.Common.Types;
using ImageGlass.UI.Windowing;
using ImageGlass.Win32.Common.Types;
using ImageGlass.Win32.UI;
using ImageGlass.Win32.WindowModels;

namespace ImageGlass.Win32.Windows;


public partial class MainWindow : Win32Window
{
    public MainWindowModel VM => (MainWindowModel)DataContext!;


    public MainWindow()
    {
        InitializeComponent();
        CloseWindowHotkeys = [new(Key.Escape)];

        Width = 600;
        Height = 400;

        Core.AppInstance.InstanceInvoked += AppInstance_InstanceInvoked;
    }


    private void AppInstance_InstanceInvoked(AppInstance sender, InstanceInvokedEventArgs e)
    {
        // handle single instance command
        if (e.Command.Equals(IgExeParams.SINGLE_INSTANCE))
        {
            if (WindowState == Avalonia.Controls.WindowState.Minimized)
            {
                WindowState = Avalonia.Controls.WindowState.Normal;
            }

            Activate();
            Topmost = true;
            Topmost = Core.Config.EnableWindowTopMost;
        }

        VM.Title = e.Command + "\r\n" + string.Join("\r\n", e.Arguments);
    }


    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Core.ColorProfileService = new Win32ColorProfileProvider();
        Core.ColorProfileService.Changed += ColorProfileService_Changed;
        await Core.ColorProfileService.InitializeAsync(this);
    }


    private void ColorProfileService_Changed(IWindowColorProfileProvider sender, ColorProfileChangedEventArgs e)
    {
        VM.Title = $"{e.IsHdr} | {e.ProfilePath}";
    }


    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;


        var res = await ModalWindow.ShowWarningAsync(this, new ModalWindowOptions
        {
            Title = "Hello World!",
            Heading = "This program is distributed in the hope that it will be useful",
            Description = "This program is free software",
            Details = "you can redistribute it and/or modify\r\n\r\nit under the terms of the GNU General Public License as published by\r\n\r\nthe Free Software Foundation, either version 3 of the License, or\r\n\r\n(at your option) any later version.\r\n\r\nImageGlass Project - Image viewer for Windows\r\n\r\nCopyright (C) 2010 - 2026 DUONG DIEU PHAP\r\nProject homepage: https://imageglass.org",
            //Note = "You should have received a copy",
            //InputValue = "999",
            //IsInputVisible = true,
            IsRememberOptionVisible = true,
            Thumbnail = new Bitmap(@"C:\Users\d2pha\Desktop\pic.jpg"),
            //ThumbnailIcon = Lib.Common.StockIconId.RecycleBin,
            //NoteStyle = InfoBarSeverity.Warning,
        }, ModalWindowButton.OK_Cancel);

        VM.Title = $"""
            ExitCode = {res.ExitCode}
            IsRememberOptionChecked = {res.IsRememberOptionChecked}
            InputValue = {res.InputValue}
            """;
    }




}

