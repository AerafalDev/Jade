namespace Jade.Wgpu;

/// <content>The members of a buffer that <c>bindings.json</c> leaves to this file (ADR 0040).</content>
public readonly unsafe partial struct GpuBuffer
{
    /// <summary>Calls <c>wgpuBufferGetMappedRange</c> for the whole mapping.</summary>
    /// <returns>The mapped bytes, valid until the buffer is unmapped or destroyed.</returns>
    /// <exception cref="WgpuException">The buffer is not mapped for writing.</exception>
    public Span<byte> GetMappedRange()
    {
        return GetMappedRange(0, WholeMapSize);
    }

    /// <summary>Calls <c>wgpuBufferGetMappedRange</c>.</summary>
    /// <param name="offset">The offset of the range, in bytes.</param>
    /// <param name="size">The size of the range, or <see cref="WholeMapSize"/> for the rest of the buffer.</param>
    /// <returns>The mapped bytes, valid until the buffer is unmapped or destroyed.</returns>
    /// <exception cref="WgpuException">The range is not mapped for writing.</exception>
    public Span<byte> GetMappedRange(nuint offset, nuint size)
    {
        var data = Raw.NativeMethods.BufferGetMappedRange(this, offset, size);

        return data == null
            ? throw new WgpuException($"wgpuBufferGetMappedRange returned no memory: the range at {offset} is not mapped for writing.")
            : new Span<byte>(data, GetLength(offset, size));
    }

    /// <summary>Calls <c>wgpuBufferGetConstMappedRange</c> for the whole mapping.</summary>
    /// <returns>The mapped bytes, valid until the buffer is unmapped or destroyed.</returns>
    /// <exception cref="WgpuException">The buffer is not mapped.</exception>
    public ReadOnlySpan<byte> GetConstMappedRange()
    {
        return GetConstMappedRange(0, WholeMapSize);
    }

    /// <summary>Calls <c>wgpuBufferGetConstMappedRange</c>.</summary>
    /// <param name="offset">The offset of the range, in bytes.</param>
    /// <param name="size">The size of the range, or <see cref="WholeMapSize"/> for the rest of the buffer.</param>
    /// <returns>The mapped bytes, valid until the buffer is unmapped or destroyed.</returns>
    /// <exception cref="WgpuException">The range is not mapped.</exception>
    public ReadOnlySpan<byte> GetConstMappedRange(nuint offset, nuint size)
    {
        var data = Raw.NativeMethods.BufferGetConstMappedRange(this, offset, size);

        return data == null
            ? throw new WgpuException($"wgpuBufferGetConstMappedRange returned no memory: the range at {offset} is not mapped.")
            : new ReadOnlySpan<byte>(data, GetLength(offset, size));
    }

    /// <summary>Gets the length of a mapped range.</summary>
    /// <param name="offset">The offset of the range.</param>
    /// <param name="size">The size of the range, or <see cref="WholeMapSize"/> for the rest of the buffer, as the library reads it.</param>
    /// <returns>The length in bytes.</returns>
    private int GetLength(nuint offset, nuint size)
    {
        return checked((int)(size == WholeMapSize ? Size - offset : size));
    }
}
