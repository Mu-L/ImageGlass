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
using Cysharp.Text;
using ImageGlass.UI;
using System;
using System.ComponentModel;

namespace ImageGlass.Common;

public partial class AppStatusInfo : DisposableImpl
{
    private VirtualViewerControl _viewer;
    private string? _filePath = null;

    public event EventHandler? Changed;


    #region Image Info Tags

    private string? AppName
    {
        get
        {
            if (AP.Config.ImageInfoTags.Contains(nameof(AppName)))
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
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(Name)))
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
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(Path)))
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
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(FileSize))
                && AP.Photos.CurrentMetadata != null)
            {
                return AP.Photos.CurrentMetadata.FileSizeFormated;
            }

            return null;
        }
    }


    private string? ModifiedDateTime
    {
        get
        {
            // skip for clipboard image
            if (AP.ClipboardImage is not null) return null;

            if ((AP.Config.ImageInfoTags.Contains(nameof(ModifiedDateTime))
                || AP.Config.ImageInfoTags.Contains(nameof(DateTimeAuto)))
                && AP.Photos.CurrentMetadata != null)
            {
                return AP.Photos.CurrentMetadata.FileLastWriteTimeFormated + " (m)";
            }

            return null;
        }
    }


    private string? Dimension
    {
        get
        {
            if (AP.Config.ImageInfoTags.Contains(nameof(Dimension))
                && AP.Photos.CurrentMetadata != null)
            {
                return $"{AP.Photos.CurrentMetadata.Width:n0}×{AP.Photos.CurrentMetadata.Height:n0}";
            }

            return null;
        }
    }


    private string? FrameCount
    {
        get
        {
            // skip for clipboard image
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(FrameCount))
                && AP.Photos.CurrentMetadata != null
                && AP.Photos.CurrentMetadata.FrameCount > 1)
            {
                using var frameInfo = ZString.CreateStringBuilder();
                frameInfo.Append(AP.Photos.CurrentMetadata.FrameIndex + 1);
                frameInfo.Append('/');
                frameInfo.Append(AP.Photos.CurrentMetadata.FrameCount);

                return AP.Config.Lang[LangId._ImageInfo_FrameCount, frameInfo];
            }

            return null;
        }
    }


    private string? ListCount
    {
        get
        {
            // skip for clipboard image
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(ListCount))
                && AP.Photos.Count > 0)
            {
                using var listInfo = ZString.CreateStringBuilder();
                listInfo.Append(AP.Photos.CurrentIndex + 1);
                listInfo.Append('/');
                listInfo.Append(AP.Photos.Count);

                return AP.Config.Lang[LangId._ImageInfo_ListCount, listInfo.ToString()];
            }

            return null;
        }
    }


    private string? Zoom
    {
        get
        {
            if (AP.Config.ImageInfoTags.Contains(nameof(Zoom)) && AP.Photos.Count > 0)
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
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(ExifRating))
                && AP.Photos.CurrentMetadata != null)
            {
                return BHelper.FormatStarRatingText(AP.Photos.CurrentMetadata.ExifRatingPercent);
            }

            return null;
        }
    }


    private string? ExifDateTime
    {
        get
        {
            // skip for clipboard image
            if (AP.ClipboardImage is not null) return null;

            if ((AP.Config.ImageInfoTags.Contains(nameof(ExifDateTime))
                || AP.Config.ImageInfoTags.Contains(nameof(DateTimeAuto)))
                && AP.Photos.CurrentMetadata != null
                && AP.Photos.CurrentMetadata.ExifDateTime != null)
            {
                return BHelper.FormatDateTime(AP.Photos.CurrentMetadata.ExifDateTime) + " (e)";
            }

            return null;
        }
    }


    private string? ExifDateTimeOriginal
    {
        get
        {
            // skip for clipboard image
            if (AP.ClipboardImage is not null) return null;

            if ((AP.Config.ImageInfoTags.Contains(nameof(ExifDateTimeOriginal))
                || AP.Config.ImageInfoTags.Contains(nameof(DateTimeAuto)))
                && AP.Photos.CurrentMetadata != null
                && AP.Photos.CurrentMetadata.ExifDateTimeOriginal != null)
            {
                return BHelper.FormatDateTime(AP.Photos.CurrentMetadata.ExifDateTimeOriginal) + " (o)";
            }

            return null;
        }
    }


    private string? DateTimeAuto
    {
        get
        {
            // skip for clipboard image
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(DateTimeAuto))
                && AP.Photos.CurrentMetadata != null)
            {
                if (AP.Photos.CurrentMetadata.ExifDateTimeOriginal != null)
                {
                    return ExifDateTimeOriginal;
                }

                if (AP.Photos.CurrentMetadata.ExifDateTime != null)
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
            if (AP.ClipboardImage is not null) return null;

            if (AP.Config.ImageInfoTags.Contains(nameof(ColorSpace))
                && AP.Photos.CurrentMetadata != null
                && AP.Photos.CurrentMetadata.ColorSpace != ImageMagick.ColorSpace.Undefined)
            {
                var colorSpace = AP.Photos.CurrentMetadata.ColorSpace.ToString();
                var colorProfile = !string.IsNullOrEmpty(AP.Photos.CurrentMetadata.ColorProfileName)
                    ? AP.Photos.CurrentMetadata.ColorProfileName
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
            using var strBuilder = ZString.CreateStringBuilder();
            int count = 0;

            if (AP.ClipboardImage is not null)
            {
                strBuilder.Append(AP.Config.Lang[LangId.FrmMain_ClipboardImage]);
                count++;
            }

            foreach (var tag in AP.Config.ImageInfoTags)
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


    public AppStatusInfo(VirtualViewerControl viewer)
    {
        _viewer = viewer;

        AP.Photos.PropertyChanged += Photos_PropertyChanged;
        _viewer.ZoomChanged += Viewer_ZoomChanged;
    }


    protected override void OnDisposing()
    {
        base.OnDisposing();

        AP.Photos.PropertyChanged -= Photos_PropertyChanged;
        _viewer.ZoomChanged -= Viewer_ZoomChanged;
    }


    private void Photos_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AP.Photos.CurrentFilePath))
        {
            _filePath = string.IsNullOrEmpty(AP.Photos.CurrentFilePath)
                ? AP.Photos.GetFilePath(AP.Photos.CurrentIndex)
                : BHelper.ResolvePath(AP.Photos.CurrentFilePath);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }


    private void Viewer_ZoomChanged(VirtualViewerControl sender, ZoomEventArgs args)
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

}
