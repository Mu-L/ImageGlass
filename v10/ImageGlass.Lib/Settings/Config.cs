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
using Avalonia;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.Common.Types.JsonTypeConverters;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageGlass.Common;


[JsonSerializable(typeof(Config))]
public partial class ConfigJsonContext : JsonSerializerContext { }


public partial class Config : PhReactive
{
    [JsonIgnore]
    private readonly Dictionary<ConfigId, object> _values = [];

    public ConfigMetadata _Metadata { get; set; } = new();



    #region Setting items

    #region Boolean items

    /// <summary>
    /// Gets, sets maximized state of main window.
    /// </summary>
    public bool IsMainWindowMaximized
    {
        get => Get(ConfigId.IsMainWindowMaximized, false);
        set => Set(ConfigId.IsMainWindowMaximized, value);
    }

    /// <summary>
    /// Gets, sets value indicating whether the slideshow mode is enabled or not.
    /// </summary>
    [JsonIgnore]
    public bool EnableSlideshow
    {
        get => Get(ConfigId.EnableSlideshow, false);
        set => Set(ConfigId.EnableSlideshow, value);
    }

    /// <summary>
    /// Gets, sets value if the countdown timer is shown or not.
    /// </summary>
    public bool ShowSlideshowCountdown
    {
        get => Get(ConfigId.ShowSlideshowCountdown, true);
        set => Set(ConfigId.ShowSlideshowCountdown, value);
    }

    /// <summary>
    /// Gets, sets value indicates whether the slide show interval is random.
    /// </summary>
    public bool UseRandomIntervalForSlideshow
    {
        get => Get(ConfigId.UseRandomIntervalForSlideshow, false);
        set => Set(ConfigId.UseRandomIntervalForSlideshow, value);
    }

    /// <summary>
    /// Gets, sets value indicates that slideshow will loop back to the first image when reaching the end of list.
    /// </summary>
    public bool EnableLoopSlideshow
    {
        get => Get(ConfigId.EnableLoopSlideshow, true);
        set => Set(ConfigId.EnableLoopSlideshow, value);
    }

    /// <summary>
    /// Gets, sets value indicates that slideshow is played in full screen, not window mode.
    /// </summary>
    public bool EnableFullscreenSlideshow
    {
        get => Get(ConfigId.EnableFullscreenSlideshow, true);
        set => Set(ConfigId.EnableFullscreenSlideshow, value);
    }

    /// <summary>
    /// Gets, sets value of FrmMain's frameless mode.
    /// </summary>
    public bool EnableFrameless
    {
        get => Get(ConfigId.EnableFrameless, false);
        set => Set(ConfigId.EnableFrameless, value);
    }

    /// <summary>
    /// Gets, sets value indicating whether the full screen mode is enabled or not.
    /// </summary>
    public bool EnableFullScreen
    {
        get => Get(ConfigId.EnableFullScreen, false);
        set => Set(ConfigId.EnableFullScreen, value);
    }

    /// <summary>
    /// Gets, sets value indicates that the toolbar should be hidden in Full screen mode
    /// </summary>
    public bool HideToolbarInFullscreen
    {
        get => Get(ConfigId.HideToolbarInFullscreen, false);
        set => Set(ConfigId.HideToolbarInFullscreen, value);
    }

    /// <summary>
    /// Gets, sets value indicates that the gallery should be hidden in Full screen mode
    /// </summary>
    public bool HideGalleryInFullscreen
    {
        get => Get(ConfigId.HideGalleryInFullscreen, false);
        set => Set(ConfigId.HideGalleryInFullscreen, value);
    }

