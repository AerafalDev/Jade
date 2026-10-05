namespace Jade.BindingGenerator.Model;

/// <summary>A declaration of the inputs that the bindings leave out, reported so that omissions are reviewed (ADR 0027).</summary>
/// <param name="Name">The name of the declaration in its input.</param>
/// <param name="Reason">Why it is left out.</param>
internal sealed record SkippedDeclaration(string Name, string Reason);
