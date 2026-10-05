namespace Jade.BindingGenerator.Model;

/// <summary>A fixed-size C array, such as the <c>Uint8 data[16]</c> member of <c>SDL_GUID</c>.</summary>
/// <param name="Element">The type of the elements.</param>
/// <param name="Length">The number of elements.</param>
/// <remarks>Only structure members hold arrays: a parameter declared as an array is a pointer in C.</remarks>
internal sealed record ArrayTypeReference(TypeReference Element, int Length) : TypeReference;