    /// <summary>
    /// Gets, sets value of gallery visibility
    /// </summary>
    public bool ShowGallery
    {
        get => Get(ConfigId.ShowGallery, true);
        set => Set(ConfigId.ShowGallery, value);
    }

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
        get => Get(ConfigId.ShowWelcomeImage, true);
        set => Set(ConfigId.ShowWelcomeImage, value);
    }

    /// <summary>
    /// Gets, sets value of visibility of toolbar on start up
    /// </summary>
    public bool ShowToolbar
    {
        get => Get(ConfigId.ShowToolbar, true);
        set => Set(ConfigId.ShowToolbar, value);
    }

    ///// <summary>
    ///// Gets, sets value of visibility of Frame Navigation tool on startup
    ///// </summary>
    //public bool ShowFrameNavTool { get; set; } = false;

    /// <summary>
    /// Gets, sets value of visibility of app icon
    /// </summary>
    public bool ShowAppIcon
    {
        get => Get(ConfigId.ShowAppIcon, true);
        set => Set(ConfigId.ShowAppIcon, value);
    }

    ///// <summary>
    ///// Gets, sets value indicating that ImageGlass will loop back viewer to the first image when reaching the end of the list.
    ///// </summary>
    //public bool EnableLoopBackNavigation { get; set; } = true;

    /// <summary>
    /// Gets, sets value indicating that multi instances is allowed or not
    /// </summary>
    public bool EnableMultiInstances
    {
        get => Get(ConfigId.EnableMultiInstances, true);
        set => Set(ConfigId.EnableMultiInstances, value);
    }

    /// <summary>
    /// Gets, sets value indicating that FrmMain is always on top or not.
    /// </summary>
    public bool EnableWindowTopMost
    {
        get => Get(ConfigId.EnableWindowTopMost, false);
        set => Set(ConfigId.EnableWindowTopMost, value);
    }

    /// <summary>
    /// Gets, sets value indicating whether free panning is allowed.
    /// </summary>
    public bool EnableFreePan
    {
        get => Get(ConfigId.EnableFreePan, false);
        set => Set(ConfigId.EnableFreePan, value);
    }

    /// <summary>
    /// Gets, sets value indicates that Confirmation dialog is displayed when deleting image
    /// </summary>
    public bool ShowDeleteConfirmation
    {
        get => Get(ConfigId.ShowDeleteConfirmation, true);
        set => Set(ConfigId.ShowDeleteConfirmation, value);
    }

    /// <summary>
    /// Gets, sets value indicates that Confirmation dialog is displayed when overriding the viewing image
    /// </summary>
    public bool ShowSaveOverrideConfirmation
    {
        get => Get(ConfigId.ShowSaveOverrideConfirmation, true);
        set => Set(ConfigId.ShowSaveOverrideConfirmation, value);
    }

    ///// <summary>
    ///// Gets, sets the setting to control whether the image's original modified date value is preserved on save
    ///// </summary>
    //public bool ShouldPreserveModifiedDate { get; set; } = false;

    /// <summary>
    /// Gets, sets value indicates that Save dialog should use the current image folder as initial directory
    /// </summary>
    public bool OpenSaveAsDialogInTheCurrentImageDir
    {
        get => Get(ConfigId.OpenSaveAsDialogInTheCurrentImageDir, true);
        set => Set(ConfigId.OpenSaveAsDialogInTheCurrentImageDir, value);
    }

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
        get => Get(ConfigId.ShouldOpenLastSeenImage, true);
        set => Set(ConfigId.ShouldOpenLastSeenImage, value);
    }

    /// <summary>
    /// Gets, sets the value indicates that the ColorProfile will be applied for all or only the images with embedded profile
    /// </summary>
    public bool ShouldUseColorProfileForAll
    {
        get => Get(ConfigId.ShouldUseColorProfileForAll, false);
        set => Set(ConfigId.ShouldUseColorProfileForAll, value);
    }

    ///// <summary>
    ///// Gets, sets the value indicates whether to show or hide the Navigation Buttons on viewer
    ///// </summary>
    //public bool EnableNavigationButtons { get; set; } = true;

    /// <summary>
    /// Gets, sets recursive value
    /// </summary>
    public bool EnableRecursiveLoading
    {
        get => Get(ConfigId.EnableRecursiveLoading, false);
        set => Set(ConfigId.EnableRecursiveLoading, value);
    }

    /// <summary>
    /// Gets, sets the value indicates that Windows File Explorer sort order is used if possible
    /// </summary>
    public bool ShouldUseExplorerSortOrder
    {
        get => Get(ConfigId.ShouldUseExplorerSortOrder, true);
        set => Set(ConfigId.ShouldUseExplorerSortOrder, value);
    }

    /// <summary>
    /// Gets, sets the value indicates that images order should be grouped by directory
    /// </summary>
    public bool ShouldGroupImagesByDirectory
    {
        get => Get(ConfigId.ShouldGroupImagesByDirectory, false);
        set => Set(ConfigId.ShouldGroupImagesByDirectory, value);
    }

    /// <summary>
    /// Gets, sets showing/loading hidden images
    /// </summary>
    public bool ShouldLoadHiddenImages
    {
        get => Get(ConfigId.ShouldLoadHiddenImages, false);
        set => Set(ConfigId.ShouldLoadHiddenImages, value);
    }


    /// <summary>
    /// Gets, sets value specifying that Window Fit mode is on
    /// </summary>
    public bool EnableWindowFit
    {
        get => Get(ConfigId.EnableWindowFit, false);
        set => Set(ConfigId.EnableWindowFit, value);
    }


    /// <summary>
    /// Gets, sets value indicates the window should be always center in Window Fit mode
    /// </summary>
    public bool CenterWindowFit
    {
        get => Get(ConfigId.CenterWindowFit, true);
        set => Set(ConfigId.CenterWindowFit, value);
    }

    ///// <summary>
    ///// Displays the embedded thumbnail for RAW formats if found.
    ///// </summary>
    //public bool UseEmbeddedThumbnailRawFormats { get; set; } = false;

    ///// <summary>
    ///// Displays the embedded thumbnail for other formats if found.
    ///// </summary>
    //public bool UseEmbeddedThumbnailOtherFormats { get; set; } = false;


    /// <summary>
    /// Gets, sets value indicates that image preview is shown while the image is being loaded.
    /// </summary>
    public bool ShowImagePreview
    {
        get => Get(ConfigId.ShowImagePreview, true);
        set => Set(ConfigId.ShowImagePreview, value);
    }


    ///// <summary>
    ///// Gets, sets value indicates that images should be loaded asynchronously.
    ///// </summary>
    //public bool EnableImageAsyncLoading { get; set; } = true;

    /// <summary>
    /// Enables / Disables copy multiple files.
    /// </summary>
    public bool EnableCopyMultipleFiles
    {
        get => Get(ConfigId.EnableCopyMultipleFiles, true);
        set => Set(ConfigId.EnableCopyMultipleFiles, value);
    }

    /// <summary>
    /// Enables / Disables cut multiple files.
    /// </summary>
    public bool EnableCutMultipleFiles
    {
        get => Get(ConfigId.EnableCutMultipleFiles, true);
        set => Set(ConfigId.EnableCutMultipleFiles, value);
    }

    /// <summary>
    /// Enables / Disables the file system watcher.
    /// </summary>
    public bool EnableRealTimeFileUpdate
    {
        get => Get(ConfigId.EnableRealTimeFileUpdate, true);
        set => Set(ConfigId.EnableRealTimeFileUpdate, value);
    }

    /// <summary>
    /// Gets, sets value indicates that ImageGlass should open the new image file added in the viewing folder.
    /// </summary>
    public bool ShouldAutoOpenNewAddedImage
    {
        get => Get(ConfigId.ShouldAutoOpenNewAddedImage, false);
        set => Set(ConfigId.ShouldAutoOpenNewAddedImage, value);
    }

    ///// <summary>
    ///// Uses Webview2 for viewing SVG format.
    ///// </summary>
    //public bool UseWebview2ForSvg { get; set; } = true;

    /// <summary>
    /// Enables, disables debug mode.
    /// </summary>
    public bool EnableDebug
    {
        get => Get(ConfigId.EnableDebug, false);
        set => Set(ConfigId.EnableDebug, value);
    }

    #endregion // Boolean items


    #region Number items

    ///// <summary>
    ///// Gets, sets the version that requires to open Quick setup ImageGlass dialog.
    ///// </summary>
    //public double QuickSetupVersion { get; set; } = 0;

    /// <summary>
    /// Gets, sets the maximum panning margin in screen pixels beyond the image edge.
    /// </summary>
    public double PanMargin
    {
        get => Get(ConfigId.PanMargin, 0d);
        set => Set(ConfigId.PanMargin, value);
    }

    /// <summary>
    /// Gets, sets the panning speed.
    /// Value range is from 0 to 100.
    /// </summary>
    public double PanSpeed
    {
        get => Get(ConfigId.PanSpeed, 20d);
        set => Set(ConfigId.PanSpeed, value);
    }

    /// <summary>
    /// Gets, sets the zooming speed.
    /// Value range is from -500 to 500.
    /// </summary>
    public double ZoomSpeed
    {
        get => Get(ConfigId.ZoomSpeed, 0d);
        set => Set(ConfigId.ZoomSpeed, value);
    }

    /// <summary>
    /// Gets, sets slide show interval (minimum value if it's random)
    /// </summary>
    public double SlideshowInterval
    {
        get => Get(ConfigId.SlideshowInterval, 5d);
        set => Set(ConfigId.SlideshowInterval, value);
    }

    /// <summary>
    /// Gets, sets the maximum slide show interval value
    /// </summary>
    public double SlideshowIntervalTo
    {
        get => Get(ConfigId.SlideshowIntervalTo, 5d);
        set => Set(ConfigId.SlideshowIntervalTo, value);
    }

    /// <summary>
    /// Gets, sets the number of image changes to play a beep sound in slideshow mode.
    /// </summary>
    public uint SlideshowImagesToNotifySound
    {
        get => Get(ConfigId.SlideshowImagesToNotifySound, 0u);
        set => Set(ConfigId.SlideshowImagesToNotifySound, value);
    }

    /// <summary>
    /// Gets, sets value of thumbnail dimension in pixel
    /// </summary>
    public uint ThumbnailSize
    {
        get => Get(ConfigId.ThumbnailSize, 70u);
        set => Set(ConfigId.ThumbnailSize, value);
    }

    ///// <summary>
    ///// Gets, sets the maximum size in MB of thumbnail persistent cache.
    ///// </summary>
    //public int GalleryCacheSizeInMb { get; set; } = 400;

    /// <summary>
    /// Gets, sets number of thumbnail columns displayed in vertical gallery.
    /// </summary>
    public uint GalleryColumns
    {
        get => Get(ConfigId.GalleryColumns, 3u);
        set => Set(ConfigId.GalleryColumns, value);
    }

    /// <summary>
    /// Gets, sets the maximum memory for image caching (in MB).
    /// </summary>
    public uint MaxMemoryCacheInMb
    {
        get => Get(ConfigId.MaxMemoryCacheInMb, 0u);
        set => Set(ConfigId.MaxMemoryCacheInMb, value);
    }

    /// <summary>
    /// Gets, sets the maximum image file size (in MB) for caching.
    /// If value is <c>0</c>, the option will be ignored.
    /// </summary>
    public double MaxFileSizeCacheInMb
    {
        get => Get(ConfigId.MaxFileSizeCacheInMb, 100d);
        set => Set(ConfigId.MaxFileSizeCacheInMb, value);
    }

    /// <summary>
    /// Gets, sets the maximum image dimension for caching.
    /// If value is <c>0</c>, the option will be ignored.
    /// </summary>
    public uint MaxDimensionCache
    {
        get => Get(ConfigId.MaxDimensionCache, 8_000u);
        set => Set(ConfigId.MaxDimensionCache, value);
    }

    /// <summary>
    /// Gets, sets fixed width on zooming
    /// </summary>
    public double ZoomLockValue
    {
        get => Get(ConfigId.ZoomLockValue, 100d);
        set => Set(ConfigId.ZoomLockValue, value);
    }

    /// <summary>
    /// Gets, sets toolbar icon height
    /// </summary>
    public uint ToolbarIconHeight
    {
        get => Get(ConfigId.ToolbarIconHeight, (uint)Const.TOOLBAR_ICON_HEIGHT);
        set => Set(ConfigId.ToolbarIconHeight, value);
    }

    /// <summary>
    /// Gets, sets value of image quality for editting
    /// </summary>
    public uint ImageEditQuality
    {
        get => Get(ConfigId.ImageEditQuality, 80u);
        set => Set(ConfigId.ImageEditQuality, value);
    }

    /// <summary>
    /// Gets, sets value of duration to display the in-app message
    /// </summary>
    public int InAppMessageDuration
    {
        get => Get(ConfigId.InAppMessageDuration, 2000);
        set => Set(ConfigId.InAppMessageDuration, value);
    }

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

    /// <summary>
    /// Gets, sets the last time to check for update. Set it to <c>0</c> to disable auto-update.
    /// </summary>
    public string AutoUpdate
    {
        get => Get(ConfigId.AutoUpdate, DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)).ToString());
        set => Set(ConfigId.AutoUpdate, value);
    }

    /// <summary>
    /// Gets, sets the version string the user chose to skip (e.g. "10.1.0.500").
    /// </summary>
    public string UpdateSkippedVersion
    {
        get => Get(ConfigId.UpdateSkippedVersion, string.Empty);
        set => Set(ConfigId.UpdateSkippedVersion, value);
    }

    /// <summary>
    /// Gets, sets color profile string. It can be a defined name or ICC/ICM file path
    /// </summary>
    public string ColorProfile
    {
        get => Get(ConfigId.ColorProfile, nameof(ColorProfileOption.CurrentMonitorProfile));
        set => Set(ConfigId.ColorProfile, value);
    }

    /// <summary>
    /// Gets, sets the absolute file path of the last seen image
    /// </summary>
    public string LastSeenImagePath
    {
        get => Get(ConfigId.LastSeenImagePath, string.Empty);
        set => Set(ConfigId.LastSeenImagePath, value);
    }

    ///// <summary>
    ///// Gets, sets the last view of settings window.
    ///// </summary>
    //public string LastOpenedSetting { get; set; } = string.Empty;

    /// <summary>
    /// Gets, sets background color of of the main window
    /// </summary>
    public string BackgroundColor
    {
        get => Get(ConfigId.BackgroundColor, "#00000000");
        set => Set(ConfigId.BackgroundColor, value);
    }

    /// <summary>
    /// Gets, sets background color of slideshow
    /// </summary>
    public string SlideshowBackgroundColor
    {
        get => Get(ConfigId.SlideshowBackgroundColor, "#000000");
        set => Set(ConfigId.SlideshowBackgroundColor, value);
    }

    /// <summary>
    /// Gets, sets the theme name for dark mode.
    /// </summary>
    public string DarkTheme
    {
        get => Get(ConfigId.DarkTheme, Const.DEFAULT_THEME);
        set => Set(ConfigId.DarkTheme, value);
    }

    /// <summary>
    /// Gets, sets the theme name for light mode.
    /// </summary>
    public string LightTheme
    {
        get => Get(ConfigId.LightTheme, "Kobe-Light");
        set => Set(ConfigId.LightTheme, value);
    }

    /// <summary>
    /// Gets, sets app language.
    /// </summary>
    public string Language
    {
        get => Get(ConfigId.Language, "English");
        set => Set(ConfigId.Language, value);
    }

    #endregion


    #region Enum items

    /// <summary>
    /// Gets, sets checkerboard mode of the viewer.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<CheckerboardType>))]
    public CheckerboardType CheckerboardMode
    {
        get => Get(ConfigId.CheckerboardMode, CheckerboardType.None);
        set => Set(ConfigId.CheckerboardMode, value);
    }

    /// <summary>
    /// Gets, sets image loading order
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ImageOrderBy>))]
    public ImageOrderBy ImageLoadingOrder
    {
        get => Get(ConfigId.ImageLoadingOrder, ImageOrderBy.Name);
        set => Set(ConfigId.ImageLoadingOrder, value);
    }

    /// <summary>
    /// Gets, sets image loading order type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ImageOrderType>))]
    public ImageOrderType ImageLoadingOrderType
    {
        get => Get(ConfigId.ImageLoadingOrderType, ImageOrderType.Asc);
        set => Set(ConfigId.ImageLoadingOrderType, value);
    }


    /// <summary>
    /// Gets, sets zoom mode value
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ZoomMode>))]
    public ZoomMode ZoomMode
    {
        get => Get(ConfigId.ZoomMode, ZoomMode.AutoZoom);
        set => Set(ConfigId.ZoomMode, value);
    }


    /// <summary>
    /// Gets, sets the interpolation mode to render the viewing image
    /// when the zoom factor is <c>less than or equals 100%</c>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ImageInterpolation>))]
    public ImageInterpolation ImageInterpolationScaleDown
    {
        get => Get(ConfigId.ImageInterpolationScaleDown, ImageInterpolation.CubicCatmullRom);
        set => Set(ConfigId.ImageInterpolationScaleDown, value);
    }

    /// <summary>
    /// Gets, sets the interpolation mode to render the viewing image
    /// when the zoom factor is <c>greater than 100%</c>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ImageInterpolation>))]
    public ImageInterpolation ImageInterpolationScaleUp
    {
        get => Get(ConfigId.ImageInterpolationScaleUp, ImageInterpolation.Nearest);
        set => Set(ConfigId.ImageInterpolationScaleUp, value);
    }

    /// <summary>
    /// Gets, sets value indicates what happens after clicking Edit menu.
    /// </summary>
    public AfterEditAppAction AfterEditingAction
    {
        get => Get(ConfigId.AfterEditingAction, AfterEditAppAction.Nothing);
        set => Set(ConfigId.AfterEditingAction, value);
    }

    /// <summary>
    /// Gets, sets the interpolation mode to render the viewing image when the zoom factor is <c>greater than 100%</c>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<BackdropStyle>))]
    public BackdropStyle WindowBackdrop
    {
        get => Get(ConfigId.WindowBackdrop, BackdropStyle.Mica);
        set => Set(ConfigId.WindowBackdrop, value);
    }

    #endregion // Enum items


    #region Array items

    /// <summary>
    /// Gets, sets the size and position of main window.
    /// </summary>
    [JsonConverter(typeof(JsonArrayToRectConverter))]
    public Rect MainWindowBounds
    {
        get => Get(ConfigId.MainWindowBounds, new Rect(200, 200, 800, 500));
        set => Set(ConfigId.MainWindowBounds, value);
    }


    /// <summary>
    /// Gets, sets zoom levels of the viewer
    /// </summary>
    [JsonConverter(typeof(JsonArrayToZoomFactorConverter))]
    public double[] ZoomLevels
    {
        get => Get(ConfigId.ZoomLevels, Array.Empty<double>());
        set => Set(ConfigId.ZoomLevels, value);
    }


    /// <summary>
    /// Gets, sets the list of apps for edit action.
    /// </summary>
    public Dictionary<string, EditingApp?> EditApps
    {
        get => Get(ConfigId.EditApps, new Dictionary<string, EditingApp?>());
        set => Set(ConfigId.EditApps, value);
    }


    /// <summary>
    /// Gets, sets the list of formats that only load the first frame forcefully
    /// </summary>
    [JsonConverter(typeof(JsonHashSetToStringConverter))]
    public HashSet<string> SingleFrameFormats
    {
        get => Get(ConfigId.SingleFrameFormats, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".avif", ".heic", ".heif", ".psd", ".jxl" });
        set => Set(ConfigId.SingleFrameFormats, value);
    }


    /// <summary>
    /// Gets, sets the list of formats that always use native codec to decode.
    /// </summary>
    [JsonConverter(typeof(JsonHashSetToStringConverter))]
    public HashSet<string> NativeCodecReadFormats
    {
        get => Get(ConfigId.NativeCodecReadFormats, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".bmp", ".gif", ".gifv", ".jpg", ".png", ".webp", });
        set => Set(ConfigId.NativeCodecReadFormats, value);
    }


    /// <summary>
    /// Gets, sets the list of supported image formats
    /// </summary>
    [JsonConverter(typeof(JsonHashSetToStringConverter))]
    public HashSet<string> FileFormats
    {
        get => Get(ConfigId.FileFormats, new HashSet<string>(DefaultFileFormats, StringComparer.OrdinalIgnoreCase));
        set => Set(ConfigId.FileFormats, value);
    }


    /// <summary>
    /// Gets, sets the tags for displaying image info.
    /// </summary>
    [JsonConverter(typeof(JsonObservableCollectionToStringConverter))]
    public ObservableCollection<string> ImageInfoTags
    {
        get => Get(ConfigId.ImageInfoTags, new ObservableCollection<string>(DefaultImageInfoTags));
        set => Set(ConfigId.ImageInfoTags, value);
    }


    /// <summary>
    /// Gets, sets hotkeys list of menu
    /// </summary>
    public Dictionary<LangId, Hotkey[]> MenuHotkeys
    {
        get => Get(ConfigId.MenuHotkeys, new Dictionary<LangId, Hotkey[]>());
        set => Set(ConfigId.MenuHotkeys, value);
    }


    ///// <summary>
    ///// Gets, sets mouse click actions
    ///// </summary>
    //public Dictionary<MouseClickEvent, ToggleAction> MouseClickActions { get; set; } = [];

    ///// <summary>
    ///// Gets, sets mouse wheel actions
    ///// </summary>
    //public Dictionary<MouseWheelEvent, MouseWheelAction> MouseWheelActions { get; set; } = [];

    /// <summary>
    /// Gets, sets layout for FrmMain. Syntax:
    /// <c>Dictionary["ControlName", "LayoutPosition"]</c>
    /// </summary>
    public Dictionary<LayoutControl, LayoutPosition> Layout
    {
        get => Get(ConfigId.Layout, new Dictionary<LayoutControl, LayoutPosition>());
        set => Set(ConfigId.Layout, value);
    }

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

    /// <summary>
    /// Gets, sets the config section of tool settings.
    /// Each tool serializes/deserializes its own <see cref="JsonElement"/> using its source-generated <see cref="JsonSerializerContext"/>.
    /// </summary>
    public Dictionary<string, JsonElement> ToolSettings
    {
        get => Get(ConfigId.ToolSettings, new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase));
        set => Set(ConfigId.ToolSettings, value);
    }

    ///// <summary>
    ///// Gets, sets the list of disabled menus
    ///// </summary>
    //public FrozenSet<string> DisabledMenus { get; set; } = FrozenSet<string>.Empty;

    /// <summary>
    /// Gets, sets the list of toolbar buttons
    /// </summary>
    public ObservableCollection<ToolbarItemModel> ToolbarButtons
    {
        get => Get(ConfigId.ToolbarButtons, new ObservableCollection<ToolbarItemModel>(DefaultToolbarItems));
        set => Set(ConfigId.ToolbarButtons, value);
    }

    #endregion // Array items


    #endregion // Setting items



    #region Public Methods

    /// <summary>
    /// Sets setting value.
    /// </summary>
    public void Set(ConfigId configName, object value)
    {
        var oldValue = Get<object?>(configName, null);
        if (Equals(value, oldValue)) return;

        _values[configName] = value;
        _ = OnPropertyChanged(value, oldValue, configName.ToString());
    }


    /// <summary>
    /// Gets setting value.
    /// </summary>
    public T Get<T>(string configName, T defaultValue)
    {
        if (!Enum.TryParse<ConfigId>(configName, out var configId)) return defaultValue;

        return Get<T>(configId, defaultValue);
    }


    /// <summary>
    /// Gets setting value as string.
    /// </summary>
    public string GetAsString(string configName)
    {
        if (!Enum.TryParse<ConfigId>(configName, out var configId)) return string.Empty;

        var value = _values.GetValueOrDefault(configId)?.ToString() ?? string.Empty;
        return value;
    }


    /// <summary>
    /// Gets setting value.
    /// </summary>
    public T Get<T>(ConfigId configName, T defaultValue)
    {
        var value = _values.GetValueOrDefault(configName) ?? defaultValue;
        return (T)value!;
    }

    #endregion // Public Methods



}



