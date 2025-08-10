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
using System.Threading.Tasks;

namespace ImageGlass.WinNT.Common;


public static partial class Config
{

    #region Public properties

    /// <summary>
    /// Gets, sets the instance of app settings.
    /// </summary>
    public static AppSettings Current { get; set; } = new();


    /// <summary>
    /// Current version of the app.
    /// </summary>
    public static float Version => 10f;


    /// <summary>
    /// Gets the user config file name.
    /// </summary>
    public static string UserFilename => "igconfig.json";


    /// <summary>
    /// Gets the default config file located.
    /// </summary>
    public static string DefaultFilename => "igconfig.default.json";


    /// <summary>
    /// Gets the admin config file name.
    /// </summary>
    public static string AdminFilename => "igconfig.admin.json";


    /// <summary>
    /// Config file description
    /// </summary>
    public static string Description => "ImageGlass Configuration File";


    /// <summary>
    /// Gets user settings from command line arguments
    /// </summary>
    public static string[] SettingsFromCmdLine => Environment.GetCommandLineArgs()
        // filter the command lines begin with '/'
        // example: ImageGlass.exe /FrmMainWidth=900
        .Where(cmd => cmd.StartsWith(Const.CONFIG_CMD_PREFIX))
        .Select(cmd => cmd[1..]) // trim '/' from the command
        .ToArray();


    public static string AppName => "ImageGlass_10";


    /// <summary>
    /// Gets the app's startup path.
    /// </summary>
    public static string StartupPath => AppDomain.CurrentDomain.BaseDirectory;


    /// <summary>
    /// Gets the app's configuration path.
    /// </summary>
    public static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    #endregion



    #region Public methods

    /// <summary>
    /// Loads and parses configs from file.
    /// </summary>
    public static void Load()
    {
        // 1. get user config file path
        var userFilePath = Path.Combine(ConfigPath, UserFilename);

        // 2. create json options
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new AppSettingsJsonContext(jsonOptions);


        // 3. load user settings
        var userSettings = ParseSettingFile(userFilePath);
        if (userSettings != null)
        {
            Config.Current = userSettings;
        }
        else
        {
            Config.Current.LoadDefaults();
        }


        // 4. migrate user config file if config version is changed
        MigrateUserConfigFile();
    }


    private static AppSettings? ParseSettingFile(string filePath)
    {
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new AppSettingsJsonContext(jsonOptions);

        try
        {
            var userSettings = BHelper.ReadJsonFromFile(filePath, jsonContext.AppSettings);

            if (userSettings == null)
                throw new FileLoadException($"Cannot parse settings from file: {filePath}");

            return userSettings;
        }
        catch { }

        return null;
    }


    /// <summary>
    /// Writes configs to file.
    /// </summary>
    public static async Task SaveAsync()
    {
        var jsonFilePath = Path.Combine(ConfigPath, UserFilename);
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new AppSettingsJsonContext(jsonOptions);

        await BHelper.WriteJsonToFileAsync(jsonFilePath, Config.Current, jsonContext.AppSettings);
    }


    /// <summary>
    /// Gets the path based on the startup folder of ImageGlass.
    /// </summary>
    public static string StartUpDir(params string[] paths)
    {
        var newPaths = paths.ToList();
        newPaths.Insert(0, StartupPath);

        return Path.Combine([.. newPaths]);
    }


    /// <summary>
    /// Returns the path based on the configuration folder of ImageGlass.
    /// For portable mode, ConfigDir = InstalledDir, else <c>%LocalAppData%\ImageGlass</c>
    /// </summary>
    /// <param name="type">Indicates if the given path is either file or directory</param>
    public static string ConfigDir(PathType type, params string[] paths)
    {
        // use StartUp dir if it's writable
        var startUpPath = StartUpDir(paths);

        if (BHelper.CheckPathWritable(type, startUpPath))
        {
            return startUpPath;
        }

        // else, use AppData dir
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

        // create the directory if not exists
        Directory.CreateDirectory(appDataDir);

        var newPaths = paths.ToList();
        newPaths.Insert(0, appDataDir);
        appDataDir = Path.Combine([.. newPaths]);

        return appDataDir;
    }

    #endregion



    // Config file migration
    #region Config file migration

    /// <summary>
    /// Migrate user config file.
    /// </summary>
    private static void MigrateUserConfigFile()
    {
        var configVersion = Current._Metadata.Version;

        // update config version
        Current._Metadata.Version = Version;

        // no change
        if (Version <= configVersion) return;


        // Migration v9 to v10
        if (configVersion < 10)
        {
            // ShowCheckerboard + ShowCheckerboardOnlyImageRegion: merged into CheckerboardMode
            // ZoomLevels: change type: number[] to string
        }
    }

    #endregion // Config file migration


}
