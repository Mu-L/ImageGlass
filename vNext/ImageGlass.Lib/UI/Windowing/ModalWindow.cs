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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ImageGlass.Common;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Types;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ImageGlass.UI.Windowing;

public partial class ModalWindow : DialogWindow
{
    private readonly double THUMBNAIL_SIZE = 80;

    protected TextBox _txtInput;
    protected CheckBox _chkRememberOption;



    #region Public Properties

    /// <summary>
    /// Gets, sets the text of modal's heading.
    /// </summary>
    public string? Heading
    {
        get => GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }
    public static readonly StyledProperty<string?> HeadingProperty =
        AvaloniaProperty.Register<ModalWindow, string?>(nameof(Heading));


    /// <summary>
    /// Gets, sets the text of modal's description.
    /// </summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<ModalWindow, string?>(nameof(Description));


    /// <summary>
    /// Gets, sets the text of modal's details.
    /// </summary>
    public string? Details
    {
        get => GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }
    public static readonly StyledProperty<string?> DetailsProperty =
        AvaloniaProperty.Register<ModalWindow, string?>(nameof(Details));


    /// <summary>
    /// Gets, sets the text of modal's note.
    /// </summary>
    public string? Note
    {
        get => GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }
    public static readonly StyledProperty<string?> NoteProperty =
        AvaloniaProperty.Register<ModalWindow, string?>(nameof(Note));


    /// <summary>
    /// Gets, sets the style of modal's note.
    /// </summary>
    public InfoBarSeverity NoteStyle
    {
        get => GetValue(NoteStyleProperty);
        set => SetValue(NoteStyleProperty, value);
    }
    public static readonly StyledProperty<InfoBarSeverity> NoteStyleProperty =
        AvaloniaProperty.Register<ModalWindow, InfoBarSeverity>(nameof(NoteStyle), InfoBarSeverity.Info);


    /// <summary>
    /// Gets, sets the thumbnail of modal.
    /// </summary>
    public Bitmap? Thumbnail
    {
        get => GetValue(ThumbnailProperty);
        set => SetValue(ThumbnailProperty, value);
    }
    public static readonly StyledProperty<Bitmap?> ThumbnailProperty =
        AvaloniaProperty.Register<ModalWindow, Bitmap?>(nameof(Thumbnail));


    /// <summary>
    /// Gets, sets the thumbnail icon of modal.
    /// </summary>
    public Bitmap? ThumbnailIcon
    {
        get => GetValue(ThumbnailIconProperty);
        set => SetValue(ThumbnailIconProperty, value);
    }
    public static readonly StyledProperty<Bitmap?> ThumbnailIconProperty =
        AvaloniaProperty.Register<ModalWindow, Bitmap?>(nameof(ThumbnailIcon));


    /// <summary>
    /// Gets, sets the value of input control.
    /// </summary>
    public string InputValue
    {
        get => GetValue(InputValueProperty);
        set => SetValue(InputValueProperty, value);
    }
    public static readonly StyledProperty<string> InputValueProperty =
        AvaloniaProperty.Register<ModalWindow, string>(nameof(InputValue), string.Empty);


    /// <summary>
    /// Gets, sets the rules for the input control.
    /// </summary>
    public TextBoxAcceptValue AcceptValue
    {
        get => GetValue(AcceptValueProperty);
        set => SetValue(AcceptValueProperty, value);
    }
    public static readonly StyledProperty<TextBoxAcceptValue> AcceptValueProperty =
        AvaloniaProperty.Register<ModalWindow, TextBoxAcceptValue>(nameof(AcceptValue), TextBoxAcceptValue.Any);


    /// <summary>
    /// Gets the visibility of heading control.
    /// </summary>
    public bool IsHeadingVisible => !string.IsNullOrWhiteSpace(Heading);
    public static readonly DirectProperty<ModalWindow, bool> IsHeadingVisibleProperty =
        AvaloniaProperty.RegisterDirect<ModalWindow, bool>(nameof(IsHeadingVisible), i => i.IsHeadingVisible);


    /// <summary>
    /// Gets the visibility of description control.
    /// </summary>
    public bool IsDescriptionVisible => !string.IsNullOrWhiteSpace(Description);
    public static readonly DirectProperty<ModalWindow, bool> IsDescriptionVisibleProperty =
        AvaloniaProperty.RegisterDirect<ModalWindow, bool>(nameof(IsDescriptionVisible), i => i.IsDescriptionVisible);


