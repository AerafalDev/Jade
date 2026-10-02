/// <summary>A C struct or union exposed by value, or an opaque struct only used through pointers.</summary>
internal sealed class StructModel
{
    /// <summary>Gets the C name.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether this is a union: every field sits at offset 0.</summary>
    public bool IsUnion { get; init; }

    /// <summary>Gets whether the struct is opaque: no fields, never used by value.</summary>
    public bool IsOpaque { get; init; }

    /// <summary>Gets the fields in C order. A union may keep only a subset (see <see cref="LibraryConfig.UnionMembers"/>).</summary>
    public required IReadOnlyList<FieldModel> Fields { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }
}
