
using ImageGlass.Common;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.WinNT.Common;


public static partial class Config
{
    private static IConfigurationRoot? _root;


    #region Public properties

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
    /// Config file version
    /// </summary>
    public static float Version => 10f;


    /// <summary>
    /// Gets user settings from command line arguments
    /// </summary>
    public static string[] SettingsFromCmdLine => Environment.GetCommandLineArgs()
        // filter the command lines begin with '/'
        // example: ImageGlass.exe /FrmMainWidth=900
        .Where(cmd => cmd.StartsWith(Const.CONFIG_CMD_PREFIX))
        .Select(cmd => cmd[1..]) // trim '/' from the command
        .ToArray();

    #endregion


    public static string StartUpDir => AppDomain.CurrentDomain.BaseDirectory;
    public static string ConfigDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageGlass_10");


    #region Public methods

    /// <summary>
    /// Loads all config files: default, user, command-lines, admin;
    /// then unify configs.
    /// </summary>
    public static IConfigurationRoot ReadUserConfigFile()
    {
        var startUpDir = StartUpDir;
        var configDir = ConfigDir;

        try
        {
            // igconfig.default.json
            var defaultConfig = new ConfigurationBuilder()
                .SetBasePath(startUpDir)
                .AddJsonFile(DefaultFilename, optional: true)
                .Build();

            // admin.igconfig.json
            var adminConfig = new ConfigurationBuilder()
                .SetBasePath(startUpDir)
                .AddJsonFile(AdminFilename, optional: true)
                .Build();

            // final config
            var userConfig = new ConfigurationBuilder()
                .AddConfiguration(defaultConfig)

                // igconfig.json
                .SetBasePath(configDir)
                .AddJsonFile(UserFilename, optional: true)

                // command line
                .AddCommandLine(SettingsFromCmdLine)

                .AddConfiguration(adminConfig)
                .Build();

            return userConfig;
        }
        catch { }


        // fall back to default config if user config is invalid
        var fallBackConfig = new ConfigurationBuilder()
            .SetBasePath(startUpDir)
            .AddJsonFile(DefaultFilename, optional: true) // igconfig.default.json
            .AddCommandLine(SettingsFromCmdLine) // command line
            .AddJsonFile(AdminFilename, optional: true) // igconfig.admin.json
            .Build();

        return fallBackConfig;
    }


    /// <summary>
    /// Loads pre-defined settings from
    /// <see cref="DefaultFilename"/> and <see cref="AdminFilename"/>.
    /// </summary>
    public static IConfigurationRoot ReadNonUserConfigs()
    {
        var startUpDir = StartUpDir;

        // igconfig.default.json
        var defaultConfig = new ConfigurationBuilder()
            .SetBasePath(startUpDir)
            .AddJsonFile(DefaultFilename, optional: true)
            .Build();

        // admin.igconfig.json
        var adminConfig = new ConfigurationBuilder()
            .SetBasePath(startUpDir)
            .AddJsonFile(AdminFilename, optional: true)
            .Build();

        // final config
        var userConfig = new ConfigurationBuilder()
            .AddConfiguration(defaultConfig)
            .AddCommandLine(SettingsFromCmdLine)
            .AddConfiguration(adminConfig)
            .Build();

        return userConfig;
    }


    /// <summary>
    /// Loads and parsse configs from file
    /// </summary>
    public static void Load(IConfigurationRoot? root = null)
    {
        _root ??= ReadUserConfigFile();

        // TODO:
    }


    /// <summary>
    /// Parses and writes configs to file
    /// </summary>
    public static async Task SaveAsync()
    {
        // TODO:

        //var jsonFile = App.ConfigDir(PathType.File, Source.UserFilename);
        //var jsonObj = PrepareJsonSettingsObject();

        //await BHelper.WriteJsonAsync(jsonFile, jsonObj);
    }

    #endregion

}
