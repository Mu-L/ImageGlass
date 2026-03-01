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
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using ImageGlass.Common;
using ImageGlass.Common.Localization;
using System;

namespace ImageGlass.UI;

public partial class ToolHostControl : PhControl
{

    #region Public Properties

    /// <summary>
    /// Gets, sets the tool content hosted by this control.
    /// </summary>
    [Content]
    public object? ToolContent
    {
        get => GetValue(ToolContentProperty);
        set => SetValue(ToolContentProperty, value);
    }
    public static readonly StyledProperty<object?> ToolContentProperty =
        AvaloniaProperty.Register<ToolHostControl, object?>(nameof(ToolContent));



    /// <summary>
    /// Gets the tooltip text displayed for the close button.
    /// </summary>
    public string CloseButtonTooltipText
    {
        get => GetValue(CloseButtonTooltipTextProperty);
        private set => SetValue(CloseButtonTooltipTextProperty, value);
    }
    public static readonly StyledProperty<string> CloseButtonTooltipTextProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, string>(nameof(CloseButtonTooltipText));


    #endregion // Public Properties



    public ToolHostControl()
    {
        InitializeComponent();
        IsContentVisible = false;
    }



    #region Control Events

    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        CloseButtonTooltipText = Core.Lang[LangId._Close];
    }


    private void PART_BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        CloseCurrentTool();
    }

    #endregion // Control Events



    #region Public Methods

    /// <summary>
    /// Opens the specified tool and sets it as the current tool content if no other tool is currently open.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public bool OpenTool(IToolControl? newTool)
    {
        if (newTool is null) return false;

        if (ToolContent is IToolControl tool)
        {
            throw new InvalidOperationException($"IGE: The current tool (ID = {tool.ToolId}) must be close before opening another tool");
        }

        // load tool settings
        newTool.LoadSettings(Core.Config);

        // open the tool
        ToolContent = newTool;
        IsContentVisible = true;
        Core.ToolMap[newTool.ToolId] = true;

        return true;
    }


    /// <summary>
    /// Closes the tool with the specified identifier if it is currently active.
    /// </summary>
    public bool CloseTool(string toolId)
    {
        if (ToolContent is not IToolControl tool) return true;
        if (tool.ToolId != toolId) return false;

        return CloseCurrentTool();
    }


    /// <summary>
    /// Closes the currently active tool and saves its settings.
    /// </summary>
    public bool CloseCurrentTool()
    {
        if (ToolContent is not IToolControl tool) return true;

        try
        {
            // save tool settings
            tool.SaveSettings(Core.Config);

            // close the tool
            ToolContent = null;
            IsContentVisible = false;
            Core.ToolMap[tool.ToolId] = false;

            return true;
        }
        catch { }

        return false;
    }

    #endregion // Public Methods

}
