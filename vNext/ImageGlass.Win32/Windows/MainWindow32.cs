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
using Avalonia.Controls;
using Avalonia.Interactivity;
using ImageGlass.Common;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using ImageGlass.Common.Windows;
using ImageGlass.Win32.Common;
using ImageGlass.Win32.Common.ServiceProviders;
using System.Threading.Tasks;

namespace ImageGlass.Win32.Windows;

public partial class MainWindow32 : MainWindow
{

    public override bool UseCustomBackdrop => true; // use Win32 API for the backdrop


    public MainWindow32()
    {

    }



    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // initialize Windows color profile service
        Core.ColorProfileProvider = Win32ColorProfileProvider.Create(this, ColorProfileProvider_Changed);
    }


    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        await SaveConfigOnClosingAsync();
    }


    protected override void OnIgBackdropStyleChanged(BackdropStyle style)
    {
        base.OnIgBackdropStyleChanged(style);

        var type = style switch
        {
            BackdropStyle.Mica => SystemBackdropType.Mica,
            BackdropStyle.MicaAlt => SystemBackdropType.MicaAlt,
            BackdropStyle.Acrylic => SystemBackdropType.Acrylic,
            BackdropStyle.None => SystemBackdropType.None,
            _ => SystemBackdropType.Auto,
        };

        // use Win32 API for the backdrop
        WindowApi.SetWindowBackdrop(Handle, type);
    }









    private void ColorProfileProvider_Changed(IWindowColorProfileProvider sender, ColorProfileChangedEventArgs e)
    {
        VM.Title = $"{e.IsHdr} | {e.ProfilePath}";
    }


    private async Task SaveConfigOnClosingAsync()
    {
        // save window maximized state
        Core.Config.IsMainWindowMaximized = WindowState == Avalonia.Controls.WindowState.Maximized;
        Core.Config.EnableFullScreen = WindowState == WindowState.FullScreen;

        // save window bounds
        if (WindowState == Avalonia.Controls.WindowState.Normal)
        {
            Core.Config.MainWindowBounds = new Avalonia.Rect(Position.X, Position.Y, Width, Height);
        }


        // fullscreen mode: use the backup value
        if (Core.Config.EnableFullScreen)
        {
            //Core.Config.ShowToolbar = _showToolbar;
            //Core.Config.ShowGallery = _showGallery;
        }


        Core.Config.LastSeenImagePath = CoreWin32.Photos.CurrentFilePath;
        //Core.Config.ZoomLockValue = Viewer.ZoomFactor * 100f;


        // save config to file
        await Core.Config.SaveAsync();


        // dispose the global singleton
        Core.Dispose();
        CoreWin32.Dispose();


        //// cleaning
        //try
        //{
        //    // delete trash
        //    Directory.Delete(Config.ConfigDir(PathType.Dir, Dir.Temporary), true);
        //}
        //catch { }
    }


}
