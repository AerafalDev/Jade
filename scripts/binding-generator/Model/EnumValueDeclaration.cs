namespace Jade.BindingGenerator.Model;

/// <summary>A value of an enum or a set of flags, with its C name such as <c>WGPUTextureFormat_RGBA8Unorm</c>.</summary>
/// <remarks>Its <see cref="Declaration.Words"/> do not repeat the enum's name (ADR 0027).</remarks>
internal sealed record EnumValueDeclaration : Declaration
{
    /// <summary>Gets the numeric value, as the C header defines it; a negative value is stored sign-extended.</summary>
    public required ulong Value { get; init; }
}
