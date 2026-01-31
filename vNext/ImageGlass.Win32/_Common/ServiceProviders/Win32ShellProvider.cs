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
using D2Phap;
using ImageGlass.Common;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using System;
using System.IO;

namespace ImageGlass.Win32.Common.ServiceProviders;

public class Win32ShellProvider : DisposableImpl, IShellProvider
{
    private static ExplorerView? _foregroundShell = null;
    private static string _foregroundShellPath = string.Empty;

    private static string Win32SearchFileExtension => ".search-ms";



    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public IDisposable? ForegroundShell
    {
        get => _foregroundShell;
        set
        {
            _foregroundShell?.Dispose();
            _foregroundShell = (ExplorerView?)value;

            try
            {
                _foregroundShellPath = _foregroundShell?.GetTabViewPath() ?? string.Empty;
                Core.UpdateInitImagePath();
            }
            catch
            {
                _foregroundShellPath = string.Empty;
                _foregroundShell?.Dispose();
                _foregroundShell = null;
            }
        }
    }



    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();
        ForegroundShell = null;
    }



    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool CanUseForegroundShell()
    {
        // check if we should load images from foreground window
        var inputImageDirPath = Path.GetDirectoryName(Core.InputImagePathFromArgs) ?? string.Empty;
        var isFromSearchWindow = _foregroundShellPath.StartsWith(EggShell.SEARCH_MS_PROTOCOL, StringComparison.OrdinalIgnoreCase);
        var isFromSavedSearch = _foregroundShellPath.EndsWith(Win32SearchFileExtension, StringComparison.OrdinalIgnoreCase);
        var isFromSameDir = inputImageDirPath.Equals(_foregroundShellPath, StringComparison.OrdinalIgnoreCase);

        var useForegroundWindow = _foregroundShell is not null
            && !string.IsNullOrEmpty(Core.InputImagePathFromArgs)
            && (isFromSearchWindow || isFromSavedSearch || isFromSameDir);

        return useForegroundWindow;
    }


}
