using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Jade.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct CString : IEquatable<CString>
{
    private readonly nint _ptr;

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ptr == nint.Zero;
    }

    private CString(nint value)
    {
        _ptr = value;
    }

    private CString(ReadOnlySpan<byte> value)
    {
        fixed (byte* ptr = value)
            _ptr = (nint)ptr;
    }

    public bool Equals(CString other)
    {
        return _ptr == other._ptr;
    }

    public override bool Equals(object? obj)
    {
        return obj is CString value && Equals(value);
    }

    public override int GetHashCode()
    {
        return _ptr.GetHashCode();
    }

    public override string ToString()
    {
        if (IsNull)
            return string.Empty;

        var end = (byte*)_ptr;

        while (*end is not 0)
            end++;

        return new string((sbyte*)_ptr, 0, (int)(end - (byte*)_ptr), Encoding.UTF8);
    }

    public static bool operator ==(CString left, CString right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CString left, CString right)
    {
        return !left.Equals(right);
    }

    public static implicit operator CString(ReadOnlySpan<byte> value)
    {
        return new CString(value);
    }

    public static explicit operator CString(nint value)
    {
        return new CString(value);
    }

    public static implicit operator nint(CString value)
    {
        return value._ptr;
    }

    public static implicit operator CString(byte* value)
    {
        return new CString((nint)value);
    }

    public static implicit operator byte*(CString value)
    {
        return (byte*)value._ptr;
    }

    public static implicit operator CString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;

        var size = (nuint)(Encoding.UTF8.GetMaxByteCount(value.Length) + 1);
        var buffer = (byte*)NativeMemory.AllocZeroed(size);

        fixed (char* strPtr = value)
            Encoding.UTF8.GetBytes(strPtr, value.Length, buffer, (int)size);

        return new CString((nint)buffer);
    }

    public static implicit operator string(CString value)
    {
        return value.ToString();
    }
}
