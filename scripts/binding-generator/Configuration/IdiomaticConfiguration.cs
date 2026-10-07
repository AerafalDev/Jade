using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>The settings of a library's idiomatic layer, which the generator writes above its raw layer (ADR 0040).</summary>
/// <remarks>
/// Every entry names a declaration of the model by C name and fails the generator when it matches
/// nothing, so that the configuration cannot drift from the API.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record IdiomaticConfiguration
{
    /// <summary>
    /// Gets the constants made public, by C name, each with the C name of the handle or structure
    /// that declares it, such as <c>WGPU_WHOLE_SIZE</c> on <c>WGPUBuffer</c>. The other constants
    /// stay internal.
    /// </summary>
    public IReadOnlyDictionary<string, string> Constants { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the functions, and the structure members as <c>Structure.member</c>, whose idiomatic
    /// form is written by hand, with the reason. The generator leaves a function out and calls a
    /// hand-written partial method to lower a member.
    /// </summary>
    public IReadOnlyDictionary<string, string> HandWritten { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the functions and structures that the idiomatic layer does not expose, with the reason;
    /// the raw layer still declares them.
    /// </summary>
    public IReadOnlyDictionary<string, string> Skip { get; init; } = FrozenDictionary<string, string>.Empty;
}
