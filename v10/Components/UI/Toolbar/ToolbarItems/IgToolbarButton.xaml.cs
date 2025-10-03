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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using Windows.Foundation;

namespace ImageGlass.UI;

public partial class IgToolbarButton : IgToolbarItem
{
    public static string _PART_Button => "PART_Button";
    public static string _PART_ButtonIcon => "PART_ButtonIcon";
    public static string _PART_ButtonText => "PART_ButtonText";
    public static double InnerSpacing => AP.Config.ToolbarIconHeight / 6f; // 4
    public static Thickness ItemPadding => new(AP.Config.ToolbarIconHeight / 4.8f); // 5


    // events
    public event TypedEventHandler<IgToolbarButton, ToolbarItemClickedEventArgs>? Clicked;


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets or sets the flyout associated with this button.
    /// </summary>
    public FlyoutBase? Flyout
    {
        get; set
        {
            if (field != value)
            {
                field = value;

                FlyoutBase.SetAttachedFlyout(this, field);
                _ = OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets value indicates that the <see cref="Flyout"/> should be open on clicked.
    /// </summary>
    public bool OpenFlyoutOnClick
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    }

    #endregion // Public Properties


    public IgToolbarButton()
    {
        InitializeComponent();
    }


    protected override void OnIgLoaded(FrameworkElement fe)
    {
        base.OnIgLoaded(fe);
        UpdateIcon_();
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);
        UpdateIcon_();
    }


    private void PART_Button_Click(object sender, RoutedEventArgs e)
    {
        OnClicked(this, e);
    }


    /// <summary>
    /// Raises event <see cref="Clicked"/>.
    /// </summary>
    protected virtual void OnClicked(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not IgButton btn) return;

        var args = new ToolbarItemClickedEventArgs(VM, btn);
        Clicked?.Invoke(this, args);

        // open flyout menu
        if (OpenFlyoutOnClick && !args.CancelFlyoutMenu)
        {
            OpenFlyoutMenu(args.FlyoutMenuPlacement);
        }
    }


    /// <summary>
    /// Updates icon.
    /// </summary>
    private void UpdateIcon_()
    {
        if (string.IsNullOrWhiteSpace(VM.Image)) return;
        var svgPath = "";

        try
        {
            // absolute path
            if (File.Exists(VM.Image)) return;

            // get toolbar icon enum from theme
            if (!Enum.TryParse<IgThemeIcon>(VM.Image, out var themeIconNameEnum)) return;

            // get icon file name from theme
            var themeIconName = AP.Config.Theme.GetIconPath(themeIconNameEnum);
            if (string.IsNullOrWhiteSpace(themeIconName)) return;

            // theme icon path
            svgPath = Path.Combine(AP.Config.Theme.FolderPath, themeIconName);
            if (!File.Exists(svgPath)) return;

            // set icon
            PART_ButtonIcon.Source = new SvgImageSource(new Uri(svgPath));
        }
        catch { }
    }


    /// <summary>
    /// Open flyout menu.
    /// </summary>
    public void OpenFlyoutMenu(FlyoutPlacementMode? placement = null)
    {
        if (Flyout is null) return;

        // set the placement
        Flyout.Placement = placement ?? FlyoutPlacementMode.BottomEdgeAlignedRight;

        // open the flyout menu on click
        FlyoutBase.ShowAttachedFlyout(this);

    }

}
