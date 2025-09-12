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
using System.Text.Json.Serialization;
using Windows.UI;

namespace ImageGlass.Win64.Common;

public partial class Config
{

    // Public Reactive properties
    #region Public Reactive properties

    /// <summary>
    /// Gets, sets the current color mode of OS.
    /// </summary>
    [JsonIgnore]
    public bool IsSystemDarkMode
    {
        get => field;
        set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;

                if (OnPropertyChanged(value, oldValue))
                {
                    // load theme
                    LoadCurrentTheme(field, AccentColor, true, true, false);
                }
            }
        }
    } = true;


    /// <summary>
    /// Gets the system accent color.
    /// </summary>
    [JsonIgnore]
    public Color AccentColor
    {
        get => field;
        set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;

                if (OnPropertyChanged(value, oldValue))
                {
                    Theme.LoadColors(AccentColor);
                    AP.RaiseThemeChangedEvent(nameof(IgTheme.ComputedColors));
                }
            }
        }
    }


    /// <summary>
    /// Gets, sets the current app theme pack.
    /// </summary>
    [JsonIgnore]
    public IgTheme Theme
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;

                if (OnPropertyChanged(value, oldValue))
                {
                    AP.RaiseThemeChangedEvent();
                }
            }
        }
    } = new();


    #endregion // Public Reactive properties


}
