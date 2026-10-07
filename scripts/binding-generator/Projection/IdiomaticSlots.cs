using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>
/// The extensions of a chain root that mirrors reach through a member rather than a parameter, such
/// as the elements of a bind group: a public structure with one nullable field per extension
/// (ADR 0040).
/// </summary>
/// <param name="Name">The .NET name of the structure, <c>{Root}Extensions</c>.</param>
/// <param name="RootName">The .NET name of the root.</param>
/// <param name="RootCName">The C name of the root.</param>
/// <param name="NextInChainField">The name of the root's raw field that points to its first extension.</param>
/// <param name="Availability">The platforms the root is available on.</param>
/// <param name="Extensions">The extensions, ordered by name.</param>
internal sealed record IdiomaticSlots(string Name, string RootName, string RootCName, string NextInChainField, Platforms Availability, IReadOnlyList<IdiomaticExtensionSlot> Extensions);
