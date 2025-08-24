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
using System.IO;
using System.Text.Json.Serialization;
using Windows.UI;

namespace ImageGlass.Win64.Common;


[JsonSerializable(typeof(IgTheme))]
public partial class IgThemeJsonContext : JsonSerializerContext { }


/// <summary>
/// Represents a theme pack for the app.
/// </summary>
public partial class IgTheme : Notify
{
    private IgThemeColorBrushes _colorBrushes = new();


    // Serializable properties
    public IgThemeMetadata _Metadata { get; set; } = new();
    public IgThemeInfo Info { get; set; } = new();
    public IgThemeSettings Settings { get; set; } = new();
    public IgThemeColors Colors { get; set; } = new();
    public IgThemeToolbarIcons ToolbarIcons { get; set; } = new();


    // Static Properties
    #region Static Properties

    /// <summary>
    /// Theme specs version, to check for compatibility.
    /// </summary>
    [JsonIgnore]
    public static float SPEC_VERSION => 9;

    /// <summary>
    /// Filename of theme configuration since v9.0.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_FILE => "igtheme.json";

    #endregion // Static Properties


    // Instance Properties
    #region Instance Properties

    /// <summary>
    /// Gets the full path of theme folder.
    /// </summary>
    [JsonIgnore]
    public string FolderPath { get; set; } = "";

    /// <summary>
    /// Gets the name of theme folder.
    /// </summary>
    [JsonIgnore]
    public string FolderName => Path.GetFileName(FolderPath);

    /// <summary>
    /// Gets the full path of theme config file (<see cref="IgTheme.CONFIG_FILE"/>).
    /// </summary>
    [JsonIgnore]
    public string ConfigFilePath => Path.Combine(FolderPath, IgTheme.CONFIG_FILE);

    /// <summary>
    /// Indicates if this theme is valid.
    /// </summary>
    [JsonIgnore]
    public bool IsValid { get; set; } = false;

    /// <summary>
    /// Gets the theme color brushes.
    /// </summary>
    [JsonIgnore]
    public IgThemeColorBrushes ColorBrushes => _colorBrushes;

    #endregion // Instance Properties


    // Public methods
    #region Public methods

    /// <summary>
    /// Reads theme config file and loads the theme properties.
    /// </summary>
    /// <returns>The current instance of this theme pack.</returns>
    public IgTheme Load(string themeFolderPath, Color? accent = null)
    {
        FolderPath = themeFolderPath;

        // 1. parse theme config file to theme pack
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new IgThemeJsonContext(jsonOptions);

        try
        {
            // 2. parse theme config
            var th = BHelper.ReadJsonFromFile(ConfigFilePath, jsonContext.IgTheme);
            IsValid = th != null;

            // 3. load theme properties
            if (th != null)
            {
                _Metadata = th._Metadata;
                Info = th.Info;
                Settings = th.Settings;
                Colors = th.Colors;
                ToolbarIcons = th.ToolbarIcons;
            }
        }
        catch { }

        // load colors
        LoadColors(accent);

        return this;
    }


    /// <summary>
    /// Loads the theme colors.
    /// </summary>
    public void LoadColors(Color? accent = null)
    {
        ColorBrushes.Load(Colors, accent);
        OnPropertyChanged(nameof(ColorBrushes));
    }

    #endregion // Public methods

}

