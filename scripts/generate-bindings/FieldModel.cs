/// <summary>A struct or union field.</summary>
internal sealed class FieldModel
{
    /// <summary>Gets the C name.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the type.</summary>
    public required TypeRef Type { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }
}
