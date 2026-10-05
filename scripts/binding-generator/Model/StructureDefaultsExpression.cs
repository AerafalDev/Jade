namespace Jade.BindingGenerator.Model;

/// <summary>A structure-typed member initialized with that structure's own defaults.</summary>
internal sealed record StructureDefaultsExpression : ValueExpression
{
    /// <summary>Gets the only instance.</summary>
    public static StructureDefaultsExpression Instance { get; } = new();
}
