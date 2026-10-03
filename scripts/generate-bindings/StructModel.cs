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

    /// <summary>Gets the fields a parameterless constructor sets; when empty, the struct has no explicit constructor.</summary>
    public IReadOnlyList<FieldInitializer> Initializers { get; init; } = [];

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }

    /// <summary>Gets the only platforms (<c>OperatingSystem.IsOSPlatform</c> names) where the struct exists, or none when it exists everywhere.</summary>
    public IReadOnlyList<string> SupportedPlatforms { get; init; } = [];

    /// <summary>Gets the platforms (<c>OperatingSystem.IsOSPlatform</c> names) where the struct does not exist.</summary>
    public IReadOnlyList<string> UnsupportedPlatforms { get; init; } = [];
}
