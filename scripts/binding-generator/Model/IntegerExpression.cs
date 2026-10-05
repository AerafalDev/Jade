namespace Jade.BindingGenerator.Model;

/// <summary>A non-negative integer.</summary>
/// <param name="Value">The integer.</param>
internal sealed record IntegerExpression(ulong Value) : ValueExpression;
