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

public partial class PluginHostControl : PhControl
{

    #region Public Properties

    /// <summary>
    /// Gets, sets the current plugin content hosted by this control.
    /// </summary>
    [Content]
    public IPluginControl? Plugin
    {
        get => GetValue(PluginContentProperty);
        set => SetValue(PluginContentProperty, value);
    }
    public static readonly StyledProperty<IPluginControl?> PluginContentProperty =
        AvaloniaProperty.Register<PluginHostControl, IPluginControl?>(nameof(Plugin));



    /// <summary>
    /// Gets the tooltip text displayed for the close button.
    /// </summary>
    public string CloseButtonTooltipText
    {
        get => GetValue(CloseButtonTooltipTextProperty);
        private set => SetValue(CloseButtonTooltipTextProperty, value);
    }
    public static readonly StyledProperty<string> CloseButtonTooltipTextProperty =
        AvaloniaProperty.Register<PluginHostControl, string>(nameof(CloseButtonTooltipText));


    /// <summary>
    /// Gets the tooltip text displayed for the settings button.
    /// </summary>
    public string SettingsButtonTooltipText
    {
        get => GetValue(SettingsButtonTooltipTextProperty);
        private set => SetValue(SettingsButtonTooltipTextProperty, value);
    }
    public static readonly StyledProperty<string> SettingsButtonTooltipTextProperty =
        AvaloniaProperty.Register<PluginHostControl, string>(nameof(SettingsButtonTooltipText));


    /// <summary>
    /// Gets the value indicates if the plugin contains settings.
    /// </summary>
    public bool HasSettings
    {
        get => GetValue(HasSettingsProperty);
        private set => SetValue(HasSettingsProperty, value);
    }
    public static readonly StyledProperty<bool> HasSettingsProperty =
        AvaloniaProperty.Register<PluginHostControl, bool>(nameof(HasSettings));


    #endregion // Public Properties



    public PluginHostControl()
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
        // Route through API so settings are saved before closing
        if (Plugin is IPluginControl plugin)
        {
            Core.API?.IG_ClosePlugin(plugin.PluginId);
        }
    }


    private async void PART_BtnSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (Plugin is not IPluginControl plugin) return;
        if (!plugin.HasSettingsUI) return;

        await plugin.ShowSettingsWindowAsync();
    }


    #endregion // Control Events



    #region Public Methods

    /// <summary>
    /// Opens the specified plugin as the current hosted content.
    /// Settings are NOT loaded here — the caller (API layer) loads them before calling this.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public bool OpenPlugin(IPluginControl? newPlugin)
    {
        if (newPlugin is null) return false;

        if (Plugin is IPluginControl plugin)
        {
            throw new InvalidOperationException($"IGE: The current plugin (ID = {plugin.PluginId}) must be closed before opening another plugin");
        }

        HasSettings = newPlugin.HasSettingsUI;

        // open the plugin
        Plugin = newPlugin;
        IsContentVisible = true;

        return true;
    }


    /// <summary>
    /// Closes the plugin with the specified identifier if it is currently active.
    /// Settings are NOT saved here — the caller (API layer) saves them before calling this.
    /// </summary>
    public void ClosePlugin(string pluginId)
    {
        if (Plugin is not IPluginControl plugin) return;
        if (plugin.PluginId != pluginId) return;

        CloseCurrentPlugin();
    }


    /// <summary>
    /// Closes the currently active plugin.
    /// Settings are NOT saved here — the caller (API layer) saves them before calling this.
    /// </summary>
    public void CloseCurrentPlugin()
    {
        if (Plugin is not IPluginControl) return;

        try
        {
            Plugin = null;
            IsContentVisible = false;
        }
        catch { }
    }


    #endregion // Public Methods

}
