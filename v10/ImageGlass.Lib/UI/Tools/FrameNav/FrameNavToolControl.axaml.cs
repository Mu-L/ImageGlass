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
using ImageGlass.UI.Viewer;

namespace ImageGlass.UI;

public partial class FrameNavToolControl : PhControl, IToolControl
{

    public static string TOOL_ID => "Tool_FrameNav";
    public string ToolId => TOOL_ID;
    public bool HasSettingsUI => false;
    public object? Settings { get; } = null;
    public ViewerControl Viewer { get; init; } = null!;


    /// <summary>
    /// Gets value indicating if the current photo has multiple frames.
    /// </summary>
    public bool HasMultiFrames
    {
        get => GetValue(HasMultiFramesProperty);
        private set => SetValue(HasMultiFramesProperty, value);
    }
    public static readonly StyledProperty<bool> HasMultiFramesProperty =
        AvaloniaProperty.Register<FrameNavToolControl, bool>(nameof(HasMultiFrames));


    /// <summary>
    /// Gets value indicating if the current photo can animate.
    /// </summary>
    public bool CanAnimate
    {
        get => GetValue(CanAnimateProperty);
        private set => SetValue(CanAnimateProperty, value);
    }
    public static readonly StyledProperty<bool> CanAnimateProperty =
        AvaloniaProperty.Register<FrameNavToolControl, bool>(nameof(CanAnimate));


    /// <summary>
    /// Gets frame text info.
    /// </summary>
    public string FrameTextInfo
    {
        get => GetValue(FrameTextInfoProperty);
        private set => SetValue(FrameTextInfoProperty, value);
    }
    public static readonly StyledProperty<string> FrameTextInfoProperty =
        AvaloniaProperty.Register<FrameNavToolControl, string>(nameof(FrameTextInfo));



    public FrameNavToolControl()
    {
        InitializeComponent();
    }



    #region Control Events

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdateFrameInfo();

        Viewer.PhotoFrameChanged += Viewer_PhotoFrameChanged;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Viewer.PhotoFrameChanged -= Viewer_PhotoFrameChanged;
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();
    }


    private void Viewer_PhotoFrameChanged(ViewerControl sender, System.EventArgs e)
    {
        UpdateFrameInfo();
    }


    #endregion // Control Events



    #region Control Methods

    /// <summary>
    /// Updates the frame-related information for the current photo.
    /// </summary>
    private void UpdateFrameInfo()
    {
        var frameCount = Viewer.Photo?.Metadata?.FrameCount ?? 0;

        HasMultiFrames = frameCount > 1;
        CanAnimate = HasMultiFrames && (Viewer.Photo?.Metadata?.CanAnimate ?? false);

        if (frameCount > 0)
        {
            var currentFrame = (Viewer.Photo?.FrameIndex ?? 0) + 1;
            var frameInfo = $"{currentFrame} / {frameCount}";

            FrameTextInfo = frameInfo;
        }
        else
        {
            FrameTextInfo = string.Empty;
        }
    }

    #endregion // Control Methods


}

