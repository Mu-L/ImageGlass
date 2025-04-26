/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
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
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using ImageGlass.Common.Photoing;
using Vortice.Direct2D1;
using Vortice.WIC;

namespace ImageGlass.WinNT.Common.Photoing;


/// <summary>
/// Provides functionality to animate WEBP images by decoding frames and rendering them sequentially.
/// </summary>
public partial class WebpAnimator : WicAnimator
{

    /// <summary>
    /// Initializes a new instance of the <see cref="WebpAnimator"/> class.
    /// </summary>
    public WebpAnimator(IWICBitmapDecoder decoder, PhotoMetadata meta) : base(decoder, meta)
    {

    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void ApplyFrameDisposal(ID2D1BitmapRenderTarget surface)
    {
        // always clear previous frame
        surface.BeginDraw();
        surface.Clear(Vortice.Mathematics.Colors.Transparent);
        surface.EndDraw();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void RenderFrame(ID2D1Bitmap1 frameBmp, ID2D1BitmapRenderTarget surface, ID2D1DeviceContext dc)
    {
        // get the frame bounds
        var currRect = new Vortice.Mathematics.Rect(frameBmp.Size.ToVector2());

        // draw current frame to composite surface
        surface.BeginDraw();
        surface.DrawBitmap(frameBmp);
        surface.EndDraw();
    }

}
