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
using ImageGlass.Common.Photoing;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImageGlass.Common;


[JsonSerializable(typeof(IgLang))]
public partial class IgLangJsonContext : JsonSerializerContext { }


/// <summary>
/// ImageGlass language pack (<c>*.iglang.json</c>)
/// </summary>
public class IgLang
{

    #region JSON Serializable Properties

    /// <summary>
    /// Gets, sets the language metadata.
    /// </summary>
    public IgLangMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets, sets the language string dictionary.
    /// </summary>
    public IDictionary<string, string> Items { get; set; } = FrozenDictionary<string, string>.Empty;

    #endregion // JSON Serializable Properties



    #region Non-Serializable Properties

    /// <summary>
    /// Gets the path of language file.
    /// Example: <c>C:\ImageGlass\Languages\Vietnameses.iglang.json</c>
    /// </summary>
    [JsonIgnore]
    private string FilePath { get; set; } = "English";


    /// <summary>
    /// Gets the name of language file.
    /// Example: <c>Vietnameses.iglang.json</c>
    /// </summary>
    [JsonIgnore]
    public string FileName => Path.GetFileName(FilePath);


    /// <summary>
    /// Gets the formatted language string. If not exist, returns the key with <c>#</c> prefix.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    /// <param name="args">The arguments to format the language string.</param>
    /// <remarks>
    /// This is a shortcut for <see cref="Get(string, object?[])"/> method.
    /// </remarks>
    [JsonIgnore]
    public string this[string? key, params object?[] args] => Get(key, args);


    #endregion // Non-Serializable Properties



    #region Instance Initialization

    /// <summary>
    /// Initializes a language pack.
    /// </summary>
    public IgLang() { }


    /// <summary>
    /// Initializes a language pack.
    /// </summary>
    /// <param name="filePath">E.g. <c>C:\ImageGlass\Language\Vietnamese.iglang.json</c></param>
    public IgLang(string filePath)
    {
        FilePath = filePath;
    }

    #endregion // Instance Initialization



    #region Public Methods

    /// <summary>
    /// Reads <see cref="FilePath"/> and loads language strings.
    /// </summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(FilePath)) return;

        // 1. create json context
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new IgLangJsonContext(jsonOptions);

