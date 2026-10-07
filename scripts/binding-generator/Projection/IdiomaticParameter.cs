using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Projection;

/// <summary>A parameter of a C function, with how its idiomatic method takes it (ADR 0040).</summary>
internal sealed record IdiomaticParameter
{
    /// <summary>Gets the parameter of the model.</summary>
    public required Parameter Parameter { get; init; }

    /// <summary>Gets the camel-case .NET name, the raw layer's.</summary>
    public required string Name { get; init; }

    /// <summary>Gets how the method takes the parameter.</summary>
    public required IdiomaticParameterKind Kind { get; init; }

    /// <summary>Gets the C# type of the exposed parameter; for text, the UTF-8 span form.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the C# type of the raw parameter, spelled from the library's namespace.</summary>
    public required string RawType { get; init; }

    /// <summary>Gets the C# type of the elements of a span parameter, <c>TData</c> for data.</summary>
    public string? ElementType { get; init; }

    /// <summary>Gets the name of the span parameter whose length a count parameter receives.</summary>
    public string? CountOf { get; init; }

    /// <summary>Gets whether a count parameter receives a size in bytes rather than a number of elements.</summary>
    public bool CountsBytes { get; init; }

    /// <summary>Gets the C# expression of the default of an optional argument, or <see langword="null"/>.</summary>
    public string? Default { get; init; }

    /// <summary>Gets whether the parameter is the last one and takes an argument list (<see langword="params"/>).</summary>
    public bool IsParams { get; init; }

    /// <summary>Gets whether a pointer parameter may be null, which an overload without it passes.</summary>
    public bool IsOptional { get; init; }

    /// <summary>Gets the mirror a descriptor parameter takes.</summary>
    public IdiomaticStructure? Mirror { get; init; }

    /// <summary>Gets the C# type of the raw structure a descriptor, a value or an output parameter points to.</summary>
    public string? RawStructure { get; init; }

    /// <summary>Gets the C name of the structure the parameter points to, for its extensions.</summary>
    public string? StructureCName { get; init; }

    /// <summary>Gets whether the structure the parameter points to is a chain root with extensions of the parameter's direction.</summary>
    public bool HasExtensions { get; init; }

    /// <summary>Gets the name of the raw field that points to the first extension of the structure the parameter points to, if it has one.</summary>
    public string? NextInChainField { get; init; }

    /// <summary>Gets the snapshot an output parameter fills, or <see langword="null"/> for a value structure.</summary>
    public IdiomaticStructure? Snapshot { get; init; }
}
