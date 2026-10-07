using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>An idiomatic method, property or static method that calls one C function (ADR 0040).</summary>
internal sealed record IdiomaticMethod
{
    /// <summary>Gets the function of the model.</summary>
    public required FunctionDeclaration Function { get; init; }

    /// <summary>Gets the .NET name of the raw function in <c>NativeMethods</c>.</summary>
    public required string RawName { get; init; }

    /// <summary>Gets the .NET name of the method or property.</summary>
    public required string Name { get; init; }

    /// <summary>Gets where and how the member is declared.</summary>
    public required IdiomaticMethodKind Kind { get; init; }

    /// <summary>Gets the parameters of the C function after the handle it belongs to, the hidden ones included.</summary>
    public required IReadOnlyList<IdiomaticParameter> Parameters { get; init; }

    /// <summary>Gets what the member returns.</summary>
    public required IdiomaticReturnKind ReturnKind { get; init; }

    /// <summary>Gets the C# return type of the member.</summary>
    public required string ReturnType { get; init; }

    /// <summary>Gets whether the C function returns a <c>WGPUStatus</c>, which the member checks and throws on.</summary>
    public bool ChecksStatus { get; init; }

    /// <summary>Gets the platforms the member is available on.</summary>
    public required Platforms Availability { get; init; }

    /// <summary>Gets how the callback completes the task, for an asynchronous method.</summary>
    public IdiomaticAsync? Async { get; init; }
}
