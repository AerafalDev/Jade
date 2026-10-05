using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Clang;

/// <summary>A declaration of a library's headers as one target declares it.</summary>
/// <param name="Declaration">The declaration, available on the target's platform only; a structure that is never defined, or that is opaque, is a handle.</param>
/// <param name="Header">The header that declares it, as written in an <c>#include</c> directive, or the repository path of a shim header.</param>
internal sealed record TargetDeclaration(Declaration Declaration, string Header);
