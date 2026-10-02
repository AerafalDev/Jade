/// <summary>A C scalar type with a fixed managed counterpart.</summary>
internal enum PrimitiveType
{
    /// <summary>C <c>char</c>: emitted as <c>byte</c>, marked so <c>const char*</c> can get UTF-8 overloads.</summary>
    Char,

    /// <summary><c>signed char</c>, <c>int8_t</c>.</summary>
    SByte,

    /// <summary><c>unsigned char</c>, <c>uint8_t</c>, and C <c>bool</c> when the library maps it to one byte.</summary>
    Byte,

    /// <summary><c>short</c>, <c>int16_t</c>.</summary>
    Int16,

    /// <summary><c>unsigned short</c>, <c>uint16_t</c>.</summary>
    UInt16,

    /// <summary><c>int</c>, <c>int32_t</c>.</summary>
    Int32,

    /// <summary><c>unsigned int</c>, <c>uint32_t</c>.</summary>
    UInt32,

    /// <summary><c>long long</c>, <c>int64_t</c>.</summary>
    Int64,

    /// <summary><c>unsigned long long</c>, <c>uint64_t</c>.</summary>
    UInt64,

    /// <summary><c>intptr_t</c>, <c>ptrdiff_t</c>.</summary>
    NInt,

    /// <summary><c>uintptr_t</c>, <c>size_t</c>.</summary>
    NUInt,

    /// <summary>C <c>long</c>: 32-bit on Windows and wasm32, 64-bit elsewhere.</summary>
    CLong,

    /// <summary>C <c>unsigned long</c>, sized like <see cref="CLong"/>.</summary>
    CULong,

    /// <summary><c>float</c>.</summary>
    Single,

    /// <summary><c>double</c>.</summary>
    Double,
}
