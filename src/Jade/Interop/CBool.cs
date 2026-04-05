using System.Runtime.InteropServices;

namespace Jade.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct CBool : IEquatable<CBool>
{
    private readonly byte _value;

    private CBool(bool value)
    {
        _value = Convert.ToByte(value);
    }

    public bool Equals(CBool other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CBool b && Equals(b);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return ((bool)this).ToString();
    }

    public static bool operator ==(CBool left, CBool right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CBool left, CBool right)
    {
        return !left.Equals(right);
    }

    public static implicit operator CBool(bool value)
    {
        return new CBool(value);
    }

    public static implicit operator bool(CBool value)
    {
        return Convert.ToBoolean(value._value);
    }
}
