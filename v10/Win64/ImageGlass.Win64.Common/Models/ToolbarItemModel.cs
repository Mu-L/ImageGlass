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
using ImageGlass.Common;
using System;
using System.Text.Json.Serialization;

namespace ImageGlass.Win64.Common;


[JsonSerializable(typeof(ToolbarItemModel))]
public partial class ToolbarItemModelJsonContext : JsonSerializerContext { }


public partial class ToolbarItemModel : IgReactive
{
    /// <summary>
    /// Gets the ID for toolbar separator.
    /// </summary>
    public static string ID_SEPARATOR => "SEPARATOR";

    // JSON properties
    protected string _id = "";
    protected string _image = string.Empty;
    protected string _text = string.Empty;
    protected bool _showText = false;
    protected bool _isToggle = false;
    protected ToolbarItemAlignment _alignment = ToolbarItemAlignment.Left;
    protected SingleAction? _onClick = null;

    // Non-JSON properties
    protected bool _isChecked = false;
    protected bool _isOverflow = false;
    protected int _sourceIndex = -1;


    #region JSON Properties

    /// <summary>
    /// Gets, sets the unique ID of toolbar button.
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSeparator));
            }
        }
    }


    /// <summary>
    /// Gets, sets the SVG icon of toolbar button.
    /// It can be an absolute path,
    /// or the toolbar button name <see cref="IgTheme.ToolbarIcons"/> in theme pack.
    /// </summary>
    public string Image
    {
        get => _image;
        set
        {
            if (_image != value)
            {
                _image = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the text of toolbar button.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTextVisible));
            }
        }
    }


    /// <summary>
    /// Gets, sets the value indicating that the toolbar button is displayed with text.
    /// </summary>
    public bool ShowText
    {
        get => _showText;
        set
        {
            if (_showText != value)
            {
                _showText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTextVisible));
            }
        }
    }


    /// <summary>
    /// Gets, sets the config name for toggle binding.
    /// </summary>
    public string ConfigBinding
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsToggle));
            }
        }
    } = string.Empty;


    /// <summary>
    /// Gets, sets the alignment of toolbar item.
    /// </summary>
    public ToolbarItemAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the click action of toolbar button.
    /// </summary>
    public SingleAction? OnClick
    {
        get => _onClick;
        set
        {
            if (_onClick != value)
            {
                _onClick = value;
                OnPropertyChanged();
            }
        }
    }


    #endregion // JSON Properties


    #region Non-JSON Properties

    /// <summary>
    /// Gets the value indicating that the toolbar button can be toggled.
    /// </summary>
    [JsonIgnore]
    public bool IsToggle => !string.IsNullOrWhiteSpace(ConfigBinding);


    /// <summary>
    /// Gets, sets the check state of toolbar button.
    /// </summary>
    [JsonIgnore]
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }


    /// <summary>
    /// Gets, sets the index of toolbar source items.
    /// </summary>
    [JsonIgnore]
    public int SourceIndex
    {
        get => _sourceIndex;
        set
        {
            if (_sourceIndex != value)
            {
                _sourceIndex = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the value indicating that the toolbar item is hidden due to overflow.
    /// </summary>
    [JsonIgnore]
    public bool IsOverflow
    {
        get => _isOverflow;
        set
        {
            if (_isOverflow != value)
            {
                _isOverflow = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Checks if the toolbar item is a separator.
    /// </summary>
    [JsonIgnore]
    public bool IsSeparator => Id.Equals(ID_SEPARATOR, StringComparison.InvariantCultureIgnoreCase);


    /// <summary>
    /// Checks if the button text is visible.
    /// </summary>
    [JsonIgnore]
    public bool IsTextVisible => ShowText && !string.IsNullOrWhiteSpace(Text);

    #endregion // Non-JSON Properties


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override string ToString()
    {
        return $"[{SourceIndex} | {Id} | {Text} | {nameof(IsOverflow)}={IsOverflow}";
    }

}


public enum ToolbarItemAlignment
{
    Left,
    Right,
}

