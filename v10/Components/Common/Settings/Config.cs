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
using ImageGlass.Common.FileSystem;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Windows.Foundation;

namespace ImageGlass.Common;


[JsonSerializable(typeof(Config))]
public partial class ConfigJsonContext : JsonSerializerContext { }


public partial class Config : IgReactive
{
    public ConfigMetadata _Metadata { get; set; } = new();


    #region Setting items

    ///// <summary>
    ///// Gets, sets the config section of tool settings.
    ///// </summary>
    //public ExpandoObject ToolSettings { get; set; } = new();


    #region Boolean items

    ///// <summary>
    ///// Gets, sets value indicating whether the slideshow mode is enabled or not.
    ///// </summary>
    //public bool EnableSlideshow { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicating whether the FrmMain should be hidden when <see cref="EnableSlideshow"/> is on.
    ///// </summary>
    //public bool HideMainWindowInSlideshow { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value if the countdown timer is shown or not.
    ///// </summary>
    //public bool ShowSlideshowCountdown { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value indicates whether the slide show interval is random.
    ///// </summary>
    //public bool UseRandomIntervalForSlideshow { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates that slideshow will loop back to the first image when reaching the end of list.
    ///// </summary>
    //public bool EnableLoopSlideshow { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value indicates that slideshow is played in full screen, not window mode.
    ///// </summary>
    //public bool EnableFullscreenSlideshow { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value of FrmMain's frameless mode.
    ///// </summary>
    //public bool EnableFrameless { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicating whether the full screen mode is enabled or not.
    ///// </summary>
    //public bool EnableFullScreen { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates that the toolbar should be hidden in Full screen mode
    ///// </summary>
    //public bool HideToolbarInFullscreen { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates that the gallery should be hidden in Full screen mode
    ///// </summary>
    //public bool HideGalleryInFullscreen { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value of gallery visibility
    ///// </summary>
    //public bool ShowGallery { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value whether gallery scrollbars visible
    ///// </summary>
    //public bool ShowGalleryScrollbars { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates that showing image file name on gallery
    ///// </summary>
    //public bool ShowGalleryFileName { get; set; } = true;

    /// <summary>
    /// Gets, sets welcome picture value
    /// </summary>
    public bool ShowWelcomeImage
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = true;

    ///// <summary>
    ///// Gets, sets value of visibility of toolbar on start up
    ///// </summary>
    //public bool ShowToolbar { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value of visibility of Frame Navigation tool on startup
    ///// </summary>
    //public bool ShowFrameNavTool { get; set; } = false;

    /// <summary>
    /// Gets, sets value of visibility of app icon
    /// </summary>
    public bool ShowAppIcon
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = true;

    ///// <summary>
    ///// Gets, sets value indicating that ImageGlass will loop back viewer to the first image when reaching the end of the list.
    ///// </summary>
    //public bool EnableLoopBackNavigation { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value indicating that multi instances is allowed or not
    ///// </summary>
    //public bool EnableMultiInstances { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value indicating that FrmMain is always on top or not.
    ///// </summary>
    //public bool EnableWindowTopMost { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates that Confirmation dialog is displayed when deleting image
    ///// </summary>
    //public bool ShowDeleteConfirmation { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value indicates that Confirmation dialog is displayed when overriding the viewing image
    ///// </summary>
    //public bool ShowSaveOverrideConfirmation { get; set; } = true;

    ///// <summary>
    ///// Gets, sets the setting to control whether the image's original modified date value is preserved on save
    ///// </summary>
    //public bool ShouldPreserveModifiedDate { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates that Save dialog should use the current image folder as initial directory
    ///// </summary>
    //public bool OpenSaveAsDialogInTheCurrentImageDir { get; set; } = true;

    ///// <summary>
    ///// Gets, sets the value indicates that there is a new version
    ///// </summary>
    //public bool ShowNewVersionIndicator { get; set; } = false;

    ///// <summary>
    ///// Gets, sets the value indicates that to toolbar buttons to be centered horizontally
    ///// </summary>
    //public bool EnableCenterToolbar { get; set; } = true;

    /// <summary>
    /// Gets, sets the value indicates that to show last seen image on startup
    /// </summary>
    public bool ShouldOpenLastSeenImage
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = true;

    ///// <summary>
    ///// Gets, sets the value indicates that the ColorProfile will be applied for all or only the images with embedded profile
    ///// </summary>
    //public bool ShouldUseColorProfileForAll { get; set; } = false;

    ///// <summary>
    ///// Gets, sets the value indicates whether to show or hide the Navigation Buttons on viewer
    ///// </summary>
    //public bool EnableNavigationButtons { get; set; } = true;

