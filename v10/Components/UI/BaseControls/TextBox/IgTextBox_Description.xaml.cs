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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace ImageGlass.UI;

public sealed partial class IgTextBox_Description : IgControl
{
    #region Control properties

    /// <summary>
    /// Gets, sets the message.
    /// </summary>
    public string? Message
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = null;


    /// <summary>
    /// Gets, sets the visibility of error icon.
    /// </summary>
    public bool IsErrorIconVisible
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = false;


    /// <summary>
    /// Gets, sets the text color of message.
    /// </summary>
    internal Brush? MessageTextBrush
    {
        get => field ?? Foreground;
        set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    }

    #endregion // Control properties


    public IgTextBox_Description()
    {
        InitializeComponent();
        UpdateMessageForeground();
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);
        UpdateMessageForeground();
    }


    /// <summary>
    /// Updates the foreground of message text.
    /// </summary>
    private void UpdateMessageForeground()
    {
        MessageTextBrush = AP.Config.Theme.ComputedColors.TextColor
            .Blend(AP.Config.Theme.BaseColor, 0.7f) // dim 30%
            .ToBrush();
    }


    /// <summary>
    /// Animates the error icon.
    /// </summary>
    public void AnimateErrorIcon()
    {
        Ani_Opacity.Duration = new Duration(TimeSpan.FromMilliseconds(150));
        Ani_Opacity.RepeatBehavior = new RepeatBehavior(2);
        PART_ErrorIconStoryboard.Begin();
    }

}
