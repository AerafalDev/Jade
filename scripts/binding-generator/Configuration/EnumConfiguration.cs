using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>The annotation of an enum of a C header library (ADR 0027).</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record EnumConfiguration
{
    /// <summary>
    /// Gets the regular expression that selects the object-like macros holding the values of an
    /// integer typedef, matched against the whole macro name; <see langword="null"/> for a C enum.
    /// </summary>
    public string? Macros { get; init; }

    /// <summary>Gets whether the values are flags that combine, which makes a <c>[Flags]</c> enum.</summary>
    public bool Flags { get; init; }
}
