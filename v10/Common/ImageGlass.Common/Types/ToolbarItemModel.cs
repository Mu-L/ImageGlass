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
using System.Text.Json.Serialization;

namespace ImageGlass.Common;


/// <summary>
/// Toolbar item model
/// </summary>
public class ToolbarItemModel : DisposableImpl
{
    protected string _id = "";
    protected ToolbarItemType _type = ToolbarItemType.Button;
    protected string _text = "";
    protected string _image = "";
    protected ToolbarItemAlignment _alignment = ToolbarItemAlignment.Left;
    protected ToolbarItemDisplayStyle _displayStyle = ToolbarItemDisplayStyle.Image;

    protected bool _isOverflow = false;



    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }
    }


    public ToolbarItemType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
    }


    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }
    }


    public string Image
    {
        get => _image;
        set
        {
            if (_image != value)
            {
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }
    }


    public ToolbarItemAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                OnPropertyChanged(nameof(Alignment));
            }
        }
    }



    public ToolbarItemDisplayStyle DisplayStyle
    {
        get => _displayStyle;
        set
        {
            if (_displayStyle != value)
            {
                _displayStyle = value;
                OnPropertyChanged(nameof(DisplayStyle));
            }
        }
    }


    [JsonIgnore]
    public bool IsOverflow
    {
        get => _isOverflow;
        set
        {
            if (_isOverflow != value)
            {
                _isOverflow = value;
                OnPropertyChanged(nameof(IsOverflow));
            }
        }
    }


    public string CheckableConfigBinding { get; set; } = "";


    ///// <summary>
    ///// Gets, sets hotkeys.
    ///// </summary>
    //[JsonConverter(typeof(HotkeyListJsonConverter))]
    //public List<Hotkey> Hotkeys { get; set; } = [];
}


public enum ToolbarItemType
{
    Button,
    Separator,
}

public enum ToolbarItemDisplayStyle
{
    Image,
    ImageAndText,
}

public enum ToolbarItemAlignment
{
    Left,
    Right,
}

public record ToolbarItemTagModel
{
    public SingleAction OnClick { get; set; } = new();
    public string Image { get; set; } = string.Empty;
    public string CheckableConfigBinding { get; set; } = string.Empty;
}


public enum ToolbarAddItemResult
{
    Success,
    ItemExists,
    InvalidModel,
    ThemeIsNull,
}
