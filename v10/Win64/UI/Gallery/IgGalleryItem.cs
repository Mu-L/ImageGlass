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
using ImageGlass.Win64.Common;
using ImageGlass.Win64.Common.Photoing;
using System.ComponentModel;
using Windows.UI;

namespace ImageGlass.Win64.UI;


public partial class IgGalleryItem : IgButton
{
    protected Photo _vm = new();


    /// <summary>
    /// Gets, sets view model for the control.
    /// </summary>
    public Photo VM
    {
        get => _vm;
        set
        {
            if (_vm != value)
            {
                _vm.PropertyChanged -= VM_PropertyChanged;
                _vm = value;
                _vm.PropertyChanged += VM_PropertyChanged;

                OnPropertyChanged();
            }
        }
    }
    private void VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Photo.IsCurrent))
        {
            UpdateStyle();
        }
    }


    public IgGalleryItem()
    {
        DefaultStyleKey = typeof(IgGalleryItem);
    }


    protected override Color GetColorForText()
    {
        return AP.Config.Theme.ComputedColors.GalleryTextColor;
    }

    protected override Color GetColorForHovered()
    {
        return AP.Config.Theme.ComputedColors.GalleryItemHoverColor;
    }

    protected override Color GetColorForPressed()
    {
        return AP.Config.Theme.ComputedColors.GalleryItemActiveColor;
    }

    protected override Color GetColorForChecked()
    {
        return AP.Config.Theme.ComputedColors.GalleryItemSelectedColor;
    }

}

