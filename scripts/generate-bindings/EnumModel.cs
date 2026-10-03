/// <summary>A C enum, an integer typedef whose values are <c>#define</c>s, or an ID typedef (an enum without members).</summary>
internal sealed class EnumModel
{
    /// <summary>Gets the C name of the enum or typedef.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the underlying type.</summary>
    public required PrimitiveType Underlying { get; init; }

    /// <summary>Gets whether the values combine as bits, which adds <c>[Flags]</c>.</summary>
    public bool IsFlags { get; init; }

    /// <summary>Gets the members in C order.</summary>
    public required IReadOnlyList<EnumMemberModel> Members { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }
}