        try
        {
            // 2. load language strings
            var lang = await BHelper.ReadJsonFromFileAsync(FilePath, jsonContext.IgLang);
            if (lang == null) return;

            // 3. store the language strings
            Metadata = lang.Metadata;
            Items = lang.Items.ToFrozenDictionary();
        }
        catch { }
    }


    /// <summary>
    /// Saves current language to JSON file.
    /// </summary>
    public async Task SaveAsFileAsync(string filePath)
    {
        var lang = new IgLang()
        {
            Metadata = Metadata,
            Items = Items,
        };

        if (Metadata.EnglishName.Equals("English", StringComparison.OrdinalIgnoreCase))
        {
            lang.Metadata.EnglishName = "<Your_language_name_in_English>";
            lang.Metadata.LocalName = "<Local_name_of_your_language>";
            lang.Metadata.Author = "<Your_name_here>";
        }


        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new IgLangJsonContext(jsonOptions);

        await BHelper.WriteJsonToFileAsync(filePath, lang, jsonContext.IgLang);
    }


    /// <summary>
    /// Gets the formatted language string. If not exist, returns the key.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    /// <param name="args">The arguments to format the language string.</param>
    public string Get(string? key, params object?[] args)
    {
        if (string.IsNullOrWhiteSpace(key)) return key ?? "";

        string? value = null;

        // 1. try getting value from language file
        _ = Items.TryGetValue(key, out value);


        // 2. try getting value from default language dictionary
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = DefaultLangDictionary.TryGetValue(key, out value);
        }

        // 3. if not found, return the key
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? key;
        }

        // 4. if value has arguments, return the formatted string
        if (args.Length > 0)
        {
            return string.Format(value, args);
        }

        // 5. returns the non-formatted string
        return value;
    }

    #endregion // Public Methods



    /// <summary>
    /// Gets the default language dictionary.
    /// </summary>
    public static FrozenDictionary<string, string> DefaultLangDictionary => new Dictionary<string, string>()
    {
        #region General
        { "_._OK", "OK" }, // v9.0
        { "_._Cancel", "Cancel" }, // v9.0
        { "_._Apply", "Apply" }, // v9.0
        { "_._Close", "Close" }, // v9.0
        { "_._Yes", "Yes" }, // v9.0
        { "_._No", "No" }, // v9.0
        { "_._LearnMore", "Learn more…" }, // v9.0
        { "_._Continue", "Continue" }, // v9.0
        { "_._Quit", "Quit" }, // v9.0
        { "_._Back", "Back" }, // v9.0
        { "_._Next", "Next" }, // v9.0
        { "_._Save", "Save" }, // v9.0
        { "_._Error", "Error" }, // v9.0
        { "_._Warning", "Warning" }, // v9.0
        { "_._Copy", "Copy" }, //v9.0
        { "_._Browse", "Browse…" }, //v9.0
        { "_._Reset", "Reset" }, //v9.0
        { "_._ResetToDefault", "Reset to default" }, //v9.0
        { "_._CheckForUpdate", "Check for update…" }, //v5.0
        { "_._Download", "Download" }, //v9.0
        { "_._Update", "Update" }, //v9.0
        { "_._Website", "Website" }, //v9.0
        { "_._Email", "Email" }, //v9.0
        { "_._Install", "Install…" },
        { "_._Refresh", "Refresh" },
        { "_._Delete", "Delete" },
        { "_._Add", "Add" },
        { "_._Add+", "Add…" },
        { "_._Edit", "Edit" },
        { "_._ID", "ID" },
        { "_._Name", "Name" },
        { "_._Hotkeys", "Hotkeys" },
        { "_._AddHotkey", "Add hotkey…" },
        { "_._Executable", "Executable" },
        { "_._Argument", "Argument" },
        { "_._CommandPreview", "Command preview" },
        { "_._FileExtension", "File extension" },
        { "_._Empty", "(empty)" },
        { "_._MoveUp", "Move up" },
        { "_._MoveDown", "Move down" },
        { "_._Separator", "Separator" },
        { "_._Icon", "Icon" },
        { "_._Description", "Description" },
        { "_._GetHelp", "Get help" },

        { "_._UnhandledException", "Unhandled exception" }, // v9.0
        { "_._UnhandledException._Description", "Unhandled exception has occurred. If you click Continue, the application will ignore this error and attempt to continue. If you click Quit, the application will close immediately." }, // v9.0
        { "_._DoNotShowThisMessageAgain", "Do not show this message again" }, // v9.0
        { $"_._CreatingFile", "Creating a temporary image file…" }, //v9.0
        { $"_._CreatingFileError", "Could not create temporary image file" }, //v9.0
        { $"_._NotSupported", "Unsupported format" }, //v9.0

        { $"_._InvalidAction", "Invalid action" }, //v9.0
        { $"_._InvalidAction._Transformation", "ImageGlass does not support rotation, flipping for this image." }, //v9.0


        { "_._UserAction._MenuNotFound", "Cannot find menu '{0}' to invoke the action" }, // v9.0
        { "_._UserAction._MethodNotFound", "Cannot find method '{0}' to invoke the action" }, // v9.0
        { "_._UserAction._MethodArgumentNotSupported", "The argument type of method '{0}' is not supported" }, // v9.0
        { "_._UserAction._Win32ExeError", "Cannot execute command '{0}'. Make sure the name is correct." }, // v9.0

        { "_._Webview2._NotFound", "Please install WebView2 Runtime to access full features of ImageGlass." }, // 9.2
        { "_._Webview2._Outdated", "Your WebView2 Runtime is not supported. Please update to version {0} or later." }, // 9.2

        // Gallery tooltip
        { $"_.Metadata._FileSize", "File size" }, //v9.0
        { $"_.Metadata._FileCreationTime", "Date created" }, //v9.0
        { $"_.Metadata._FileLastAccessTime", "Date accessed" }, //v9.0
        { $"_.Metadata._FileLastWriteTime", "Date modified" }, //v9.0
        { $"_.Metadata._FrameCount", "Frames" }, //v9.0
        { $"_.Metadata._ExifRatingPercent", "Rating" }, //v9.0
        { $"_.Metadata._ColorSpace", "Color space" }, //v9.0
        { $"_.Metadata._ColorProfile", "Color profile" }, //v9.0
        { $"_.Metadata._ExifDateTime", "EXIF: DateTime" }, //v9.0
        { $"_.Metadata._ExifDateTimeOriginal", "EXIF: DateTimeOriginal" }, //v9.0

        // image info
        { $"_.ImageInfo._ListCount", "{0} file(s)" }, //v9.0
        { $"_.ImageInfo._FrameCount", "{0} frame(s)" }, //v9.0

        // layout position
        { $"_.Position._Left", "Left" },
        { $"_.Position._Right", "Right" },
        { $"_.Position._Top", "Top" },
        { $"_.Position._Bottom", "Bottom" },

        #endregion // General


        #region Enums

        // ImageOrderBy
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.Name)}", "Name (default)" }, //v8.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.Random)}", "Random" }, //v8.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.FileSize)}", "File size" }, //v8.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.Extension)}", "Extension" }, //v8.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.DateCreated)}", "Date created" }, //v8.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.DateAccessed)}", "Date accessed" }, //v8.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.DateModified)}", "Date modified" }, //v8.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.ExifDateTaken)}", "EXIF: Date taken" }, //v9.0
        { $"_.{nameof(ImageOrderBy)}._{nameof(ImageOrderBy.ExifRating)}", "EXIF: Rating" }, //v9.0


        // ImageOrderType
        { $"_.{nameof(ImageOrderType)}._{nameof(ImageOrderType.Asc)}", "Ascending" },  //v8.0
        { $"_.{nameof(ImageOrderType)}._{nameof(ImageOrderType.Desc)}", "Descending" },  //v8.0

        //// AfterEditAppAction
        //{ $"_.{nameof(AfterEditAppAction)}._{nameof(AfterEditAppAction.Nothing)}", "Nothing" }, //v8.0
        //{ $"_.{nameof(AfterEditAppAction)}._{nameof(AfterEditAppAction.Minimize)}", "Minimize" }, //v8.0
        //{ $"_.{nameof(AfterEditAppAction)}._{nameof(AfterEditAppAction.Close)}", "Close" }, //v8.0

        // ColorProfileOption
        { $"_.{nameof(ColorProfileOption)}._{nameof(ColorProfileOption.None)}", "None" },
        { $"_.{nameof(ColorProfileOption)}._{nameof(ColorProfileOption.CurrentMonitorProfile)}", "Current monitor profile" },
        { $"_.{nameof(ColorProfileOption)}._{nameof(ColorProfileOption.Custom)}", "Custom…" },

        // BackdropStyle
        { $"_.{nameof(BackdropStyle)}._{nameof(BackdropStyle.None)}", "None" },

        //// MouseWheelEvent
        //{ $"_.{nameof(MouseWheelEvent)}._{nameof(MouseWheelEvent.Scroll)}", "Scroll" },
        //{ $"_.{nameof(MouseWheelEvent)}._{nameof(MouseWheelEvent.CtrlAndScroll)}", "Hold Ctrl and scroll" },
        //{ $"_.{nameof(MouseWheelEvent)}._{nameof(MouseWheelEvent.ShiftAndScroll)}", "Hold Shift and scroll" },
        //{ $"_.{nameof(MouseWheelEvent)}._{nameof(MouseWheelEvent.AltAndScroll)}", "Hold Alt and scroll" },

        //// MouseWheelAction
        //{ $"_.{nameof(MouseWheelAction)}._{nameof(MouseWheelAction.DoNothing)}", "Do nothing" },
        //{ $"_.{nameof(MouseWheelAction)}._{nameof(MouseWheelAction.Zoom)}", "Zoom in / out" },
        //{ $"_.{nameof(MouseWheelAction)}._{nameof(MouseWheelAction.PanVertically)}", "Pan up / down" },
        //{ $"_.{nameof(MouseWheelAction)}._{nameof(MouseWheelAction.PanHorizontally)}", "Pan left / right" },
        //{ $"_.{nameof(MouseWheelAction)}._{nameof(MouseWheelAction.BrowseImages)}", "View next / previous Image" },

        // ImageInterpolation
        { $"_.{nameof(ImageInterpolation)}._{nameof(ImageInterpolation.NearestNeighbor)}", "Nearest neighbor" },
        { $"_.{nameof(ImageInterpolation)}._{nameof(ImageInterpolation.Linear)}", "Linear" },
        { $"_.{nameof(ImageInterpolation)}._{nameof(ImageInterpolation.Cubic)}", "Cubic" },
        { $"_.{nameof(ImageInterpolation)}._{nameof(ImageInterpolation.MultiSampleLinear)}", "Multi-sample linear" },
        { $"_.{nameof(ImageInterpolation)}._{nameof(ImageInterpolation.Antisotropic)}", "Antisotropic" },
        { $"_.{nameof(ImageInterpolation)}._{nameof(ImageInterpolation.HighQualityBicubic)}", "High quality bicubic" },

        #endregion // Enums


        #region FrmMain

        #region Main menu

        #region File
        { "FrmMain.MnuFile", "File" }, //v7.0
        { "FrmMain.MnuOpenFile", "Open file…" }, //v3.0
        { "FrmMain.MnuNewWindow", "Open new window" }, //v7.0
        { "FrmMain.MnuNewWindow._Error", "Cannot open new window because only one instance is allowed" }, //v7.0
        { "FrmMain.MnuSave", "Save" }, //v8.1
        { "FrmMain.MnuSave._Confirm", "Are you sure you want to override this image?" }, //v9.0
        { "FrmMain.MnuSave._ConfirmDescription", "ImageGlass is not a professional photo editor, please be aware of losing the quality, metadata, layers,… when saving your image." }, //v9.0
        { "FrmMain.MnuSave._Saving", "Saving image…" }, //v9.0
        { "FrmMain.MnuSave._Success", "Image is saved" }, //v9.0
        { "FrmMain.MnuSave._Error", "Could not save the image" }, //v9.0
        { "FrmMain.MnuSaveAs", "Save as…" }, //v3.0
        { "FrmMain.MnuRefresh", "Refresh" }, //v3.0
        { "FrmMain.MnuReload", "Reload image" }, //v5.5
        { "FrmMain.MnuReloadImageList", "Reload image list" }, //v7.0
        { "FrmMain.MnuUnload", "Unload image" }, //v9.0
        { "FrmMain.MnuOpenWith", "Open with…" }, //v7.6
        { "FrmMain.MnuEdit", "Edit image {0}…" }, //v3.0,
        { "FrmMain.MnuEdit._AppNotFound", "Could not find the associated app for editing. You can assign an app for editing this format in ImageGlass Settings > Edit." }, //v9.0
        { "FrmMain.MnuPrint", "Print…" }, //v3.0
        { "FrmMain.MnuPrint._Error", "Could not print the viewing image" }, //v9.0
        { "FrmMain.MnuShare", "Share…" }, //v8.6
        { "FrmMain.MnuShare._Error", "Could not open Share dialog." }, //v9.0
        #endregion

        #region Navigation
        { "FrmMain.MnuNavigation", "Navigation" }, //v3.0
        { "FrmMain.MnuViewNext", "View next image" }, //v3.0
        { "FrmMain.MnuViewPrevious", "View previous image" }, //v3.0

        { "FrmMain.MnuGoTo", "Go to…" }, //v3.0
        { "FrmMain.MnuGoTo._Description", "Enter the image index to view, and then press ENTER" },
        { "FrmMain.MnuGoToFirst", "Go to first image" }, //v3.0
        { "FrmMain.MnuGoToLast", "Go to last image" }, //v3.0

        { "FrmMain.MnuViewNextFrame", "View next frame" }, //v7.5
        { "FrmMain.MnuViewPreviousFrame", "View previous frame" }, //v7.5
        { "FrmMain.MnuViewFirstFrame", "View first frame" }, //v7.5
        { "FrmMain.MnuViewLastFrame", "View last frame" }, //v7.5
        #endregion // Navigation

        #region Zoom
        { "FrmMain.MnuZoom", "Zoom" }, //v7.0
        { "FrmMain.MnuZoomIn", "Zoom in" }, //v3.0
        { "FrmMain.MnuZoomOut", "Zoom out" }, //v3.0
        { "FrmMain.MnuCustomZoom", "Custom zoom…" }, // v8.3
        { "FrmMain.MnuCustomZoom._Description", "Enter a new zoom value" }, // v8.3
        { "FrmMain.MnuScaleToFit", "Scale to fit" }, //v3.5
        { "FrmMain.MnuScaleToFill", "Scale to fill" }, //v7.5
        { "FrmMain.MnuActualSize", "Actual size" }, //v3.0
        { "FrmMain.MnuLockZoom", "Lock zoom ratio" }, //v3.0
        { "FrmMain.MnuAutoZoom", "Auto zoom" }, //v5.5
        { "FrmMain.MnuScaleToWidth", "Scale to width" }, //v3.0
        { "FrmMain.MnuScaleToHeight", "Scale to height" }, //v3.0
        #endregion

        #region Panning
        { "FrmMain.MnuPanning", "Panning" }, //v9.0

        { "FrmMain.MnuPanLeft", "Pan image left" }, //v9.0
        { "FrmMain.MnuPanRight", "Pan image right" }, //v9.0
        { "FrmMain.MnuPanUp", "Pan image up" }, //v9.0
        { "FrmMain.MnuPanDown", "Pan image down" }, //v9.0

        { "FrmMain.MnuPanToLeftSide", "Pan image to left edge" }, //v9.0
        { "FrmMain.MnuPanToRightSide", "Pan image to right edge" }, //v9.0
        { "FrmMain.MnuPanToTop", "Pan image to top" }, //v9.0
        { "FrmMain.MnuPanToBottom", "Pan image to bottom" }, //v9.0
        #endregion // Panning

        #region Image
        { "FrmMain.MnuImage", "Image" }, //v7.0

        { "FrmMain.MnuViewChannels", "View channels" }, //v7.0
        { "FrmMain.MnuLoadingOrders", "Loading orders" }, //v8.0

        { "FrmMain.MnuInvertColors", "Invert colors" }, // v9.3
        { "FrmMain.MnuRotateLeft", "Rotate left" }, //v7.5
        { "FrmMain.MnuRotateRight", "Rotate right" }, //v7.5
        { "FrmMain.MnuFlipHorizontal", "Flip Horizontal" }, // V6.0
        { "FrmMain.MnuFlipVertical", "Flip Vertical" }, // V6.0
        { "FrmMain.MnuRename", "Rename image…" }, //v3.0
        { "FrmMain.MnuRename._Description", "Enter a new filename:" }, // v9.0
        { "FrmMain.MnuMoveToRecycleBin", "Move to the Recycle Bin" }, //v3.0
        { "FrmMain.MnuMoveToRecycleBin._Description", "Do you want to move this file to the Recycle bin?" }, //v3.0
        { "FrmMain.MnuDeleteFromHardDisk", "Delete permanently" }, //v3.0
        { "FrmMain.MnuDeleteFromHardDisk._Description", "Are you sure you want to permanently delete this file?" }, //v3.0
        { "FrmMain.MnuExportFrames", "Export image frames…" }, //v7.5
        { "FrmMain.MnuToggleImageAnimation", "Start / stop animating image" }, //v3.0
        { "FrmMain.MnuSetDesktopBackground", "Set as Desktop background" }, //v3.0
        { "FrmMain.MnuSetDesktopBackground._Error", "Could not set the viewing image as desktop background" }, // v6.0
        { "FrmMain.MnuSetDesktopBackground._Success", "Desktop background is updated" }, // v6.0
        { "FrmMain.MnuSetLockScreen", "Set as Lock screen image" }, // V6.0
        { "FrmMain.MnuSetLockScreen._Error", "Could not set the viewing image as lock screen image" }, // v6.0
        { "FrmMain.MnuSetLockScreen._Success", "Lock screen image is updated" }, // v6.0
        { "FrmMain.MnuOpenLocation", "Open image location" }, //v3.0
        { "FrmMain.MnuImageProperties", "Image properties" }, //v3.0
        #endregion // Image

        #region Clipboard
        { "FrmMain.MnuClipboard", "Clipboard" }, //v3.0
        { "FrmMain.MnuCopyFile", "Copy file" }, //v3.0
        { "FrmMain.MnuCopyFile._Success", "Copied {0} file(s)." }, // v2.0 final
        { "FrmMain.MnuCopyImageData", "Copy image data" }, //v5.0
        { "FrmMain.MnuCopyImageData._Copying", "Copying the image data. It's going to take a while…" }, // v9.0
        { "FrmMain.MnuCopyImageData._Success", "Copied the current image data." }, // v5.0
        { "FrmMain.MnuCutFile", "Cut file" }, //v3.0
        { "FrmMain.MnuCutFile._Success", "Cut {0} file(s)." }, // v2.0 final
        { "FrmMain.MnuCopyPath", "Copy image path" }, //v3.0
        { "FrmMain.MnuCopyPath._Success", "Copied the current image path." }, // v9.0
        { "FrmMain.MnuPasteImage", "Paste image" }, //v3.0
        { "FrmMain.MnuPasteImage._Error", "Could not find image data in the Clipboard" }, // v8.0
        { "FrmMain.MnuClearClipboard", "Clear clipboard" }, //v3.0
        { "FrmMain.MnuClearClipboard._Success", "Cleared clipboard." }, // v2.0 final

        #endregion

        { "FrmMain.MnuWindowFit", "Window Fit" }, //v7.5
        { "FrmMain.MnuFullScreen", "Full Screen" }, //v3.0

        { "FrmMain.MnuFrameless", "Frameless" }, //v7.5
        { "FrmMain.MnuFrameless._EnableDescription", "Hold Shift key to move the window." }, // v7.5

        { "FrmMain.MnuSlideshow", "Slideshow" }, //v3.0

        #region Layout
        { "FrmMain.MnuLayout", "Layout" }, //v3.0
        { "FrmMain.MnuToggleToolbar", "Toolbar" }, //v3.0
        { "FrmMain.MnuToggleGallery", "Gallery panel" }, //v3.0
        { "FrmMain.MnuToggleCheckerboard", "Checkerboard background" }, //v3.0, updated v5.0
        { "FrmMain.MnuToggleTopMost", "Keep window always on top" }, //v3.2
        { "FrmMain.MnuToggleTopMost._Enable", "Enabled window always on top" }, // v9.0
        { "FrmMain.MnuToggleTopMost._Disable", "Disabled window always on top" }, // v9.0
        { "FrmMain.MnuChangeBackgroundColor", "Change background color…" }, // v9.0
        #endregion // Layout

        #region Tools
        { "FrmMain.MnuTools", "Tools" }, //v3.0
        { "FrmMain.MnuColorPicker", "Color picker" }, //v5.0
        { "FrmMain.MnuCropTool", "Crop image" }, // v7.6
        { "FrmMain.MnuResizeTool", "Resize image" }, // v9.2
        { "FrmMain.MnuFrameNav", "Frame navigation" }, // v7.5
        { "FrmMain.MnuGetMoreTools", "Get more tools…" }, // v9.0

        { "FrmMain.MnuLosslessCompression", "Magick.NET Lossless Compression" }, // v9.1
        { "FrmMain.MnuLosslessCompression._Confirm", "Are you sure you want to proceed?" }, // v9.1
        { "FrmMain.MnuLosslessCompression._Description", "This tool uses Magick.NET library for lossless compression, optimizing file size. Overwrites only if the compressed file is smaller than the original." }, // v9.1
        { "FrmMain.MnuLosslessCompression._Compressing", "Performing lossless compression…" }, // v9.1
        { "FrmMain.MnuLosslessCompression._Done", "Done lossless compression.\r\nThe new file size is {0}, saved {1}." }, // v9.1

        #endregion

        { "FrmMain.MnuSettings", "Settings" }, // v3.0

        #region Help
        { "FrmMain.MnuHelp", "Help" }, //v7.0
        { "FrmMain.MnuAbout", "About" }, //v3.0
        { "FrmMain.MnuQuickSetup", "Open ImageGlass Quick Setup" }, //v9.0
        { "FrmMain.MnuCheckForUpdate._NewVersion", "A new version is available!" }, //v5.0
        { "FrmMain.MnuReportIssue", "Report an issue…" }, //v3.0

        { "FrmMain.MnuSetDefaultPhotoViewer", "Set default photo viewer" }, //v9.0
        { "FrmMain.MnuSetDefaultPhotoViewer._Success", "You have successfully set ImageGlass as default photo viewer." }, //v9.0
        { "FrmMain.MnuSetDefaultPhotoViewer._Error", "Could not set ImageGlass as default photo viewer." }, //v9.0

        { "FrmMain.MnuRemoveDefaultPhotoViewer", "Remove default photo viewer" }, //v9.0
        { "FrmMain.MnuRemoveDefaultPhotoViewer._Success", "ImageGlass is no longer the default photo viewer." }, //v9.0
        { "FrmMain.MnuRemoveDefaultPhotoViewer._Error", "Could not remove ImageGlass as the default photo viewer." }, //v9.0

        #endregion

        { "FrmMain.MnuExit", "Exit" }, //v7.0

        #endregion


        #region Form message texts
        { "FrmMain.PicMain._ErrorText", "Could not load this image" }, // v2.0 beta, updated 4.0, 9.0, 10.0
        { "FrmMain.MnuMain", "Main menu" }, // v3.0

        { "FrmMain._OpenFileDialog", "All supported files" },
        { "FrmMain._Loading", "Loading…" }, // v3.0
        { "FrmMain._OpenWith", "Open with {0}" }, //v9.0
        { "FrmMain._ReachedFirstImage", "Reached the first image" }, // v4.0
        { "FrmMain._ReachedLastLast", "Reached the last image" }, // v4.0
        { "FrmMain._ClipboardImage", "Clipboard image" }, //v9.0

        #endregion


        #endregion


        #region FrmAbout
        { "FrmAbout._Slogan", "A lightweight, versatile image viewer" },
        { "FrmAbout._Version", "Version:" },
        { "FrmAbout._License", "Software license" },
        { "FrmAbout._Privacy", "Privacy policy" },
        { "FrmAbout._Thanks", "Special thanks to" },
        { "FrmAbout._LogoDesigner", "Logo designer:" },
        { "FrmAbout._Collaborator", "Collaborator:" },
        { "FrmAbout._Contact", "Contact" },
        { "FrmAbout._Homepage", "Homepage:" },
        { "FrmAbout._Email", "Email:" },
        { "FrmAbout._Credits", "Credits" },
        { "FrmAbout._Donate", "Donate" },
        #endregion


        #region FrmSettings

        { "FrmSettings._ResetSettings", "Reset settings" }, // v9.1
        { "FrmSettings._UnmanagedSettingReminder", "This setting is not managed by ImageGlass. Don't forget to disable it before you remove or relocate the app because ImageGlass does not handle this automatically." }, // v9.1


        #region Nav bar
        { "FrmSettings.Nav._General", "General" },
        { "FrmSettings.Nav._Image", "Image" },
        { "FrmSettings.Nav._Slideshow", "Slideshow" },
        { "FrmSettings.Nav._Edit", "Edit" },
        { "FrmSettings.Nav._Viewer", "Viewer" },
        { "FrmSettings.Nav._Toolbar", "Toolbar" },
        { "FrmSettings.Nav._Gallery", "Gallery" },
        { "FrmSettings.Nav._Layout", "Layout" },
        { "FrmSettings.Nav._Mouse", "Mouse" },
        { "FrmSettings.Nav._Keyboard", "Keyboard" },
        { "FrmSettings.Nav._FileTypeAssociations", "File type associations" },
        { "FrmSettings.Nav._Tools", "Tools" },
        { "FrmSettings.Nav._Language", "Language" },
        { "FrmSettings.Nav._Appearance", "Appearance" },
        #endregion // Nav bar


        #region Tab General
        // General > General
        { "FrmSettings._StartupDir", "Startup location" },
        { "FrmSettings._ConfigDir", "Configuration location" },
        { "FrmSettings._UserConfigFile", "User settings file (igconfig.json)" },

        // General > Startup
        { "FrmSettings._Startup", "Startup" },
        { "FrmSettings._ShowWelcomeImage", "Show welcome image" },
        { "FrmSettings._ShouldOpenLastSeenImage", "Open the last seen image" },

        { "FrmSettings._StartupBoost", "Startup Boost" }, // v9.1
        { "FrmSettings._StartupBoost._Description", "Preload and run ImageGlass in the background for a few seconds during Windows startup to accelerate the first launch." }, // v9.1
        { "FrmSettings._StartupBoost._Enabled", "Startup Boost is enabled" }, // v9.1
        { "FrmSettings._StartupBoost._Disabled", "Startup Boost is disabled" }, // v9.1
        { "FrmSettings._StartupBoost._Error", "Could not change Startup Boost setting" }, // v9.1
        { "FrmSettings._EnableStartupBoost", "Enable Startup Boost" }, // v9.1
        { "FrmSettings._DisableStartupBoost", "Disable Startup Boost" }, // v9.1
        { "FrmSettings._OpenStartupAppsSetting", "Open Startup apps setting" }, // v9.1

        // General > Real-time update
        { "FrmSettings._RealTimeFileUpdate", "Real-time file update" },
        { "FrmSettings._EnableRealTimeFileUpdate", "Monitor file changes in the viewing folder and update in realtime" },
        { "FrmSettings._ShouldAutoOpenNewAddedImage", "Open the new added image automatically" },

        // General > Others
        { "FrmSettings._Others", "Others" },
        { "FrmSettings._AutoUpdate", "Check for update automatically" },
        { "FrmSettings._EnableMultiInstances", "Allow multiple instances of the program" },
        { "FrmSettings._ShowAppIcon", "Show app icon on the title bar" },
        { "FrmSettings._InAppMessageDuration", "In-app message duration (milliseconds)" },
        { "FrmSettings._ImageInfoTags", "Image information tags" },
        { "FrmSettings._AvailableImageInfoTags", "Available tags:" },
        #endregion // Tab General

            
        #region Tab Image
        // Image > Image loading
        { "FrmSettings._ImageLoading", "Image loading" },
        { "FrmSettings._ImageLoadingOrder", "Image loading order" },
        { "FrmSettings._ShouldUseExplorerSortOrder", "Use Explorer sort order if possible" },
        { "FrmSettings._EnableRecursiveLoading", "Load images in subfolders" },
        { "FrmSettings._ShouldGroupImagesByDirectory", "Group images by directory" },
        { "FrmSettings._ShouldLoadHiddenImages", "Load hidden images" },
        { "FrmSettings._EnableLoopBackNavigation", "Loop back to the first image when reaching the end of the image list" },
        { "FrmSettings._ShowImagePreview", "Display image preview while it's being loaded" },
        { "FrmSettings._EnableImageAsyncLoading", "Enable image asynchronous loading" },

        { "FrmSettings._EmbeddedThumbnail", "Embedded thumbnail" },
        { "FrmSettings._UseEmbeddedThumbnailRawFormats", "Load only the embedded thumbnail for RAW formats" },
        { "FrmSettings._UseEmbeddedThumbnailOtherFormats", "Load only the embedded thumbnail for other formats" },
        { "FrmSettings._MinEmbeddedThumbnailSize", "Minimum size of the embedded thumbnail to be loaded" },
        { "FrmSettings._MinEmbeddedThumbnailSize._Width", "Width" },
        { "FrmSettings._MinEmbeddedThumbnailSize._Height", "Height" },

        // Image > Image Booster
        { "FrmSettings._ImageBooster", "Image Booster" },
        { "FrmSettings._ImageBoosterCacheCount", "Number of images cached by Image Booster (one direction)" },
        { "FrmSettings._ImageBoosterCacheMaxDimension", "Maximum image dimension to be cached (in pixels)" },
        { "FrmSettings._ImageBoosterCacheMaxFileSizeInMb", "Maximum image file size to be cached (in megabytes)" },

        // Image > Color management
        { "FrmSettings._ColorManagement", "Color management" },
        { "FrmSettings._ShouldUseColorProfileForAll", "Apply also for images without embedded color profile" },
        { "FrmSettings._ColorProfile", "Color profile" },
        { "FrmSettings._CurrentMonitorProfile._Description", "ImageGlass does not auto-update the color when moving its window between monitors" },
        #endregion // Tab Image


        #region Tab Slideshow
        // Slideshow > Slideshow
        { "FrmSettings._HideMainWindowInSlideshow", "Automatically hide main window" },
        { "FrmSettings._ShowSlideshowCountdown", "Show slideshow countdown" },
        { "FrmSettings._EnableFullscreenSlideshow", "Start slideshow in Full Screen mode" },
        { "FrmSettings._UseRandomIntervalForSlideshow", "Use random interval" },
        { "FrmSettings._SlideshowInterval", "Slideshow interval:" },
        { "FrmSettings._SlideshowInterval._From", "From" },
        { "FrmSettings._SlideshowInterval._To", "To" },
        { "FrmSettings._SlideshowBackgroundColor", "Slideshow background color" },

        // Slideshow > Slideshow notification
        { "FrmSettings._SlideshowNotification", "Slideshow notification" },
        { "FrmSettings._SlideshowImagesToNotifySound", "Number of images to trigger a notification sound" },
        #endregion // Tab Slideshow


        #region Tab Edit
        // Edit > Edit
        { "FrmSettings._ShowDeleteConfirmation", "Show confirmation dialog when deleting file" },
        { "FrmSettings._ShowSaveOverrideConfirmation", "Show confirmation dialog when overriding file" },
        { "FrmSettings._ShouldPreserveModifiedDate", "Preserve the image's modified date on save" },
        { "FrmSettings._OpenSaveAsDialogInTheCurrentImageDir", "Open the Save As dialog in the current image directory" }, // v9.1
        { "FrmSettings._ImageEditQuality", "Image quality" },
        { "FrmSettings._AfterEditingAction", "After opening editing app" },

        // Edit > Clipboard
        { "FrmSettings._Clipboard", "Clipboard" },
        { "FrmSettings._EnableCopyMultipleFiles", "Enable the copying of multiple files at once" },
        { "FrmSettings._EnableCutMultipleFiles", "Enable the cutting of multiple files at once" },

        // Edit > Image editing apps
        { "FrmSettings._EditApps", "Image editing apps" },
        { "FrmSettings._EditApps._AppName", "App name" },
        { "FrmSettings.EditAppDialog._AddApp", "Add an app for editing" },
        { "FrmSettings.EditAppDialog._EditApp", "Edit app" },

        #endregion // Tab Edit


        #region Tab Layout
        // Layout > Layout
        { "FrmSettings.Layout._Order", "Order" },
        { "FrmSettings.Layout._Toolbar", "Toolbar" },
        { "FrmSettings.Layout._ToolbarContext", "Contextual toolbar" },
        { "FrmSettings.Layout._Gallery", "Gallery" },
        { "FrmSettings.Layout._ToolbarPosition", "Toolbar position" },
        { "FrmSettings.Layout._ToolbarContextPosition", "Contextual toolbar position" },
        { "FrmSettings.Layout._GalleryPosition", "Gallery position" },
        #endregion // Tab Layout


        #region Tab Viewer
        // Viewer > Viewer
        { "FrmSettings._ShowCheckerboardOnlyImageRegion", "Show checkerboard only within the image region" },
        { "FrmSettings._EnableNavigationButtons", "Show navigation arrow buttons" },
        { "FrmSettings._CenterWindowFit", "Automatically center the window in Window Fit mode" },
        { "FrmSettings._UseWebview2ForSvg", "Use Webview2 for viewing SVG format" },
        { "FrmSettings._PanSpeed", "Panning speed" },

        // Viewer > Zooming
        { "FrmSettings._Zooming", "Zooming" },
        { "FrmSettings._ImageInterpolation", "Image interpolation" },
        { "FrmSettings._ImageInterpolation._ScaleDown", "When zoom < 100%" },
        { "FrmSettings._ImageInterpolation._ScaleUp", "When zoom > 100%" },
        { "FrmSettings._ZoomSpeed", "Zoom speed" },
        { "FrmSettings._ZoomLevels", "Zoom levels" },
        { "FrmSettings._UseSmoothZooming", "Use smooth zooming" },
        { "FrmSettings._LoadDefaultZoomLevels", "Load default zoom levels" },
        #endregion // Tab Viewer


        #region Tab Toolbar
        // Toolbar > Toolbar
        { "FrmSettings.Toolbar._HideToolbarInFullscreen", "Hide toolbar in Full Screen mode" },
        { "FrmSettings.Toolbar._EnableCenterToolbar", "Use center alignment for toolbar" },
        { "FrmSettings.Toolbar._ToolbarIconHeight", "Toolbar icon size" },

        { "FrmSettings.Toolbar._AddNewButton", "Add a custom toolbar button" },
        { "FrmSettings.Toolbar._EditButton", "Edit toolbar button" },
        { "FrmSettings.Toolbar._ButtonJson", "Button JSON" },


        { "FrmSettings.Toolbar._ToolbarButtons", "Toolbar buttons" },
        { "FrmSettings.Toolbar._AddCustomButton", "Add a custom button…" },
        { "FrmSettings.Toolbar._AvailableButtons", "Available buttons:" },
        { "FrmSettings.Toolbar._CurrentButtons", "Current buttons:" },
        { "FrmSettings.Toolbar._Errors._ButtonIdRequired", "Button ID required." },
        { "FrmSettings.Toolbar._Errors._ButtonIdDuplicated", "A button with the ID '{0}' has already been defined. Please choose a different and unique ID for your button to avoid conflicts." },
        { "FrmSettings.Toolbar._Errors._ButtonExecutableRequired", "Button executable required." },

        #endregion // TAB Toolbar


        #region Tab Gallery
        // Gallery > Gallery
        { "FrmSettings._HideGalleryInFullscreen", "Hide gallery in Full Screen mode" },
        { "FrmSettings._ShowGalleryScrollbars", "Show gallery scrollbars" },
        { "FrmSettings._ShowGalleryFileName", "Show thumbnail filename" },
        { "FrmSettings._ThumbnailSize", "Thumbnail size (in pixels)" },
        { "FrmSettings._GalleryCacheSizeInMb", "Maximum gallery cache size (in megabytes)" },
        { "FrmSettings._GalleryColumns", "Number of thumbnail columns in vertical gallery layout" },
        #endregion // Tab Gallery


        #region Tab Mouse
        // Mouse > Mouse wheel action
        { "FrmSettings._MouseWheelAction", "Mouse wheel action" },
        #endregion // Tab Mouse


        #region Tab Keyboard

        #endregion // Tab Mouse & Keyboard


        #region Tab File type associations
        // File type associations > File extension icons
        { "FrmSettings._FileExtensionIcons", "File extension icons" },
        { "FrmSettings._FileExtensionIcons._Description", "For customizing file extension icons, download an icon pack, place all .ICO files in the extension icon folder, and click the '{0}' button. This will also set ImageGlass as default photo viewer." },
        { "FrmSettings._OpenExtensionIconFolder", "Open extension icon folder" },
        { "FrmSettings._GetExtensionIconPacks", "Get extension icon packs…" },

        // File type associations > Default photo viewer
        { "FrmSettings._DefaultPhotoViewer", "Default photo viewer" },
        { "FrmSettings._DefaultPhotoViewer._Description", "Register the supported formats of ImageGlass with Windows. You might need to open the Default apps settings and manually select ImageGlass from the list for it to take effect." },
        { "FrmSettings._MakeDefault", "Make default" },
        { "FrmSettings._RemoveDefault", "Remove default" },
        { "FrmSettings._OpenDefaultAppsSetting", "Open Default apps setting" },

        // File type associations > File formats
        { "FrmSettings._FileFormats", "File formats" },
        { "FrmSettings._TotalSupportedFormats", "Total supported formats: {0}" },
        { "FrmSettings._AddNewFileExtension", "Add new file extension" },

        #endregion // Tab File type associations


        #region Tab Tools
        // Tools > Tools
        { "FrmSettings.Tools._AddNewTool", "Add an external tool" },
        { "FrmSettings.Tools._EditTool", "Edit external tool" },
        { "FrmSettings.Tools._Integrated", "Integrated" },
        { "FrmSettings.Tools._IntegratedWith", "Integrated with {0}" },
        #endregion // Tab Tools


        #region Tab Language
        // Language > Language
        { "FrmSettings._DisplayLanguage", "Display language" },
        { "FrmSettings._Refresh", "Refresh" },
        { "FrmSettings._InstallNewLanguagePack", "Install new language packs…" },
        { "FrmSettings._GetMoreLanguagePacks", "Get more language packs…" },
        { "FrmSettings._ExportLanguagePack", "Export language pack…" },
        { "FrmSettings._Contributors", "Contributors" },
        #endregion // Tab Language


        #region Tab Appearance
        // Appearance > Appearance
        { "FrmSettings._WindowBackdrop", "Window backdrop" },
        { "FrmSettings._BackgroundColor", "Viewer background color" },

        // Appearance > Theme
        { "FrmSettings._Theme", "Theme" },
        { "FrmSettings._DarkTheme", "Dark" },
        { "FrmSettings._LightTheme", "Light" },
        { "FrmSettings._Author", "Author" },
        { "FrmSettings._Theme._OpenThemeFolder", "Open theme folder" },
        { "FrmSettings._Theme._GetMoreThemes", "Get more theme packs…" },
        { "FrmSettings._Theme._InstallTheme", "Install theme packs" },
        { "FrmSettings._Theme._UninstallTheme", "Uninstall a theme pack" },

        { "FrmSettings._UseThemeForDarkMode", "Use this theme for dark mode" },
        { "FrmSettings._UseThemeForLightMode", "Use this theme for light mode" },
        #endregion // Tab Appearance

        #endregion // FrmSettings
        
            
        #region FrmCrop
        { "FrmCrop.LblAspectRatio", "Aspect ratio:" }, //v9.0
        { "FrmCrop.LblLocation", "Location:" }, //v9.0
        { "FrmCrop.LblSize", "Size:" }, //v9.0

        { "FrmCrop.SelectionAspectRatio._FreeRatio", "Free ratio" }, //v9.0
        { "FrmCrop.SelectionAspectRatio._Custom", "Custom…" }, //v9.0
        { "FrmCrop.SelectionAspectRatio._Original", "Original" }, //v9.0

        { "FrmCrop.BtnQuickSelect._Tooltip", "Quick select…" }, //v9.0
        { "FrmCrop.BtnReset._Tooltip", "Reset selection" }, //v9.0
        { "FrmCrop.BtnSettings._Tooltip", "Open Crop tool settings" }, //v9.0

        { "FrmCrop.BtnSave", "Save" }, //v9.0
        { "FrmCrop.BtnSave._Tooltip", "Save image" }, //v9.0
        { "FrmCrop.BtnSaveAs", "Save as…" }, //v9.0
        { "FrmCrop.BtnSaveAs._Tooltip", "Save as a copy…" }, //v9.0
        { "FrmCrop.BtnCrop", "Crop" }, //v9.0
        { "FrmCrop.BtnCrop._Tooltip", "Crop the image only" }, //v9.0
        { "FrmCrop.BtnCopy", "Copy" }, //v9.0
        { "FrmCrop.BtnCopy._Tooltip", "Copy the selection to clipboard" }, //v9.0


        // Crop settings
        { "FrmCropSettings._Title", "Crop settings" }, //v9.0
        { "FrmCropSettings.ChkCloseToolAfterSaving", "Close Crop tool after saving" }, //v9.0
        { "FrmCropSettings.LblDefaultSelection", "Default selection" }, //v9.0
        { "FrmCropSettings.ChkAutoCenterSelection", "Auto-center selection" }, //v9.0

        { "FrmCropSettings.DefaultSelectionType._UseTheLastSelection", "Use the last selection" }, //v9.0
        { "FrmCropSettings.DefaultSelectionType._SelectNone", "Select none" }, //v9.0
        { "FrmCropSettings.DefaultSelectionType._SelectX", "Select {0}" }, //v9.0
        { "FrmCropSettings.DefaultSelectionType._SelectAll", "Select all" }, //v9.0
        { "FrmCropSettings.DefaultSelectionType._CustomArea", "Custom area…" }, //v9.0

        #endregion // FrmCrop


        #region FrmColorPicker

        { "FrmColorPicker.BtnSettings._Tooltip", "Open Color picker settings…" }, //v9.0

        // Color picker settings
        { "FrmColorPickerSettings._Title", "Color picker settings" }, //v9.0
        { "FrmColorPickerSettings.ChkShowRgbA", "Use RGB format with alpha value" }, //v5.0
        { "FrmColorPickerSettings.ChkShowHexA", "Use HEX format with alpha value" }, //v5.0
        { "FrmColorPickerSettings.ChkShowHslA", "Use HSL format with alpha value" }, //v5.0
        { "FrmColorPickerSettings.ChkShowHsvA", "Use HSV format with alpha value" }, //v8.0
        { "FrmColorPickerSettings.ChkShowCIELabA", "Use CIELAB format with alpha value" }, //v9.0

        #endregion


        #region FrmToolNotFound
        { "FrmToolNotFound._Title", "Tool not found" }, // v9.0
        { "FrmToolNotFound.BtnSelectExecutable", "Select…" }, // v9.0
        { "FrmToolNotFound.LblHeading", "'{0}' is not found!" }, // v9.0
        { "FrmToolNotFound.LblDescription", "ImageGlass was unable to locate the path to the '{0}' executable. To resolve this issue, please update the path to the '{0}' as necessary." }, // v9.0
        { "FrmToolNotFound.LblDownloadToolText", "You can download more tools for ImageGlass at:" }, // v9.0
        #endregion // FrmToolNotFound


        #region FrmHotkeyPicker
        { "FrmHotkeyPicker.LblHotkey", "Press hotkeys" }, // v9.0
        #endregion // FrmHotkeyPicker


        #region FrmResize
        { "FrmResize.RadResizeByPixels", "Pixels" }, // v9.2
        { "FrmResize.RadResizeByPercentage", "Percentage" }, // v9.2
        { "FrmResize.ChkKeepRatio", "Keep ratio propotional" }, // v9.2
        { "FrmResize.LblResample", "Resample:" }, // v9.2
        { "FrmResize.LblCurrentSize", "Current Size:" }, // v9.2
        { "FrmResize.LblNewSize", "New Size:" }, // v9.2
        #endregion // FrmResize


        #region igcmd.exe

        { "_._IgCommandExe._DefaultError._Heading", "Invalid commands" }, //v9.0
        { "_._IgCommandExe._DefaultError._Description", "Make sure you pass the correct commands!\r\nThis executable file contains command-line functions for ImageGlass software.\r\n\r\nTo explore all command lines, please visit:\r\n{0}" }, //v9.0


        #region FrmSlideshow

        { "FrmSlideshow._PauseSlideshow", "Slideshow is paused." }, // v9.0
        { "FrmSlideshow._ResumeSlideshow", "Slideshow is resumed." }, // v9.0

        // menu
        { "FrmSlideshow.MnuPauseResumeSlideshow", "Pause/resume slideshow" }, // v9.0
        { "FrmSlideshow.MnuExitSlideshow", "Exit slideshow" }, // v9.0

        { "FrmSlideshow.MnuToggleCountdown", "Show slideshow countdown" }, // v9.0
        { "FrmSlideshow.MnuZoomModes", "Zoom modes" }, // v9.0

        #endregion


        #region FrmExportFrames
        { "FrmExportFrames._Title", "Export image frames" }, //v9.0
        { "FrmExportFrames._FileNotExist", "Image file does not exist" }, //v7.5
        { "FrmExportFrames._FolderPickerTitle", "Select output folder for exporting image frames" }, //v9.0
        { "FrmExportFrames._Exporting", "Exporting {0}/{1} frames \r\n{2}…" }, //v9.0
        { "FrmExportFrames._ExportDone", "Exported {0} frames successfully to \r\n{1}" }, //v9.0
        { "FrmExportFrames._OpenOutputFolder", "Open output folder" }, //v9.0
        #endregion


        #region FrmUpdate
        { "FrmUpdate._StatusChecking", "Checking for update…" }, //v9.0
        { "FrmUpdate._StatusUpdated", "You are using the latest version!" }, //v9.0
        { "FrmUpdate._StatusOutdated", "A new update is available!" }, //v9.0
        { "FrmUpdate._CurrentVersion", "Current version: {0}" }, //v9.0
        { "FrmUpdate._LatestVersion", "The latest version: {0}" }, //v9.0
        { "FrmUpdate._PublishedDate", "Published date: {0}" }, //v9.0
        #endregion


        #region FrmQuickSetup

        { "FrmQuickSetup._Text", "ImageGlass Quick Setup" }, //v9.0
        { "FrmQuickSetup._StepInfo", "Step {0}" }, //v9.0
        { "FrmQuickSetup._SkipQuickSetup", "Skip this and launch ImageGlass" }, //v9.0

        { "FrmQuickSetup._SeeWhatNew", "See what's new in this version…" }, // v9.0
        { "FrmQuickSetup._SelectProfile", "Select a profile" }, //v9.0
        { "FrmQuickSetup._StandardUser", "Standard user" }, //v9.0
        { "FrmQuickSetup._ProfessionalUser", "Professional user" }, //v9.0
        { "FrmQuickSetup._SettingProfileDescription", "To modify these settings, simply access app settings." }, // v9.0

        { "FrmQuickSetup._SettingsWillBeApplied", "Settings will be applied:" }, //v9.0
        { "FrmQuickSetup._SetDefaultViewer", "Do you want to set ImageGlass as the default photo viewer?" }, //v9.0
        { "FrmQuickSetup._SetDefaultViewer._Description", "You can reset it in the app settings > File type associations tab." }, //v9.0

        { "FrmQuickSetup._ConfirmCloseProcess", "Before applying the new settings, it's essential to close all ImageGlass processes. Are you ready to proceed?" }, //v7.5

        #endregion

        #endregion // igcmd.exe

    }.ToFrozenDictionary();



    public static FrozenDictionary<LangId, string> DefaultLangMap => new Dictionary<LangId, string>(_defaultLangList)
        .ToFrozenDictionary();


    private static IReadOnlyCollection<KeyValuePair<LangId, string>> _defaultLangList = [

        #region General

        new(LangId._OK, "OK"), // v9.0
        new(LangId._Cancel, "Cancel"), // v9.0
        new(LangId._Apply, "Apply"), // v9.0
        new(LangId._Close, "Close"), // v9.0
        new(LangId._Yes, "Yes"), // v9.0
        new(LangId._No, "No"), // v9.0
        new(LangId._LearnMore, "Learn more…"), // v9.0
        new(LangId._Continue, "Continue"), // v9.0
        new(LangId._Quit, "Quit"), // v9.0
        new(LangId._Back, "Back"), // v9.0
        new(LangId._Next, "Next"), // v9.0
        new(LangId._Save, "Save"), // v9.0
        new(LangId._Error, "Error"), // v9.0
        new(LangId._Warning, "Warning"), // v9.0
        new(LangId._Copy, "Copy"), //v9.0
        new(LangId._Browse, "Browse…"), //v9.0
        new(LangId._Reset, "Reset"), //v9.0
        new(LangId._ResetToDefault, "Reset to default"), //v9.0
        new(LangId._CheckForUpdate, "Check for update…"), //v5.0
        new(LangId._Download, "Download"), //v9.0
        new(LangId._Update, "Update"), //v9.0
        new(LangId._Website, "Website"), //v9.0
        new(LangId._Email, "Email"), //v9.0
        new(LangId._Install, "Install…"),
        new(LangId._Refresh, "Refresh"),
        new(LangId._Delete, "Delete"),
        new(LangId._Add, "Add"),
        new(LangId._Edit, "Edit"),
        new(LangId._ID, "ID"),
        new(LangId._Name, "Name"),
        new(LangId._Hotkeys, "Hotkeys"),
        new(LangId._AddHotkey, "Add hotkey…"),
        new(LangId._Executable, "Executable"),
        new(LangId._Argument, "Argument"),
        new(LangId._CommandPreview, "Command preview"),
        new(LangId._FileExtension, "File extension"),
        new(LangId._Empty, "(empty)"),
        new(LangId._MoveUp, "Move up"),
        new(LangId._MoveDown, "Move down"),
        new(LangId._Separator, "Separator"),
        new(LangId._Icon, "Icon"),
        new(LangId._Description, "Description"),
        new(LangId._GetHelp, "Get help"),

        new(LangId._UnhandledException, "Unhandled exception"), // v9.0
        new(LangId._UnhandledException_Description, "Unhandled exception has occurred. If you click Continue, the application will ignore this error and attempt to continue. If you click Quit, the application will close immediately."), // v9.0
        new(LangId._DoNotShowThisMessageAgain, "Do not show this message again"), // v9.0
        new(LangId._CreatingFile, "Creating a temporary image file…"), //v9.0
        new(LangId._CreatingFileError, "Could not create temporary image file"), //v9.0
        new(LangId._NotSupported, "Unsupported format"), //v9.0

        new(LangId._InvalidAction, "Invalid action"), //v9.0
        new(LangId._InvalidAction_Transformation, "ImageGlass does not support rotation, flipping for this image."), //v9.0

        new(LangId._UserAction_MenuNotFound, "Cannot find menu '{0}' to invoke the action"), // v9.0
        new(LangId._UserAction_MethodNotFound, "Cannot find method '{0}' to invoke the action"), // v9.0
        new(LangId._UserAction_MethodArgumentNotSupported, "The argument type of method '{0}' is not supported"), // v9.0
        new(LangId._UserAction_Win32ExeError, "Cannot execute command '{0}'. Make sure the name is correct."), // v9.0

        new(LangId._Webview2_NotFound, "Please install WebView2 Runtime to access full features of ImageGlass."), // 9.2
        new(LangId._Webview2_Outdated, "Your WebView2 Runtime is not supported. Please update to version {0} or later."), // 9.2

        // Gallery tooltip
        new(LangId._Metadata_FileSize, "File size"), //v9.0
        new(LangId._Metadata_FileCreationTime, "Date created"), //v9.0
        new(LangId._Metadata_FileLastAccessTime, "Date accessed"), //v9.0
        new(LangId._Metadata_FileLastWriteTime, "Date modified"), //v9.0
        new(LangId._Metadata_FrameCount, "Frames"), //v9.0
        new(LangId._Metadata_ExifRatingPercent, "Rating"), //v9.0
        new(LangId._Metadata_ColorSpace, "Color space"), //v9.0
        new(LangId._Metadata_ColorProfile, "Color profile"), //v9.0
        new(LangId._Metadata_ExifDateTime, "EXIF: DateTime"), //v9.0
        new(LangId._Metadata_ExifDateTimeOriginal, "EXIF: DateTimeOriginal"), //v9.0

        // image info
        new(LangId._ImageInfo_ListCount, "{0} file(s)"), //v9.0
        new(LangId._ImageInfo_FrameCount, "{0} frame(s)"), //v9.0

        // layout position
        new(LangId._Position_Left, "Left"),
        new(LangId._Position_Right, "Right"),
        new(LangId._Position_Top, "Top"),
        new(LangId._Position_Bottom, "Bottom"),

        #endregion // General
    
        
        #region Enums

        // ImageOrderBy
        new(LangId.ImageOrderBy_Name, "Name (default)"), //v8.0
        new(LangId.ImageOrderBy_Random, "Random"), //v8.0
        new(LangId.ImageOrderBy_FileSize, "File size"), //v8.0
        new(LangId.ImageOrderBy_Extension, "Extension"), //v8.0
        new(LangId.ImageOrderBy_DateCreated, "Date created"), //v8.0
        new(LangId.ImageOrderBy_DateAccessed, "Date accessed"), //v8.0
        new(LangId.ImageOrderBy_DateModified, "Date modified"), //v8.0
        new(LangId.ImageOrderBy_ExifDateTaken, "EXIF: Date taken"), //v9.0
        new(LangId.ImageOrderBy_ExifRating, "EXIF: Rating"), //v9.0


        // ImageOrderType
        new(LangId.ImageOrderType_Asc, "Ascending"),  //v8.0
        new(LangId.ImageOrderType_Desc, "Descending"),  //v8.0

        // AfterEditAppAction
        new(LangId.AfterEditAppAction_Nothing, "Nothing"), //v8.0
        new(LangId.AfterEditAppAction_Minimize, "Minimize"), //v8.0
        new(LangId.AfterEditAppAction_Close, "Close"), //v8.0

        // ColorProfileOption
        new(LangId.ColorProfileOption_None, "None"),
        new(LangId.ColorProfileOption_CurrentMonitorProfile, "Current monitor profile"),
        new(LangId.ColorProfileOption_Custom, "Custom…"),

        // BackdropStyle
        new(LangId.BackdropStyle_None, "None"),

        // MouseWheelEvent
        new(LangId.MouseWheelEvent_Scroll, "Scroll"),
        new(LangId.MouseWheelEvent_CtrlAndScroll, "Hold Ctrl and scroll"),
        new(LangId.MouseWheelEvent_ShiftAndScroll, "Hold Shift and scroll"),
        new(LangId.MouseWheelEvent_AltAndScroll, "Hold Alt and scroll"),

        // MouseWheelAction
        new(LangId.MouseWheelAction_DoNothing, "Do nothing"),
        new(LangId.MouseWheelAction_Zoom, "Zoom in / out"),
        new(LangId.MouseWheelAction_PanVertically, "Pan up / down"),
        new(LangId.MouseWheelAction_PanHorizontally, "Pan left / right"),
        new(LangId.MouseWheelAction_BrowseImages, "View next / previous Image"),

        // ImageInterpolation
        new(LangId.ImageInterpolation_NearestNeighbor, "Nearest neighbor"),
        new(LangId.ImageInterpolation_Linear, "Linear"),
        new(LangId.ImageInterpolation_Cubic, "Cubic"),
        new(LangId.ImageInterpolation_MultiSampleLinear, "Multi-sample linear"),
        new(LangId.ImageInterpolation_Antisotropic, "Antisotropic"),
        new(LangId.ImageInterpolation_HighQualityBicubic, "High quality bicubic"),

        #endregion // Enums


        #region Main Window

        #region Main Window > General
        new(LangId.FrmMain_PicMain_ErrorText, "Could not load this image"), // v2.0 beta, updated 4.0, 9.0, 10.0
        new(LangId.FrmMain_MnuMain, "Main menu"), // v3.0

        new(LangId.FrmMain_OpenFileDialog, "All supported files"),
        new(LangId.FrmMain_Loading, "Loading…"), // v3.0
        new(LangId.FrmMain_OpenWith, "Open with {0}"), //v9.0
        new(LangId.FrmMain_ReachedFirstImage, "Reached the first image"), // v4.0
        new(LangId.FrmMain_ReachedLastLast, "Reached the last image"), // v4.0
        new(LangId.FrmMain_ClipboardImage, "Clipboard image"), //v9.0

        #endregion // Main Window > General


        #region Main Window > Main Menu

        #region Main Menu > File
        new(LangId.FrmMain_MnuFile, "File"), //v7.0
        new(LangId.FrmMain_MnuOpenFile, "Open file…"), //v3.0
        new(LangId.FrmMain_MnuNewWindow, "Open new window"), //v7.0
        new(LangId.FrmMain_MnuNewWindow_Error, "Cannot open new window because only one instance is allowed"), //v7.0
        new(LangId.FrmMain_MnuSave, "Save"), //v8.1
        new(LangId.FrmMain_MnuSave_Confirm, "Are you sure you want to override this image?"), //v9.0
        new(LangId.FrmMain_MnuSave_ConfirmDescription, "ImageGlass is not a professional photo editor, please be aware of losing the quality, metadata, layers,… when saving your image."), //v9.0
        new(LangId.FrmMain_MnuSave_Saving, "Saving image…"), //v9.0
        new(LangId.FrmMain_MnuSave_Success, "Image is saved"), //v9.0
        new(LangId.FrmMain_MnuSave_Error, "Could not save the image"), //v9.0
        new(LangId.FrmMain_MnuSaveAs, "Save as…"), //v3.0
        new(LangId.FrmMain_MnuRefresh, "Refresh"), //v3.0
        new(LangId.FrmMain_MnuReload, "Reload image"), //v5.5
        new(LangId.FrmMain_MnuReloadImageList, "Reload image list"), //v7.0
        new(LangId.FrmMain_MnuUnload, "Unload image"), //v9.0
        new(LangId.FrmMain_MnuOpenWith, "Open with…"), //v7.6
        new(LangId.FrmMain_MnuEdit, "Edit image {0}…"), //v3.0,
        new(LangId.FrmMain_MnuEdit_AppNotFound, "Could not find the associated app for editing. You can assign an app for editing this format in ImageGlass Settings > Edit."), //v9.0
        new(LangId.FrmMain_MnuPrint, "Print…"), //v3.0
        new(LangId.FrmMain_MnuPrint_Error, "Could not print the viewing image"), //v9.0
        new(LangId.FrmMain_MnuShare, "Share…"), //v8.6
        new(LangId.FrmMain_MnuShare_Error, "Could not open Share dialog."), //v9.0
        #endregion // Main Menu > File

        #region Main Menu > Navigation
        new(LangId.FrmMain_MnuNavigation, "Navigation"), //v3.0
        new(LangId.FrmMain_MnuViewNext, "View next image"), //v3.0
        new(LangId.FrmMain_MnuViewPrevious, "View previous image"), //v3.0

        new(LangId.FrmMain_MnuGoTo, "Go to…"), //v3.0
        new(LangId.FrmMain_MnuGoTo_Description, "Enter the image index to view, and then press ENTER"),
        new(LangId.FrmMain_MnuGoToFirst, "Go to first image"), //v3.0
        new(LangId.FrmMain_MnuGoToLast, "Go to last image"), //v3.0

        new(LangId.FrmMain_MnuViewNextFrame, "View next frame"), //v7.5
        new(LangId.FrmMain_MnuViewPreviousFrame, "View previous frame"), //v7.5
        new(LangId.FrmMain_MnuViewFirstFrame, "View first frame"), //v7.5
        new(LangId.FrmMain_MnuViewLastFrame, "View last frame"), //v7.5
        #endregion // Main Menu > Navigation

        #region Main Menu > Zoom
        new(LangId.FrmMain_MnuZoom, "Zoom"), //v7.0
        new(LangId.FrmMain_MnuZoomIn, "Zoom in"), //v3.0
        new(LangId.FrmMain_MnuZoomOut, "Zoom out"), //v3.0
        new(LangId.FrmMain_MnuCustomZoom, "Custom zoom…"), // v8.3
        new(LangId.FrmMain_MnuCustomZoom_Description, "Enter a new zoom value"), // v8.3
        new(LangId.FrmMain_MnuScaleToFit, "Scale to fit"), //v3.5
        new(LangId.FrmMain_MnuScaleToFill, "Scale to fill"), //v7.5
        new(LangId.FrmMain_MnuActualSize, "Actual size"), //v3.0
        new(LangId.FrmMain_MnuLockZoom, "Lock zoom ratio"), //v3.0
        new(LangId.FrmMain_MnuAutoZoom, "Auto zoom"), //v5.5
        new(LangId.FrmMain_MnuScaleToWidth, "Scale to width"), //v3.0
        new(LangId.FrmMain_MnuScaleToHeight, "Scale to height"), //v3.0
        #endregion // Main Menu > Zoom

        #region Main Menu > Panning
        new(LangId.FrmMain_MnuPanning, "Panning"), //v9.0

        new(LangId.FrmMain_MnuPanLeft, "Pan image left"), //v9.0
        new(LangId.FrmMain_MnuPanRight, "Pan image right"), //v9.0
        new(LangId.FrmMain_MnuPanUp, "Pan image up"), //v9.0
        new(LangId.FrmMain_MnuPanDown, "Pan image down"), //v9.0

        new(LangId.FrmMain_MnuPanToLeftSide, "Pan image to left edge"), //v9.0
        new(LangId.FrmMain_MnuPanToRightSide, "Pan image to right edge"), //v9.0
        new(LangId.FrmMain_MnuPanToTop, "Pan image to top"), //v9.0
        new(LangId.FrmMain_MnuPanToBottom, "Pan image to bottom"), //v9.0
        #endregion // Main Menu > Panning

        #region Main Menu > Image
        new(LangId.FrmMain_MnuImage, "Image"), //v7.0

        new(LangId.FrmMain_MnuViewChannels, "View channels"), //v7.0
        new(LangId.FrmMain_MnuLoadingOrders, "Loading orders"), //v8.0

        new(LangId.FrmMain_MnuInvertColors, "Invert colors"), // v9.3
        new(LangId.FrmMain_MnuRotateLeft, "Rotate left"), //v7.5
        new(LangId.FrmMain_MnuRotateRight, "Rotate right"), //v7.5
        new(LangId.FrmMain_MnuFlipHorizontal, "Flip Horizontal"), // V6.0
        new(LangId.FrmMain_MnuFlipVertical, "Flip Vertical"), // V6.0
        new(LangId.FrmMain_MnuRename, "Rename image…"), //v3.0
        new(LangId.FrmMain_MnuRename_Description, "Enter a new filename:"), // v9.0
        new(LangId.FrmMain_MnuMoveToRecycleBin, "Move to the Recycle Bin"), //v3.0
        new(LangId.FrmMain_MnuMoveToRecycleBin_Description, "Do you want to move this file to the Recycle bin?"), //v3.0
        new(LangId.FrmMain_MnuDeleteFromHardDisk, "Delete permanently"), //v3.0
        new(LangId.FrmMain_MnuDeleteFromHardDisk_Description, "Are you sure you want to permanently delete this file?"), //v3.0
        new(LangId.FrmMain_MnuExportFrames, "Export image frames…"), //v7.5
        new(LangId.FrmMain_MnuToggleImageAnimation, "Start / stop animating image"), //v3.0
        new(LangId.FrmMain_MnuSetDesktopBackground, "Set as Desktop background"), //v3.0
        new(LangId.FrmMain_MnuSetDesktopBackground_Error, "Could not set the viewing image as desktop background"), // v6.0
        new(LangId.FrmMain_MnuSetDesktopBackground_Success, "Desktop background is updated"), // v6.0
        new(LangId.FrmMain_MnuSetLockScreen, "Set as Lock screen image"), // V6.0
        new(LangId.FrmMain_MnuSetLockScreen_Error, "Could not set the viewing image as lock screen image"), // v6.0
        new(LangId.FrmMain_MnuSetLockScreen_Success, "Lock screen image is updated"), // v6.0
        new(LangId.FrmMain_MnuOpenLocation, "Open image location"), //v3.0
        new(LangId.FrmMain_MnuImageProperties, "Image properties"), //v3.0
        #endregion // Main Menu > Image

        #region Main Menu > Clipboard
        new(LangId.FrmMain_MnuClipboard, "Clipboard"), //v3.0
        new(LangId.FrmMain_MnuCopyFile, "Copy file"), //v3.0
        new(LangId.FrmMain_MnuCopyFile_Success, "Copied {0} file(s)."), // v2.0 final
        new(LangId.FrmMain_MnuCopyImageData, "Copy image data"), //v5.0
        new(LangId.FrmMain_MnuCopyImageData_Copying, "Copying the image data. It's going to take a while…"), // v9.0
        new(LangId.FrmMain_MnuCopyImageData_Success, "Copied the current image data."), // v5.0
        new(LangId.FrmMain_MnuCutFile, "Cut file"), //v3.0
        new(LangId.FrmMain_MnuCutFile_Success, "Cut {0} file(s)."), // v2.0 final
        new(LangId.FrmMain_MnuCopyPath, "Copy image path"), //v3.0
        new(LangId.FrmMain_MnuCopyPath_Success, "Copied the current image path."), // v9.0
        new(LangId.FrmMain_MnuPasteImage, "Paste image"), //v3.0
        new(LangId.FrmMain_MnuPasteImage_Error, "Could not find image data in the Clipboard"), // v8.0
        new(LangId.FrmMain_MnuClearClipboard, "Clear clipboard"), //v3.0
        new(LangId.FrmMain_MnuClearClipboard_Success, "Cleared clipboard."), // v2.0 final
        #endregion // Main Menu > Clipboard

        new(LangId.FrmMain_MnuWindowFit, "Window Fit"), //v7.5
        new(LangId.FrmMain_MnuFullScreen, "Full Screen"), //v3.0
        new(LangId.FrmMain_MnuFrameless, "Frameless"), //v7.5
        new(LangId.FrmMain_MnuFrameless_EnableDescription, "Hold Shift key to move the window."), // v7.5
        new(LangId.FrmMain_MnuSlideshow, "Slideshow"), //v3.0

        #region Main Menu > Layout
        new(LangId.FrmMain_MnuLayout, "Layout"), //v3.0
        new(LangId.FrmMain_MnuToggleToolbar, "Toolbar"), //v3.0
        new(LangId.FrmMain_MnuToggleGallery, "Gallery panel"), //v3.0
        new(LangId.FrmMain_MnuToggleCheckerboard, "Checkerboard background"), //v3.0, updated v5.0
        new(LangId.FrmMain_MnuToggleTopMost, "Keep window always on top"), //v3.2
        new(LangId.FrmMain_MnuToggleTopMost_Enable, "Enabled window always on top"), // v9.0
        new(LangId.FrmMain_MnuToggleTopMost_Disable, "Disabled window always on top"), // v9.0
        new(LangId.FrmMain_MnuChangeBackgroundColor, "Change background color…"), // v9.0
        #endregion // Main Menu > Layout

        #region Main Menu > Tools
        new(LangId.FrmMain_MnuTools, "Tools"), //v3.0
        new(LangId.FrmMain_MnuColorPicker, "Color picker"), //v5.0
        new(LangId.FrmMain_MnuCropTool, "Crop image"), // v7.6
        new(LangId.FrmMain_MnuResizeTool, "Resize image"), // v9.2
        new(LangId.FrmMain_MnuFrameNav, "Frame navigation"), // v7.5
        new(LangId.FrmMain_MnuGetMoreTools, "Get more tools…"), // v9.0

        new(LangId.FrmMain_MnuLosslessCompression, "Magick.NET Lossless Compression"), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Confirm, "Are you sure you want to proceed?"), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Description, "This tool uses Magick.NET library for lossless compression, optimizing file size. Overwrites only if the compressed file is smaller than the original."), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Compressing, "Performing lossless compression…"), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Done, "Done lossless compression.\r\nThe new file size is {0}, saved {1}."), // v9.1
        #endregion // Main Menu > Tools

        new(LangId.FrmMain_MnuSettings, "Settings"), // v3.0

        #region Main Menu > Help
        new(LangId.FrmMain_MnuHelp, "Help"), //v7.0
        new(LangId.FrmMain_MnuAbout, "About"), //v3.0
        new(LangId.FrmMain_MnuQuickSetup, "Open ImageGlass Quick Setup"), //v9.0
        new(LangId.FrmMain_MnuCheckForUpdate_NewVersion, "A new version is available!"), //v5.0
        new(LangId.FrmMain_MnuReportIssue, "Report an issue…"), //v3.0

        new(LangId.FrmMain_MnuSetDefaultPhotoViewer, "Set default photo viewer"), //v9.0
        new(LangId.FrmMain_MnuSetDefaultPhotoViewer_Success, "You have successfully set ImageGlass as default photo viewer."), //v9.0
        new(LangId.FrmMain_MnuSetDefaultPhotoViewer_Error, "Could not set ImageGlass as default photo viewer."), //v9.0

        new(LangId.FrmMain_MnuRemoveDefaultPhotoViewer, "Remove default photo viewer"), //v9.0
        new(LangId.FrmMain_MnuRemoveDefaultPhotoViewer_Success, "ImageGlass is no longer the default photo viewer."), //v9.0
        new(LangId.FrmMain_MnuRemoveDefaultPhotoViewer_Error, "Could not remove ImageGlass as the default photo viewer."), //v9.0
        #endregion // Main Menu > Help

        new(LangId.FrmMain_MnuExit, "Exit"), //v7.0

        #endregion

        #endregion // Main Window

        
        #region FrmAbout
        new(LangId.FrmAbout_Slogan, "A lightweight, versatile image viewer"),
        new(LangId.FrmAbout_Version, "Version:"),
        new(LangId.FrmAbout_License, "Software license"),
        new(LangId.FrmAbout_Privacy, "Privacy policy"),
        new(LangId.FrmAbout_Thanks, "Special thanks to"),
        new(LangId.FrmAbout_LogoDesigner, "Logo designer:"),
        new(LangId.FrmAbout_Collaborator, "Collaborator:"),
        new(LangId.FrmAbout_Contact, "Contact"),
        new(LangId.FrmAbout_Homepage, "Homepage:"),
        new(LangId.FrmAbout_Email, "Email:"),
        new(LangId.FrmAbout_Credits, "Credits"),
        new(LangId.FrmAbout_Donate, "Donate"),
        #endregion // FrmAbout

        
        #region FrmSettings

        new(LangId.FrmSettings_ResetSettings, "Reset settings"), // v9.1
        new(LangId.FrmSettings_UnmanagedSettingReminder, "This setting is not managed by ImageGlass. Don't forget to disable it before you remove or relocate the app because ImageGlass does not handle this automatically."), // v9.1


        #region FrmSettings > Navbar
        new(LangId.FrmSettings_Nav_General, "General"),
        new(LangId.FrmSettings_Nav_Image, "Image"),
        new(LangId.FrmSettings_Nav_Slideshow, "Slideshow"),
        new(LangId.FrmSettings_Nav_Edit, "Edit"),
        new(LangId.FrmSettings_Nav_Viewer, "Viewer"),
        new(LangId.FrmSettings_Nav_Toolbar, "Toolbar"),
        new(LangId.FrmSettings_Nav_Gallery, "Gallery"),
        new(LangId.FrmSettings_Nav_Layout, "Layout"),
        new(LangId.FrmSettings_Nav_Mouse, "Mouse"),
        new(LangId.FrmSettings_Nav_Keyboard, "Keyboard"),
        new(LangId.FrmSettings_Nav_FileTypeAssociations, "File type associations"),
        new(LangId.FrmSettings_Nav_Tools, "Tools"),
        new(LangId.FrmSettings_Nav_Language, "Language"),
        new(LangId.FrmSettings_Nav_Appearance, "Appearance"),
        #endregion // FrmSettings > Navbar


        #region FrmSettings > Tab General
        // General > General
        new(LangId.FrmSettings_StartupDir, "Startup location"),
        new(LangId.FrmSettings_ConfigDir, "Configuration location"),
        new(LangId.FrmSettings_UserConfigFile, "User settings file (igconfig.json)"),

        // General > Startup
        new(LangId.FrmSettings_Startup, "Startup"),
        new(LangId.FrmSettings_ShowWelcomeImage, "Show welcome image"),
        new(LangId.FrmSettings_ShouldOpenLastSeenImage, "Open the last seen image"),

        new(LangId.FrmSettings_StartupBoost, "Startup Boost"), // v9.1
        new(LangId.FrmSettings_StartupBoost_Description, "Preload and run ImageGlass in the background for a few seconds during Windows startup to accelerate the first launch."), // v9.1
        new(LangId.FrmSettings_StartupBoost_Enabled, "Startup Boost is enabled"), // v9.1
        new(LangId.FrmSettings_StartupBoost_Disabled, "Startup Boost is disabled"), // v9.1
        new(LangId.FrmSettings_StartupBoost_Error, "Could not change Startup Boost setting"), // v9.1
        new(LangId.FrmSettings_EnableStartupBoost, "Enable Startup Boost"), // v9.1
        new(LangId.FrmSettings_DisableStartupBoost, "Disable Startup Boost"), // v9.1
        new(LangId.FrmSettings_OpenStartupAppsSetting, "Open Startup apps setting"), // v9.1

        // General > Real-time update
        new(LangId.FrmSettings_RealTimeFileUpdate, "Real-time file update"),
        new(LangId.FrmSettings_EnableRealTimeFileUpdate, "Monitor file changes in the viewing folder and update in realtime"),
        new(LangId.FrmSettings_ShouldAutoOpenNewAddedImage, "Open the new added image automatically"),

        // General > Others
        new(LangId.FrmSettings_Others, "Others"),
        new(LangId.FrmSettings_AutoUpdate, "Check for update automatically"),
        new(LangId.FrmSettings_EnableMultiInstances, "Allow multiple instances of the program"),
        new(LangId.FrmSettings_ShowAppIcon, "Show app icon on the title bar"),
        new(LangId.FrmSettings_InAppMessageDuration, "In-app message duration (milliseconds)"),
        new(LangId.FrmSettings_ImageInfoTags, "Image information tags"),
        new(LangId.FrmSettings_AvailableImageInfoTags, "Available tags:"),
        #endregion // FrmSettings > Tab General

            
        #region FrmSettings > Tab Image
        // Image > Image loading
        new(LangId.FrmSettings_ImageLoading, "Image loading"),
        new(LangId.FrmSettings_ImageLoadingOrder, "Image loading order"),
        new(LangId.FrmSettings_ShouldUseExplorerSortOrder, "Use Explorer sort order if possible"),
        new(LangId.FrmSettings_EnableRecursiveLoading, "Load images in subfolders"),
        new(LangId.FrmSettings_ShouldGroupImagesByDirectory, "Group images by directory"),
        new(LangId.FrmSettings_ShouldLoadHiddenImages, "Load hidden images"),
        new(LangId.FrmSettings_EnableLoopBackNavigation, "Loop back to the first image when reaching the end of the image list"),
        new(LangId.FrmSettings_ShowImagePreview, "Display image preview while it's being loaded"),
        new(LangId.FrmSettings_EnableImageAsyncLoading, "Enable image asynchronous loading"),

        new(LangId.FrmSettings_EmbeddedThumbnail, "Embedded thumbnail"),
        new(LangId.FrmSettings_UseEmbeddedThumbnailRawFormats, "Load only the embedded thumbnail for RAW formats"),
        new(LangId.FrmSettings_UseEmbeddedThumbnailOtherFormats, "Load only the embedded thumbnail for other formats"),
        new(LangId.FrmSettings_MinEmbeddedThumbnailSize, "Minimum size of the embedded thumbnail to be loaded"),
        new(LangId.FrmSettings_MinEmbeddedThumbnailSize_Width, "Width"),
        new(LangId.FrmSettings_MinEmbeddedThumbnailSize_Height, "Height"),

        // Image > Image Booster
        new(LangId.FrmSettings_ImageBooster, "Image Booster"),
        new(LangId.FrmSettings_ImageBoosterCacheCount, "Number of images cached by Image Booster (one direction)"),
        new(LangId.FrmSettings_ImageBoosterCacheMaxDimension, "Maximum image dimension to be cached (in pixels)"),
        new(LangId.FrmSettings_ImageBoosterCacheMaxFileSizeInMb, "Maximum image file size to be cached (in megabytes)"),

        // Image > Color management
        new(LangId.FrmSettings_ColorManagement, "Color management"),
        new(LangId.FrmSettings_ShouldUseColorProfileForAll, "Apply also for images without embedded color profile"),
        new(LangId.FrmSettings_ColorProfile, "Color profile"),
        new(LangId.FrmSettings_CurrentMonitorProfile_Description, "ImageGlass does not auto-update the color when moving its window between monitors"),
        #endregion // FrmSettings > Tab Image


        #region FrmSettings > Tab Slideshow
        // Slideshow > Slideshow
        new(LangId.FrmSettings_HideMainWindowInSlideshow, "Automatically hide main window"),
        new(LangId.FrmSettings_ShowSlideshowCountdown, "Show slideshow countdown"),
        new(LangId.FrmSettings_EnableFullscreenSlideshow, "Start slideshow in Full Screen mode"),
        new(LangId.FrmSettings_UseRandomIntervalForSlideshow, "Use random interval"),
        new(LangId.FrmSettings_SlideshowInterval, "Slideshow interval:"),
        new(LangId.FrmSettings_SlideshowInterval_From, "From"),
        new(LangId.FrmSettings_SlideshowInterval_To, "To"),
        new(LangId.FrmSettings_SlideshowBackgroundColor, "Slideshow background color"),

        // Slideshow > Slideshow notification
        new(LangId.FrmSettings_SlideshowNotification, "Slideshow notification"),
        new(LangId.FrmSettings_SlideshowImagesToNotifySound, "Number of images to trigger a notification sound"),
        #endregion // FrmSettings > Tab Slideshow


        #region FrmSettings > Tab Edit
        // Edit > Edit
        new(LangId.FrmSettings_ShowDeleteConfirmation, "Show confirmation dialog when deleting file"),
        new(LangId.FrmSettings_ShowSaveOverrideConfirmation, "Show confirmation dialog when overriding file"),
        new(LangId.FrmSettings_ShouldPreserveModifiedDate, "Preserve the image's modified date on save"),
        new(LangId.FrmSettings_OpenSaveAsDialogInTheCurrentImageDir, "Open the Save As dialog in the current image directory"), // v9.1
        new(LangId.FrmSettings_ImageEditQuality, "Image quality"),
        new(LangId.FrmSettings_AfterEditingAction, "After opening editing app"),

        // Edit > Clipboard
        new(LangId.FrmSettings_Clipboard, "Clipboard"),
        new(LangId.FrmSettings_EnableCopyMultipleFiles, "Enable the copying of multiple files at once"),
        new(LangId.FrmSettings_EnableCutMultipleFiles, "Enable the cutting of multiple files at once"),

        // Edit > Image editing apps
        new(LangId.FrmSettings_EditApps, "Image editing apps"),
        new(LangId.FrmSettings_EditApps_AppName, "App name"),
        new(LangId.FrmSettings_EditAppDialog_AddApp, "Add an app for editing"),
        new(LangId.FrmSettings_EditAppDialog_EditApp, "Edit app"),

        #endregion // FrmSettings > Tab Edit


        #region FrmSettings > Tab Layout
        // Layout > Layout
        new(LangId.FrmSettings_Layout_Order, "Order"),
        new(LangId.FrmSettings_Layout_Toolbar, "Toolbar"),
        new(LangId.FrmSettings_Layout_ToolbarContext, "Contextual toolbar"),
        new(LangId.FrmSettings_Layout_Gallery, "Gallery"),
        new(LangId.FrmSettings_Layout_ToolbarPosition, "Toolbar position"),
        new(LangId.FrmSettings_Layout_ToolbarContextPosition, "Contextual toolbar position"),
        new(LangId.FrmSettings_Layout_GalleryPosition, "Gallery position"),
        #endregion // FrmSettings > Tab Layout


        #region FrmSettings > Tab Viewer
        // Viewer > Viewer
        new(LangId.FrmSettings_ShowCheckerboardOnlyImageRegion, "Show checkerboard only within the image region"),
        new(LangId.FrmSettings_EnableNavigationButtons, "Show navigation arrow buttons"),
        new(LangId.FrmSettings_CenterWindowFit, "Automatically center the window in Window Fit mode"),
        new(LangId.FrmSettings_UseWebview2ForSvg, "Use Webview2 for viewing SVG format"),
        new(LangId.FrmSettings_PanSpeed, "Panning speed"),

        // Viewer > Zooming
        new(LangId.FrmSettings_Zooming, "Zooming"),
        new(LangId.FrmSettings_ImageInterpolation, "Image interpolation"),
        new(LangId.FrmSettings_ImageInterpolation_ScaleDown, "When zoom < 100%"),
        new(LangId.FrmSettings_ImageInterpolation_ScaleUp, "When zoom > 100%"),
        new(LangId.FrmSettings_ZoomSpeed, "Zoom speed"),
        new(LangId.FrmSettings_ZoomLevels, "Zoom levels"),
        new(LangId.FrmSettings_UseSmoothZooming, "Use smooth zooming"),
        new(LangId.FrmSettings_LoadDefaultZoomLevels, "Load default zoom levels"),
        #endregion // FrmSettings > Tab Viewer


        #region FrmSettings > Tab Toolbar
        // Toolbar > Toolbar
        new(LangId.FrmSettings_Toolbar_HideToolbarInFullscreen, "Hide toolbar in Full Screen mode"),
        new(LangId.FrmSettings_Toolbar_EnableCenterToolbar, "Use center alignment for toolbar"),
        new(LangId.FrmSettings_Toolbar_ToolbarIconHeight, "Toolbar icon size"),

        new(LangId.FrmSettings_Toolbar_AddNewButton, "Add a custom toolbar button"),
        new(LangId.FrmSettings_Toolbar_EditButton, "Edit toolbar button"),
        new(LangId.FrmSettings_Toolbar_ButtonJson, "Button JSON"),


        new(LangId.FrmSettings_Toolbar_ToolbarButtons, "Toolbar buttons"),
        new(LangId.FrmSettings_Toolbar_AddCustomButton, "Add a custom button…"),
        new(LangId.FrmSettings_Toolbar_AvailableButtons, "Available buttons:"),
        new(LangId.FrmSettings_Toolbar_CurrentButtons, "Current buttons:"),
        new(LangId.FrmSettings_Toolbar_Errors_ButtonIdRequired, "Button ID required."),
        new(LangId.FrmSettings_Toolbar_Errors_ButtonIdDuplicated, "A button with the ID '{0}' has already been defined. Please choose a different and unique ID for your button to avoid conflicts."),
        new(LangId.FrmSettings_Toolbar_Errors_ButtonExecutableRequired, "Button executable required."),

        #endregion // FrmSettings > Tab Toolbar


        #region FrmSettings > Tab Gallery
        // Gallery > Gallery
        new(LangId.FrmSettings_HideGalleryInFullscreen, "Hide gallery in Full Screen mode"),
        new(LangId.FrmSettings_ShowGalleryScrollbars, "Show gallery scrollbars"),
        new(LangId.FrmSettings_ShowGalleryFileName, "Show thumbnail filename"),
        new(LangId.FrmSettings_ThumbnailSize, "Thumbnail size (in pixels)"),
        new(LangId.FrmSettings_GalleryCacheSizeInMb, "Maximum gallery cache size (in megabytes)"),
        new(LangId.FrmSettings_GalleryColumns, "Number of thumbnail columns in vertical gallery layout"),
        #endregion // FrmSettings > Tab Gallery


        #region FrmSettings > Tab Mouse
        // Mouse > Mouse wheel action
        new(LangId.FrmSettings_MouseWheelAction, "Mouse wheel action"),
        #endregion // FrmSettings > Tab Mouse


        #region FrmSettings > Tab Keyboard

        #endregion // FrmSettings > Tab Mouse & Keyboard


        #region FrmSettings > Tab File type associations
        // File type associations > File extension icons
        new(LangId.FrmSettings_FileExtensionIcons, "File extension icons"),
        new(LangId.FrmSettings_FileExtensionIcons_Description, "For customizing file extension icons, download an icon pack, place all .ICO files in the extension icon folder, and click the '{0}' button. This will also set ImageGlass as default photo viewer."),
        new(LangId.FrmSettings_OpenExtensionIconFolder, "Open extension icon folder"),
        new(LangId.FrmSettings_GetExtensionIconPacks, "Get extension icon packs…"),

        // File type associations > Default photo viewer
        new(LangId.FrmSettings_DefaultPhotoViewer, "Default photo viewer"),
        new(LangId.FrmSettings_DefaultPhotoViewer_Description, "Register the supported formats of ImageGlass with Windows. You might need to open the Default apps settings and manually select ImageGlass from the list for it to take effect."),
        new(LangId.FrmSettings_MakeDefault, "Make default"),
        new(LangId.FrmSettings_RemoveDefault, "Remove default"),
        new(LangId.FrmSettings_OpenDefaultAppsSetting, "Open Default apps setting"),

        // File type associations > File formats
        new(LangId.FrmSettings_FileFormats, "File formats"),
        new(LangId.FrmSettings_TotalSupportedFormats, "Total supported formats: {0}"),
        new(LangId.FrmSettings_AddNewFileExtension, "Add new file extension"),

        #endregion // FrmSettings > Tab File type associations


        #region FrmSettings > Tab Tools
        // Tools > Tools
        new(LangId.FrmSettings_Tools_AddNewTool, "Add an external tool"),
        new(LangId.FrmSettings_Tools_EditTool, "Edit external tool"),
        new(LangId.FrmSettings_Tools_Integrated, "Integrated"),
        new(LangId.FrmSettings_Tools_IntegratedWith, "Integrated with {0}"),
        #endregion // FrmSettings > Tab Tools


        #region FrmSettings > Tab Language
        // Language > Language
        new(LangId.FrmSettings_DisplayLanguage, "Display language"),
        new(LangId.FrmSettings_Refresh, "Refresh"),
        new(LangId.FrmSettings_InstallNewLanguagePack, "Install new language packs…"),
        new(LangId.FrmSettings_GetMoreLanguagePacks, "Get more language packs…"),
        new(LangId.FrmSettings_ExportLanguagePack, "Export language pack…"),
        new(LangId.FrmSettings_Contributors, "Contributors"),
        #endregion // FrmSettings > Tab Language


        #region FrmSettings > Tab Appearance
        // Appearance > Appearance
        new(LangId.FrmSettings_WindowBackdrop, "Window backdrop"),
        new(LangId.FrmSettings_BackgroundColor, "Viewer background color"),

        // Appearance > Theme
        new(LangId.FrmSettings_Theme, "Theme"),
        new(LangId.FrmSettings_DarkTheme, "Dark"),
        new(LangId.FrmSettings_LightTheme, "Light"),
        new(LangId.FrmSettings_Author, "Author"),
        new(LangId.FrmSettings_Theme_OpenThemeFolder, "Open theme folder"),
        new(LangId.FrmSettings_Theme_GetMoreThemes, "Get more theme packs…"),
        new(LangId.FrmSettings_Theme_InstallTheme, "Install theme packs"),
        new(LangId.FrmSettings_Theme_UninstallTheme, "Uninstall a theme pack"),

        new(LangId.FrmSettings_UseThemeForDarkMode, "Use this theme for dark mode"),
        new(LangId.FrmSettings_UseThemeForLightMode, "Use this theme for light mode"),
        #endregion // FrmSettings > Tab Appearance

        #endregion // FrmSettings
        

        #region FrmCrop
        new(LangId.FrmCrop_LblAspectRatio, "Aspect ratio:"), //v9.0
        new(LangId.FrmCrop_LblLocation, "Location:"), //v9.0
        new(LangId.FrmCrop_LblSize, "Size:"), //v9.0

        new(LangId.FrmCrop_SelectionAspectRatio_FreeRatio, "Free ratio"), //v9.0
        new(LangId.FrmCrop_SelectionAspectRatio_Custom, "Custom…"), //v9.0
        new(LangId.FrmCrop_SelectionAspectRatio_Original, "Original"), //v9.0

        new(LangId.FrmCrop_BtnQuickSelect_Tooltip, "Quick select…"), //v9.0
        new(LangId.FrmCrop_BtnReset_Tooltip, "Reset selection"), //v9.0
        new(LangId.FrmCrop_BtnSettings_Tooltip, "Open Crop tool settings"), //v9.0

        new(LangId.FrmCrop_BtnSave, "Save"), //v9.0
        new(LangId.FrmCrop_BtnSave_Tooltip, "Save image"), //v9.0
        new(LangId.FrmCrop_BtnSaveAs, "Save as…"), //v9.0
        new(LangId.FrmCrop_BtnSaveAs_Tooltip, "Save as a copy…"), //v9.0
        new(LangId.FrmCrop_BtnCrop, "Crop"), //v9.0
        new(LangId.FrmCrop_BtnCrop_Tooltip, "Crop the image only"), //v9.0
        new(LangId.FrmCrop_BtnCopy, "Copy"), //v9.0
        new(LangId.FrmCrop_BtnCopy_Tooltip, "Copy the selection to clipboard"), //v9.0

        // Crop settings
        new(LangId.FrmCropSettings_Title, "Crop settings"), //v9.0
        new(LangId.FrmCropSettings_ChkCloseToolAfterSaving, "Close Crop tool after saving"), //v9.0
        new(LangId.FrmCropSettings_LblDefaultSelection, "Default selection"), //v9.0
        new(LangId.FrmCropSettings_ChkAutoCenterSelection, "Auto-center selection"), //v9.0

        new(LangId.FrmCropSettings_DefaultSelectionType_UseTheLastSelection, "Use the last selection"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_SelectNone, "Select none"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_SelectX, "Select {0}"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_SelectAll, "Select all"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_CustomArea, "Custom area…"), //v9.0

        #endregion // FrmCrop


        #region FrmColorPicker

        new(LangId.FrmColorPicker_BtnSettings_Tooltip, "Open Color picker settings…"), //v9.0

        // Color picker settings
        new(LangId.FrmColorPickerSettings_Title, "Color picker settings"), //v9.0
        new(LangId.FrmColorPickerSettings_ChkShowRgbA, "Use RGB format with alpha value"), //v5.0
        new(LangId.FrmColorPickerSettings_ChkShowHexA, "Use HEX format with alpha value"), //v5.0
        new(LangId.FrmColorPickerSettings_ChkShowHslA, "Use HSL format with alpha value"), //v5.0
        new(LangId.FrmColorPickerSettings_ChkShowHsvA, "Use HSV format with alpha value"), //v8.0
        new(LangId.FrmColorPickerSettings_ChkShowCIELabA, "Use CIELAB format with alpha value"), //v9.0

        #endregion // FrmColorPicker


        #region FrmToolNotFound
        new(LangId.FrmToolNotFound_Title, "Tool not found" ), // v9.0
        new(LangId.FrmToolNotFound_BtnSelectExecutable, "Select…" ), // v9.0
        new(LangId.FrmToolNotFound_LblHeading, "'{0}' is not found!" ), // v9.0
        new(LangId.FrmToolNotFound_LblDescription, "ImageGlass was unable to locate the path to the '{0}' executable. To resolve this issue, please update the path to the '{0}' as necessary." ), // v9.0
        new(LangId.FrmToolNotFound_LblDownloadToolText, "You can download more tools for ImageGlass at:" ), // v9.0
        #endregion // FrmToolNotFound


        #region FrmHotkeyPicker
        new(LangId.FrmHotkeyPicker_LblHotkey, "Press hotkeys" ), // v9.0
        #endregion // FrmHotkeyPicker


        #region FrmResize
        new(LangId.FrmResize_RadResizeByPixels, "Pixels" ), // v9.2
        new(LangId.FrmResize_RadResizeByPercentage, "Percentage" ), // v9.2
        new(LangId.FrmResize_ChkKeepRatio, "Keep ratio propotional" ), // v9.2
        new(LangId.FrmResize_LblResample, "Resample:" ), // v9.2
        new(LangId.FrmResize_LblCurrentSize, "Current Size:" ), // v9.2
        new(LangId.FrmResize_LblNewSize, "New Size:" ), // v9.2
        #endregion // FrmResize

        
        #region igcmd.exe

        new(LangId._IgCommandExe_DefaultError_Heading, "Invalid commands" ), //v9.0
        new(LangId._IgCommandExe_DefaultError_Description, "Make sure you pass the correct commands!\r\nThis executable file contains command-line functions for ImageGlass software.\r\n\r\nTo explore all command lines, please visit:\r\n{0}" ), //v9.0


        #region FrmSlideshow

        new(LangId.FrmSlideshow_PauseSlideshow, "Slideshow is paused." ), // v9.0
        new(LangId.FrmSlideshow_ResumeSlideshow, "Slideshow is resumed." ), // v9.0

        // menu
        new(LangId.FrmSlideshow_MnuPauseResumeSlideshow, "Pause/resume slideshow" ), // v9.0
        new(LangId.FrmSlideshow_MnuExitSlideshow, "Exit slideshow" ), // v9.0

        new(LangId.FrmSlideshow_MnuToggleCountdown, "Show slideshow countdown" ), // v9.0
        new(LangId.FrmSlideshow_MnuZoomModes, "Zoom modes" ), // v9.0

        #endregion // FrmSlideshow


        #region FrmExportFrames
        new(LangId.FrmExportFrames_Title, "Export image frames" ), //v9.0
        new(LangId.FrmExportFrames_FileNotExist, "Image file does not exist" ), //v7.5
        new(LangId.FrmExportFrames_FolderPickerTitle, "Select output folder for exporting image frames" ), //v9.0
        new(LangId.FrmExportFrames_Exporting, "Exporting {0}/{1} frames \r\n{2}…" ), //v9.0
        new(LangId.FrmExportFrames_ExportDone, "Exported {0} frames successfully to \r\n{1}" ), //v9.0
        new(LangId.FrmExportFrames_OpenOutputFolder, "Open output folder" ), //v9.0
        #endregion // FrmExportFrames


        #region FrmUpdate
        new(LangId.FrmUpdate_StatusChecking, "Checking for update…" ), //v9.0
        new(LangId.FrmUpdate_StatusUpdated, "You are using the latest version!" ), //v9.0
        new(LangId.FrmUpdate_StatusOutdated, "A new update is available!" ), //v9.0
        new(LangId.FrmUpdate_CurrentVersion, "Current version: {0}" ), //v9.0
        new(LangId.FrmUpdate_LatestVersion, "The latest version: {0}" ), //v9.0
        new(LangId.FrmUpdate_PublishedDate, "Published date: {0}" ), //v9.0
        #endregion // FrmUpdate


        #region FrmQuickSetup

        new(LangId.FrmQuickSetup_Text, "ImageGlass Quick Setup" ), //v9.0
        new(LangId.FrmQuickSetup_StepInfo, "Step {0}" ), //v9.0
        new(LangId.FrmQuickSetup_SkipQuickSetup, "Skip this and launch ImageGlass" ), //v9.0

        new(LangId.FrmQuickSetup_SeeWhatNew, "See what's new in this version…" ), // v9.0
        new(LangId.FrmQuickSetup_SelectProfile, "Select a profile" ), //v9.0
        new(LangId.FrmQuickSetup_StandardUser, "Standard user" ), //v9.0
        new(LangId.FrmQuickSetup_ProfessionalUser, "Professional user" ), //v9.0
        new(LangId.FrmQuickSetup_SettingProfileDescription, "To modify these settings, simply access app settings." ), // v9.0

        new(LangId.FrmQuickSetup_SettingsWillBeApplied, "Settings will be applied:" ), //v9.0
        new(LangId.FrmQuickSetup_SetDefaultViewer, "Do you want to set ImageGlass as the default photo viewer?" ), //v9.0
        new(LangId.FrmQuickSetup_SetDefaultViewer_Description, "You can reset it in the app settings > File type associations tab." ), //v9.0

        new(LangId.FrmQuickSetup_ConfirmCloseProcess, "Before applying the new settings, it's essential to close all ImageGlass processes. Are you ready to proceed?" ), //v7.5

        #endregion // FrmQuickSetup

        #endregion // igcmd.exe

    ];

}



