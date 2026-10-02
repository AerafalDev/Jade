/// <summary>A function parameter.</summary>
internal sealed class ParameterModel
{
    /// <summary>Gets the C name, which upstream docs refer to.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# name, already escaped if it is a keyword.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the raw type.</summary>
    public required TypeRef Type { get; init; }

    /// <summary>Gets how the friendly overload exposes the parameter.</summary>
    public ParameterKind Kind { get; init; }

    /// <summary>Gets the other parameter of a pointer and count pair: the count of a <see cref="ParameterKind.Span"/>, or the span of a <see cref="ParameterKind.Count"/>.</summary>
    public string? Pair { get; init; }
}
