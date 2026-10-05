namespace Jade.BindingGenerator.Projection;

/// <summary>A boolean structure over an integer that the C API uses as a boolean (ADR 0027).</summary>
internal sealed record ProjectedBoolean : ProjectedType
{
    /// <summary>Gets the C# keyword of the integer type, which gives the structure its size.</summary>
    public required string UnderlyingType { get; init; }
}
