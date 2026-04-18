/*
ImageGlass - A lightweight, versatile image viewer
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
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using SkiaSharp;
using Svg.Skia;

namespace ImageGlass.UI.Viewer;


public partial class ViewerControl
{
    /// <summary>
    /// The current SVG picture for vector rendering.
    /// Owned by <see cref="_svgDocument"/> - do NOT dispose separately.
    /// </summary>
    internal SKPicture? _svgPicture;

    /// <summary>
    /// The SVG document that owns <see cref="_svgPicture"/>.
    /// </summary>
    internal SKSvg? _svgDocument;


    /// <summary>
    /// Disposes vector-specific resources.
    /// Must be called inside <see cref="_lock"/>.
    /// </summary>
    private void DisposeVectorResources()
    {
        _svgPicture = null;
        _svgDocument?.Dispose();
        _svgDocument = null;
    }


    /// <summary>
    /// Handles loading a vector photo from decoded SVG output.
    /// Must be called inside <see cref="_lock"/>.
    /// </summary>
    /// <returns><c>true</c> if the vector photo was handled successfully.</returns>
    private bool HandleVectorPhotoLoaded(SkiaDecoderOutput vectorOutput)
    {
        if (vectorOutput.VectorPicture is null) return false;

        // take ownership of the SVG document and picture
        _svgDocument = vectorOutput.SvgDocument;
        _svgPicture = vectorOutput.VectorPicture;

        // null out references in the output so Photo.UnloadBitmap() won't dispose them
        vectorOutput.SvgDocument = null;
        vectorOutput.VectorPicture = null;

        SourceKind = PhotoSource.VectorRenderer;

        // compute bitmap size from CullRect
        var cullRect = _svgPicture.CullRect;
        BitmapSize = new Size(
            System.Math.Max(1, cullRect.Width),
            System.Math.Max(1, cullRect.Height));

        // set rasterized fallback as _imgSource (for pixel operations: copy, export)
        var rasterized = SvgCodec.RasterizeThumbnail(_svgPicture,
            (int)System.Math.Min(System.Math.Max(BitmapSize.Width, BitmapSize.Height), 4096));
        SKImageRef.Set(ref _imgSource, rasterized);

        // mark for first draw
        _isFirstDraw.SetTrue();

        return true;
    }


    /// <summary>
    /// Checks if the current source is a vector renderer.
    /// </summary>
    private bool IsVectorSource() => SourceKind == PhotoSource.VectorRenderer;


    /// <summary>
    /// Computes the transform matrix that maps SVG coordinates to screen coordinates.
    /// </summary>
    internal static SKMatrix ComputeVectorTransform(SKRect srcRect, SKRect destRect)
    {
        var scaleX = destRect.Width / srcRect.Width;
        var scaleY = destRect.Height / srcRect.Height;
        var tx = destRect.Left - srcRect.Left * scaleX;
        var ty = destRect.Top - srcRect.Top * scaleY;

        return SKMatrix.CreateScaleTranslation(scaleX, scaleY, tx, ty);
    }
}
