using System.Runtime.InteropServices;

namespace Jade.Interop;

/// <summary>
/// A NUL-terminated view of a UTF-8 string for a C <c>const char*</c> parameter, built without GC allocation (ADR-0006).
/// A span that already ends with NUL is used as is; a shorter one is copied into caller-provided stack memory; a
/// longer one into native memory released by <see cref="Dispose"/>.
/// </summary>
internal unsafe ref struct NulTerminatedUtf8
{
    /// <summary>The stack buffer size generated overloads allocate: strings shorter than this are not copied to the heap.</summary>
    internal const int StackLength = 256;

    private readonly ReadOnlySpan<byte> _terminated;
    private byte* _native;

    /// <summary>Initializes the view.</summary>
    /// <param name="value">The UTF-8 string, with or without its terminator.</param>
    /// <param name="stack">Scratch memory, normally <c>stackalloc byte[StackLength]</c>.</param>
    public NulTerminatedUtf8(ReadOnlySpan<byte> value, Span<byte> stack)
    {
        if (!value.IsEmpty && value[^1] == 0)
        {
            _terminated = value;
            return;
        }

        Span<byte> copy;
        if (value.Length < stack.Length)
        {
            copy = stack[..(value.Length + 1)];
        }
        else
        {
            _native = (byte*)NativeMemory.Alloc((nuint)value.Length + 1);
            copy = new Span<byte>(_native, value.Length + 1);
        }

        value.CopyTo(copy);
        copy[^1] = 0;
        _terminated = copy;
    }

    /// <summary>Returns a reference to the first byte, for <c>fixed</c>.</summary>
    /// <returns>The first byte of the terminated string.</returns>
    public readonly ref readonly byte GetPinnableReference() => ref MemoryMarshal.GetReference(_terminated);

    /// <summary>Releases the native copy, if one was made.</summary>
    public void Dispose()
    {
        if (_native is not null)
        {
            NativeMemory.Free(_native);
            _native = null;
        }
    }
}
