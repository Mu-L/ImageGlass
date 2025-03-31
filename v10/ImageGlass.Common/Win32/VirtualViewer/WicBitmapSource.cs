using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.WIC;



namespace ImageGlass.Common.WinOS;

public static class WicBitmapSource
{

    public static IWICBitmapSource Load(string filePath, uint frameIndex = 0)
    {
        using var wicFactory = new IWICImagingFactory2();
        using var decoder = wicFactory.CreateDecoderFromFileName(filePath);
        using var frameBmp = decoder.GetFrame(frameIndex);

        return WIC.WICConvertBitmapSource(Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA, frameBmp);
    }

}