    /// <summary>
    /// Gets, sets recursive value
    /// </summary>
    public bool EnableRecursiveLoading
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = false;

    /// <summary>
    /// Gets, sets the value indicates that Windows File Explorer sort order is used if possible
    /// </summary>
    public bool ShouldUseExplorerSortOrder
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = true;

    /// <summary>
    /// Gets, sets the value indicates that images order should be grouped by directory
    /// </summary>
    public bool ShouldGroupImagesByDirectory
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = false;

    /// <summary>
    /// Gets, sets showing/loading hidden images
    /// </summary>
    public bool ShouldLoadHiddenImages
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = false;

    ///// <summary>
    ///// Gets, sets value specifying that Window Fit mode is on
    ///// </summary>
    //public bool EnableWindowFit { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates the window should be always center in Window Fit mode
    ///// </summary>
    //public bool CenterWindowFit { get; set; } = true;

    ///// <summary>
    ///// Displays the embedded thumbnail for RAW formats if found.
    ///// </summary>
    //public bool UseEmbeddedThumbnailRawFormats { get; set; } = false;

    ///// <summary>
    ///// Displays the embedded thumbnail for other formats if found.
    ///// </summary>
    //public bool UseEmbeddedThumbnailOtherFormats { get; set; } = false;

    ///// <summary>
    ///// Gets, sets value indicates that image preview is shown while the image is being loaded.
    ///// </summary>
    //public bool ShowImagePreview { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value indicates that images should be loaded asynchronously.
    ///// </summary>
    //public bool EnableImageAsyncLoading { get; set; } = true;

    ///// <summary>
    ///// Enables / Disables copy multiple files.
    ///// </summary>
    //public bool EnableCopyMultipleFiles { get; set; } = true;

    ///// <summary>
    ///// Enables / Disables cut multiple files.
    ///// </summary>
    //public bool EnableCutMultipleFiles { get; set; } = true;

    ///// <summary>
    ///// Enables / Disables the file system watcher.
    ///// </summary>
    //public bool EnableRealTimeFileUpdate { get; set; } = true;

    ///// <summary>
    ///// Gets, sets value indicates that ImageGlass should open the new image file added in the viewing folder.
    ///// </summary>
    //public bool ShouldAutoOpenNewAddedImage { get; set; } = false;

    ///// <summary>
    ///// Uses Webview2 for viewing SVG format.
    ///// </summary>
    //public bool UseWebview2ForSvg { get; set; } = true;

    /// <summary>
    /// Enables, disables debug mode.
    /// </summary>
    public bool EnableDebug
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = false;

    #endregion // Boolean items


    #region Number items

    ///// <summary>
    ///// Gets, sets the version that requires to open Quick setup ImageGlass dialog.
    ///// </summary>
    //public float QuickSetupVersion { get; set; } = 0f;

    /// <summary>
    /// Gets, sets the panning speed.
    /// Value range is from 0 to 100.
    /// </summary>
    public float PanSpeed
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = 20f;

    /// <summary>
    /// Gets, sets the zooming speed.
    /// Value range is from -500 to 500.
    /// </summary>
    public float ZoomSpeed
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = 0f;

    ///// <summary>
    ///// Gets, sets slide show interval (minimum value if it's random)
    ///// </summary>
    //public float SlideshowInterval { get; set; } = 5f;

    ///// <summary>
    ///// Gets, sets the maximum slide show interval value
    ///// </summary>
    //public float SlideshowIntervalTo { get; set; } = 5f;

    ///// <summary>
    ///// Gets, sets the number of image changes to notify <see cref="SlideshowNotificationSound"/> sound in slideshow mode.
    ///// </summary>
    //public int SlideshowImagesToNotifySound { get; set; } = 0;

    /// <summary>
    /// Gets, sets value of thumbnail dimension in pixel
    /// </summary>
    public int ThumbnailSize
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = 70;

    ///// <summary>
    ///// Gets, sets the maximum size in MB of thumbnail persistent cache.
    ///// </summary>
    //public int GalleryCacheSizeInMb { get; set; } = 400;

    ///// <summary>
    ///// Gets, sets number of thumbnail columns displayed in vertical gallery.
    ///// </summary>
    //public int GalleryColumns { get; set; } = 3;

    ///// <summary>
    ///// Gets, sets the number of images cached by <see cref="Base.Services.ImageBooster"/>.
    ///// </summary>
    //public int ImageBoosterCacheCount { get; set; } = 1;

    ///// <summary>
    ///// Gets, sets the maximum image dimension when caching by <see cref="Base.Services.ImageBooster"/>.
    ///// If this value is <c>less than or equals 0</c>, the option will be ignored.
    ///// </summary>
    //public int ImageBoosterCacheMaxDimension { get; set; } = 8_000;

