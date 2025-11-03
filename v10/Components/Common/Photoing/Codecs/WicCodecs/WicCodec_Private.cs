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
using SharpGen.Runtime;
using SharpGen.Runtime.Win32;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Vortice.WIC;

namespace ImageGlass.Common.Photoing;


public static partial class WicCodec
{
    private static readonly Lazy<FrozenDictionary<Guid, WicCodecInfo>> _allCodecs = new(GetWicCodecList__, isThreadSafe: true);
    private static readonly Lazy<FrozenSet<string>> _encoderExtensions = new(GetEncoderExtensions__, isThreadSafe: true);
    private static readonly Lazy<FrozenSet<string>> _decoderExtensions = new(GetDecoderExtensions__, isThreadSafe: true);



    #region Methods for WIC Components Listing

    /// <summary>
    /// Gets the dictionary of WIC Codecs.
    /// </summary>
    private static FrozenDictionary<Guid, WicCodecInfo> GetWicCodecList__()
    {
        var dict = new Dictionary<Guid, WicCodecInfo>();
        var components = GetWICComponents__(ComponentType.Decoder | ComponentType.Encoder);

        foreach (var item in components)
        {
            WicCodecInfo? codec = null;
            if (item is IWICBitmapDecoderInfo decoder)
            {
                codec = WicCodecInfo.FromWICComponent(decoder);
            }
            else if (item is IWICBitmapEncoderInfo encoder)
            {
                codec = WicCodecInfo.FromWICComponent(encoder);
            }
            if (codec is null) continue;

            if (!dict.TryAdd(codec.CLSID, codec))
            {
                item.Dispose();
            }
        }

        return dict.ToFrozenDictionary();
    }


    /// <summary>
    /// Gets the list of WIC Components.
    /// </summary>
    private static List<IWICComponentInfo> GetWICComponents__(ComponentType types)
    {
        var list = new List<IWICComponentInfo>();
        IEnumUnknown? enumerator = null;

        try
        {
            enumerator = CreateComponentEnumerator__(types, ComponentEnumerateOptions.Default);
        }
        catch { }
        if (enumerator is null) return list;


        while (true)
        {
            var buffer = new IUnknown[1];
            var fetched = enumerator.Next(buffer);
            if (fetched != 1) break;

            using var unknown = buffer[0];
            if (unknown is not ComObject comObj) continue;

            // convert IUnknown → IWICComponentInfo
            IWICComponentInfo? compInfo = null;
            try
            {
                compInfo = comObj.QueryInterfaceOrNull<IWICComponentInfo>();
            }
            catch { continue; }

            if (compInfo is not null)
            {
                IWICComponentInfo? item = compInfo.ComponentType switch
                {
                    ComponentType.PixelFormat => compInfo.QueryInterfaceOrNull<IWICPixelFormatInfo2>(),
                    ComponentType.Decoder => compInfo.QueryInterfaceOrNull<IWICBitmapDecoderInfo>(),
                    ComponentType.Encoder => compInfo.QueryInterfaceOrNull<IWICBitmapEncoderInfo>(),
                    _ => compInfo,
                };

                if (item is not null)
                {
                    list.Add(item);
                }
            }

            comObj.Dispose();
        }

        return list;
    }


    /// <summary>
    /// Implements API for <c>IWICImagingFactory2.CreateComponentEnumerator();</c>.
    /// <para>
    /// See: <see href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nf-wincodec-iwicimagingfactory-createcomponentenumerator"/>
    /// </para>
    /// </summary>
    private static unsafe IEnumUnknown? CreateComponentEnumerator__(ComponentType type, ComponentEnumerateOptions options)
    {
        using var fac = new IWICImagingFactory2();

        var vtbl = (*(void***)fac.NativePointer)[23];
        var method = (delegate* unmanaged[Stdcall]<nint, uint, uint, nint*, int>)vtbl;
        nint zero = IntPtr.Zero;

        Result result = method(fac.NativePointer, (uint)type, (uint)options, &zero);
        var result2 = (zero != IntPtr.Zero) ? new IEnumUnknown(zero) : null;

        result.CheckError();
        return result2;
    }


    /// <summary>
    /// Gets all file extensions of for decoding.
    /// </summary>
    private static FrozenSet<string> GetDecoderExtensions__()
    {
        return GetCodecExtensions__(ComponentType.Decoder);
    }


    /// <summary>
    /// Gets all file extensions of for encoding.
    /// </summary>
    private static FrozenSet<string> GetEncoderExtensions__()
    {
        return GetCodecExtensions__(ComponentType.Encoder);
    }


    /// <summary>
    /// Gets file extensions of for WIC component type.
    /// </summary>
    private static FrozenSet<string> GetCodecExtensions__(ComponentType type)
    {
        var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in _allCodecs.Value)
        {
            if (item.Value.ComponentType != type) continue;
            foreach (string exts in item.Value.Extensions)
            {
                hashSet.Add(exts);
            }
        }

        return hashSet.ToFrozenSet();
    }

    #endregion // Methods for WIC Components Listing


}
