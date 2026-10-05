using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Clang;

/// <summary>Declarations that the same set of targets declares.</summary>
/// <param name="Targets">The targets that declare them, in the order of ADR 0012.</param>
/// <param name="Declarations">The declarations, ordered by <see cref="HeaderDeclaration.ByName"/>.</param>
internal sealed record DeclarationGroup(IReadOnlyList<Target> Targets, IReadOnlyList<HeaderDeclaration> Declarations);
