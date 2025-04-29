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

namespace ImageGlass.Common.Photoing;


public class PhotoColorProfile(PhotoColorSpace colorSpace, byte[]? data, IDisposable? native = null) : IDisposable
{

    #region IDisposable Disposing

    public bool IsDisposed { get; private set; } = false;


    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            Native?.Dispose();
            Native = null;

            Data = null;
            ColorSpace = PhotoColorSpace.None;
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PhotoColorProfile()
    {
        Dispose(false);
    }

    #endregion


    public PhotoColorSpace ColorSpace { get; private set; } = colorSpace;


    public IDisposable? Native { get; private set; } = native;


    public byte[]? Data { get; private set; } = data;


    public PhotoColorProfile() : this(PhotoColorSpace.Unknown, null) { }


    public new string ToString()
    {
        return $"{ColorSpace} ({Data?.Length} bytes)";
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
