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
namespace ImageGlass.UI.Viewer;

public partial class ViewerControl
{

    /// <summary>
    /// Enables the renderer loop for drawing amination source.
    /// </summary>
    public bool EnableDrawingAnimation { get; set; } = false;


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
        InvalidateVisual();
    }


}
