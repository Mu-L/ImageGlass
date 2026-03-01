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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ImageGlass.Common;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Types;
using ImageGlass.UI.Viewer;

namespace ImageGlass.UI;

public partial class ColorPickerToolControl : PhControl, IToolControl
{
    public string ToolId => "ColorPicker";
    public IToolConfig Settings { get; set; }
    public ViewerControl Viewer { get; init; } = null!;



    #region Public Properties

    /// <summary>
    /// Gets the tooltip text displayed for the copy button.
    /// </summary>
    public string CopyButtonTooltipText
    {
        get => GetValue(CopyButtonTooltipTextProperty);
        private set => SetValue(CopyButtonTooltipTextProperty, value);
    }
    public static readonly StyledProperty<string> CopyButtonTooltipTextProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, string>(nameof(CopyButtonTooltipText));


    /// <summary>
    /// Gets, sets the current point.
    /// </summary>
    public Point? CurrentPoint
    {
        get => GetValue(CurrentPointProperty);
        set => SetValue(CurrentPointProperty, value);
    }
    public static readonly StyledProperty<Point?> CurrentPointProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, Point?>(nameof(CurrentPoint));


    /// <summary>
    /// Gets, sets the selected point.
    /// </summary>
    public Point? SelectedPoint
    {
        get => GetValue(SelectedPointProperty);
        set => SetValue(SelectedPointProperty, value);
    }
    public static readonly StyledProperty<Point?> SelectedPointProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, Point?>(nameof(SelectedPoint));


    /// <summary>
    /// Gets, sets the current text color.
    /// </summary>
    public Color CurrentTextColor => GetPositionTextColor(CurrentColor);
    public static readonly DirectProperty<ColorPickerToolControl, Color> CurrentTextColorProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, Color>(nameof(CurrentTextColor), i => i.CurrentTextColor);


    /// <summary>
    /// Gets, sets the selected text color.
    /// </summary>
    public Color SelectedTextColor => GetPositionTextColor(SelectedColor);
    public static readonly DirectProperty<ColorPickerToolControl, Color> SelectedTextColorProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, Color>(nameof(SelectedTextColor), i => i.SelectedTextColor);


    /// <summary>
    /// Gets, sets the current color.
    /// </summary>
    public Color CurrentColor
    {
        get => GetValue(CurrentColorProperty);
        set => SetValue(CurrentColorProperty, value);
    }
    public static readonly StyledProperty<Color> CurrentColorProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, Color>(nameof(CurrentColor), Const.COLOR_EMPTY);


    /// <summary>
    /// Gets, sets the selected color.
    /// </summary>
    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPickerToolControl, Color>(nameof(SelectedColor));





    /// <summary>
    /// Gets the selected color in RGB format.
    /// </summary>
    public string? ColorRGB => FormatColor(ColorFormat.RGB);
    public static readonly DirectProperty<ColorPickerToolControl, string?> ColorRGBProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, string?>(nameof(ColorRGB), i => i.ColorRGB);


    /// <summary>
    /// Gets the selected color in HEX format.
    /// </summary>
    public string? ColorHEX => FormatColor(ColorFormat.HEX);
    public static readonly DirectProperty<ColorPickerToolControl, string?> ColorHEXProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, string?>(nameof(ColorHEX), i => i.ColorHEX);


    /// <summary>
    /// Gets the selected color in HSL format.
    /// </summary>
    public string? ColorHSL => FormatColor(ColorFormat.HSL);
    public static readonly DirectProperty<ColorPickerToolControl, string?> ColorHSLProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, string?>(nameof(ColorHSL), i => i.ColorHSL);


    /// <summary>
    /// Gets the selected color in HSV format.
    /// </summary>
    public string? ColorHSV => FormatColor(ColorFormat.HSV);
    public static readonly DirectProperty<ColorPickerToolControl, string?> ColorHSVProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, string?>(nameof(ColorHSV), i => i.ColorHSV);


    /// <summary>
    /// Gets the selected color in CMYK format.
    /// </summary>
    public string? ColorCMYK => FormatColor(ColorFormat.CMYK);
    public static readonly DirectProperty<ColorPickerToolControl, string?> ColorCMYKProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, string?>(nameof(ColorCMYK), i => i.ColorCMYK);


    /// <summary>
    /// Gets the selected color in CIELAB format.
    /// </summary>
    public string? ColorCIELAB => FormatColor(ColorFormat.CIELAB);
    public static readonly DirectProperty<ColorPickerToolControl, string?> ColorCIELABProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerToolControl, string?>(nameof(ColorCIELAB), i => i.ColorCIELAB);


    #endregion // Public Properties



    public ColorPickerToolControl()
    {
        InitializeComponent();

        Settings = new ColorPickerConfig(ToolId);
    }



