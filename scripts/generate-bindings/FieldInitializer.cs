/// <summary>
/// A field that a struct's parameterless constructor sets to an enum member, such as the <c>sType</c> a WebGPU chained
/// struct must carry. Every other field starts at zero.
/// </summary>
internal sealed class FieldInitializer
{
    /// <summary>Gets the C names of the fields leading to the one to set, outermost first (for example <c>chain</c>, <c>sType</c>).</summary>
    public required IReadOnlyList<string> Path { get; init; }

    /// <summary>Gets the C name of the enum member to store.</summary>
    public required string Member { get; init; }
}
