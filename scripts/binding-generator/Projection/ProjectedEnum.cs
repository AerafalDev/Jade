namespace Jade.BindingGenerator.Projection;

/// <summary>A C# enum, or a <c>[Flags]</c> enum for a set of flags.</summary>
internal sealed record ProjectedEnum : ProjectedType
{
    /// <summary>Gets the C# keyword of the underlying integer type.</summary>
    public required string UnderlyingType { get; init; }

    /// <summary>Gets whether the values are flags.</summary>
    public required bool IsFlags { get; init; }

    /// <summary>Gets the size of the underlying type, in bytes, which sets the number of hexadecimal digits of the values.</summary>
    public required int Size { get; init; }

    /// <summary>Gets the values, in declaration order.</summary>
    public required IReadOnlyList<ProjectedEnumValue> Values { get; init; }
}
