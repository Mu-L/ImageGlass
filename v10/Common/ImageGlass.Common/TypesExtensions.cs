

using ImageMagick;
using System.Numerics;

namespace ImageGlass.Common;

public static class TypesExtensions
{

    /// <summary>
    /// Converts the current <see cref="MagickColor"/>
    /// to a <see cref="Vector4"/> <c>(R = X, G = Y, B = Z, A = W)</c>.
    /// </summary>
    public static Vector4 ToVector4(this MagickColor self)
    {
        return new Vector4(self.R, self.G, self.B, self.A);
    }

}
