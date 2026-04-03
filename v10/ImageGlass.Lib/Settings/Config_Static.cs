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
using ImageGlass.Common.Actions;
using ImageGlass.Common.AppThemes;
using ImageGlass.Common.Localization;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace ImageGlass.Common;

public partial class Config
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
    /// Gets the exception while loading app settings.
    /// </summary>
    [JsonIgnore]
    public static Exception? LoadingException { get; private set; } = null;


    /// <summary>
    /// Gets the default image formats.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<string> DefaultFileFormats { get; } = new(
        Const.IMAGE_FORMATS.Split(';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        )
    );


    /// <summary>
    /// Gets the default image info tags.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<string> DefaultImageInfoTags { get; } = new([
        "Name",
        "ListCount",
        "FrameCount",
        "Zoom",
        "Dimension",
        "FileSize",
        "ColorSpace",
        "ExifRating",
        "DateTimeAuto",
        "AppName",
    ]);


    /// <summary>
    /// Gets the default mouse wheel actions.
    /// </summary>
    [JsonIgnore]
    public static Dictionary<MouseWheelEvent, MouseWheelAction> DefaultMouseWheelActions { get; } = new()
    {
        [MouseWheelEvent.Scroll] = MouseWheelAction.Zoom,
        [MouseWheelEvent.CtrlAndScroll] = MouseWheelAction.PanVertically,
        [MouseWheelEvent.ShiftAndScroll] = MouseWheelAction.PanHorizontally,
        [MouseWheelEvent.AltAndScroll] = MouseWheelAction.BrowseImages,
    };


    /// <summary>
    /// Gets the default mouse click actions.
    /// </summary>
    [JsonIgnore]
    public static Dictionary<MouseClickEvent, SingleAction> DefaultMouseClickActions { get; } = new()
    {
        [MouseClickEvent.LeftDoubleClick] = new SingleAction(API.IG_SetZoomForMouseClick),
        [MouseClickEvent.WheelClick] = new SingleAction(API.IG_Refresh),
        [MouseClickEvent.XButton1Click] = new SingleAction(API.IG_ViewPrevious),
        [MouseClickEvent.XButton2Click] = new SingleAction(API.IG_ViewNext),
    };


    /// <summary>
    /// Gets the default toolbar items.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<ToolbarItemModel> DefaultToolbarItems =>
    [
        // open file
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.OpenFile) }",
            Image = nameof(IgThemeIcon.OpenFile),
            Text = Lang.KeysMap[LangId.FrmMain_MnuOpenFile],
            Alignment = ToolbarItemAlignment.Right,
            OnClick = new(LangId.FrmMain_MnuOpenFile, API.IG_OpenFile),
        },



        // view previous
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewPreviousImage)}",
            Image = nameof(IgThemeIcon.ViewPreviousImage),
            Text = Lang.KeysMap[LangId.FrmMain_MnuViewPrevious],
            OnClick = new(LangId.FrmMain_MnuViewPrevious, API.IG_ViewPrevious),
        },
        // view next
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewNextImage)}",
            Image = nameof(IgThemeIcon.ViewNextImage),
            Text = Lang.KeysMap[LangId.FrmMain_MnuViewNext],
            OnClick = new(LangId.FrmMain_MnuViewNext, API.IG_ViewNext),
        },
        ToolbarItemModel.Separator,


        // auto zoom
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.AutoZoom)}",
            Image = nameof(IgThemeIcon.AutoZoom),
            Text = Lang.KeysMap[LangId.FrmMain_MnuAutoZoom],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.AutoZoom.ToString(),
            OnClick = new(LangId.FrmMain_MnuAutoZoom, API.IG_SetZoomMode, nameof(ZoomMode.AutoZoom)),
        },
        // lock zoom
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.LockZoom)}",
            Image = nameof(IgThemeIcon.LockZoom),
            Text = Lang.KeysMap[LangId.FrmMain_MnuLockZoom],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.LockZoom.ToString(),
            OnClick = new(LangId.FrmMain_MnuLockZoom, API.IG_SetZoomMode, nameof(ZoomMode.LockZoom)),
        },
        // scale to width
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToWidth)}",
            Image = nameof(IgThemeIcon.ScaleToWidth),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToWidth],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToWidth.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToWidth, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToWidth)),
        },
        // scale to height
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToHeight)}",
            Image = nameof(IgThemeIcon.ScaleToHeight),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToHeight],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToHeight.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToHeight, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToHeight)),
        },
        // scale to fit
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToFit)}",
            Image = nameof(IgThemeIcon.ScaleToFit),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToFit],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToFit.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToFit, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFit)),
        },
        // scale to fill
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToFill)}",
            Image = nameof(IgThemeIcon.ScaleToFill),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToFill],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToFill.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToFill, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFill)),
        },
        ToolbarItemModel.Separator,


        // refresh
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Refresh)}",
            Image = nameof(IgThemeIcon.Refresh),
            Text = Lang.KeysMap[LangId.FrmMain_MnuRefresh],
            OnClick = new(LangId.FrmMain_MnuRefresh, API.IG_Refresh),
        },
        // toggle gallery
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Gallery)}",
            Image = nameof(IgThemeIcon.Gallery),
            Text = Lang.KeysMap[LangId.FrmMain_MnuToggleGallery],
            ConfigBinding = nameof(Config.ShowGallery),
            ConfigBindingValue = "True",
            OnClick = new(LangId.FrmMain_MnuToggleGallery, API.IG_ToggleGallery),
        },
        // toggle checkerboard
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Checkerboard)}",
            Image = nameof(IgThemeIcon.Checkerboard),
            Text = Lang.KeysMap[LangId.FrmMain_MnuToggleCheckerboard],
            ConfigBinding = nameof(Config.CheckerboardMode),
            ConfigBindingValue = $"!{nameof(CheckerboardType.None)}",
            OnClick = new(LangId.FrmMain_MnuToggleCheckerboard, API.IG_ToggleCheckerboard),
        },
        // toggle fullscreen
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FullScreen)}",
            Image = nameof(IgThemeIcon.FullScreen),
            Text = Lang.KeysMap[LangId.FrmMain_MnuFullScreen],
            ConfigBinding = nameof(Config.EnableFullScreen),
            ConfigBindingValue = "True",
            OnClick = new(LangId.FrmMain_MnuFullScreen, API.IG_ToggleFullScreen),
        },
        ToolbarItemModel.Separator,


        // delete
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Delete)}",
            Image = nameof(IgThemeIcon.Delete),
            Text = Lang.KeysMap[LangId.FrmMain_MnuMoveToRecycleBin],
            OnClick = new(LangId.FrmMain_MnuMoveToRecycleBin, API.IG_Delete),
        }
    ];

    #endregion // Public static properties



    // Public static methods
    #region Public static methods

    /// <summary>
    /// Loads and parses configs from file.
    /// </summary>
    public static Config Load(string configFileName)
    {
        Config? appConfig = null;


        // 1. get user config file path
        var configPath = BHelper.ConfigDir(configFileName);

        if (File.Exists(configPath))
        {
            // 2. load user settings
            var jsonOptions = BHelper.CreateJsonOptions();
            var jsonContext = new ConfigJsonContext(jsonOptions);


            try
            {
                var config = BHelper.ReadJsonFromFile(configPath, jsonContext.Config)
                    ?? throw new FileLoadException($"Could not parse settings from file: {configPath}");


                // 3. migrate user config file if config version is changed
                appConfig = MigrateUserConfigFile(config);
            }
            catch (Exception ex)
            {
                // save error
                LoadingException = ex;
            }
        }

        // initialize app config
        appConfig ??= new();

        return appConfig;
    }


    /// <summary>
    /// Migrates user config file.
    /// </summary>
    private static Config MigrateUserConfigFile(Config config)
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
        var jsonContext = new ConfigJsonContext(jsonOptions);

        await BHelper.WriteJsonToFileAsync(jsonFilePath, this, jsonContext.Config);
    }


    /// <summary>
    /// Loads app language <see cref="Config.Lang"/>.
    /// </summary>
    public async Task LoadCurrentLanguageAsync()
    {
        var langPath = BHelper.BaseDir(Dir.Language, Language);
        var lang = new Lang(langPath);

        // load language pack
        await lang.LoadAsync();

        // set app language
        Core.Lang = lang;
    }


    /// <summary>
    /// Loads theme pack <see cref="Config.Theme"/>.
    /// </summary>
    /// <param name="darkMode">
    /// Determine which theme should be loaded: <see cref="DarkTheme"/> or <see cref="LightTheme"/>.
    /// </param>
    /// <param name="useFallBackTheme">
    /// If theme pack is invalid, should load the default theme pack <see cref="Const.DEFAULT_THEME"/>.
    /// </param>
    /// <param name="throwIfThemeInvalid">
    /// If theme pack is invalid, should throw exception.
    /// </param>
    /// <param name="forceUpdateBackground">Force updating background according to theme value</param>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> LoadCurrentThemeAsync(bool darkMode,
        bool useFallBackTheme, bool throwIfThemeInvalid, bool forceUpdateBackground)
    {
        // 1. get the theme folder name
        var themeFolderName = darkMode ? DarkTheme : LightTheme;
        if (string.IsNullOrEmpty(themeFolderName))
        {
            themeFolderName = Const.DEFAULT_THEME;
        }

        // 2. check if theme pack is already loaded
        if (themeFolderName.Equals(Core.Theme.FolderName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 3. load theme pack
        var th = await FindAndLoadThemePackAsync(themeFolderName, useFallBackTheme, throwIfThemeInvalid);

        // 4. update the name of dark/light theme
        if (darkMode) DarkTheme = th.FolderName;
        else LightTheme = th.FolderName;


        // 5. load background color
        if (BackgroundColor == Core.Theme.Colors.BgColor || forceUpdateBackground)
        {
            BackgroundColor = th.Colors.BgColor;
        }


        // 6. set to the current theme
        var success = Core.SetTheme(th);
        return success;
    }


    /// <summary>
    /// Finds the correct location of theme name and loads it.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private static async Task<IgTheme> FindAndLoadThemePackAsync(string themeFolderName,
        bool useFallBackTheme, bool throwIfThemeInvalid)
    {
        // 1. look for theme pack in the Config dir
        var themeConfigPath = BHelper.ConfigDir(Dir.Themes, themeFolderName);
        var th = await new IgTheme().LoadAsync(themeConfigPath);

        if (!th.IsValid)
        {
            // 2. look for theme pack in the base dir
            var baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, themeFolderName);
            th = await new IgTheme().LoadAsync(baseThemeConfigPath);

            // 3. cannot find theme, use fall back theme
            if (!th.IsValid && useFallBackTheme)
            {
                // 4. load default theme
                baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, Const.DEFAULT_THEME);
                th = await new IgTheme().LoadAsync(baseThemeConfigPath);
            }
        }

        // 5. throw error if theme is invalid
        if (!th.IsValid && throwIfThemeInvalid)
        {
            throw new ArgumentException($"IGE: Unable to load '{themeFolderName}' theme pack. " +
                $"Please make sure '{themeConfigPath}' file is valid.", nameof(themeFolderName));
        }

        return th;
    }


    /// <summary>
    /// Gets control layout position.
    /// </summary>
    public static LayoutPosition GetControlLayout(LayoutControl control)
    {
        var defaultPos = control == LayoutControl.Toolbar
            ? LayoutPosition.Top
            : LayoutPosition.Bottom;


        // 1. read control's layouts from setting
        var pos = Core.Config.Layout.GetValueOrDefault(control, defaultPos);


        // 2. standardize toolbar position
        if (control == LayoutControl.Toolbar)
        {
            if (pos is LayoutPosition.Left or LayoutPosition.Right)
            {
                pos = LayoutPosition.Top;
            }
        }

        return pos;
    }


    #endregion // Public methods




}
