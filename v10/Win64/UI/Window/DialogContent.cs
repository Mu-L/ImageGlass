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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace ImageGlass.Win64.UI;


[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_Titlebar", Type = typeof(TitlebarControl))]
[TemplatePart(Name = "PART_Content", Type = typeof(Grid))]
[TemplatePart(Name = "PART_Footer", Type = typeof(Grid))]
[TemplatePart(Name = "PART_Button1", Type = typeof(Button))]
[TemplatePart(Name = "PART_Button2", Type = typeof(Button))]
[TemplatePart(Name = "PART_Button2", Type = typeof(Button))]
public sealed partial class DialogContent : ContentControl
{
    public static string _PART_Titlebar => "PART_Titlebar";
    public static string _PART_Content => "PART_Content";
    public static string _PART_Footer => "PART_Footer";
    public static string _PART_Button1 => "PART_Button1";
    public static string _PART_Button2 => "PART_Button2";
    public static string _PART_Button3 => "PART_Button3";

    public event RoutedEventHandler? Button1Click;
    public event RoutedEventHandler? Button2Click;
    public event RoutedEventHandler? Button3Click;


#nullable disable
    public TitlebarControl Titlebar;

    public Button Button1;
    public Button Button2;
    public Button Button3;
#nullable enable


    public DialogContent()
    {
        DefaultStyleKey = typeof(DialogContent);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        Titlebar = (TitlebarControl)GetTemplateChild(_PART_Titlebar);
        Button1 = (Button)GetTemplateChild(_PART_Button1);
        Button2 = (Button)GetTemplateChild(_PART_Button2);
        Button3 = (Button)GetTemplateChild(_PART_Button3);

        Button1.Click += Button1_Click;
        Button2.Click += Button2_Click;
        Button3.Click += Button3_Click;
    }


    protected override Size MeasureOverride(Size availableSize)
    {
        var size = this.ActualSize;

        return base.MeasureOverride(availableSize);
    }

    private void Button1_Click(object sender, RoutedEventArgs e)
    {
        Button1Click?.Invoke(sender, e);
    }

    private void Button2_Click(object sender, RoutedEventArgs e)
    {
        Button2Click?.Invoke(sender, e);
    }

    private void Button3_Click(object sender, RoutedEventArgs e)
    {
        Button3Click?.Invoke(sender, e);
    }


}
