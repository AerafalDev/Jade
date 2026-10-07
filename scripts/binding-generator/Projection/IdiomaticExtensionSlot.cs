using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>One extension of a nested chain root, a nullable field of its slots structure (ADR 0040).</summary>
/// <param name="Name">The .NET name of the extension, a value structure, which also names the field.</param>
/// <param name="ChainField">The name of the extension's field that holds its chain header.</param>
/// <param name="StructureType">The C# expression of the extension's structure type.</param>
/// <param name="Availability">The platforms the extension is available on.</param>
internal sealed record IdiomaticExtensionSlot(string Name, string ChainField, string StructureType, Platforms Availability);
