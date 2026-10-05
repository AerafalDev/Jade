namespace Jade.BindingGenerator.Model;

/// <summary>A parameter of a function or a function pointer type.</summary>
internal sealed record Parameter
{
    /// <summary>Gets the C name of the parameter.</summary>
    public required string CName { get; init; }

    /// <summary>Gets the words of the name, which the projection turns into a .NET name.</summary>
    public required IReadOnlyList<string> Words { get; init; }

    /// <summary>Gets the type of the parameter.</summary>
    public required TypeReference Type { get; init; }

    /// <summary>Gets the length of the array a pointer parameter points to, or <see langword="null"/> for a single element.</summary>
    public ArrayLength? Length { get; init; }

    /// <summary>Gets whether the parameter may be null.</summary>
    public bool IsOptional { get; init; }
}
