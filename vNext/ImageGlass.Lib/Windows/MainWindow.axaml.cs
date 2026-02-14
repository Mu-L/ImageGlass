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
using ImageGlass.Common.Types;
using ImageGlass.UI;
using ImageGlass.UI.Windowing;
using ImageGlass.ViewModels;
using System;
using System.Linq;

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
        PART_MainView.PART_Gallery.ItemClicked += PART_Gallery_ItemClicked;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // control events
        _status.Changed -= Status_Changed;
        _status.Dispose();

        PART_MainView.PART_Toolbar.ItemClicked -= PART_Toolbar_ItemClicked;
        PART_MainView.PART_Gallery.ItemClicked -= PART_Gallery_ItemClicked;
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
    }


    protected override async void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Handled) return;

        // process app hotkeys
        if (Core.API is not null)
        {
            await Core.API.HandleKeyUpAsync(e);
            if (e.Handled) return;
        }
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
            var modulePath = Core.Args.ElementAtOrDefault(0) ?? string.Empty;
            Core.Args = [modulePath, .. e.Arguments];
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


    private void PART_Gallery_ItemClicked(GalleryItem sender, GalleryItemClickEventArgs e)
    {
        var photoIndex = Core.Photos.IndexOf(sender.VM.FilePath);
        Core.API?.IG_ViewByIndex(photoIndex);
    }


    #endregion Control Events




}