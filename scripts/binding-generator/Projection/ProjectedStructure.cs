namespace Jade.BindingGenerator.Projection;

/// <summary>A blittable structure: a public value structure or an internal raw structure (ADR 0029).</summary>
internal sealed record ProjectedStructure : ProjectedType
{
    /// <summary>Gets the fields, in C layout order.</summary>
    public required IReadOnlyList<ProjectedField> Fields { get; init; }

    /// <summary>Gets whether the structure maps a C union: every field starts at offset 0 (ADR 0009).</summary>
    public bool IsUnion { get; init; }

    /// <summary>
    /// Gets the C name of the macro whose defaults the parameterless constructor applies, or
    /// <see langword="null"/> when the C API defines none.
    /// </summary>
    public string? InitializerCName { get; init; }

    /// <summary>Gets the assignments of the parameterless constructor; empty when every default is zero.</summary>
    public required IReadOnlyList<ProjectedAssignment> Defaults { get; init; }
}
