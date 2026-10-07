using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>What the idiomatic layer adds to a value structure of the raw layer: the extension interfaces it implements and its constants (ADR 0040).</summary>
internal sealed record IdiomaticValueStructure
{
    /// <summary>Gets the .NET name of the value structure.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the platforms the structure is available on.</summary>
    public required Platforms Availability { get; init; }

    /// <summary>Gets the .NET names of the roots the structure can be chained to as an input extension.</summary>
    public IReadOnlyList<string> InputRoots { get; init; } = [];

    /// <summary>Gets the .NET names of the roots the structure can be chained to as an output extension.</summary>
    public IReadOnlyList<string> OutputRoots { get; init; } = [];

    /// <summary>Gets the name of the field that holds the chain header, for an extension.</summary>
    public string? ChainField { get; init; }

    /// <summary>Gets the C# expression of the structure type, for an extension.</summary>
    public string? StructureType { get; init; }

    /// <summary>Gets the constants the configuration places on the structure.</summary>
    public IReadOnlyList<IdiomaticConstant> Constants { get; init; } = [];
}
