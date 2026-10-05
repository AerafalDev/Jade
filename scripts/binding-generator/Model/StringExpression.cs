namespace Jade.BindingGenerator.Model;

/// <summary>A NUL-terminated string literal, such as the name of an SDL3 hint.</summary>
/// <param name="Value">The string, without its terminator.</param>
internal sealed record StringExpression(string Value) : ValueExpression;
