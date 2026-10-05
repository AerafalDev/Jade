namespace Jade.BindingGenerator.Projection;

/// <summary>An assignment of the parameterless constructor, which gives a field its default.</summary>
/// <param name="Target">The assigned field, or a field of it.</param>
/// <param name="Value">The C# expression of the default.</param>
internal sealed record ProjectedAssignment(string Target, string Value);
