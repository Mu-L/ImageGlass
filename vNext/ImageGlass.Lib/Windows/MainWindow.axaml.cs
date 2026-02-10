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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ImageGlass.Common.Types;
using ImageGlass.UI;
using ImageGlass.UI.Windowing;
using ImageGlass.ViewModels;
using System;

namespace ImageGlass.Common.Windows;


public partial class MainWindow : PhWindow
{
    private readonly AppStatusInfo _status;

    public MainWindowModel VM => (MainWindowModel)DataContext!;


    public MainWindow()
    {
        InitializeComponent();
        CloseWindowHotkeys = [new(Key.Escape)];

        _status = new AppStatusInfo(PART_MainView.PART_Viewer);


        // load window bounds & state
        Position = new PixelPoint((int)Core.Config.MainWindowBounds.X, (int)Core.Config.MainWindowBounds.Y);
        Width = Core.Config.MainWindowBounds.Width;
        Height = Core.Config.MainWindowBounds.Height;

        if (Core.Config.IsMainWindowMaximized) WindowState = WindowState.Maximized;
        if (Core.Config.EnableFullScreen) WindowState = WindowState.FullScreen;

        // load color profile
        Core.UpdateDestColorProfile();

        // events
        Core.AppInstance.InstanceInvoked += AppInstance_InstanceInvoked;
    }



    #region Override Methods

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        // check if the config loading was failed
        if (Config.LoadingException is not null)
        {
            var isContinue = await ModalWindow.ShowUnhandledErrorAsync(
                Config.LoadingException, this,
                "IGE: There was an error while loading user settings");
            if (!isContinue) return;
        }

        base.OnLoaded(e);


        // register app hotkeys
        Core.API?.RegisterHotkeys();

        // control events
        _status.Changed += Status_Changed;
        PART_MainView.PART_Toolbar.ItemClicked += PART_Toolbar_ItemClicked;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // control events
        _status.Changed -= Status_Changed;
        _status.Dispose();

        PART_MainView.PART_Toolbar.ItemClicked -= PART_Toolbar_ItemClicked;
    }


    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;

        // process app hotkeys
        if (Core.API is not null)
        {
            await Core.API.HandleKeyDownAsync(e);
            if (e.Handled) return;
        }




        // enter full screen
        if (e.Key == Key.F11)
        {
            Core.Config.EnableFullScreen = !Core.Config.EnableFullScreen;

            if (Core.Config.EnableFullScreen)
            {
                Core.Config.IsMainWindowMaximized = WindowState == WindowState.Maximized;
                Core.Config.MainWindowBounds = new Avalonia.Rect(Position.X, Position.Y, Width, Height);
                WindowState = WindowState.FullScreen;
            }
            else
            {
                if (Core.Config.IsMainWindowMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    WindowState = WindowState.Normal;
                    Width = Core.Config.MainWindowBounds.Width;
                    Height = Core.Config.MainWindowBounds.Height;
                }
            }

            return;
        }



        if (e.Key != Key.Enter) return;
        var res = await ModalWindow.ShowInputAsync(this, new ModalWindowOptions
        {
            Title = "Hello World!",
            Heading = "This program is distributed in the hope that it will be useful",
            Description = "This program is free software",
            Details = "you can redistribute it and/or modify\r\n\r\nit under the terms of the GNU General Public License as published by\r\n\r\nthe Free Software Foundation, either version 3 of the License, or\r\n\r\n(at your option) any later version.\r\n\r\nImageGlass Project - Image viewer for Windows\r\n\r\nCopyright (C) 2010 - 2026 DUONG DIEU PHAP\r\nProject homepage: https://imageglass.org",
            Note = "You should have received a copy",
            InputValue = "99x9",
            IsInputVisible = true,
            AcceptValue = ImageGlass.UI.TextBoxAcceptValue.IntValueOnly,
            IsRememberOptionVisible = true,
            Thumbnail = new Bitmap(@"C:\Users\d2pha\Desktop\pic.jpg"),
            ThumbnailIcon = StockIconId.Delete,
            NoteStyle = InfoBarSeverity.Warning,
        }, ModalWindowButton.OK_Cancel);

        VM.Title = $"""
            ExitCode = {res.ExitCode}
            IsRememberOptionChecked = {res.IsRememberOptionChecked}
            InputValue = {res.InputValue}
            """;
    }


    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Handled) return;


        Core.API?.HandleKeyUp(e);
        if (e.Handled) return;
    }


    #endregion // Override Methods



    #region Control Events

    private void AppInstance_InstanceInvoked(AppInstance sender, InstanceInvokedEventArgs e)
    {
        // handle single instance command
        if (e.Command.Equals(ExeParams.SINGLE_INSTANCE))
        {
            if (WindowState == Avalonia.Controls.WindowState.Minimized)
            {
                WindowState = Avalonia.Controls.WindowState.Normal;
            }

            // set instance arguments
            Core.Args = e.Arguments;
            Core.UpdateInitImagePath();

            // load image path
            Core.API?.IG_OpenPath(Core.InputImagePathFromArgs);

            Activate();
            Topmost = true;
            Topmost = Core.Config.EnableWindowTopMost;
        }
    }


    private void Status_Changed(object? sender, EventArgs e)
    {
        VM.Title = _status.Text;
    }


    private void PART_Toolbar_ItemClicked(object sender, ToolbarItemClickEventArgs e)
    {
        _ = Core.API?.RunActionAsync(e.VM.OnClick);
    }


    #endregion Control Events










}