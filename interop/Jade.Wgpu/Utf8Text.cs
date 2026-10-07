using System.Runtime.InteropServices;
using System.Text;

namespace Jade.Wgpu;

/// <summary>
/// Text passed to the native library: a UTF-8 span, passed without a copy where the call can pin
/// it, or a string, encoded to UTF-8 for the call (ADR 0016, ADR 0029).
/// </summary>
/// <remarks>
/// The null string and the empty string stay distinct, as <c>WGPUStringView</c> requires: a
/// <see langword="default"/> value or a <see langword="null"/> string is the null string, an empty
/// span or string is the empty one.
/// </remarks>
public readonly ref struct Utf8Text
{
    /// <summary>The string, when the text is a string.</summary>
    private readonly string? _text;

    /// <summary>Whether the text is a span, which may be empty.</summary>
    private readonly bool _isUtf8;

    /// <summary>Initializes a new instance of the <see cref="Utf8Text"/> struct from UTF-8 bytes.</summary>
    /// <param name="utf8">The bytes, which the caller keeps alive and unchanged during the call.</param>
    public Utf8Text(ReadOnlySpan<byte> utf8)
    {
        Bytes = utf8;
        _isUtf8 = true;
    }

    /// <summary>Initializes a new instance of the <see cref="Utf8Text"/> struct from a string.</summary>
    /// <param name="text">The string, or <see langword="null"/> for the null string.</param>
    public Utf8Text(string? text)
    {
        _text = text;
    }

    /// <summary>Gets whether the text is the null string.</summary>
    public bool IsNull => !_isUtf8 && _text is null;

    /// <summary>Gets the bytes of a span, which a call pins; empty for a string.</summary>
    internal ReadOnlySpan<byte> Bytes { get; }

    /// <summary>Converts UTF-8 bytes, such as a <c>u8</c> literal.</summary>
    /// <param name="utf8">The bytes.</param>
    public static implicit operator Utf8Text(ReadOnlySpan<byte> utf8)
    {
        return new Utf8Text(utf8);
    }

    /// <summary>Converts a string.</summary>
    /// <param name="text">The string, or <see langword="null"/> for the null string.</param>
    public static implicit operator Utf8Text(string? text)
    {
        return new Utf8Text(text);
    }

    /// <summary>Creates text from UTF-8 bytes, as the implicit conversion does.</summary>
    /// <param name="utf8">The bytes.</param>
    /// <returns>The text.</returns>
    public static Utf8Text FromReadOnlySpan(ReadOnlySpan<byte> utf8)
    {
        return new Utf8Text(utf8);
    }

    /// <summary>Creates text from a string, as the implicit conversion does.</summary>
    /// <param name="text">The string, or <see langword="null"/> for the null string.</param>
    /// <returns>The text.</returns>
    public static Utf8Text FromString(string? text)
    {
        return new Utf8Text(text);
    }

    /// <summary>Decodes the text.</summary>
    /// <returns>The text, or an empty string for the null string.</returns>
    public override string ToString()
    {
        return _isUtf8 ? Encoding.UTF8.GetString(Bytes) : _text ?? string.Empty;
    }

    /// <summary>Writes the string view of the text.</summary>
    /// <param name="pinned">The address of <see cref="Bytes"/> when the caller pinned it, or <see langword="null"/> to copy the bytes.</param>
    /// <param name="arena">The memory of the call.</param>
    /// <returns>The string view, valid until the call returns.</returns>
    internal unsafe Raw.StringView Lower(byte* pinned, scoped ref Arena arena)
    {
        return _isUtf8
            ? new Raw.StringView { Data = pinned != null ? pinned : arena.Copy(Bytes), Length = (nuint)Bytes.Length }
            : Lower(_text, ref arena);
    }

    /// <summary>Writes the string view of a string.</summary>
    /// <param name="text">The string, or <see langword="null"/> for the null string.</param>
    /// <param name="arena">The memory of the call.</param>
    /// <returns>The string view, valid until the call returns.</returns>
    internal static unsafe Raw.StringView Lower(string? text, scoped ref Arena arena)
    {
        if (text is null)
        {
            return new Raw.StringView();
        }

        var data = arena.CopyUtf8(text, out var length);

        return new Raw.StringView { Data = data, Length = (nuint)length };
    }

    /// <summary>Decodes a string view that the library returns.</summary>
    /// <param name="view">The string view.</param>
    /// <returns>The string, or <see langword="null"/> for the null string.</returns>
    internal static unsafe string? ToManagedString(Raw.StringView view)
    {
        return view.Length == Raw.NativeMethods.Strlen
            ? view.Data == null ? null : Marshal.PtrToStringUTF8((nint)view.Data)
            : view.Length == 0 ? string.Empty : Encoding.UTF8.GetString(view.Data, checked((int)view.Length));
    }
}
