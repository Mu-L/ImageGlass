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
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Vortice.WIC;

namespace ImageGlass.Common.Photoing;

public partial class WicCodecInfo : DisposableImpl
{
    public required IWICComponentInfo ComponentInfo { get; init; }
    public required ComponentType ComponentType { get; init; }
    public required Guid CLSID { get; init; }
    public required Guid ContainerFormat { get; init; }
    public required string Name { get; init; }
    public required ReadOnlyCollection<string> Extensions { get; init; }

    public required bool SupportsAnimation { get; init; }
    public required bool SupportsMultiframes { get; init; }
    public required bool SupportsLossless { get; init; }


    protected override void OnDisposing()
    {
        base.OnDisposing();

        ComponentInfo.Dispose();
    }



    /// <summary>
    /// Create a new instance from WIC Decoder.
    /// </summary>
    public static unsafe WicCodecInfo FromWICComponent(IWICBitmapDecoderInfo info)
    {
        var name = string.Empty;
        var extensions = Enumerable.Empty<string>();

        // 1. get friendly name
        var nameLength = info.GetFriendlyName(0, 0); // get required length of string
        if (nameLength > 0)
        {
            Span<char> buffer = new char[nameLength];
            fixed (char* pBuffer = buffer)
            {
                _ = info.GetFriendlyName(nameLength, (nint)pBuffer);
            }
            name = new string(buffer).TrimEnd('\0');
        }


        // 2. get supported extensions
        var extStrLength = info.GetFileExtensions(0, 0); // get required length of string
        if (extStrLength > 0)
        {
            Span<char> buffer = new char[extStrLength];
            fixed (char* pBuffer = buffer)
            {
                _ = info.GetFileExtensions(extStrLength, (nint)pBuffer);
            }

            var extStr = new string(buffer).TrimEnd('\0');
            extensions = extStr
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.ToLowerInvariant());
        }


        // 3. other information
        var supportAnimation = info.DoesSupportAnimation();
        var supportsMultiframes = info.DoesSupportMultiframe();
        var supportsLossless = info.DoesSupportLossless();


        return new WicCodecInfo()
        {
            ComponentInfo = info,
            ComponentType = info.ComponentType,
            CLSID = info.CLSID,
            ContainerFormat = info.ContainerFormat,
            Name = name,
            Extensions = extensions.ToList().AsReadOnly(),
            SupportsAnimation = supportAnimation,
            SupportsMultiframes = supportsMultiframes,
            SupportsLossless = supportsLossless,
        };
    }


    /// <summary>
    /// Create a new instance from WIC Encoder.
    /// </summary>
    public static unsafe WicCodecInfo FromWICComponent(IWICBitmapEncoderInfo info)
    {
        var name = string.Empty;
        var extensions = Enumerable.Empty<string>();

        // 1. get friendly name
        var nameLength = info.GetFriendlyName(0, 0); // get required length of string
        if (nameLength > 0)
        {
            Span<char> buffer = new char[nameLength];
            fixed (char* pBuffer = buffer)
            {
                _ = info.GetFriendlyName(nameLength, (nint)pBuffer);
            }
            name = new string(buffer).TrimEnd('\0');
        }


        // 2. get supported extensions
        var extStrLength = info.GetFileExtensions(0, 0); // get required length of string
        if (extStrLength > 0)
        {
            Span<char> buffer = new char[extStrLength];
            fixed (char* pBuffer = buffer)
            {
                _ = info.GetFileExtensions(extStrLength, (nint)pBuffer);
            }

            var extStr = new string(buffer).TrimEnd('\0');
            extensions = extStr
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.ToLowerInvariant());
        }


        // 3. other information
        var supportAnimation = info.DoesSupportAnimation();
        var supportsMultiframes = info.DoesSupportMultiframe();
        var supportsLossless = info.DoesSupportLossless();


        return new WicCodecInfo()
        {
            ComponentInfo = info,
            ComponentType = info.ComponentType,
            CLSID = info.CLSID,
            ContainerFormat = info.ContainerFormat,
            Name = name,
            Extensions = extensions.ToList().AsReadOnly(),
            SupportsAnimation = supportAnimation,
            SupportsMultiframes = supportsMultiframes,
            SupportsLossless = supportsLossless,
        };
    }

}