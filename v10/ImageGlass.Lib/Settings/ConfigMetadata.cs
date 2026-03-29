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
namespace ImageGlass.Common;

public record ConfigMetadata
{
    public string Description { get; set; } = "ImageGlass Configuration File";
    public float Version { get; set; } = 10.0f;
}



public enum ConfigId
{

    #region Boolean settings

    IsMainWindowMaximized,
    EnableSlideshow,
    ShowSlideshowCountdown,
    UseRandomIntervalForSlideshow,
    EnableLoopSlideshow,
    EnableFullscreenSlideshow,
    EnableFrameless,
    EnableFullScreen,
    HideToolbarInFullscreen,
    HideGalleryInFullscreen,
    ShowGallery,
    ShowGalleryFileName,
    ShowWelcomeImage,
    ShowToolbar,
    // ShowFrameNavTool,
    ShowAppIcon,
    EnableLoopBackNavigation,
    EnableMultiInstances,
    EnableWindowTopMost,
    EnableFreePan,
    ShowDeleteConfirmation,
    ShowSaveOverrideConfirmation,
    PreserveModifiedDate,
    OpenSaveAsDialogInTheCurrentImageDir,
    ShouldOpenLastSeenImage,
    AlwaysApplyColorProfile,
    EnableNavigationButtons,
    EnableRecursiveLoading,
    UseExplorerSortOrder,
    ShouldGroupImagesByDirectory,
    ShouldLoadHiddenImages,
    EnableWindowFit,
    CenterWindowFit,
    // UseEmbeddedThumbnailRawFormats,
    // UseEmbeddedThumbnailOtherFormats,
    EnableImagePreview,
    EnableCopyMultipleFiles,
    EnableCutMultipleFiles,
    EnableRealTimeFileUpdate,
    ShouldAutoOpenNewAddedImage,
    // UseWebview2ForSvg,
    EnableDebug,

    #endregion // Boolean settings


    #region Number settings

    // QuickSetupVersion,
    PanMargin,
    PanSpeed,
    ZoomSpeed,
    SlideshowInterval,
    SlideshowIntervalTo,
    SlideshowImagesToNotifySound,
    ThumbnailSize,
    // GalleryCacheSizeInMb,
    GalleryColumns,
    MaxMemoryCacheInMb,
    MaxDimensionCache,
    MaxFileSizeCacheInMb,
    ZoomLockValue,
    ToolbarIconHeight,
    ImageEditQuality,
    InAppMessageDuration,
    // EmbeddedThumbnailMinWidth,
    // EmbeddedThumbnailMinHeight,

    #endregion // Number settings


    #region String settings

    AutoUpdate,
    UpdateSkippedVersion,
    ColorProfile,
    LastSeenImagePath,
    // LastOpenedSetting,
    BackgroundColor,
    SlideshowBackgroundColor,
    DarkTheme,
    LightTheme,
    Language,

    #endregion // String settings


    #region Enum settings

    CheckerboardMode,
    ImageLoadingOrder,
    ImageLoadingOrderType,
    ZoomMode,
    ImageInterpolationScaleDown,
    ImageInterpolationScaleUp,
    AfterEditingAction,
    WindowBackdrop,

    #endregion // Enum settings


    #region Array settings

    MainWindowBounds,
    ZoomLevels,
    EditApps,
    SingleFrameFormats,
    NativeCodecReadFormats,
    FileFormats,
    ImageInfoTags,
    MenuHotkeys,
    // MouseClickActions,
    // MouseWheelActions,
    Layout,
    ToolSettings,
    // DisabledMenus,
    ToolbarButtons,

    #endregion // Array settings

}


