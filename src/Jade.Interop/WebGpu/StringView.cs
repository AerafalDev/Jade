using System.Runtime.InteropServices;

namespace Jade.Interop.WebGpu;

// The WGPUStringView conventions of webgpu.h, which dawn.json does not describe: `{NULL, WGPU_STRLEN}` is the null
// value, `WGPU_STRLEN` with a pointer is a NUL-terminated string, and any other length counts the bytes, so `default`
// is the empty string rather than null.
public unsafe partial struct StringView
{
    /// <summary>
    /// Gets the null string view, <c>{NULL, WGPU_STRLEN}</c>, which <c>WGPU_STRING_VIEW_INIT</c> also gives: an absent
    /// string, where <see langword="default"/> is the empty one.
    /// </summary>
    public static StringView Null => new() { Length = Wgpu.Strlen };

    /// <summary>
    /// Returns the UTF-8 bytes the view points to, without copying them. The span is only valid while the native
    /// memory is, for example during the callback that received the view.
    /// </summary>
    /// <returns>The bytes, without a terminator; empty for the null and the empty view.</returns>
    public readonly ReadOnlySpan<byte> AsSpan()
    {
        if (Data is null)
        {
            return default;
        }

        return Length == Wgpu.Strlen ? MemoryMarshal.CreateReadOnlySpanFromNullTerminated(Data) : new ReadOnlySpan<byte>(Data, checked((int)Length));
    }
}
