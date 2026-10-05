namespace Jade.BindingGenerator.Model;

/// <summary>An enum, or a set of flags whose values combine.</summary>
internal sealed record EnumDeclaration : Declaration
{
    /// <summary>Gets the integer type the values are stored in.</summary>
    public required BuiltinTypeReference UnderlyingType { get; init; }

    /// <summary>Gets whether the values are flags, as for WebGPU bitmasks.</summary>
    public bool IsFlags { get; init; }

    /// <summary>Gets the values, in declaration order.</summary>
    public required IReadOnlyList<EnumValueDeclaration> Values { get; init; }
}
