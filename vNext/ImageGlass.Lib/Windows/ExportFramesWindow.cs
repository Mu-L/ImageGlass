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
using Avalonia.Threading;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.UI.Windowing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.Windows;

public partial class ExportFramesWindow : DialogWindow
{
    private CancellationTokenSource _cancel = new();

    private string _srcFilePath;
    private string _destDirPath;
    private bool _isDone = false;


    #region Public Properties

    /// <summary>
    /// Gets, sets the description.
    /// </summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<ExportFramesWindow, string?>(nameof(Description));


    /// <summary>
    /// Gets, sets the progress value.
    /// </summary>
    public double ProgressValue
    {
        get => GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }
    public static readonly StyledProperty<double> ProgressValueProperty =
        AvaloniaProperty.Register<ExportFramesWindow, double>(nameof(ProgressValue));


    #endregion // Public Properties



    public ExportFramesWindow(string srcFilePath, string destDirPath)
    {
        if (string.IsNullOrEmpty(srcFilePath)) throw new ArgumentNullException(nameof(srcFilePath));
        if (string.IsNullOrEmpty(destDirPath)) throw new ArgumentNullException(nameof(destDirPath));

        _srcFilePath = srcFilePath;
        _destDirPath = destDirPath;

        IsButton1Visible = true;
        IsButton2Visible = true;
        IsButton3Visible = false;
        Width = 500;
        MaxWidth = 500;
        Description = destDirPath;

        DialogContent = CreateDialogContentElement();
    }


    #region Override Methods

    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        Title = Core.Lang[LangId.FrmExportFrames_Title];
        Button1Text = Core.Lang[LangId._Start];
        Button2Text = Core.Lang[LangId._Cancel];
    }


    protected override void OnDialogSubmitted(DialogEventArgs e)
    {
        if (_isDone)
        {
            BHelper.OpenFolderPath(_destDirPath);
        }
        else
        {
            _ = ExportAsync(_srcFilePath, _destDirPath);
        }
    }


    protected override void OnDialogCancelled(DialogEventArgs e)
    {
        _cancel?.Cancel();
        _cancel?.Dispose();

        base.OnDialogCancelled(e);
    }


    protected override void OnDialogAborted()
    {
        _cancel?.Cancel();
        _cancel?.Dispose();

        base.OnDialogAborted();
    }

    #endregion // Override Methods



    #region Private methods

    /// <summary>
    /// Creates dialog content element.
    /// </summary>
    private StackPanel CreateDialogContentElement()
    {

        // 1. create root element
        var root = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Spacing = 16,
        };

        root.Children.Add(new TextBlock
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            [!TextBlock.TextProperty] = this[!DescriptionProperty],
        });

        root.Children.Add(new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            [!ProgressBar.ValueProperty] = this[!ProgressValueProperty],
        });

        return root;
    }


    /// <summary>
    /// Exports frames from the specified source file to the destination directory asynchronously.
    /// </summary>
    private async Task ExportAsync(string srcFilePath, string destDirPath)
    {
        _isDone = false;
        ProgressValue = 0;
        IsButton1Visible = false;
        Button2Text = Core.Lang[LangId._Cancel];


        _ = Task.Factory.StartNew(async () =>
        {
            // start exporting frames
            await foreach (var info in MagickCodec.SaveFramesAsync(srcFilePath, destDirPath, _cancel.Token))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var percent = Math.Round((info.FrameNumber * 100f) / info.FrameCount, 0);

                    ProgressValue = percent;
                    Title = $"{Core.Lang[LangId.FrmExportFrames_Title]} ({percent}%)";

                    // done
                    if (info.FrameNumber == info.FrameCount)
                    {
                        IsButton1Visible = true;
                        Button1Text = Core.Lang[LangId.FrmExportFrames_OpenOutputFolder];
                        Button2Text = Core.Lang[LangId._Close];
                        Description = string.Format(Core.Lang[LangId.FrmExportFrames_ExportDone],
                            info.FrameNumber,
                            $"\"{destDirPath}\"");

                        _isDone = true;
                    }

                    // in progress
                    else
                    {
                        var frameFilePath = Path.Combine(destDirPath, info.FileName);

                        Description = string.Format(Core.Lang[LangId.FrmExportFrames_Exporting],
                            info.FrameNumber,
                            info.FrameCount,
                            $"\"{frameFilePath}\"");
                    }
                });
            }
        }, _cancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
    }

    #endregion // Private Methods


}
