namespace Jade.BindingGenerator.Model;

/// <summary>A value of an enum or a set of flags.</summary>
/// <param name="EnumCName">The C name of the enum.</param>
/// <param name="ValueCName">The C name of the value.</param>
internal sealed record EnumValueReferenceExpression(string EnumCName, string ValueCName) : ValueExpression;
