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
using ImageGlass.Common;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Types;
using ImageGlass.UI.Viewer;
using ImageGlass.UI.Viewer.ZoomAndPan;
using System;
using System.ComponentModel;
using System.Text;

namespace ImageGlass.Win32.Common;

public partial class AppStatusInfo : DisposableImpl
{
    private ViewerControl _viewer;
    private string? _filePath = null;

    public event EventHandler? Changed;


    #region Image Info Tags

    private string? AppName
    {
        get
        {
            if (Core.Config.ImageInfoTags.Contains(nameof(AppName)))
            {
                return BHelper.AppName;
            }

            return null;
        }
    }


    private string? Name
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(Name)))
            {
                return System.IO.Path.GetFileName(_filePath);
            }

            return null;
        }
    }


    private string? Path
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(Path)))
            {
                return _filePath;
            }

            return null;
        }
    }


    private string? FileSize
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(FileSize))
                && CoreWin32.Photos.CurrentMetadata != null)
            {
                return CoreWin32.Photos.CurrentMetadata.FileSizeFormatted;
            }

            return null;
        }
    }


    private string? ModifiedDateTime
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if ((Core.Config.ImageInfoTags.Contains(nameof(ModifiedDateTime))
                || Core.Config.ImageInfoTags.Contains(nameof(DateTimeAuto)))
                && CoreWin32.Photos.CurrentMetadata != null)
            {
                return CoreWin32.Photos.CurrentMetadata.FileLastWriteTimeFormatted + " (m)";
            }

            return null;
        }
    }


    private string? Dimension
    {
        get
        {
            if (Core.Config.ImageInfoTags.Contains(nameof(Dimension)))
            {
                if (Core.ClipboardImage is not null)
                {
                    return $"{Core.ClipboardImage.Width:n0}×{Core.ClipboardImage.Height:n0}";
                }
                else if (CoreWin32.Photos.CurrentMetadata is not null)
                {
                    return $"{CoreWin32.Photos.CurrentMetadata.Width:n0}×{CoreWin32.Photos.CurrentMetadata.Height:n0}";
                }
            }

            return null;
        }
    }


    private string? FrameCount
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(FrameCount))
                && CoreWin32.Photos.CurrentMetadata != null
                && CoreWin32.Photos.CurrentMetadata.FrameCount > 1)
            {
                var frameInfo = new StringBuilder();
                frameInfo.Append(CoreWin32.Photos.CurrentMetadata.FrameIndex + 1);
                frameInfo.Append('/');
                frameInfo.Append(CoreWin32.Photos.CurrentMetadata.FrameCount);

                return Core.Lang[LangId._ImageInfo_FrameCount, frameInfo];
            }

            return null;
        }
    }


    private string? ListCount
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(ListCount))
                && CoreWin32.Photos.Count > 0)
            {
                var listInfo = new StringBuilder();
                listInfo.Append(CoreWin32.Photos.CurrentIndex + 1);
                listInfo.Append('/');
                listInfo.Append(CoreWin32.Photos.Count);

                return Core.Lang[LangId._ImageInfo_ListCount, listInfo.ToString()];
            }

            return null;
        }
    }


    private string? Zoom
    {
        get
        {
            if (Core.Config.ImageInfoTags.Contains(nameof(Zoom)) && CoreWin32.Photos.Count > 0)
            {
                return $"{Math.Round(_viewer.ZoomFactor * 100, 2):n2}%";
            }

            return null;
        }
    }


    private string? ExifRating
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(ExifRating))
                && CoreWin32.Photos.CurrentMetadata != null)
            {
                return CoreWin32.Photos.CurrentMetadata.ExifRatingFormatted;
            }

            return null;
        }
    }


    private string? ExifDateTime
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if ((Core.Config.ImageInfoTags.Contains(nameof(ExifDateTime))
                || Core.Config.ImageInfoTags.Contains(nameof(DateTimeAuto)))
                && CoreWin32.Photos.CurrentMetadata != null
                && CoreWin32.Photos.CurrentMetadata.ExifDateTime != null)
            {
                return BHelper.FormatDateTime(CoreWin32.Photos.CurrentMetadata.ExifDateTime) + " (e)";
            }

            return null;
        }
    }


    private string? ExifDateTimeOriginal
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if ((Core.Config.ImageInfoTags.Contains(nameof(ExifDateTimeOriginal))
                || Core.Config.ImageInfoTags.Contains(nameof(DateTimeAuto)))
                && CoreWin32.Photos.CurrentMetadata != null
                && CoreWin32.Photos.CurrentMetadata.ExifDateTimeOriginal != null)
            {
                return BHelper.FormatDateTime(CoreWin32.Photos.CurrentMetadata.ExifDateTimeOriginal) + " (o)";
            }

            return null;
        }
    }


    private string? DateTimeAuto
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(DateTimeAuto))
                && CoreWin32.Photos.CurrentMetadata != null)
            {
                if (CoreWin32.Photos.CurrentMetadata.ExifDateTimeOriginal != null)
                {
                    return ExifDateTimeOriginal;
                }

                if (CoreWin32.Photos.CurrentMetadata.ExifDateTime != null)
                {
                    return ExifDateTime;
                }

                return ModifiedDateTime;
            }

            return null;
        }
    }


    private string? ColorSpace
    {
        get
        {
            // skip for clipboard image
            if (Core.ClipboardImage is not null) return null;

            if (Core.Config.ImageInfoTags.Contains(nameof(ColorSpace))
                && CoreWin32.Photos.CurrentMetadata != null
                && CoreWin32.Photos.CurrentMetadata.ColorSpace != ImageMagick.ColorSpace.Undefined)
            {
                var colorSpace = CoreWin32.Photos.CurrentMetadata.ColorSpace.ToString();
                var colorProfile = !string.IsNullOrEmpty(CoreWin32.Photos.CurrentMetadata.ColorProfileName)
                    ? CoreWin32.Photos.CurrentMetadata.ColorProfileName
                    : "-";

                if (colorSpace.Equals(colorProfile, StringComparison.OrdinalIgnoreCase))
                {
                    return colorSpace;
                }

                return $"{colorSpace}/{colorProfile}";
            }

            return null;
        }
    }

    #endregion // Image Info Tags



    /// <summary>
    /// Gets the status text.
    /// </summary>
    public string Text
    {
        get
        {
            var strBuilder = new StringBuilder();
            int count = 0;

            if (Core.ClipboardImage is not null)
            {
                strBuilder.Append(Core.Lang[LangId.FrmMain_ClipboardImage]);
                count++;
            }

            foreach (var tag in Core.Config.ImageInfoTags)
            {
                var tagValue = tag switch
                {
                    nameof(AppName) => AppName,
                    nameof(Name) => Name,
                    nameof(Path) => Path,
                    nameof(FileSize) => FileSize,
                    nameof(ModifiedDateTime) => ModifiedDateTime,

                    nameof(Dimension) => Dimension,
                    nameof(FrameCount) => FrameCount,
                    nameof(ListCount) => ListCount,
                    nameof(Zoom) => Zoom,

                    nameof(ExifRating) => ExifRating,
                    nameof(ExifDateTime) => ExifDateTime,
                    nameof(ExifDateTimeOriginal) => ExifDateTimeOriginal,
                    nameof(DateTimeAuto) => DateTimeAuto,
                    nameof(ColorSpace) => ColorSpace,
                    _ => null,
                };

                if (!string.IsNullOrWhiteSpace(tagValue))
                {
                    if (count > 0)
                    {
                        strBuilder.Append("  ︱  ");
                    }

                    strBuilder.Append(tagValue);
                    count++;
                }
            }

            return strBuilder.ToString();
        }
    }


    public AppStatusInfo(ViewerControl viewer)
    {
        _viewer = viewer;

        CoreWin32.Photos.PropertyChanged += Photos_PropertyChanged;
        _viewer.ZoomChanged += Viewer_ZoomChanged;
    }


    protected override void OnDisposing()
    {
        base.OnDisposing();

        CoreWin32.Photos.PropertyChanged -= Photos_PropertyChanged;
        _viewer.ZoomChanged -= Viewer_ZoomChanged;
    }


    private void Photos_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CoreWin32.Photos.CurrentFilePath))
        {
            _filePath = string.IsNullOrEmpty(CoreWin32.Photos.CurrentFilePath)
                ? CoreWin32.Photos.GetFilePath(CoreWin32.Photos.CurrentIndex)
                : BHelper.ResolvePath(CoreWin32.Photos.CurrentFilePath);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }


    private void Viewer_ZoomChanged(ViewerControl sender, ViewerZoomEventArgs e)
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

}
