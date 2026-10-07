using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A public type the idiomatic layer declares for a raw structure: a mirror, an element mirror or a snapshot (ADR 0029, ADR 0040).</summary>
internal sealed record IdiomaticStructure
{
    /// <summary>Gets the structure of the model.</summary>
    public required StructureDeclaration Declaration { get; init; }

    /// <summary>Gets the .NET name, the raw structure's, in the library's namespace.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the C# type of the raw structure, spelled from the library's namespace.</summary>
    public required string RawType { get; init; }

    /// <summary>Gets what the type is: <see cref="IdiomaticRole.Mirror"/>, <see cref="IdiomaticRole.ElementMirror"/> or <see cref="IdiomaticRole.Snapshot"/>.</summary>
    public required IdiomaticRole Role { get; init; }

    /// <summary>Gets the platforms the type is available on.</summary>
    public required Platforms Availability { get; init; }

    /// <summary>Gets the members, in C layout order, the hidden ones included.</summary>
    public required IReadOnlyList<IdiomaticMember> Members { get; init; }

    /// <summary>Gets the assignments of a mirror's parameterless constructor, which apply the C initializer's defaults.</summary>
    public IReadOnlyList<ProjectedAssignment> Defaults { get; init; } = [];

    /// <summary>Gets whether a mirror records that its constructor ran, because a member reaches it through an optional pointer.</summary>
    public bool HasPresence { get; init; }

    /// <summary>Gets the name of the raw function that frees what the library allocated for a snapshot's members, if any.</summary>
    public string? FreeMembers { get; init; }

    /// <summary>Gets the .NET names of the roots an input extension can be chained to.</summary>
    public IReadOnlyList<string> InputRoots { get; init; } = [];

    /// <summary>Gets the .NET names of the roots an output extension can be chained to.</summary>
    public IReadOnlyList<string> OutputRoots { get; init; } = [];

    /// <summary>Gets the C# expression of an extension's structure type.</summary>
    public string? StructureType { get; init; }

    /// <summary>Gets the constants the configuration places on the type.</summary>
    public IReadOnlyList<IdiomaticConstant> Constants { get; init; } = [];
}
