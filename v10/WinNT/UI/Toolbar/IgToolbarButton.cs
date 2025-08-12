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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ImageGlass.WinNT.UI;

public partial class IgToolbarButton : AppBarButton
{
    /// <summary>
    /// Gets or sets the selection state of the button.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set
        {
            SetValue(IsSelectedProperty, value);

            // set selected styles
            if (value)
            {
                // background
                Resources["AppBarButtonBackground"] = Application.Current.Resources["IgButtonBackgroundSelected"];

                // border
                Resources["AppBarButtonBorderBrush"] = Application.Current.Resources["IgButtonBorderSelected"];
                Resources["AppBarButtonBorderBrushPointerOver"] = Application.Current.Resources["IgButtonBorderSelected"];
                Resources["AppBarButtonBorderBrushPressed"] = Application.Current.Resources["IgButtonBorderSelected"];
            }
            // default style
            else
            {
                // background
                Resources["AppBarButtonBackground"] = Application.Current.Resources["IgButtonBackground"];

                // border
                Resources["AppBarButtonBorderBrush"] = Application.Current.Resources["IgButtonBackground"];
                Resources["AppBarButtonBorderBrushPointerOver"] = Application.Current.Resources["IgButtonBackgroundHovered"];
                Resources["AppBarButtonBorderBrushPressed"] = Application.Current.Resources["IgButtonBackgroundPressed"];
            }
        }
    }
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(IgToolbarButton),
            new PropertyMetadata(default));


    public IgToolbarButton()
    {
        DefaultStyleKey = typeof(IgToolbarButton);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();


        // Border lement: remove background transition
        if (GetTemplateChild("AppBarButtonInnerBorder") is Border borderEl)
        {
            borderEl.BackgroundTransition = null;
            borderEl.BorderThickness = new Thickness(1);
        }


        // Overrides Background styles
        Resources["AppBarButtonBackgroundPointerOver"] = Application.Current.Resources["IgButtonBackgroundHovered"];
        Resources["AppBarButtonBackgroundPressed"] = Application.Current.Resources["IgButtonBackgroundPressed"];
    }
}
