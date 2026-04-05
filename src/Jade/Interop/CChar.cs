using System.Globalization;
using System.Runtime.InteropServices;

namespace Jade.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct CChar : IEquatable<CChar>
{
    private readonly byte _value;

    private CChar(byte value)
    {
        _value = value;
    }

    public bool Equals(CChar other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CChar b && Equals(b);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return _value.ToString(CultureInfo.CurrentCulture);
    }

    public static bool operator ==(CChar left, CChar right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CChar left, CChar right)
    {
        return !left.Equals(right);
    }

    public static implicit operator CChar(byte value)
    {
        return new CChar(value);
    }

    public static implicit operator byte(CChar value)
    {
        return value._value;
    }
}
