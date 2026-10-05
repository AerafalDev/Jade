namespace Jade.BindingGenerator.Projection;

/// <summary>A field of a projected structure, in C layout order.</summary>
/// <param name="CName">The C name of the member.</param>
/// <param name="Name">The .NET name.</param>
/// <param name="Type">The C# type.</param>
/// <param name="IsPublic">Whether the field is public; the chain members of a public structure are internal (ADR 0029).</param>
internal sealed record ProjectedField(string CName, string Name, string Type, bool IsPublic);
