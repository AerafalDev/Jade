namespace Jade.BindingGenerator.Model;

/// <summary>A function pointer type, such as a callback's.</summary>
internal sealed record FunctionPointerDeclaration : Declaration
{
    /// <summary>Gets the return type.</summary>
    public required TypeReference ReturnType { get; init; }

    /// <summary>Gets the parameters, the userdata pointers of a callback included.</summary>
    public required IReadOnlyList<Parameter> Parameters { get; init; }
}