    /// <summary>
    /// Gets the visibility of details control.
    /// </summary>
    public bool IsDetailsVisible => !string.IsNullOrWhiteSpace(Details);
    public static readonly DirectProperty<ModalWindow, bool> IsDetailsVisibleProperty =
        AvaloniaProperty.RegisterDirect<ModalWindow, bool>(nameof(IsDetailsVisible), i => i.IsDetailsVisible);


    /// <summary>
    /// Gets the visibility of thumbnail.
    /// </summary>
    public bool IsThumbnailVisible => Thumbnail is not null;
    public static readonly DirectProperty<ModalWindow, bool> IsThumbnailVisibleProperty =
        AvaloniaProperty.RegisterDirect<ModalWindow, bool>(nameof(IsThumbnailVisible), i => i.IsThumbnailVisible);


    /// <summary>
    /// Gets the visibility of thumbnail icon.
    /// </summary>
    public bool IsThumbnailIconVisible => ThumbnailIcon is not null;
    public static readonly DirectProperty<ModalWindow, bool> IsThumbnailIconVisibleProperty =
        AvaloniaProperty.RegisterDirect<ModalWindow, bool>(nameof(IsThumbnailIconVisible), i => i.IsThumbnailIconVisible);


    /// <summary>
    /// Gets the visibility of thumbnail section.
    /// </summary>
    public bool IsThumbnailSectionVisible => Thumbnail is not null || ThumbnailIcon is not null;
    public static readonly DirectProperty<ModalWindow, bool> IsThumbnailSectionVisibleProperty =
        AvaloniaProperty.RegisterDirect<ModalWindow, bool>(nameof(IsThumbnailSectionVisible), i => i.IsThumbnailSectionVisible);


    /// <summary>
    /// Gets the visibility of note control.
    /// </summary>
    public bool IsNoteVisible => !string.IsNullOrWhiteSpace(Note);
    public static readonly DirectProperty<ModalWindow, bool> IsNoteVisibleProperty =
        AvaloniaProperty.RegisterDirect<ModalWindow, bool>(nameof(IsNoteVisible), i => i.IsNoteVisible);


    /// <summary>
    /// Gets, sets the visibility of input control.
    /// </summary>
    public bool IsInputVisible
    {
        get => GetValue(IsInputVisibleProperty);
        set => SetValue(IsInputVisibleProperty, value);
    }
    public static readonly StyledProperty<bool> IsInputVisibleProperty =
        AvaloniaProperty.Register<ModalWindow, bool>(nameof(IsInputVisible));


    /// <summary>
    /// Gets, sets the visibility of remember checkbox.
    /// </summary>
    public bool IsRememberOptionVisible
    {
        get => GetValue(IsRememberOptionVisibleProperty);
        set => SetValue(IsRememberOptionVisibleProperty, value);
    }
    public static readonly StyledProperty<bool> IsRememberOptionVisibleProperty =
        AvaloniaProperty.Register<ModalWindow, bool>(nameof(IsRememberOptionVisible));


    /// <summary>
    /// Gets, sets the text of remember option checkbox.
    /// </summary>
    public string RememberOptionText
    {
        get => GetValue(RememberOptionTextProperty);
        set => SetValue(RememberOptionTextProperty, value);
    }
    public static readonly StyledProperty<string> RememberOptionTextProperty =
        AvaloniaProperty.Register<ModalWindow, string>(nameof(RememberOptionText), "[Don't show this message again]");


    /// <summary>
    /// Gets the check state of Remember option checkbox.
    /// </summary>
    public bool IsRememberOptionChecked => _chkRememberOption?.IsChecked ?? false;

    #endregion // Public Properties



