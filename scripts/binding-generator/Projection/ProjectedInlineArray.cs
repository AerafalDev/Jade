namespace Jade.BindingGenerator.Projection;

/// <summary>An <c>[InlineArray]</c> structure that maps a fixed-size C array member (ADR 0009).</summary>
/// <remarks>
/// Each array member gets its own type, named after its structure and itself, <c>{Structure}{Member}</c>,
/// in the namespace of its structure (ADR 0033).
/// </remarks>
internal sealed record ProjectedInlineArray : ProjectedType
{
    /// <summary>Gets the C# type of the elements.</summary>
    public required string ElementType { get; init; }

    /// <summary>Gets the number of elements.</summary>
    public required int Length { get; init; }
}
