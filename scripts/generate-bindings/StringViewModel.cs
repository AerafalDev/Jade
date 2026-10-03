/// <summary>A struct that passes UTF-8 text by pointer and length, such as WebGPU's <c>WGPUStringView</c>.</summary>
internal sealed class StringViewModel
{
    /// <summary>Gets the C name of the struct.</summary>
    public required string Struct { get; init; }

    /// <summary>Gets the C name of the field pointing to the first byte.</summary>
    public required string Data { get; init; }

    /// <summary>Gets the C name of the field holding the length in bytes.</summary>
    public required string Length { get; init; }
}