    ///// <summary>
    ///// Gets, sets the maximum image file size (in MB) when caching by <see cref="Base.Services.ImageBooster"/>.
    ///// If this value is <c>less than or equals 0</c>, the option will be ignored.
    ///// </summary>
    //public float ImageBoosterCacheMaxFileSizeInMb { get; set; } = 100f;

    ///// <summary>
    ///// Gets, sets fixed width on zooming
    ///// </summary>
    //public float ZoomLockValue { get; set; } = 100f;

    /// <summary>
    /// Gets, sets toolbar icon height
    /// </summary>
    public uint ToolbarIconHeight
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = Const.TOOLBAR_ICON_HEIGHT;

    ///// <summary>
    ///// Gets, sets value of image quality for editting
    ///// </summary>
    //public uint ImageEditQuality { get; set; } = 80;

    ///// <summary>
    ///// Gets, sets value of duration to display the in-app message
    ///// </summary>
    //public int InAppMessageDuration { get; set; } = 2000;

    ///// <summary>
    ///// Gets, sets the minimum width of the embedded thumbnail to use for displaying
    ///// image when the setting <see cref="UseEmbeddedThumbnailRawFormats"/> or <see cref="UseEmbeddedThumbnailOtherFormats"/> is <c>true</c>.
    ///// </summary>
    //public int EmbeddedThumbnailMinWidth { get; set; } = 0;

    ///// <summary>
    ///// Gets, sets the minimum height of the embedded thumbnail to use for displaying
    ///// image when the setting <see cref="UseEmbeddedThumbnailRawFormats"/> or <see cref="UseEmbeddedThumbnailOtherFormats"/> is <c>true</c>.
    ///// </summary>
    //public int EmbeddedThumbnailMinHeight { get; set; } = 0;

    #endregion // Number items


    #region String items

    ///// <summary>
    ///// Gets, sets color profile string. It can be a defined name or ICC/ICM file path
    ///// </summary>
    //public string ColorProfile { get; set; } = nameof(ColorProfileOption.CurrentMonitorProfile);

    ///// <summary>
    ///// Gets, sets the last time to check for update. Set it to <c>0</c> to disable auto-update.
    ///// </summary>
    //public string AutoUpdate { get; set; } = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)).ToISO8601String();

    /// <summary>
    /// Gets, sets the absolute file path of the last seen image
    /// </summary>
    public string LastSeenImagePath
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = "";

    ///// <summary>
    ///// Gets, sets the last view of settings window.
    ///// </summary>
    //public string LastOpenedSetting { get; set; } = string.Empty;

    /// <summary>
    /// Gets, sets background color of of the main window
    /// </summary>
    public string BackgroundColor
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = "#00000000";

    ///// <summary>
    ///// Gets, sets background color of slideshow
    ///// </summary>
    //public Color SlideshowBackgroundColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets, sets the theme name for dark mode.
    /// </summary>
    public string DarkTheme
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = Const.DEFAULT_THEME;

    /// <summary>
    /// Gets, sets the theme name for light mode.
    /// </summary>
    public string LightTheme
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = "Kobe-Light";

    /// <summary>
    /// Gets, sets app language.
    /// </summary>
    public string Language
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = "English";

    #endregion


    #region Array items

    /// <summary>
    /// Gets, sets the size and position of main window.
    /// </summary>
    [JsonConverter(typeof(JsonArrayToRectConverter))]
    public Rect MainWindowBounds
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = new(200, 200, 1500, 1000);

    /// <summary>
    /// Gets, sets zoom levels of the viewer
    /// </summary>
    [JsonConverter(typeof(JsonArrayToZoomFactorConverter))]
    public double[] ZoomLevels
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = [];

    ///// <summary>
    ///// Gets, sets the list of apps for edit action.
    ///// </summary>
    //public Dictionary<string, EditApp?> EditApps { get; set; } = [];

    /// <summary>
    /// Gets, sets the list of supported image formats
    /// </summary>
    [JsonConverter(typeof(JsonHashSetToStringConverter))]
    public HashSet<string> FileFormats
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = [];

    /// <summary>
    /// Gets, sets the list of formats that only load the first frame forcefully
    /// </summary>
    [JsonConverter(typeof(JsonHashSetToStringConverter))]
    public HashSet<string> SingleFrameFormats
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = [".avif", ".heic", ".heif", ".psd", ".jxl"];

