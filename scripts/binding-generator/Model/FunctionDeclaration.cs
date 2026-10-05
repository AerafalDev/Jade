namespace Jade.BindingGenerator.Model;

/// <summary>An exported function.</summary>
/// <remarks>
/// For a function that belongs to a type, <see cref="Declaration.Words"/> name the operation alone
/// (<c>create buffer</c> for <c>wgpuDeviceCreateBuffer</c>) and <see cref="Owner"/> names the type.
/// </remarks>
internal sealed record FunctionDeclaration : Declaration
{
    /// <summary>Gets the return type.</summary>
    public required TypeReference ReturnType { get; init; }

    /// <summary>Gets whether the function may return null.</summary>
    public bool ReturnIsOptional { get; init; }

    /// <summary>Gets the parameters, the one that receives the owner included.</summary>
    public required IReadOnlyList<Parameter> Parameters { get; init; }

    /// <summary>Gets what the function is for.</summary>
    public FunctionKind Kind { get; init; }

    /// <summary>Gets the C name of the handle or structure the function belongs to, or <see langword="null"/> for a free function.</summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets the C header that declares the function, such as <c>SDL3/SDL_video.h</c>, or
    /// <see langword="null"/> when its input is not a set of headers.
    /// </summary>
    public string? Header { get; init; }
}
