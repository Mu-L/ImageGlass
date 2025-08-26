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
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.UI;

namespace ImageGlass.Win64.Common;

public partial class AppSettings
{
    #region Public static properties

    /// <summary>
    /// App setting specs version, to check for compatibility.
    /// </summary>
    [JsonIgnore]
    public static float SPEC_VERSION => 10f;


    /// <summary>
    /// Gets the user config file name.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_USER => "igconfig.json";


    /// <summary>
    /// Gets the default config file located.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_DEFAULT => "igconfig.default.json";


    /// <summary>
    /// Gets the admin config file name.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_ADMIN => "igconfig.admin.json";


    /// <summary>
    /// Gets user settings from command line arguments
    /// </summary>
    [JsonIgnore]
    public static string[] SettingsFromCmdLine => Environment.GetCommandLineArgs()
        // filter the command lines begin with '/'
        // example: ImageGlass.exe /FrmMainWidth=900
        .Where(cmd => cmd.StartsWith(Const.CONFIG_CMD_PREFIX))
        .Select(cmd => cmd[1..]) // trim '/' from the command
        .ToArray();


    #endregion // Public static properties



    // Public static methods
    #region Public static methods

    /// <summary>
    /// Loads and parses configs from file.
    /// </summary>
    /// <exception cref="FileLoadException"></exception>
    public static AppSettings Load(string configFileName)
    {
        // 1. get user config file path
        var configPath = BHelper.ConfigDir(configFileName);
        if (!File.Exists(configPath)) return new AppSettings();


        // 2. load user settings
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new AppSettingsJsonContext(jsonOptions);

        var config = BHelper.ReadJsonFromFile(configPath, jsonContext.AppSettings);
        if (config == null)
            throw new FileLoadException($"Cannot parse settings from file: {configPath}");


        // 3. migrate user config file if config version is changed
        config = MigrateUserConfigFile(config);

        return config;
    }


    /// <summary>
    /// Migrates user config file.
    /// </summary>
    private static AppSettings MigrateUserConfigFile(AppSettings config)
    {
        var configVersion = config._Metadata.Version;

        // update config version
        config._Metadata.Version = SPEC_VERSION;

        // no change
        if (SPEC_VERSION <= configVersion) return config;


        // Migration v9 to v10
        if (configVersion < 10)
        {
            // ShowCheckerboard + ShowCheckerboardOnlyImageRegion: merged into CheckerboardMode
            // ZoomLevels: change type: number[] to string
        }

        return config;
    }


    #endregion // Public static methods



    // Public methods
    #region Public methods

    /// <summary>
    /// Writes configs to file.
    /// </summary>
    public async Task SaveAsync()
    {
        var jsonFilePath = BHelper.ConfigDir(CONFIG_USER);
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new AppSettingsJsonContext(jsonOptions);

        await BHelper.WriteJsonToFileAsync(jsonFilePath, this, jsonContext.AppSettings);
    }


    /// <summary>
    /// Sets default value of app settings
    /// </summary>
    public void LoadDefaults()
    {
        // set default value for file formats
        if (FileFormats.Count == 0)
        {
            FileFormats = Const.IMAGE_FORMATS
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet();
        }
    }


    /// <summary>
    /// Loads theme pack <see cref="AP.Theme"/>.
    /// </summary>
    /// <param name="darkMode">
    /// Determine which theme should be loaded: <see cref="DarkTheme"/> or <see cref="LightTheme"/>.
    /// </param>
    /// <param name="useFallBackTheme">
    /// If theme pack is invalid, should load the default theme pack <see cref="Const.DEFAULT_THEME"/>?
    /// </param>
    /// <param name="throwIfThemeInvalid">
    /// If theme pack is invalid, should throw exception?
    /// </param>
    /// <param name="forceUpdateBackground">Force updating background according to theme value</param>
    /// <exception cref="ArgumentException"></exception>
    public void LoadCurrentTheme(bool darkMode, Color? accent, bool useFallBackTheme, bool throwIfThemeInvalid, bool forceUpdateBackground)
    {
        // 1. save instance settings
        _isSystemDarkMode = darkMode;
        if (accent != null) _accentColor = accent.Value;

        // 2. get the theme folder name
        var themeFolderName = darkMode ? DarkTheme : LightTheme;
        if (string.IsNullOrEmpty(themeFolderName))
        {
            themeFolderName = Const.DEFAULT_THEME;
        }

        // 3. check if theme pack is already loaded
        if (themeFolderName.Equals(Theme.FolderName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 4. load theme pack
        var th = FindAndLoadThemePack(themeFolderName, accent, useFallBackTheme, throwIfThemeInvalid);

        // 5. update the name of dark/light theme
        if (darkMode) DarkTheme = th.FolderName;
        else LightTheme = th.FolderName;


        // 6. load background color
        if (BackgroundColor == Theme.Colors.BgColor || forceUpdateBackground)
        {
            BackgroundColor = th.Colors.BgColor;
        }

        // 7. set to the current theme
        Theme = th;
    }


    /// <summary>
    /// Finds the correct location of theme name and loads it.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private static IgTheme FindAndLoadThemePack(string themeFolderName, Color? accent, bool useFallBackTheme, bool throwIfThemeInvalid)
    {
        // 1. look for theme pack in the Config dir
        var themeConfigPath = BHelper.ConfigDir(Dir.Themes, themeFolderName);
        var th = new IgTheme().Load(themeConfigPath, accent);

        if (!th.IsValid)
        {
            // 2. look for theme pack in the base dir
            var baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, themeFolderName);
            th = new IgTheme().Load(baseThemeConfigPath, accent);

            // 3. cannot find theme, use fall back theme
            if (!th.IsValid && useFallBackTheme)
            {
                // 4. load default theme
                baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, Const.DEFAULT_THEME);
                th = new IgTheme().Load(baseThemeConfigPath, accent);
            }
        }

        // 5. throw error if theme is invalid
        if (!th.IsValid && throwIfThemeInvalid)
        {
            throw new ArgumentException($"Unable to load '{themeFolderName}' theme pack. " +
                $"Please make sure '{themeConfigPath}' file is valid.", nameof(themeFolderName));
        }

        return th;
    }


    #endregion // Public methods




}
