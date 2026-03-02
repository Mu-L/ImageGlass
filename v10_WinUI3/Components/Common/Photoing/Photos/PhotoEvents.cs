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
using System;
using System.Threading;
using Windows.Graphics.Imaging;

namespace ImageGlass.Common.Photoing;


public enum PhotoLoadingState
{
    None,
    Loading,
    Loaded,
}


public class PhotoLoadingEventArgs(PhotoLoadingState state, Photo photo, CancellationToken token) : EventArgs
{
    /// <summary>
    /// Checks if the event is fired when photo is loaded.
    /// </summary>
    public PhotoLoadingState State => state;

    /// <summary>
    /// Gets the current photo instance.
    /// </summary>
    public Photo Photo => photo;

    /// <summary>
    /// Gets the current metadata instance.
    /// </summary>
    public PhotoMetadata Metadata => photo.Metadata;

    /// <summary>
    /// Gets the cancellation token of the current photo.
    /// </summary>
    public CancellationToken CancelToken => token;

}


public class ThumbnailLoadedEventArgs(Photo sender, SoftwareBitmap? bmp) : EventArgs
{
    public Photo Sender { get; set; } = sender;

    public SoftwareBitmap? Bitmap { get; set; } = bmp;
}


