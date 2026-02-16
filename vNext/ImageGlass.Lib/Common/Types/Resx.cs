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
using Avalonia.Markup.Xaml.MarkupExtensions;
using System;

namespace ImageGlass.Common.Types;


public static class Resx
{

    /// <summary>
    /// Gets resource.
    /// </summary>
    public static T Get<T>(ResxId resxId)
    {
        var resName = GetResxName(resxId);
        var value = Application.Current?.Resources[resName]!;

        return (T)value;
    }


    /// <summary>
    /// Sets resource.
    /// </summary>
    public static void Set(ResxId resxId, object resValue)
    {
        var resName = GetResxName(resxId);
        Application.Current?.Resources[resName] = resValue;
    }


    /// <summary>
    /// Gets the resource name from resource id.
    /// </summary>
    public static string GetResxName(ResxId resxId)
    {
        return Enum.GetName(resxId) ?? string.Empty;
    }


    /// <summary>
    /// Creates a binding to the input resource name.
    /// </summary>
    public static DynamicResourceExtension CreateBinding(ResxId resxId)
    {
        var resName = GetResxName(resxId);
        return new DynamicResourceExtension(resName);
    }

}


public enum ResxId
{
    // accent colors
    SystemAccentColor,
    SystemAccentColorLight1,
    SystemAccentColorLight2,
    SystemAccentColorLight3,
    SystemAccentColorDark1,
    SystemAccentColorDark2,
    SystemAccentColorDark3,


    // control styles
    ControlCornerRadius,
    ContentControlThemeFontFamily,
    TextControlBackground,


    // text color
    SystemControlForegroundBaseHighBrush,
    TextControlForeground,
    CheckBoxForegroundChecked,
    CheckBoxForegroundCheckedPointerOver,
    CheckBoxForegroundUnchecked,
    CheckBoxForegroundUncheckedPointerOver,


    // border color
    TextControlBorderBrush,
    TextControlBorderBrushPointerOver,
    CheckBoxCheckBackgroundStrokeUnchecked,
    CheckBoxCheckBackgroundStrokeUncheckedPointerOver,


    // menu
    MenuFlyoutPresenterBackground,
    MenuFlyoutPresenterBorderBrush,

    MenuFlyoutSeparatorThemePadding,
    IG_MenuSeparatorBackground,
    MenuFlyoutItemBackgroundPointerOver,
    MenuFlyoutItemBackgroundPressed,

    // menu text
    MenuFlyoutItemForeground,
    MenuFlyoutItemForegroundPointerOver,
    MenuFlyoutItemForegroundPressed,
    MenuFlyoutItemForegroundDisabled,

    // menu hotkey text
    MenuFlyoutItemKeyboardAcceleratorTextForeground,
    MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver,
    MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed,
    MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled,

    // menu chevron
    MenuFlyoutSubItemChevron,
    MenuFlyoutSubItemChevronPointerOver,
    MenuFlyoutSubItemChevronPressed,
    MenuFlyoutSubItemChevronDisabled,
    MenuFlyoutSubItemChevronSubMenuOpened,

    // tooltip
    ToolTipBackground,
    ToolTipForeground,
    ToolTipBorder,


    // situational backgrounds
    IG_BackgroundInfoBrush,
    IG_BackgroundSuccessBrush,
    IG_BackgroundWarningBrush,
    IG_BackgroundDangerBrush,
    IG_BackgroundNeutralBrush,
    IG_BorderNeutralBrush,
    IG_BorderControlBrush,
    IG_MessageBackgroundBrush,

    // tool button styles
    IG_ToolButtonBackground,
    IG_ToolButtonBackgroundHover,
    IG_ToolButtonBackgroundPressed,
    IG_ToolButtonBackgroundChecked,


}
