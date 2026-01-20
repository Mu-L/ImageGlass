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
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using ImageGlass.Common.Photoing;
using ImageGlass.UI;

namespace ImageGlass.Common;

public partial class App : Application
{
    private IgWindow? _mainWindow = null;


    /// <summary>
    /// Gets the main window.
    /// </summary>
    public IgWindow MainWindow => _mainWindow!;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        ApplyUIConfigs();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }


    /// <summary>
    /// Create a new main window.
    /// </summary>
    public void CreateMainWindowIfNotExist(IgWindow window)
    {
        if (_mainWindow is not null) return;

        _mainWindow = window;
    }


    private void ApplyUIConfigs()
    {
        LoadAppTheme();

        // initialize Magick decoder
        MagickCodec.Initialize();

        // load app language
        _ = Core.Config.LoadCurrentLanguageAsync();
    }


    private void LoadAppTheme()
    {
        // get accent, color mode & load theme for the first time
        var info = PlatformSettings!.GetColorValues();
        var isSystemDarkMode = info.ThemeVariant == PlatformThemeVariant.Dark;

        BHelper.RunSync(() => Core.Config.LoadCurrentThemeAsync(isSystemDarkMode, info.AccentColor1, true, true, false));

        // set the initial app color mode
        if (Core.Config.Theme.Settings.IsDarkMode)
        {
            RequestedThemeVariant = ThemeVariant.Dark;
        }
        else RequestedThemeVariant = ThemeVariant.Light;
    }

}