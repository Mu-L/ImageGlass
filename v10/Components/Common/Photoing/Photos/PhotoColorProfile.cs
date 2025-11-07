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

using Vortice.WIC;

namespace ImageGlass.Common.Photoing;


public partial class PhotoColorProfile(PhotoColorSpace colorSpace, byte[]? data, IWICColorContext? context = null) : DisposableImpl
{
    public PhotoColorSpace ColorSpace { get; private set; } = colorSpace;


    public IWICColorContext? ColorContext { get; private set; } = context;


    public byte[]? ProfileData { get; private set; } = data;


    public PhotoColorProfile() : this(PhotoColorSpace.Unknown, null) { }



    protected override void OnDisposing()
    {
        base.OnDisposing();

        ColorContext?.Dispose();
        ColorContext = null;
        ColorSpace = PhotoColorSpace.None;
    }



    public new string ToString()
    {
        return $"{ColorSpace} ({ProfileData?.Length} bytes)";
    }



    /// <summary>
    /// Gets Description tag of ICC profile.
    /// </summary>
    public string GetIccDescription()
    {
        if (ProfileData is null) return string.Empty;

        // ICC profiles contain a "desc" tag with a text description.
        // It starts with 4 bytes tag count, then entries.
        try
        {
            // number of tags is a big-endian 32-bit integer at offset 128
            var tagCount = ReadBE32__(ProfileData, 128);

            // each tag record = 12 bytes: [4-byte tag sig][4-byte offset][4-byte size]
            for (int i = 132; i < 132 + tagCount * 12; i += 12)
            {
                string tag = System.Text.Encoding.ASCII.GetString(ProfileData, i, 4);
                int offset = ReadBE32__(ProfileData, i + 4);
                int size = ReadBE32__(ProfileData, i + 8);

                if (tag == "desc")
                {
                    // ASCII description
                    int asciiLength = ReadBE32__(ProfileData, offset + 8);
                    return System.Text.Encoding.ASCII.GetString(ProfileData, offset + 12, asciiLength - 1);
                }
                else if (tag == "mluc")
                {
                    // unicode (UTF-16BE) localized description
                    int recordCount = ReadBE32__(ProfileData, offset + 8);
                    int recordSize = ReadBE32__(ProfileData, offset + 12);

                    if (recordCount > 0)
                    {
                        // first record (usually English)
                        int firstRecord = offset + 16;
                        int stringLength = ReadBE32__(ProfileData, firstRecord + 8);
                        int stringOffset = ReadBE32__(ProfileData, firstRecord + 12);

                        return System.Text.Encoding.BigEndianUnicode.GetString(
                            ProfileData,
                            offset + stringOffset,
                            stringLength
                        );
                    }
                }
            }
        }
        catch { }

        return string.Empty;
    }


    private static int ReadBE32__(byte[] data, int index)
    {
        // Manually convert 4 bytes from big-endian to host order
        return (data[index] << 24)
             | (data[index + 1] << 16)
             | (data[index + 2] << 8)
             | (data[index + 3]);
    }
}


public enum PhotoColorSpace
{
    None = 0,
    sRGB = 1,
    AdobeRGB = 2,
    Uncalibrated = 3,
    Unknown = 4,
}
