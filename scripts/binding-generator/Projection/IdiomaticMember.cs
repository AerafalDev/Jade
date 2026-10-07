using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Projection;

/// <summary>A member of a mirror or a snapshot, with what it takes to convert it to or from its raw field (ADR 0029, ADR 0040).</summary>
internal sealed record IdiomaticMember
{
    /// <summary>Gets the member of the model.</summary>
    public required StructureMember Member { get; init; }

    /// <summary>Gets the .NET name, which is also the name of the raw field.</summary>
    public required string Name { get; init; }

    /// <summary>Gets how the member is exposed and converted.</summary>
    public required IdiomaticMemberKind Kind { get; init; }

    /// <summary>Gets the C# type of the exposed member; empty for a member the idiomatic layer hides.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the C# type of the raw field, spelled from the library's namespace.</summary>
    public required string RawType { get; init; }

    /// <summary>Gets the C# type of the exposed elements of a span member: a blittable type, a mirror, a snapshot or <see langword="string"/>.</summary>
    public string? ElementType { get; init; }

    /// <summary>Gets the C# type of the raw elements of a span member.</summary>
    public string? RawElementType { get; init; }

    /// <summary>Gets the name of the raw field that holds the count of a span member, or <see langword="null"/> for a fixed length.</summary>
    public string? CountField { get; init; }

    /// <summary>Gets the C# type of <see cref="CountField"/>.</summary>
    public string? CountType { get; init; }

    /// <summary>Gets the number of elements of a span member whose length the API fixes.</summary>
    public int? FixedLength { get; init; }

    /// <summary>Gets whether the member may be left unset: a null pointer or an empty span.</summary>
    public bool IsOptional { get; init; }

    /// <summary>Gets the slots of the extensions of a nested chain root this member holds, if any.</summary>
    public IdiomaticSlots? Slots { get; init; }

    /// <summary>Gets whether the member, or its elements, are handles, which a snapshot takes a reference to.</summary>
    public bool IsHandle { get; init; }

    /// <summary>Gets the C# expression of the structure type a chain header member identifies.</summary>
    public string? StructureType { get; init; }

    /// <summary>Gets the C# type of the raw structure a nested or pointed-to member lowers to.</summary>
    public string? RawNestedType { get; init; }
}
