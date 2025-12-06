/*
ImageGlass Project - Image viewer for Windows
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

using Microsoft.Graphics.Display;
using Microsoft.UI;
using System;
using System.Threading.Tasks;

namespace ImageGlass.Common;


/// <summary>
/// Manages the color profile for a window.
/// </summary>
public interface IWindowColorProfileProvider
{
    /// <summary>
    /// Occurs when a physical display's color profile is changed.
    /// </summary>
    event EventHandler? Changed;


    /// <summary>
    /// Represents color profile data in byte array format.
    /// </summary>
    byte[]? Data { get; }


    /// <summary>
    /// Indicates whether the instance has been initialized.
    /// </summary>
    bool IsInitialized { get; }


    /// <summary>
    /// Initializes the color profile setting instance for the given window.
    /// </summary>
    Task InitializeAsync(WindowId windowId);

}


/// <summary>
/// Manages the color profile for a window.
/// </summary>
public partial class WindowColorProfileProvider : DisposableImpl, IWindowColorProfileProvider
{
    private DisplayInformation? _display;


    public event EventHandler? Changed;
    public byte[]? Data { get; private set; }
    public bool IsInitialized { get; private set; } = false;



    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        if (_display != null)
        {
            _display.ColorProfileChanged += DisplayInformation_ColorProfileChanged;
        }

        _display?.Dispose();
        _display = null;

        Data = [];
        IsInitialized = false;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task InitializeAsync(WindowId windowId)
    {
        if (IsInitialized) return;

        try
        {
            _display = DisplayInformation.CreateForWindowId(windowId);
            _display.ColorProfileChanged += DisplayInformation_ColorProfileChanged;

            Data = await LoadColorProfileAsync(_display);
        }
        catch { }

        IsInitialized = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }


    private async void DisplayInformation_ColorProfileChanged(DisplayInformation sender, object args)
    {
        Data = await LoadColorProfileAsync(sender);
        Changed?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Loads a color profile from the given display.
    /// </summary>
    private static async Task<byte[]?> LoadColorProfileAsync(DisplayInformation display)
    {
        byte[]? data = null;

        try
        {
            var stream = await display.GetColorProfileAsync().AsTask().ConfigureAwait(false);
            if (stream == null) return null;

            data = await stream.ReadBytesAsync().ConfigureAwait(false);
        }
        catch { }

        return data;
    }

}



