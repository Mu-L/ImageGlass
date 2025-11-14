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
using System;
using System.Text.Json.Serialization;

namespace ImageGlass.Common;


[JsonSerializable(typeof(ToolbarItemModel))]
public partial class ToolbarItemModelJsonContext : JsonSerializerContext { }


public partial class ToolbarItemModel : IgReactive, IJsonOnDeserialized
{
    /// <summary>
    /// Gets the ID for toolbar separator.
    /// </summary>
    public static string ID_SEPARATOR => "SEPARATOR";

    /// <summary>
    /// Gets a separator toolbar item.
    /// </summary>
    public static ToolbarItemModel Separator => new(ID_SEPARATOR);


    #region JSON Properties

    /// <summary>
    /// Gets, sets the unique ID of toolbar button.
    /// </summary>
    public string Id
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsSeparator));
            }
        }
    } = "";


    /// <summary>
    /// Gets, sets the SVG icon of toolbar button.
    /// It can be an absolute path,
    /// or the toolbar button name <see cref="IgTheme.ToolbarIcons"/> in theme pack.
    /// </summary>
    public string Image
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = "";


    /// <summary>
    /// Gets, sets the text of toolbar button, or a language key for localization.
    /// </summary>
    public string Text
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(DisplayText));
                _ = OnPropertyChanged(nameof(IsTextVisible));
                _ = OnPropertyChanged(nameof(Tooltip));
            }
        }
    } = "";


    /// <summary>
    /// Gets the display text of toolbar button
    /// </summary>
    public string DisplayText => AP.Config.Lang[Text];


    /// <summary>
    /// Gets, sets the value indicating that the toolbar button is displayed with text.
    /// </summary>
    public bool ShowText
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsTextVisible));
            }
        }
    } = false;


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
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(IsToggle));
            }
        }
    } = string.Empty;


    /// <summary>
    /// Gets, sets the alignment of toolbar item.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ToolbarItemAlignment>))]
    public ToolbarItemAlignment Alignment
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = ToolbarItemAlignment.Left;


    /// <summary>
    /// Gets, sets the click action of toolbar button.
    /// </summary>
    public HotkeySingleAction? OnClick
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = null;


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
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged(nameof(IsChecked));
            }
        }
    } = false;


    /// <summary>
    /// Gets, sets the index of toolbar source items.
    /// </summary>
    [JsonIgnore]
    public int SourceIndex
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = -1;


    /// <summary>
    /// Gets, sets the value indicating that the toolbar item is hidden due to overflow.
    /// </summary>
    [JsonIgnore]
    public bool IsOverflow
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
            }
        }
    } = false;


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


    /// <summary>
    /// Gets, sets the hotkey string for toolbar item.
    /// </summary>
    [JsonIgnore]
    public string HotkeyText
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                _ = OnPropertyChanged();
                _ = OnPropertyChanged(nameof(Tooltip));
            }
        }
    } = string.Empty;


    /// <summary>
    /// Gets the tooltip of toolbar item.
    /// </summary>
    public string Tooltip
    {
        get
        {
            if (string.IsNullOrWhiteSpace(HotkeyText))
                return DisplayText;

            return ZString.Concat(DisplayText, $" ({HotkeyText})");
        }
    }

    #endregion // Non-JSON Properties



    public ToolbarItemModel() { }
    public ToolbarItemModel(string id)
    {
        Id = id;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override string ToString()
    {
        return $"[{SourceIndex} | {Id} | {Text} | {nameof(IsOverflow)}={IsOverflow}";
    }



    public void OnDeserialized()
    {
        // save action display text
        OnClick?.LangKey = Text;
    }

}


public enum ToolbarItemAlignment
{
    Left,
    Right,
}

