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
using Avalonia.Media;
using ImageGlass.Common;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Types;
using ImageGlass.UI;
using ImageGlass.UI.Windowing;

namespace ImageGlass.ViewModels;

public partial class MainViewModel : IgReactive
{
    public static Config Config => Core.Config;
    public static IBrush ViewerBackground => Core.Theme.ComputedColors.BgColor.ToBrush();


    /// <summary>
    /// Gets view model of toolbar control.
    /// </summary>
    public static ToolbarControlModel ToolbarVM => new();


    /// <summary>
    /// Gets the owner window.
    /// </summary>
    public virtual IgWindow Window { get; }


    /// <summary>
    /// Gets, sets the window title.
    /// </summary>
    public string Title
    {
        get; set
        {
            if (field.Equals(value)) return;

            field = value;
            OnPropertyChanged();
        }
    } = "ImageGlass";


    public MainViewModel(IgWindow window)
    {
        Window = window;
    }

}
