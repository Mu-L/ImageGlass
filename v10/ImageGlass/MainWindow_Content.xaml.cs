using ImageGlass.Win64.UI;
using ImageGlass.WinNT;
using Microsoft.UI.Xaml;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;


namespace ImageGlass;

public sealed partial class MainWindow_Content : IgControl
{
    public event TypedEventHandler<IgToolbarButton, ToolbarItemClickedEventArgs>? ToolbarButtonClicked;
    public event TypedEventHandler<IgGalleryItem, EventArgs>? GalleryItemClicked;
    public event TypedEventHandler<VirtualViewerControl, DragEventArgs>? ViewerDrop;


    public ToolbarControl ToolbarMain => PART_ToolbarMain;
    public GalleryControl Gallery => PART_Gallery;
    public VirtualViewerControl Viewer => PART_Viewer;



    public MainWindow_Content()
    {
        InitializeComponent();
    }



    private void PART_ToolbarMain_ItemClicked(IgToolbarButton sender, ToolbarItemClickedEventArgs e)
    {
        ToolbarButtonClicked?.Invoke(sender, e);
    }


    private void PART_Gallery_ItemClicked(IgGalleryItem sender, EventArgs e)
    {
        GalleryItemClicked?.Invoke(sender, e);
    }


    private void PART_Viewer_DragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = "Open with ImageGlass";
    }


    private void PART_Viewer_Drop(object sender, DragEventArgs e)
    {
        ViewerDrop?.Invoke((VirtualViewerControl)sender, e);
    }


}
