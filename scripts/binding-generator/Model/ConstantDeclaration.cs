namespace Jade.BindingGenerator.Model;

/// <summary>A constant defined by a macro, such as <c>WGPU_WHOLE_SIZE</c>.</summary>
internal sealed record ConstantDeclaration : Declaration
{
    /// <summary>Gets the type of the constant.</summary>
    public required BuiltinTypeReference Type { get; init; }

    /// <summary>Gets the value: an integer, a floating-point number or the maximum of <see cref="Type"/>.</summary>
    public required ValueExpression Value { get; init; }
}