    #region Control Events

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Viewer.ViewerPointerMoved += Viewer_ViewerPointerMoved;
        Viewer.ViewerPointerPressed += Viewer_ViewerPointerPressed;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        Viewer.ViewerPointerMoved -= Viewer_ViewerPointerMoved;
        Viewer.ViewerPointerPressed -= Viewer_ViewerPointerPressed;
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == CurrentColorProperty)
        {
            RaisePropertyChanged(CurrentTextColorProperty, default, CurrentTextColor);
        }
        else if (e.Property == SelectedColorProperty)
        {
            RaisePropertyChanged(SelectedTextColorProperty, default, SelectedTextColor);

            RaisePropertyChanged(ColorRGBProperty, default, ColorRGB);
            RaisePropertyChanged(ColorHEXProperty, default, ColorHEX);
            RaisePropertyChanged(ColorHSLProperty, default, ColorHSL);
            RaisePropertyChanged(ColorHSVProperty, default, ColorHSV);
            RaisePropertyChanged(ColorCMYKProperty, default, ColorCMYK);
            RaisePropertyChanged(ColorCIELABProperty, default, ColorCIELAB);
        }
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        CopyButtonTooltipText = Core.Lang[LangId._Copy];
    }


    private void ButtonCopy_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not PhToolButton btn) return;
        if (btn.Content is not string colorCode) return;

        var top = TopLevel.GetTopLevel(this)?.Clipboard;
        _ = top?.SetTextAsync(colorCode);
    }


    private void Viewer_ViewerPointerMoved(ViewerControl sender, ViewerPointerEventArgs e)
    {
        var isValidSrcPoint = new Rect(sender.BitmapSize).Contains(e.SourcePoint.ToPoint(1));
        if (!isValidSrcPoint)
        {
            CurrentPoint = null;
            CurrentColor = Const.COLOR_EMPTY;
            return;
        }

        CurrentPoint = e.SourcePoint.ToPoint(1);
        CurrentColor = sender.GetColorAt(e.SourcePoint.X, e.SourcePoint.Y);
    }


    private void Viewer_ViewerPointerPressed(ViewerControl sender, ViewerPointerEventArgs e)
    {
        var isValidSrcPoint = new Rect(sender.BitmapSize).Contains(e.SourcePoint.ToPoint(1));
        if (!isValidSrcPoint) return;

        SelectedPoint = e.SourcePoint.ToPoint(1);
        SelectedColor = sender.GetColorAt(e.SourcePoint.X, e.SourcePoint.Y);
    }


    #endregion // Control Events



    #region Control Methods

    /// <summary>
    /// Determines the appropriate text color to use based on the specified background color.
    /// </summary>
    private Color GetPositionTextColor(Color c)
    {
        if (c.IsEmpty) return Core.Theme.InvertedBaseColor;
        if (c.A < 130) return Core.Theme.BaseColor.InvertBlackOrWhite();
        return CurrentColor.InvertBlackOrWhite();
    }


    /// <summary>
    /// Formats the currently selected color as a string in the specified color format.
    /// </summary>
    private string FormatColor(ColorFormat format, bool skipAlpha = false)
    {
        var code = format switch
        {
            ColorFormat.RGB => SelectedColor.ToRgbaString(),
            ColorFormat.HEX => SelectedColor.ToHex(skipAlpha),
            ColorFormat.HSL => SelectedColor.ToHslString(skipAlpha),
            ColorFormat.HSV => SelectedColor.ToHsvString(skipAlpha),
            ColorFormat.CMYK => SelectedColor.ToCmykString(skipAlpha),
            ColorFormat.CIELAB => SelectedColor.ToCIELABString(skipAlpha),
            _ => string.Empty,
        };

        return code;
    }

    #endregion // Control Methods


}



public enum ColorFormat
{
    RGB,
    HEX,
    HSL,
    HSV,
    CMYK,
    CIELAB,
}



/// <summary>
/// Provides settings for Color Picker tool.
/// </summary>
public class ColorPickerConfig(string toolId) : IToolConfig
{
    public string ToolId { get; init; } = toolId;


    /// <summary>
    /// Shows alpha value of RGB code.
    /// </summary>
    public bool ShowRgbWithAlpha { get; set; } = true;

    /// <summary>
    /// Shows alpha value of HEX code.
    /// </summary>
    public bool ShowHexWithAlpha { get; set; } = true;

    /// <summary>
    /// Shows alpha value of HSL code.
    /// </summary>
    public bool ShowHslWithAlpha { get; set; } = true;

    /// <summary>
    /// Shows alpha value of HSV code.
    /// </summary>
    public bool ShowHsvWithAlpha { get; set; } = true;

    /// <summary>
    /// Shows alpha value of CIELAB code.
    /// </summary>
    public bool ShowCIELabWithAlpha { get; set; } = true;


    public void LoadFromAppConfig(Config config)
    {
        //var toolConfig = Config.ToolSettings.GetValue(ToolId);
        //if (toolConfig is not ExpandoObject config) return;

        //// load configs
        //ShowRgbWithAlpha = config.GetValue(nameof(ShowRgbWithAlpha), ShowRgbWithAlpha);
        //ShowHexWithAlpha = config.GetValue(nameof(ShowHexWithAlpha), ShowHexWithAlpha);
        //ShowHslWithAlpha = config.GetValue(nameof(ShowHslWithAlpha), ShowHslWithAlpha);
        //ShowHsvWithAlpha = config.GetValue(nameof(ShowHsvWithAlpha), ShowHsvWithAlpha);
        //ShowCIELabWithAlpha = config.GetValue(nameof(ShowCIELabWithAlpha), ShowCIELabWithAlpha);
    }


    public void SaveToAppConfig(Config config)
    {
        //var settings = new ExpandoObject();

        //_ = settings.TryAdd(nameof(ShowRgbWithAlpha), ShowRgbWithAlpha);
        //_ = settings.TryAdd(nameof(ShowHexWithAlpha), ShowHexWithAlpha);
        //_ = settings.TryAdd(nameof(ShowHslWithAlpha), ShowHslWithAlpha);
        //_ = settings.TryAdd(nameof(ShowHsvWithAlpha), ShowHsvWithAlpha);
        //_ = settings.TryAdd(nameof(ShowCIELabWithAlpha), ShowCIELabWithAlpha);

        //// save to app config
        //Config.ToolSettings.Set(ToolId, settings);
    }
}