    /// <summary>
    /// Gets, sets the list of toolbar buttons
    /// </summary>
    public ObservableCollection<ToolbarItemModel> ToolbarButtons
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } =
    [
        new ToolbarItemModel {
            Id = "Btn_Open",
            Text = "Open",
            Image = "OpenFile",
        },
        new ToolbarItemModel {
            Id = "Btn_Save",
            Text = "Save",
            Image = "Save",
            ShowText = true,
        },
        new ToolbarItemModel {
            Id = "Btn_Print",
            Text = "Print",
            Image = "Print",
            ShowText = true,
        },
        new ToolbarItemModel {
            Id = "Btn_Crop",
            Text = "Crop",
            Image = "Crop",
        },


        new ToolbarItemModel {
            Id = "Btn_Checkerboard",
            Text = "Checkerboard",
            Image = "Checkerboard",
            Alignment = ToolbarItemAlignment.Right,
        },
        new ToolbarItemModel {
            Id = "Btn_ColorPicker",
            Text = "ColorPicker",
            Image = "ColorPicker",
            Alignment = ToolbarItemAlignment.Right,
        },
    ];

    ///// <summary>
    ///// Gets, sets the tags for displaying image info
    ///// </summary>
    //public List<string> ImageInfoTags { get; set; } = DefaultImageInfoTags;

    ///// <summary>
    ///// Gets, sets hotkeys list of menu
    ///// </summary>
    //public Dictionary<string, List<Hotkey>> MenuHotkeys { get; set; } = [];

    ///// <summary>
    ///// Gets, sets mouse click actions
    ///// </summary>
    //public Dictionary<MouseClickEvent, ToggleAction> MouseClickActions { get; set; } = [];

    ///// <summary>
    ///// Gets, sets mouse wheel actions
    ///// </summary>
    //public Dictionary<MouseWheelEvent, MouseWheelAction> MouseWheelActions { get; set; } = [];

    ///// <summary>
    ///// Gets, sets layout for FrmMain. Syntax:
    ///// <c>Dictionary["ControlName", "DockStyle;order"]</c>
    ///// </summary>
    //public Dictionary<string, string?> Layout { get; set; } = [];

    ///// <summary>
    ///// Gets, sets tools.
    ///// </summary>
    //public List<IgTool?> Tools { get; set; } = [
    //    new IgTool()
    //    {
    //        ToolId = Const.IGTOOL_EXIFTOOL,
    //        ToolName = "ExifGlass - EXIF metadata viewer",
    //        Executable = "exifglass",
    //        Argument = Const.FILE_MACRO,
    //        IsIntegrated = true,
    //        Hotkeys = [new Hotkey(Keys.X)],
    //    },
    //];

    ///// <summary>
    ///// Gets, sets the list of disabled menus
    ///// </summary>
    //public FrozenSet<string> DisabledMenus { get; set; } = FrozenSet<string>.Empty;

    #endregion // Array items


    #region Enum items

    ///// <summary>
    ///// Gets, sets state of main window
    ///// </summary>
    //public FormWindowState FrmMainState { get; set; } = FormWindowState.Normal;

    ///// <summary>
    ///// Gets, sets state of settings window
    ///// </summary>
    //public FormWindowState FrmSettingsState { get; set; } = FormWindowState.Normal;


    /// <summary>
    /// Gets, sets checkerboard mode of the viewer.
    /// </summary>
    public CheckerboardMode CheckerboardMode
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = CheckerboardMode.None;

    /// <summary>
    /// Gets, sets image loading order
    /// </summary>
    public ImageOrderBy ImageLoadingOrder
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = ImageOrderBy.Name;

    /// <summary>
    /// Gets, sets image loading order type
    /// </summary>
    public ImageOrderType ImageLoadingOrderType
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = ImageOrderType.Asc;

    /// <summary>
    /// Gets, sets zoom mode value
    /// </summary>
    public ZoomMode ZoomMode
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = ZoomMode.AutoZoom;

    /// <summary>
    /// Gets, sets the interpolation mode to render the viewing image when the zoom factor is <c>less than or equals 100%</c>.
    /// </summary>
    public ImageInterpolation ImageInterpolationScaleDown
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = ImageInterpolation.MultiSampleLinear;

    /// <summary>
    /// Gets, sets the interpolation mode to render the viewing image when the zoom factor is <c>greater than 100%</c>.
    /// </summary>
    public ImageInterpolation ImageInterpolationScaleUp
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = ImageInterpolation.NearestNeighbor;

    ///// <summary>
    ///// Gets, sets value indicates what happens after clicking Edit menu
    ///// </summary>
    //public AfterEditAppAction AfterEditingAction { get; set; } = AfterEditAppAction.Nothing;

    /// <summary>
    /// Gets, sets the interpolation mode to render the viewing image when the zoom factor is <c>greater than 100%</c>.
    /// </summary>
    public BackdropStyle WindowBackdrop
    {
        get; set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                _ = OnPropertyChanged(value, oldValue);
            }
        }
    } = BackdropStyle.Mica;

    #endregion // Enum items


    #endregion // Setting items


}



