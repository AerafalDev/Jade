namespace Jade.BindingGenerator.Model;

/// <summary>A builtin C type or a standard typedef, referenced by its C spelling, such as <c>uint32_t</c> or <c>size_t</c>.</summary>
/// <param name="Spelling">The C spelling of the type.</param>
/// <remarks>
/// Standard typedefs are kept by name and never resolved to their canonical type, which varies
/// between targets (ADR 0026).
/// </remarks>
internal sealed record BuiltinTypeReference(string Spelling) : TypeReference
{
    /// <summary>Gets the <c>void</c> type, which a function without result returns.</summary>
    public static BuiltinTypeReference Void { get; } = new("void");
}
