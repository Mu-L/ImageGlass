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
using Avalonia.Interactivity;
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.UI;
using ImageGlass.ViewModels;
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
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
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










    private async Task ViewPhotoAsync(Photo? photo, bool useCache = true, bool scrollToThumbnail = true)
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