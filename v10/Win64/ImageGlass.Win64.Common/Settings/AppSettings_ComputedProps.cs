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

public partial class AppSettings
{
    private bool _isDarkMode = true;
    private Color _accentColor = new();
    private IgTheme _theme = new();



    // Public Reactive properties
    #region Public Reactive properties

    /// <summary>
    /// Gets, sets the current color mode.
    /// </summary>
    [JsonIgnore]
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged();

                // load theme
                LoadCurrentTheme(_isDarkMode, AccentColor, true, true, false);
            }
        }
    }


    /// <summary>
    /// Gets the system accent color.
    /// </summary>
    [JsonIgnore]
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor != value)
            {
                _accentColor = value;
                OnPropertyChanged();

                Theme.LoadColors(AccentColor);
            }
        }
    }


    /// <summary>
    /// Gets, sets the current app theme pack.
    /// </summary>
    [JsonIgnore]
    public IgTheme Theme
    {
        get => _theme;
        set
        {
            if (_theme != value)
            {
                _theme = value;

                OnPropertyChanged();
                AP.TriggerThemeChangedEvent();
            }
        }
    }


    #endregion // Public Reactive properties


}
