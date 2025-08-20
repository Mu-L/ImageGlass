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

using ImageGlass.Common;
using ImageGlass.Win64.Common;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ImageGlass;

/// <summary>
/// Stores reactive data for <see cref="MainWindow"/>.
/// </summary>
public partial class MainWindowViewModel(MainWindow win) : DisposableImpl
{
    private MainWindow _win = win;
    private double _titleBarHeight = 0;
    private double _titleBarRightInset = 0;

    /// <summary>
    /// Gets DPI scale of <see cref="MainWindow"/>.
    /// </summary>
    public double DpiScale => _win.Content.XamlRoot?.RasterizationScale ?? 1;


    /// <summary>
    /// Gets, set the title of <see cref="MainWindow"/>.
    /// </summary>
    public string? Title
    {
        get => _win.Title;
        set
        {
            if (value != _win.Title)
            {
                _win.Title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }


    /// <summary>
    /// Gets, sets the title bar height of <see cref="MainWindow"/>.
    /// </summary>
    public double TitleBarHeight
    {
        get => _titleBarHeight / DpiScale;
        set
        {
            if (_titleBarHeight != value)
            {
                _titleBarHeight = value;
                OnPropertyChanged(nameof(TitleBarHeight));
            }
        }
    }


    /// <summary>
    /// Gets, sets the title bar's right inset width of <see cref="MainWindow"/>.
    /// </summary>
    public double TitleBarRightInset
    {
        get => _titleBarRightInset / DpiScale;
        set
        {
            if (_titleBarRightInset != value)
            {
                _titleBarRightInset = value;
                OnPropertyChanged(nameof(TitleBarRightInset));
                OnPropertyChanged(nameof(TitleBarPadding));
            }
        }
    }

    /// <summary>
    /// Gets the title bar padding of <see cref="MainWindow"/>.
    /// </summary>
    public Thickness TitleBarPadding => new Thickness(0, 0, TitleBarRightInset, 0);


    public static SystemBackdrop? WindowBackdrop
    {
        get
        {
            if (Config.Current.WindowBackdrop == BackdropStyle.None) return null;
            if (Config.Current.WindowBackdrop == BackdropStyle.Acrylic)
            {
                return new DesktopAcrylicBackdrop();
            }
            else
            {
                return new MicaBackdrop()
                {
                    Kind = Config.Current.WindowBackdrop == BackdropStyle.MicaAlt
                        ? MicaKind.BaseAlt
                        : MicaKind.Base
                };
            }
        }
    }

}
