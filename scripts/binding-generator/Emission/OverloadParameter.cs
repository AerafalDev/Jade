namespace Jade.BindingGenerator.Emission;

/// <summary>A parameter of a public overload, as written in its signature and documentation.</summary>
/// <param name="Name">The name.</param>
/// <param name="Declaration">The declaration, with its modifiers, type and default.</param>
/// <param name="Documentation">The text of its <c>param</c> documentation element.</param>
internal sealed record OverloadParameter(string Name, string Declaration, string Documentation);
