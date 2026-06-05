/*
ImageGlass - A Fast, Seamless Photo Viewer
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
using System;
using System.Runtime.InteropServices;

namespace ImageGlass.Mac.Common.Sandbox;


/// <summary>
/// macOS App Sandbox helpers: detection plus security-scoped bookmark interop.
/// <para>
/// Under the App Store sandbox the process only has access to files/folders the
/// user explicitly grants (via Launch Services "Open With", drag-drop, or an
/// <c>NSOpenPanel</c>). Those grants are path-based and last for the process
/// lifetime, but they are NOT persisted across launches. To browse sibling files
/// in a folder on later launches without re-prompting, we create a
/// <i>security-scoped bookmark</i> for the folder and resolve it on startup,
/// calling <c>startAccessingSecurityScopedResource</c> so plain
/// <c>System.IO</c> directory enumeration keeps working.
/// </para>
/// <para>
/// All Objective-C calls follow the same AOT-safe <c>libobjc</c> P/Invoke pattern
/// used by <c>MacShellProvider</c>. Every entry point is defensive: any failure
/// returns null / false so the caller can fall back to re-prompting.
/// </para>
/// </summary>
internal static unsafe partial class MacSandbox
{
    // NSURLBookmarkCreationWithSecurityScope   = 1 << 11
    private const ulong BOOKMARK_CREATE_SECURITY_SCOPE = 1UL << 11;
    // NSURLBookmarkResolutionWithSecurityScope = 1 << 10
    private const ulong BOOKMARK_RESOLVE_SECURITY_SCOPE = 1UL << 10;


    /// <summary>
    /// Gets a value indicating whether the app is running inside the macOS App Sandbox.
    /// The sandbox sets <c>APP_SANDBOX_CONTAINER_ID</c> for the process.
    /// </summary>
    public static bool IsSandboxed { get; } =
        OperatingSystem.IsMacOS()
        && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_SANDBOX_CONTAINER_ID"));


    /// <summary>
    /// Creates a security-scoped bookmark for the given folder path. The process
    /// must currently have access to the path (e.g. just granted via a picker).
    /// </summary>
    /// <returns>The bookmark bytes, or <c>null</c> on failure.</returns>
    public static byte[]? CreateFolderBookmark(string folderPath)
    {
        try
        {
            var url = FileUrl(folderPath, isDirectory: true);
            if (url == 0) return null;

            // [url bookmarkDataWithOptions:NSURLBookmarkCreationWithSecurityScope
            //   includingResourceValuesForKeys:nil relativeToURL:nil error:NULL]
            var data = objc_msgSend_bookmarkCreate(url, _selBookmarkCreate.Value,
                (nuint)BOOKMARK_CREATE_SECURITY_SCOPE, 0, 0, 0);
            if (data == 0) return null;

            var len = (long)objc_msgSend_ret(data, _selLength.Value);
            if (len <= 0) return null;

            var bytesPtr = objc_msgSend(data, _selBytes.Value);
            if (bytesPtr == 0) return null;

            var buffer = new byte[len];
            Marshal.Copy(bytesPtr, buffer, 0, (int)len);
            return buffer;
        }
        catch
        {
            return null;
        }
    }


    /// <summary>
    /// Resolves a security-scoped bookmark and begins accessing it. The returned
    /// handle is a retained <c>NSURL</c> that MUST be passed to
    /// <see cref="StopAccess"/> when access is no longer needed.
    /// </summary>
    /// <returns>
    /// A tuple of the resolved folder path and an opaque access handle. The handle
    /// is <c>0</c> (and path <c>null</c>) on failure.
    /// </returns>
    public static (string? Path, nint Handle, bool IsStale) ResolveAndStartAccess(byte[] bookmark)
    {
        try
        {
            nint data;
            fixed (byte* p = bookmark)
            {
                // [NSData dataWithBytes:length:]
                data = objc_msgSend_dataWithBytes(_clsNSData.Value, _selDataWithBytes.Value,
                    (nint)p, (nuint)bookmark.Length);
            }
            if (data == 0) return (null, 0, false);

            byte stale = 0;
            // [NSURL URLByResolvingBookmarkData:options:relativeToURL:
            //   bookmarkDataIsStale:&stale error:NULL]
            var url = objc_msgSend_resolveBookmark(_clsNSURL.Value, _selResolveBookmark.Value,
                data, (nuint)BOOKMARK_RESOLVE_SECURITY_SCOPE, 0, (nint)(&stale), 0);
            if (url == 0) return (null, 0, false);

            // retain so the autoreleased NSURL survives past this run-loop turn
            url = objc_msgSend(url, _selRetain.Value);

            var ok = objc_msgSend_bool(url, _selStartAccessing.Value);
            if (!ok)
            {
                objc_msgSend_void(url, _selRelease.Value);
                return (null, 0, false);
            }

            var path = NSStringToManaged(objc_msgSend(url, _selPath.Value));
            return (path, url, stale != 0);
        }
        catch
        {
            return (null, 0, false);
        }
    }


    /// <summary>
    /// Stops accessing a previously started security-scoped resource and releases
    /// the handle returned by <see cref="ResolveAndStartAccess"/>.
    /// </summary>
    public static void StopAccess(nint handle)
    {
        if (handle == 0) return;
        try
        {
            objc_msgSend_void(handle, _selStopAccessing.Value);
            objc_msgSend_void(handle, _selRelease.Value);
        }
        catch { }
    }


    #region ObjC helpers

    /// <summary>
    /// Builds a file <c>NSURL</c> from a managed path string.
    /// </summary>
    private static nint FileUrl(string path, bool isDirectory)
    {
        var nsPath = ManagedToNSString(path);
        if (nsPath == 0) return 0;

        // [NSURL fileURLWithPath:isDirectory:]
        return objc_msgSend_fileUrl(_clsNSURL.Value, _selFileUrl.Value, nsPath, isDirectory);
    }


    /// <summary>
    /// Creates an autoreleased <c>NSString</c> from a managed string.
    /// </summary>
    private static nint ManagedToNSString(string s)
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(s + "\0");
        fixed (byte* p = utf8)
        {
            return objc_msgSend(_clsNSString.Value, _selStringWithUtf8.Value, (nint)p);
        }
    }


    /// <summary>
    /// Converts an <c>NSString</c> to a managed string (returns <c>null</c> if nil).
    /// </summary>
    private static string? NSStringToManaged(nint nsString)
    {
        if (nsString == 0) return null;
        var utf8 = objc_msgSend(nsString, _selUtf8String.Value);
        return utf8 == 0 ? null : Marshal.PtrToStringUTF8(utf8);
    }

    #endregion // ObjC helpers



    #region ObjC runtime interop

    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend(nint receiver, nint selector);

    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend(nint receiver, nint selector, nint arg1);

    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial void objc_msgSend_void(nint receiver, nint selector);

    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend_ret(nint receiver, nint selector);

    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool objc_msgSend_bool(nint receiver, nint selector);

    // [NSURL fileURLWithPath:(NSString*) isDirectory:(BOOL)]
    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend_fileUrl(nint receiver, nint selector,
        nint path, [MarshalAs(UnmanagedType.Bool)] bool isDirectory);

    // [url bookmarkDataWithOptions:(NSUInteger) includingResourceValuesForKeys:(id) relativeToURL:(id) error:(id*)]
    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend_bookmarkCreate(nint receiver, nint selector,
        nuint options, nint keys, nint relativeUrl, nint error);

    // [NSData dataWithBytes:(void*) length:(NSUInteger)]
    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend_dataWithBytes(nint receiver, nint selector,
        nint bytes, nuint length);

    // [NSURL URLByResolvingBookmarkData:(NSData*) options:(NSUInteger) relativeToURL:(id) bookmarkDataIsStale:(BOOL*) error:(id*)]
    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend_resolveBookmark(nint receiver, nint selector,
        nint data, nuint options, nint relativeUrl, nint isStale, nint error);

    [LibraryImport("/usr/lib/libobjc.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint sel_registerName(string name);

    [LibraryImport("/usr/lib/libobjc.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_getClass(string name);


    private static readonly Lazy<nint> _clsNSURL = new(() => objc_getClass("NSURL"));
    private static readonly Lazy<nint> _clsNSString = new(() => objc_getClass("NSString"));
    private static readonly Lazy<nint> _clsNSData = new(() => objc_getClass("NSData"));

    private static readonly Lazy<nint> _selFileUrl = new(() => sel_registerName("fileURLWithPath:isDirectory:"));
    private static readonly Lazy<nint> _selBookmarkCreate = new(() => sel_registerName("bookmarkDataWithOptions:includingResourceValuesForKeys:relativeToURL:error:"));
    private static readonly Lazy<nint> _selResolveBookmark = new(() => sel_registerName("URLByResolvingBookmarkData:options:relativeToURL:bookmarkDataIsStale:error:"));
    private static readonly Lazy<nint> _selStartAccessing = new(() => sel_registerName("startAccessingSecurityScopedResource"));
    private static readonly Lazy<nint> _selStopAccessing = new(() => sel_registerName("stopAccessingSecurityScopedResource"));
    private static readonly Lazy<nint> _selPath = new(() => sel_registerName("path"));
    private static readonly Lazy<nint> _selBytes = new(() => sel_registerName("bytes"));
    private static readonly Lazy<nint> _selLength = new(() => sel_registerName("length"));
    private static readonly Lazy<nint> _selDataWithBytes = new(() => sel_registerName("dataWithBytes:length:"));
    private static readonly Lazy<nint> _selStringWithUtf8 = new(() => sel_registerName("stringWithUTF8String:"));
    private static readonly Lazy<nint> _selUtf8String = new(() => sel_registerName("UTF8String"));
    private static readonly Lazy<nint> _selRetain = new(() => sel_registerName("retain"));
    private static readonly Lazy<nint> _selRelease = new(() => sel_registerName("release"));

    #endregion // ObjC runtime interop

}
