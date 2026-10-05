namespace Jade.BindingGenerator.Model;

/// <summary>A pointer type.</summary>
/// <param name="Pointee">The type pointed to.</param>
/// <param name="IsConst">Whether the pointee is <c>const</c>, as in <c>const T*</c>.</param>
internal sealed record PointerTypeReference(TypeReference Pointee, bool IsConst) : TypeReference;
