namespace Jade.BindingGenerator.Model;

/// <summary>A boolean, stored in the API's boolean type.</summary>
/// <param name="Value">The boolean.</param>
internal sealed record BooleanExpression(bool Value) : ValueExpression;
