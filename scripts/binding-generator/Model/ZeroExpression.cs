namespace Jade.BindingGenerator.Model;

/// <summary>The zero value of a type: <c>0</c>, <c>NULL</c> or a zeroed structure.</summary>
internal sealed record ZeroExpression : ValueExpression
{
    /// <summary>Gets the only instance.</summary>
    public static ZeroExpression Instance { get; } = new();
}
