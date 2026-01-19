using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ImageGlass.Common.Types;
using System;

namespace ImageGlass.Lib.UI;


public partial class IgWindow : Window
{

    #region Public Properties

    /// <summary>
    /// Gets the handle of this window.
    /// </summary>
    public nint Handle => GetTopLevel(this)?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;



    /// <summary>
    /// Gets, sets the window backdrop style.
    /// </summary>
    public BackdropStyle BackdropStyle
    {
        get => GetValue(BackdropStyleProperty);
        set => SetValue(BackdropStyleProperty, value);
    }
    public static readonly StyledProperty<BackdropStyle> BackdropStyleProperty =
        AvaloniaProperty.Register<Window, BackdropStyle>(nameof(BackdropStyle), BackdropStyle.Mica);


    #endregion // Public Properties




    public IgWindow()
    {
        OnBackdropStyleChanged(BackdropStyle);
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // BackdropStyle
        if (e.Property == BackdropStyleProperty)
        {
            OnBackdropStyleChanged((BackdropStyle)e.NewValue!);
        }

    }


    /// <summary>
    /// Updates backdrop style of the window.
    /// </summary>
    protected virtual void OnBackdropStyleChanged(BackdropStyle style)
    {
        Background = style == BackdropStyle.None
            ? null
            : Brushes.Transparent;
    }

}
