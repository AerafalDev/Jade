namespace Jade.BindingGenerator.Model;

/// <summary>A floating-point number, NaN included.</summary>
/// <param name="Value">The number.</param>
internal sealed record FloatExpression(double Value) : ValueExpression;
