namespace Jade.BindingGenerator.Model;

/// <summary>The value of a constant of the API.</summary>
/// <param name="CName">The C name of the constant.</param>
internal sealed record ConstantReferenceExpression(string CName) : ValueExpression;
