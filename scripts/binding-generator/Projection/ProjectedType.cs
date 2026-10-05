using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A C# type of the raw layer, projected from a declaration of the intermediate representation.</summary>
internal abstract record ProjectedType
{
    /// <summary>Gets the C name of the declaration the type maps.</summary>
    public required string CName { get; init; }

    /// <summary>Gets the .NET name (ADR 0034).</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the type is public, because it is identical in both layers; an internal type lives in the <c>Raw</c> namespace (ADR 0034).</summary>
    public required bool IsPublic { get; init; }

    /// <summary>Gets the platform families the type is available on.</summary>
    public required Platforms Availability { get; init; }
}
