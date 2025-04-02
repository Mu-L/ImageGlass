using Microsoft.UI.Xaml.Controls;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using Vortice.WIC;



namespace ImageGlass.WinNT;


public static class Wic
{

    /// <summary>
    /// Loads a bitmap image and convert
    /// to <see cref="Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA"/> format.
    /// </summary>
    /// <exception cref="DirectXException"></exception>
    public static IWICBitmapSource? Load(string filePath, uint frameIndex = 0)
    {
        IWICBitmapSource? bmp = null;

        try
        {
            using var wicFactory = new IWICImagingFactory2();
            using var decoder = wicFactory.CreateDecoderFromFileName(filePath);
            using var frameBmp = decoder.GetFrame(frameIndex);

            bmp = WIC.WICConvertBitmapSource(Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA, frameBmp);
        }
        catch (SharpGenException ex)
        {
            // TODO
        }

        return bmp;
    }




}



