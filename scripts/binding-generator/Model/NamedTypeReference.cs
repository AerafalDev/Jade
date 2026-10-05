namespace Jade.BindingGenerator.Model;

/// <summary>A reference to a declaration of the same library: an enum, a handle, a structure, a typedef or a function pointer type.</summary>
/// <param name="CName">The C name of the declaration.</param>
internal sealed record NamedTypeReference(string CName) : TypeReference;
