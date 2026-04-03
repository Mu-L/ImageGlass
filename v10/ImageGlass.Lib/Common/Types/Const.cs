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
using Avalonia.Media;
using System.Globalization;

namespace ImageGlass.Common.Types;


/// <summary>
/// Constants list of the app
/// </summary>
public static class Const
{
    public const int MENU_ICON_HEIGHT = 24;
    public const int TOOLBAR_ICON_HEIGHT = 24;
    public const int THUMBNAIL_HEIGHT = 70;
    public const string CONFIG_CMD_PREFIX = "-p:";
    public const string DATETIME_FORMAT = "yyyy/MM/dd HH:mm:ss";
    public const string DATE_FORMAT = "yyyy/MM/dd";
    public const string APP_PROTOCOL = "imageglass";
    public const string MS_APPSTORE_ID = "9N33VZK3C7TH";

    public static readonly string SIGN_POSITIVE = NumberFormatInfo.CurrentInfo.PositiveSign;
    public static readonly string SIGN_NEGATIVE = NumberFormatInfo.CurrentInfo.NegativeSign;
    public static readonly string DECIMAL_SEPARATOR = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
    public static readonly Color COLOR_EMPTY = new Color(0, 0, 0, 0);
    public static readonly int MOUSE_WHEEL_SCROLL_DELTA = 120;


    /// <summary>
    /// A file macro to replace with the current viewing image file path in double quotes.
    /// Example: <c>"C:\my\photo.jpg"</c>
    /// </summary>
    public const string FILE_MACRO = "<file>";
    public const string THEME_SYSTEM_ACCENT = "accent";

    // predefined built-in tool names
    public const string IGTOOL_EXIFTOOL = "Tool_ExifGlass";


    /// <summary>
    /// Quick setup version constant.
    /// If the value read from config file is less than this value,
    /// the Quick setup dialog will be opened.
    /// </summary>
    public const double QUICK_SETUP_VERSION = 10f;

    /// <summary>
    /// The default theme pack
    /// </summary>
    public const string DEFAULT_THEME = "Kobe";

    /// <summary>
    /// Gets built-in image formats
    /// </summary>
    public const string IMAGE_FORMATS = ".3fr;.apng;.ari;.arw;.avif;.b64;.bay;.bmp;.cap;.cr2;.cr3;.crw;.cur;.cut;.dcr;.dcs;.dds;.dib;.dng;.drf;.eip;.emf;.erf;.exif;.exr;.fff;.fits;.flif;.gif;.gifv;.gpr;.hdp;.hdr;.heic;.heif;.hif;.ico;.iiq;.jfif;.jp2;.jpe;.jpeg;.jpg;.jxl;.jxr;.k25;.kdc;.mdc;.mef;.mjpeg;.mos;.mrw;.nef;.nrw;.obm;.orf;.pbm;.pcx;.pef;.pgm;.png;.ppm;.psb;.psd;.ptx;.pxn;.qoi;.r3d;.raf;.raw;.rw2;.rwl;.rwz;.sr2;.srf;.srw;.svg;.tga;.tif;.tiff;.viff;.wdp;.webp;.wmf;.wpg;.x3f;.xbm;.xpm;.xv";



    public const string FONT_CODE = "Cascadia Code, Consolas, SF Mono, Menlo, Monaco, Courier New, monospace";
    public const double FONT_SIZE_TITLE = 20;
    public const double FONT_SIZE_SUBTITLE = 18;
    public const double FONT_SIZE_SMALL = 13;

}

