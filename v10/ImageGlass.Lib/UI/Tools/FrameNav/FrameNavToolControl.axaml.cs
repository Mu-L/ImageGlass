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
using Avalonia.Interactivity;
using ImageGlass.UI.Viewer;

namespace ImageGlass.UI;

public partial class FrameNavToolControl : PhControl, IToolControl
{

    public static string TOOL_ID => "Tool_FrameNav";
    public string ToolId => TOOL_ID;
    public bool HasSettingsUI => false;
    public object? Settings { get; } = null;
    public ViewerControl Viewer { get; init; } = null!;


    public FrameNavToolControl()
    {
        InitializeComponent();
    }



    #region Control Events

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();
    }


    #endregion // Control Events



    #region Control Methods



    #endregion // Control Methods


}

