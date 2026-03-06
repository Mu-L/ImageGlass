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
using ImageGlass.Common;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ImageGlass.Linux.Common.ServiceProviders;

internal class LinuxPrintProvider : IPrintProvider
{
    private readonly HashSet<string> _nativeLinuxFormats = [".bmp", ".jpg", ".jpeg", ".png", ".gif", ".tif", ".tiff"];


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task OpenPrintAsync(string filePath, PhotoMetadata? meta, bool isClipboardFile)
    {
        var fileToPrint = filePath;
        var srcFilePath = meta?.FilePath ?? string.Empty;
        var ext = meta?.FileExtension.ToLowerInvariant() ?? string.Empty;
        var isNativeSingleFrameFormat = meta?.FrameCount == 1 && !_nativeLinuxFormats.Contains(ext);


        // print clipboard image
        if (Core.ClipboardImage != null || isNativeSingleFrameFormat)
        {
            // save image to temp file
            fileToPrint = await Core.SavePhotoAsTempFileAsync();
        }
        // rename ext FAX -> TIFF to multi-frame printing
        else if (ext.Equals(".fax", StringComparison.OrdinalIgnoreCase))
        {
            var tempDir = BHelper.ConfigDir(Dir.Temporary);
            Directory.CreateDirectory(tempDir);

            fileToPrint = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(srcFilePath) + ".tiff");
            File.Copy(srcFilePath, fileToPrint, true);
        }
        else if (meta?.FrameCount > 1
            && !ext.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            && !ext.Equals(".tif", StringComparison.OrdinalIgnoreCase)
            && !ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase))
        {
            // save image to temp file
            fileToPrint = await Core.SavePhotoAsTempFileAsync();
        }

        if (string.IsNullOrEmpty(fileToPrint)) return;


        // print via CUPS lp command, scaling the image to fit the page
        using var proc = new Process();
        proc.StartInfo.FileName = "lp";
        proc.StartInfo.Arguments = $"-o fit-to-page -- \"{fileToPrint}\"";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.CreateNoWindow = true;
        proc.Start();
    }
}
