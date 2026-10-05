namespace Jade.BindingGenerator.Model;

/// <summary>A length held by another member or parameter of the same record.</summary>
/// <param name="CName">The C name of the member or parameter that holds the length.</param>
internal sealed record MemberArrayLength(string CName) : ArrayLength;
