namespace Jade.BindingGenerator.Projection;

/// <summary>A member of a record whose offset and size the tests compare (ADR 0036).</summary>
/// <param name="CName">The C name of the member.</param>
/// <param name="Name">The C# name of the field.</param>
/// <param name="IsArray">Whether the member is a fixed-size array, whose first element is compared too.</param>
internal sealed record ProjectedMemberLayout(string CName, string Name, bool IsArray);
