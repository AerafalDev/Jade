namespace Jade.BindingGenerator.Model;

/// <summary>An integer.</summary>
/// <param name="Value">The integer, as the bits of a 64-bit two's complement value: a negative value of a signed type is stored sign-extended.</param>
internal sealed record IntegerExpression(ulong Value) : ValueExpression;
