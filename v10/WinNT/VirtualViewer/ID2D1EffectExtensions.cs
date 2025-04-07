using Vortice.DCommon;
using Vortice.Direct2D1;

namespace ImageGlass.WinNT;

public static class ID2D1EffectExtensions
{

    /// <summary>
    /// Gets <see cref="ID2D1Bitmap1"/> from <see cref="ID2D1Effect"/>.
    /// </summary>
    public static ID2D1Bitmap1 GetD2D1Bitmap1(this ID2D1Effect effect,
        ID2D1DeviceContext7 dc, bool ignoreAlpha = false)
    {
        var bmpProps = new BitmapProperties1()
        {
            BitmapOptions = BitmapOptions.Target,
            PixelFormat = new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
        };


        // create empty bitmap from the effect output
        using var effectOutputImage = effect.Output;
        var outputRect = dc.GetImageLocalBounds(effectOutputImage);
        var outputSize = new Vortice.Mathematics.SizeI(
            (int)(outputRect.Right - outputRect.Left),
            (int)(outputRect.Bottom - outputRect.Top));
        var newD2dBitmap = dc.CreateBitmap(outputSize, bmpProps);


        // save current Target, replace by ID2D1Bitmap
        using var oldTarget = dc.Target;
        dc.Target = newD2dBitmap;


        // draw Image on Target
        dc.BeginDraw();
        if (ignoreAlpha)
        {
            // fill back background if alpha is ignored
            using var brush = dc.CreateSolidColorBrush(Vortice.Mathematics.Colors.Black);
            dc.FillRectangle(outputRect, brush);
        }
        dc.DrawImage(effectOutputImage);
        dc.EndDraw();


        // set previous Target
        dc.Target = oldTarget;


        // release resources
        oldTarget.Dispose();
        effectOutputImage.Dispose();

        return newD2dBitmap;
    }

}
