namespace Jade.BindingGenerator.Model;

/// <summary>A pointer to a function, spelled in place rather than through a typedef, as in a structure member.</summary>
/// <param name="ReturnType">The return type.</param>
/// <param name="ParameterTypes">The types of the parameters.</param>
internal sealed record FunctionPointerTypeReference(TypeReference ReturnType, IReadOnlyList<TypeReference> ParameterTypes) : TypeReference;
