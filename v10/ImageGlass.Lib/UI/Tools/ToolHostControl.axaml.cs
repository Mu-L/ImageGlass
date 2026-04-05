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
using System.Collections.Generic;

namespace ImageGlass.UI;

public partial class ToolHostControl : PhControl
{

    #region Public Properties

    /// <summary>
    /// Gets, sets the tool content hosted by this control.
    /// </summary>
    [Content]
    public IToolControl? Tool
    {
        get => GetValue(ToolContentProperty);
        set => SetValue(ToolContentProperty, value);
    }
    public static readonly StyledProperty<IToolControl?> ToolContentProperty =
        AvaloniaProperty.Register<ToolHostControl, IToolControl?>(nameof(Tool));



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


    /// <summary>
    /// Gets the tooltip text displayed for the settings button.
    /// </summary>
    public string SettingsButtonTooltipText
    {
        get => GetValue(SettingsButtonTooltipTextProperty);
        private set => SetValue(SettingsButtonTooltipTextProperty, value);
    }
    public static readonly StyledProperty<string> SettingsButtonTooltipTextProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, string>(nameof(SettingsButtonTooltipText));


    /// <summary>
    /// Gets the value indicates if the tool contains settings.
    /// </summary>
    public bool HasSettings
    {
        get => GetValue(HasSettingsProperty);
        private set => SetValue(HasSettingsProperty, value);
    }
    public static readonly StyledProperty<bool> HasSettingsProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, bool>(nameof(HasSettings));


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
        SettingsButtonTooltipText = Core.Lang[LangId.FrmMain_MnuSettings];
    }


    private void PART_BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        CloseCurrentTool();
    }


    private async void PART_BtnSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (Tool is not IToolControl tool) return;
        if (!tool.HasSettingsUI) return;

        await tool.ShowSettingsWindowAsync();
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

        if (Tool is IToolControl tool)
        {
            throw new InvalidOperationException($"IGE: The current tool (ID = {tool.ToolId}) must be close before opening another tool");
        }

        try
        {
            HasSettings = newTool.HasSettingsUI;

            // load tool settings
            var jsonEl = Core.Config.ToolSettings.GetValueOrDefault(newTool.ToolId);
            newTool.LoadSettings(jsonEl);
        }
        catch { }

        // open the tool
        Tool = newTool;
        IsContentVisible = true;
        Core.ToolMap[newTool.ToolId] = true;

        return true;
    }


    /// <summary>
    /// Closes the tool with the specified identifier if it is currently active.
    /// </summary>
    public bool CloseTool(string toolId)
    {
        if (Tool is not IToolControl tool) return true;
        if (tool.ToolId != toolId) return false;

        return CloseCurrentTool();
    }


    /// <summary>
    /// Closes the currently active tool and saves its settings.
    /// </summary>
    public bool CloseCurrentTool()
    {
        if (Tool is not IToolControl tool) return true;

        try
        {
            // save tool settings
            SaveCurrentToolSettings();

            // close the tool
            Tool = null;
            IsContentVisible = false;
            Core.ToolMap[tool.ToolId] = false;

            return true;
        }
        catch { }

        return false;
    }


    /// <summary>
    /// Saves the current tool's settings to the app config.
    /// </summary>
    public void SaveCurrentToolSettings()
    {
        if (Tool is not IToolControl tool) return;

        try
        {
            // save tool settings
            var jsonEl = tool.SaveSettings();

            if (jsonEl is null)
            {
                Core.Config.ToolSettings.Remove(tool.ToolId);
            }
            else
            {
                Core.Config.ToolSettings[tool.ToolId] = jsonEl.Value;
            }
        }
        catch { }
    }


    #endregion // Public Methods

}
