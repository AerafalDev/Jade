namespace Jade.BindingGenerator.Model;

/// <summary>
/// The largest value of an unsigned integer type, as <c>UINT32_MAX</c> or <c>SIZE_MAX</c> give it.
/// </summary>
/// <remarks>Kept symbolic because the maximum of <c>size_t</c> differs between 32-bit and 64-bit targets.</remarks>
internal sealed record TypeMaximumExpression : ValueExpression
{
    /// <summary>Gets the only instance.</summary>
    public static TypeMaximumExpression Instance { get; } = new();
}
