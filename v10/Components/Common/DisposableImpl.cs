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
using System.ComponentModel;

namespace ImageGlass.Common;


/// <summary>
/// Provides a base implementation of <see cref="IDisposable"/>
/// and <see cref="INotifyPropertyChanged"/> interface,
/// including support for managed and unmanaged resource cleanup.
/// </summary>
public partial class DisposableImpl : IgReactive, IDisposable
{
    #region IDisposable Disposing

    /// <summary>
    /// Gets a value indicating whether the object has been disposed.
    /// </summary>
    public bool IsDisposed { get; protected set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Free any other managed objects here.
            OnDisposing();

            // remove PropertyChanged events
            CleanUpPropertyChangedEvents();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DisposableImpl()
    {
        Dispose(false);
    }

    #endregion


    /// <summary>
    /// Releases the managed objects.
    /// </summary>
    protected virtual void OnDisposing()
    {
        //
    }

}
