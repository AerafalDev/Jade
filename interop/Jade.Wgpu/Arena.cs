using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Jade.Wgpu;

/// <summary>
/// The memory a call lowers its arguments into: the stack buffer the call allocates, then native
/// blocks freed by <see cref="Dispose"/> (ADR 0029).
/// </summary>
/// <remarks>
/// Each call owns its arena, so nothing is shared between calls or threads and a callback that
/// makes another call cannot reset memory still in use. The buffer must be stack memory, whose
/// address does not move while the call runs.
/// </remarks>
internal unsafe ref struct Arena
{
    /// <summary>The size of the stack buffer of a call, enough for the descriptors of every common call.</summary>
    public const int StackSize = 1024;

    /// <summary>The alignment of every allocation: the largest of the API's types (pointers, 64-bit integers, doubles).</summary>
    private const nuint Alignment = 8;

    /// <summary>The smallest native block, so that many small copies past the stack buffer do not each allocate.</summary>
    private const nuint BlockSize = 4096;

    /// <summary>The size of a native block's header, which links the previous block and keeps the data aligned.</summary>
    private const nuint HeaderSize = 16;

    /// <summary>The next free byte.</summary>
    private byte* _next;

    /// <summary>The number of free bytes after <see cref="_next"/>.</summary>
    private nuint _remaining;

    /// <summary>The last native block, whose header points to the one before it.</summary>
    private void* _blocks;

    /// <summary>Initializes a new instance of the <see cref="Arena"/> struct over a stack buffer.</summary>
    /// <param name="buffer">The buffer, allocated with <see langword="stackalloc"/> by the call.</param>
    public Arena(Span<byte> buffer)
    {
        _next = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
        _remaining = (nuint)buffer.Length;
    }

    /// <summary>Allocates memory for one value, uninitialized.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The memory.</returns>
    public T* Allocate<T>()
        where T : unmanaged
    {
        return (T*)Allocate((nuint)sizeof(T));
    }

    /// <summary>Allocates memory for an array, uninitialized.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="count">The number of elements.</param>
    /// <returns>The memory, or <see langword="null"/> for an empty array, which C passes as a null pointer.</returns>
    public T* Allocate<T>(int count)
        where T : unmanaged
    {
        return count == 0 ? null : (T*)Allocate(checked((nuint)count * (nuint)sizeof(T)));
    }

    /// <summary>Copies a value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The copy.</returns>
    public T* Copy<T>(scoped in T value)
        where T : unmanaged
    {
        var copy = Allocate<T>();

        *copy = value;

        return copy;
    }

    /// <summary>Copies an array.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="values">The elements.</param>
    /// <returns>The copy, or <see langword="null"/> for an empty array.</returns>
    public T* Copy<T>(scoped ReadOnlySpan<T> values)
        where T : unmanaged
    {
        var copy = Allocate<T>(values.Length);

        values.CopyTo(new Span<T>(copy, values.Length));

        return copy;
    }

    /// <summary>Encodes a string to UTF-8, without terminator.</summary>
    /// <param name="value">The string.</param>
    /// <param name="length">The number of bytes.</param>
    /// <returns>The bytes.</returns>
    public byte* CopyUtf8(string value, out int length)
    {
        length = Encoding.UTF8.GetByteCount(value);

        var bytes = (byte*)Allocate((nuint)length);

        _ = Encoding.UTF8.GetBytes(value, new Span<byte>(bytes, length));

        return bytes;
    }

    /// <summary>Encodes strings to NUL-terminated UTF-8 and copies an array of pointers to them.</summary>
    /// <param name="values">The strings.</param>
    /// <returns>The array of pointers, or <see langword="null"/> for an empty array.</returns>
    /// <exception cref="ArgumentException">A string is null, which an array of C strings cannot hold.</exception>
    public byte** CopyStrings(scoped ReadOnlySpan<string> values)
    {
        var pointers = (byte**)Allocate<nint>(values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i] ?? throw new ArgumentException("An array of strings passed to the native library cannot hold null.", nameof(values));
            var length = Encoding.UTF8.GetByteCount(value);
            var bytes = (byte*)Allocate((nuint)length + 1);

            _ = Encoding.UTF8.GetBytes(value, new Span<byte>(bytes, length));
            bytes[length] = 0;
            pointers[i] = bytes;
        }

        return pointers;
    }

    /// <summary>Frees the native blocks.</summary>
    public void Dispose()
    {
        while (_blocks != null)
        {
            var previous = *(void**)_blocks;

            NativeMemory.Free(_blocks);
            _blocks = previous;
        }

        _remaining = 0;
    }

    /// <summary>Allocates aligned memory.</summary>
    /// <param name="size">The number of bytes.</param>
    /// <returns>The memory.</returns>
    private void* Allocate(nuint size)
    {
        var padding = (Alignment - ((nuint)_next & (Alignment - 1))) & (Alignment - 1);

        if (size > _remaining || padding > _remaining - size)
        {
            Grow(size);
            padding = 0;
        }

        var memory = _next + padding;

        _next = memory + size;
        _remaining -= padding + size;

        return memory;
    }

    /// <summary>Starts a native block that holds at least a number of bytes.</summary>
    /// <param name="size">The number of bytes the next allocation needs.</param>
    private void Grow(nuint size)
    {
        var capacity = Math.Max(size, BlockSize);
        var block = (byte*)NativeMemory.Alloc(checked(capacity + HeaderSize));

        *(void**)block = _blocks;
        _blocks = block;
        _next = block + HeaderSize;
        _remaining = capacity;
    }
}
