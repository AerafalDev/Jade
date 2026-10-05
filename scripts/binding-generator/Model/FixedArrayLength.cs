namespace Jade.BindingGenerator.Model;

/// <summary>A length that the API fixes, such as the 9 floats of a 3×3 matrix.</summary>
/// <param name="Count">The number of elements.</param>
internal sealed record FixedArrayLength(int Count) : ArrayLength;
