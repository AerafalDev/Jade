namespace Jade.BindingGenerator.Model;

/// <summary>A constant defined by a macro, such as <c>WGPU_WHOLE_SIZE</c> or <c>SDL_HINT_VIDEO_DRIVER</c>.</summary>
internal sealed record ConstantDeclaration : Declaration
{
    /// <summary>Gets the type of the constant: a builtin type, or <c>const char*</c> for a string.</summary>
    public required TypeReference Type { get; init; }

    /// <summary>Gets the value: an integer, a floating-point number, a string or the maximum of <see cref="Type"/>.</summary>
    public required ValueExpression Value { get; init; }

    /// <summary>Gets the C header that defines the constant, or <see langword="null"/> when its input is not a set of headers.</summary>
    public string? Header { get; init; }
}