    public ModalWindow()
    {
        DialogContent = CreateModalWindowContentElement();
    }


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (IsInputVisible)
        {
            _txtInput.Focus(Avalonia.Input.NavigationMethod.Tab);
            _txtInput.SelectAll();
        }
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ThumbnailProperty)
        {
            RaisePropertyChanged(IsThumbnailVisibleProperty, default, IsThumbnailVisible);
            RaisePropertyChanged(IsThumbnailSectionVisibleProperty, default, IsThumbnailSectionVisible);
        }
        else if (e.Property == ThumbnailIconProperty)
        {
            RaisePropertyChanged(IsThumbnailIconVisibleProperty, default, IsThumbnailIconVisible);
            RaisePropertyChanged(IsThumbnailSectionVisibleProperty, default, IsThumbnailSectionVisible);
        }
        else if (e.Property == NoteProperty)
        {
            RaisePropertyChanged(IsNoteVisibleProperty, default, IsNoteVisible);
        }
        else if (e.Property == HeadingProperty)
        {
            RaisePropertyChanged(IsHeadingVisibleProperty, default, IsHeadingVisible);
        }
        else if (e.Property == DescriptionProperty)
        {
            RaisePropertyChanged(IsDescriptionVisibleProperty, default, IsDescriptionVisible);
        }
        else if (e.Property == DetailsProperty)
        {
            RaisePropertyChanged(IsDetailsVisibleProperty, default, IsDetailsVisible);
        }
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        RememberOptionText = Core.Lang[LangId._DoNotShowThisMessageAgain];
    }


    protected override void OnDialogSubmitted(DialogEventArgs e)
    {
        // TODO: validate

        base.OnDialogSubmitted(e);
    }




    [MemberNotNull(nameof(_txtInput), nameof(_chkRememberOption))]
    private StackPanel CreateModalWindowContentElement()
    {
        // 1. create the main 2-column layout
        // 1.1 create left section
        var thumbnailViewbox = new Viewbox
        {
            Width = THUMBNAIL_SIZE,
            Height = THUMBNAIL_SIZE,
            Stretch = Avalonia.Media.Stretch.Uniform,
            StretchDirection = Avalonia.Media.StretchDirection.DownOnly,
            [!Viewbox.IsVisibleProperty] = this[!IsThumbnailVisibleProperty],
            Child = new Image
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                [!Image.SourceProperty] = this[!ThumbnailProperty],
            },
        };
        var thumbnailIconImage = new Image
        {
            Width = 40,
            Height = 40,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
            [!Image.SourceProperty] = this[!ThumbnailIconProperty],
            [!Image.IsVisibleProperty] = this[!IsThumbnailIconVisibleProperty],
        };
        var leftSection = new Grid
        {
            Margin = new Thickness(0, 0, 24, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            [!Grid.IsVisibleProperty] = this[!IsThumbnailSectionVisibleProperty],
        };
        leftSection.Children.Add(thumbnailViewbox);
        leftSection.Children.Add(thumbnailIconImage);


        // 1.2 create right section
        var rightSection = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Spacing = 16,
        };
        var lblHeading = new TextBlock
        {
            MaxHeight = 200,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            IsTabStop = false,
            FontSize = Const.FONT_SIZE_TITLE,
            FontWeight = Avalonia.Media.FontWeight.Medium,
            [!TextBlock.TextProperty] = this[!HeadingProperty],
            [!TextBlock.IsVisibleProperty] = this[!IsHeadingVisibleProperty],
            [!TextBlock.ForegroundProperty] = Resx.CreateBinding(ResxId.SystemAccentColor),
        };
        var lblDescription = new TextBlock
        {
            MaxHeight = 200,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            IsTabStop = false,
            [!TextBlock.TextProperty] = this[!DescriptionProperty],
            [!TextBlock.IsVisibleProperty] = this[!IsDescriptionVisibleProperty],
        };
        _txtInput = new TextBox
        {
            [!TextBox.TextProperty] = this[!InputValueProperty],
            [!TextBox.IsVisibleProperty] = this[!IsInputVisibleProperty],
        };
        var lblDetails = new Border
        {
            ClipToBounds = true,
            [!Border.CornerRadiusProperty] = Resx.CreateBinding(ResxId.ControlCornerRadius),

            Child = new ScrollViewer
            {
                MinHeight = 50,
                MaxHeight = 150,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = new SelectableTextBlock
                {
                    Padding = new Thickness(6),
                    FontSize = Const.FONT_SIZE_SMALL,
                    FontFamily = Const.FONT_CODE,
                    FontWeight = Avalonia.Media.FontWeight.SemiLight,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    [!SelectableTextBlock.TextProperty] = this[!DetailsProperty],
                    [!SelectableTextBlock.IsVisibleProperty] = this[!IsDetailsVisibleProperty],
                    [!SelectableTextBlock.BackgroundProperty] = Resx.CreateBinding(ResxId.TextControlBackground),
                },
            }
        };
        rightSection.Children.Add(lblHeading);
        rightSection.Children.Add(lblDescription);
        rightSection.Children.Add(_txtInput);
        rightSection.Children.Add(lblDetails);

        Grid.SetColumn(leftSection, 0);
        Grid.SetColumn(rightSection, 1);
        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *"),
        };
        mainGrid.Children.Add(leftSection);
        mainGrid.Children.Add(rightSection);


        // 2. create note panel
        var noteContainer = new Border
        {
            ClipToBounds = true,
            [!Border.CornerRadiusProperty] = Resx.CreateBinding(ResxId.ControlCornerRadius),

            Child = new TextBlock
            {
                Padding = new Thickness(12),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                [!TextBlock.BackgroundProperty] = Resx.CreateBinding(ResxId.IG_BackgroundDangerBrush),
                [!TextBlock.TextProperty] = this[!NoteProperty],
                [!TextBlock.IsVisibleProperty] = this[!IsNoteVisibleProperty],
            }
        };
        _chkRememberOption = new CheckBox
        {
            [!CheckBox.IsVisibleProperty] = this[!IsRememberOptionVisibleProperty],
            Content = new TextBlock
            {
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                [!TextBlock.TextProperty] = this[!RememberOptionTextProperty],
            },
        };
        var notePanel = new StackPanel
        {
            Spacing = 8,
            Orientation = Avalonia.Layout.Orientation.Vertical,
        };
        notePanel.Children.Add(noteContainer);
        notePanel.Children.Add(_chkRememberOption);



        // 3. create root element
        var root = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Spacing = 16,
        };
        root.Children.Add(mainGrid);
        root.Children.Add(notePanel);

        return root;
    }





    /// <summary>
    /// Shows modal dialog.
    /// </summary>
    public static async Task<ModalWindowResult> ShowAsync(IgWindow? owner,
        string? title,
        ModalWindowButton buttons,
        ModalWindowOptions options,
        DialogFocus defaultFocus = DialogFocus.Button1)
    {
        var modal = new ModalWindow()
        {
            Title = title,
            DefaultFocus = defaultFocus,
            Heading = options.Heading,
            Description = options.Description,
            Details = options.Details,
            Note = options.Note,
            NoteStyle = options.NoteStyle,
            Thumbnail = options.Thumbnail,
            ThumbnailIcon = options.ThumbnailIcon,
            InputValue = options.InputValue,
            IsInputVisible = options.IsInputVisible,
            IsRememberOptionVisible = options.IsRememberOptionVisible,
        };

        switch (buttons)
        {
            case ModalWindowButton.OK:
                modal.Button1Text = Core.Lang[LangId._OK];
                modal.IsButton1Visible = true;
                modal.IsButton2Visible = modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.Close:
                modal.Button1Text = Core.Lang[LangId._Close];
                modal.IsButton1Visible = true;
                modal.IsButton2Visible = modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.Yes_No:
                modal.Button1Text = Core.Lang[LangId._Yes];
                modal.Button2Text = Core.Lang[LangId._No];
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.OK_Cancel:
                modal.Button1Text = Core.Lang[LangId._OK];
                modal.Button2Text = Core.Lang[LangId._Cancel];
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.OK_Close:
                modal.Button1Text = Core.Lang[LangId._OK];
                modal.Button2Text = Core.Lang[LangId._Close];
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.LearnMore_Close:
                modal.Button1Text = Core.Lang[LangId._LearnMore];
                modal.Button2Text = Core.Lang[LangId._Close];
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            case ModalWindowButton.Continue_Quit:
                modal.Button1Text = Core.Lang[LangId._Continue];
                modal.Button2Text = Core.Lang[LangId._Quit];
                modal.IsButton1Visible = modal.IsButton2Visible = true;
                modal.IsButton3Visible = false;
                break;

            default:
                break;
        }


        var exitCode = await modal.ShowAsync(owner);

        // get dialog result
        var result = new ModalWindowResult()
        {
            ExitCode = exitCode,
            InputValue = modal.InputValue,
            IsRememberOptionChecked = modal.IsRememberOptionChecked,
        };

        return result;
    }



}
