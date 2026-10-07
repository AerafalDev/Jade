namespace Jade.BindingGenerator.Projection;

/// <summary>What an idiomatic method returns, from the C function's result and output parameter (ADR 0040).</summary>
internal enum IdiomaticReturnKind
{
    /// <summary>Nothing.</summary>
    Void,

    /// <summary>The C result, as is; a <c>WGPUBool</c> converts to <see langword="bool"/>.</summary>
    Value,

    /// <summary>Nothing: a <c>WGPUStatus</c> other than success throws.</summary>
    Status,

    /// <summary>The structure of the output parameter: a value structure or a managed copy.</summary>
    Output,

    /// <summary>A task completed by the function's callback.</summary>
    Task,
}
