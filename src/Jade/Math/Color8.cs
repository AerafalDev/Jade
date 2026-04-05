using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Jade.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Color8 :
    IFormattable,
    IEquatable<Color8>
{
    public readonly byte R;

    public readonly byte G;

    public readonly byte B;

    public readonly byte A;

    public Color8(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public bool Equals(Color8 other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    public override bool Equals(object? obj)
    {
        return obj is Color8 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        return $"Color8(R={R.ToString(format, formatProvider)}, G={G.ToString(format, formatProvider)}, B={B.ToString(format, formatProvider)}, A={A.ToString(format, formatProvider)}";
    }

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
    {
        return ToString(format, CultureInfo.CurrentCulture);
    }

    public override string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    public static bool operator ==(Color8 left, Color8 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color8 left, Color8 right)
    {
        return !left.Equals(right);
    }
}
