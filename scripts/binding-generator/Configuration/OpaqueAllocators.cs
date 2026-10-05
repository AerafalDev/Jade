using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>The names of the shim functions that allocate and free the opaque types of a library (ADR 0006).</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record OpaqueAllocators
{
    /// <summary>The placeholder that stands for the C name of the opaque type.</summary>
    public const string NamePlaceholder = "{name}";

    /// <summary>Gets the name of the allocation function, such as <c>jade_{name}_alloc</c>.</summary>
    public required string Allocate { get; init; }

    /// <summary>Gets the name of the function that frees what the allocation function returned, such as <c>jade_{name}_free</c>.</summary>
    public required string Free { get; init; }
}
