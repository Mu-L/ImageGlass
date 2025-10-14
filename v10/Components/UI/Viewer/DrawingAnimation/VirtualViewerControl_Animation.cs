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
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;

namespace ImageGlass.UI;


public partial class VirtualViewerControl
{
    /// <summary>
    /// Gets the current animating source.
    /// </summary>
    public AnimationSources AnimationSource { get; protected set; } = AnimationSources.None;


    /// <summary>
    /// Starts a built-in animation.
    /// </summary>
    /// <param name="sources">Source of animation</param>
    public void StartDrawingAnimation(AnimationSources sources)
    {
        //if (UseWebview2)
        //{
        //    StartWeb2Animation(sources);
        //}

        AnimationSource = sources;
        EnableDrawingAnimation = true;
    }


    /// <summary>
    /// Stops a built-in animation.
    /// </summary>
    /// <param name="sources">Source of animation</param>
    public void StopDrawingAnimation(AnimationSources sources)
    {
        //if (UseWebview2)
        //{
        //    StopWeb2Animations();
        //}

        AnimationSource ^= sources;
        EnableDrawingAnimation = false;
        Invalidate();
    }

}


[Flags]
public enum AnimationSources
{
    None = 0,

    PanLeft = 1 << 1,
    PanRight = 1 << 2,
    PanUp = 1 << 3,
    PanDown = 1 << 4,

    /// <summary>
    /// Zoom in animation. It does nothing if <see cref="ViewerCanvas.ZoomLevels"/> is set.
    /// </summary>
    ZoomIn = 1 << 5,
    /// <summary>
    /// Zoom out animation. It does nothing if <see cref="ViewerCanvas.ZoomLevels"/> is set.
    /// </summary>
    ZoomOut = 1 << 6,
}