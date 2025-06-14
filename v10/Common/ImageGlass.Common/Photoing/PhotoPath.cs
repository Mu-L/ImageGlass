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
using System.ComponentModel;

namespace ImageGlass.Common.Photoing;


/// <summary>
/// Represents a photo path with its associated selection state.
/// </summary>
public class PhotoPath(string path) : INotifyPropertyChanged
{
    private string _path = path;
    private bool _isSelected = false;



    /// <summary>
    /// Gets, sets value indicating if the photo is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }


    /// <summary>
    /// Gets, set the path of photo
    /// </summary>
    public string FilePath
    {
        get => _path;
        set
        {
            _path = value;
            OnPropertyChanged(nameof(FilePath));

            OnPropertyChanged(nameof(Extension));
            OnPropertyChanged(nameof(FileTitle));
            OnPropertyChanged(nameof(GalleryFileTitle));
            OnPropertyChanged(nameof(GalleryFileExtension));
        }
    }


    /// <summary>
    /// Gets original file extension. E.g: <c>".png"</c>.
    /// </summary>
    public string Extension => Path.GetExtension(FilePath);


    /// <summary>
    /// Gets original file name without extension. E.g. <c>"My photo"</c>.
    /// </summary>
    public string FileTitle => Path.GetFileNameWithoutExtension(FilePath);


    /// <summary>
    /// Gets the file name without extension and including a trailing dot. E.g. <c>"My photo."</c>.
    /// </summary>
    public string GalleryFileTitle => FileTitle + ".";


    /// <summary>
    /// Gets file extension without dot. E.g. <c>"png"</c>.
    /// </summary>
    public string GalleryFileExtension => Extension.Length > 1 ? Extension.Substring(1) : string.Empty;





    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public PhotoPath() : this(string.Empty) { }

}
