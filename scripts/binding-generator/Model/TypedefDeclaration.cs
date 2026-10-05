namespace Jade.BindingGenerator.Model;

/// <summary>A typedef that the API declares itself, such as <c>WGPUBool</c>.</summary>
internal sealed record TypedefDeclaration : Declaration
{
    /// <summary>Gets the aliased type.</summary>
    public required TypeReference Target { get; init; }

    /// <summary>Gets whether the integer type is used as a boolean, which maps to a dedicated boolean structure (ADR 0027).</summary>
    public bool IsBoolean { get; init; }
}
