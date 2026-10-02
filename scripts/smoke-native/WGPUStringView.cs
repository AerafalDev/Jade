using System.Text;

/// <summary><c>WGPUStringView</c>: a UTF-8 string that is not necessarily null-terminated.</summary>
internal unsafe struct WGPUStringView
{
    /// <summary>The UTF-8 bytes, or null.</summary>
    public byte* Data;

    /// <summary>The length in bytes, or <c>WGPU_STRLEN</c> (<c>SIZE_MAX</c>) for a null-terminated string.</summary>
    public nuint Length;

    /// <summary>Decodes the string.</summary>
    /// <returns>The string, empty when <see cref="Data"/> is null.</returns>
    public override readonly string ToString() =>
        Data is null ? string.Empty
        : Length == nuint.MaxValue ? new string((sbyte*)Data)
        : Encoding.UTF8.GetString(Data, checked((int)Length));
}
