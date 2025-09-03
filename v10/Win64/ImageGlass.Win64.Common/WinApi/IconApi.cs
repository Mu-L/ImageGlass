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
using ImageGlass.Win64.Common.Photoing;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
namespace ImageGlass.Win64.Common;

public static class IconApi
{
    private const uint WM_SETICON = 0x80u;
    private const int ICON_BIG = 1;


    /// <summary>
    /// Sets taskbar icon.
    /// </summary>
    public static void SetTaskbarIcon(IntPtr windowHandle, IntPtr hIcon)
    {
        _ = PInvoke.SendMessage(new HWND(windowHandle), WM_SETICON, ICON_BIG, hIcon);
    }


    /// <summary>
    /// Creates HICON from pixel byte array.
    /// </summary>
    public static unsafe IntPtr CreateHIcon(byte[] colorBytes, int width, int height)
    {
        var hbmColor = new HBITMAP(IntPtr.Zero);
        var hbmMask = new HBITMAP(IntPtr.Zero);
        var hIcon = IntPtr.Zero;

        try
        {
            // create color and mask bitmaps
            fixed (byte* pBytes = colorBytes)
            {
                hbmColor = PInvoke.CreateBitmap(width, height, 1, 32, pBytes); // Assuming 32-bit color
                hbmMask = PInvoke.CreateBitmap(width, height, 1, 1, pBytes);   // Monochrome mask
            }

            if (hbmColor == IntPtr.Zero || hbmMask == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var iconInfo = new ICONINFO()
            {
                fIcon = true, // This is an icon
                xHotspot = 0,
                yHotspot = 0,
                hbmColor = hbmColor,
                hbmMask = hbmMask,
            };

            // create icon
            var iconObj = PInvoke.CreateIconIndirect(in iconInfo);

            // get icon handle
            hIcon = iconObj.DangerousGetHandle();

            return hIcon;
        }
        finally
        {
            // clean up GDI objects
            if (hbmColor != IntPtr.Zero) PInvoke.DeleteObject(hbmColor);
            if (hbmMask != IntPtr.Zero) PInvoke.DeleteObject(hbmMask);
        }
    }


    /// <summary>
    /// Destroys HICON.
    /// </summary>
    public static void DestroyHIcon(nint hIcon)
    {
        if (hIcon != IntPtr.Zero)
        {
            _ = PInvoke.DeleteObject(new HGDIOBJ(hIcon));
            hIcon = IntPtr.Zero;
        }
    }


    /// <summary>
    /// Gets the system icon.
    /// </summary>
    public static nint GetSystemHIcon(ShellStockIcon? iconType, bool useLargeIcon = true)
    {
        if (iconType == null) return IntPtr.Zero;


        var iconRes = new SHSTOCKICONINFO();
        iconRes.cbSize = (uint)Marshal.SizeOf(iconRes);

        var size = SHGSI_FLAGS.SHGSI_ICON;
        if (useLargeIcon)
        {
            size |= SHGSI_FLAGS.SHGSI_LARGEICON;
        }
        else
        {
            size |= SHGSI_FLAGS.SHGSI_SMALLICON;
        }

        try
        {
            _ = PInvoke.SHGetStockIconInfo((SHSTOCKICONID)iconType.Value, size, ref iconRes);

            return iconRes.hIcon;
        }
        catch { }

        return IntPtr.Zero;
    }


    /// <summary>
    /// Gets system icon.
    /// </summary>
    public static async Task<SoftwareBitmap?> GetSystemIconAsync(ShellStockIcon? iconType, bool useLargeIcon = true)
    {
        var hIcon = IntPtr.Zero;

        try
        {
            hIcon = GetSystemHIcon(iconType, useLargeIcon);
            using var gdiBmp = Icon.FromHandle(hIcon).ToBitmap();

            using var wicBmp = PhotoWIC.ConvertFromGdiBitmap(gdiBmp);
            var sb = await PhotoWIC.ConvertToSoftwareBitmapAsync(wicBmp);

            return sb;
        }
        catch { }
        finally
        {
            DestroyHIcon(hIcon);
        }

        return null;
    }

}


public enum ShellStockIcon : uint
{
    SIID_DOCNOASSOC = 0,          // document (blank page), no associated program
    SIID_DOCASSOC = 1,            // document with an associated program
    SIID_APPLICATION = 2,         // generic application with no custom icon
    SIID_FOLDER = 3,              // folder (closed)
    SIID_FOLDEROPEN = 4,          // folder (open)
    SIID_DRIVE525 = 5,            // 5.25" floppy disk drive
    SIID_DRIVE35 = 6,             // 3.5" floppy disk drive
    SIID_DRIVEREMOVE = 7,         // removable drive
    SIID_DRIVEFIXED = 8,          // fixed (hard disk) drive
    SIID_DRIVENET = 9,            // network drive
    SIID_DRIVENETDISABLED = 10,   // disconnected network drive
    SIID_DRIVECD = 11,            // CD drive
    SIID_DRIVERAM = 12,           // RAM disk drive
    SIID_WORLD = 13,              // entire network
    SIID_SERVER = 15,             // a computer on the network
    SIID_PRINTER = 16,            // printer
    SIID_MYNETWORK = 17,          // My network places
    SIID_FIND = 22,               // Find
    SIID_HELP = 23,               // Help
    SIID_SHARE = 28,              // overlay for shared items
    SIID_LINK = 29,               // overlay for shortcuts to items
    SIID_SLOWFILE = 30,           // overlay for slow items
    SIID_RECYCLER = 31,           // empty recycle bin
    SIID_RECYCLERFULL = 32,       // full recycle bin
    SIID_MEDIACDAUDIO = 40,       // Audio CD Media
    SIID_LOCK = 47,               // Security lock
    SIID_AUTOLIST = 49,           // AutoList
    SIID_PRINTERNET = 50,         // Network printer
    SIID_SERVERSHARE = 51,        // Server share
    SIID_PRINTERFAX = 52,         // Fax printer
    SIID_PRINTERFAXNET = 53,      // Networked Fax Printer
    SIID_PRINTERFILE = 54,        // Print to File
    SIID_STACK = 55,              // Stack
    SIID_MEDIASVCD = 56,          // SVCD Media
    SIID_STUFFEDFOLDER = 57,      // Folder containing other items
    SIID_DRIVEUNKNOWN = 58,       // Unknown drive
    SIID_DRIVEDVD = 59,           // DVD Drive
    SIID_MEDIADVD = 60,           // DVD Media
    SIID_MEDIADVDRAM = 61,        // DVD-RAM Media
    SIID_MEDIADVDRW = 62,         // DVD-RW Media
    SIID_MEDIADVDR = 63,          // DVD-R Media
    SIID_MEDIADVDROM = 64,        // DVD-ROM Media
    SIID_MEDIACDAUDIOPLUS = 65,   // CD+ (Enhanced CD) Media
    SIID_MEDIACDRW = 66,          // CD-RW Media
    SIID_MEDIACDR = 67,           // CD-R Media
    SIID_MEDIACDBURN = 68,        // Burning CD
    SIID_MEDIABLANKCD = 69,       // Blank CD Media
    SIID_MEDIACDROM = 70,         // CD-ROM Media
    SIID_AUDIOFILES = 71,         // Audio files
    SIID_IMAGEFILES = 72,         // Image files
    SIID_VIDEOFILES = 73,         // Video files
    SIID_MIXEDFILES = 74,         // Mixed files
    SIID_FOLDERBACK = 75,         // Folder back
    SIID_FOLDERFRONT = 76,        // Folder front
    SIID_SHIELD = 77,             // Security shield. Use for UAC prompts only.
    SIID_WARNING = 78,            // Warning
    SIID_INFO = 79,               // Informational
    SIID_ERROR = 80,              // Error
    SIID_KEY = 81,                // Key / Secure
    SIID_SOFTWARE = 82,           // Software
    SIID_RENAME = 83,             // Rename
    SIID_DELETE = 84,             // Delete
    SIID_MEDIAAUDIODVD = 85,      // Audio DVD Media
    SIID_MEDIAMOVIEDVD = 86,      // Movie DVD Media
    SIID_MEDIAENHANCEDCD = 87,    // Enhanced CD Media
    SIID_MEDIAENHANCEDDVD = 88,   // Enhanced DVD Media
    SIID_MEDIAHDDVD = 89,         // HD-DVD Media
    SIID_MEDIABLURAY = 90,        // BluRay Media
    SIID_MEDIAVCD = 91,           // VCD Media
    SIID_MEDIADVDPLUSR = 92,      // DVD+R Media
    SIID_MEDIADVDPLUSRW = 93,     // DVD+RW Media
    SIID_DESKTOPPC = 94,          // desktop computer
    SIID_MOBILEPC = 95,           // mobile computer (laptop/notebook)
    SIID_USERS = 96,              // users
    SIID_MEDIASMARTMEDIA = 97,    // Smart Media
    SIID_MEDIACOMPACTFLASH = 98,  // Compact Flash
    SIID_DEVICECELLPHONE = 99,    // Cell phone
    SIID_DEVICECAMERA = 100,      // Camera
    SIID_DEVICEVIDEOCAMERA = 101, // Video camera
    SIID_DEVICEAUDIOPLAYER = 102, // Audio player
    SIID_NETWORKCONNECT = 103,    // Connect to network
    SIID_INTERNET = 104,          // Internet
    SIID_ZIPFILE = 105,           // ZIP file
    SIID_SETTINGS = 106,          // Settings
    // 107-131 are internal Vista RTM icons

    // 132-159 for SP1 icons
    SIID_DRIVEHDDVD = 132,        // HDDVD Drive (all types)
    SIID_DRIVEBD = 133,           // BluRay Drive (all types)
    SIID_MEDIAHDDVDROM = 134,     // HDDVD-ROM Media
    SIID_MEDIAHDDVDR = 135,       // HDDVD-R Media
    SIID_MEDIAHDDVDRAM = 136,     // HDDVD-RAM Media
    SIID_MEDIABDROM = 137,        // BluRay ROM Media
    SIID_MEDIABDR = 138,          // BluRay R Media
    SIID_MEDIABDRE = 139,         // BluRay RE Media (Rewriable and RAM)
    SIID_CLUSTEREDDRIVE = 140,    // Clustered disk

    // 160+ are for Windows 7 icons
    //SIID_MAX_ICONS = 181,
}

