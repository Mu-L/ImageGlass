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
using Avalonia.Interactivity;
using ImageGlass.Common;
using System;

namespace ImageGlass.UI;

public partial class PhControl : ContentControl
{
    protected override Type StyleKeyOverride => typeof(ContentControl);

    public double Dpi => VisualRoot?.RenderScaling ?? 1d;


    #region Control Events

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        OnIgLanguageChanged();
        Core.ThemeChanged += Core_ThemeChanged;
        Core.LanguageChanged += Core_LanguageChanged;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Core.ThemeChanged -= Core_ThemeChanged;
        Core.LanguageChanged -= Core_LanguageChanged;
    }


    private void Core_ThemeChanged(object? sender, ThemePackChangedEventArgs e)
    {
        OnIgThemeChanged(e);
    }


    private void Core_LanguageChanged(object? sender, EventArgs e)
    {
        OnIgLanguageChanged();
    }

    #endregion // Control Events


    #region Virtual Methods

    /// <summary>
    /// Occurs when the app theme is changed.
    /// </summary>
    protected virtual void OnIgThemeChanged(ThemePackChangedEventArgs e) { }


    /// <summary>
    /// Occurs when the app language is changed.
    /// </summary>
    protected virtual void OnIgLanguageChanged() { }

    #endregion // Virtual Methods


    #region Public Methods

    /// <summary>
    /// Scales the given number on the DPI scaling factor.
    /// </summary>
    public double DpiScale(double value) => Dpi * value;


    /// <summary>
    /// Scales the given size based on the DPI scaling factor.
    /// </summary>
    public Size DpiScale(Size value) => new Size(DpiScale(value.Width), DpiScale(value.Height));


    /// <summary>
    /// Scales the given point on the DPI scaling factor.
    /// </summary>
    public Point DpiScale(Point value) => new Point(DpiScale(value.X), DpiScale(value.Y));

    #endregion // Public Methods



}
