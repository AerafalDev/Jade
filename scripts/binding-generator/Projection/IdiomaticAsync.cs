namespace Jade.BindingGenerator.Projection;

/// <summary>How an asynchronous function's callback completes the task its idiomatic method returns (ADR 0040).</summary>
internal sealed record IdiomaticAsync
{
    /// <summary>Gets the C# type of the raw callback info.</summary>
    public required string CallbackInfoType { get; init; }

    /// <summary>Gets the name of the callback info's raw field that holds the mode, or <see langword="null"/> when it has none.</summary>
    public string? ModeField { get; init; }

    /// <summary>Gets the C# expression of the mode that delivers the callback when the instance processes events.</summary>
    public string? ModeValue { get; init; }

    /// <summary>Gets the name of the callback info's raw field that holds the callback.</summary>
    public required string CallbackField { get; init; }

    /// <summary>Gets the name of the callback info's raw field that holds the first userdata pointer.</summary>
    public required string UserdataField { get; init; }

    /// <summary>Gets the name of the trampoline's parameter that receives the first userdata pointer.</summary>
    public required string UserdataParameter { get; init; }

    /// <summary>Gets the name of the trampoline, the <c>[UnmanagedCallersOnly]</c> method the library calls.</summary>
    public required string Trampoline { get; init; }

    /// <summary>Gets the parameters of the trampoline, as C# types and names, the userdata included.</summary>
    public required IReadOnlyList<(string Type, string Name)> TrampolineParameters { get; init; }

    /// <summary>Gets the C# type of the status, the callback's first parameter.</summary>
    public required string StatusType { get; init; }

    /// <summary>Gets the C# expression of the status that completes the task successfully.</summary>
    public required string SuccessValue { get; init; }

    /// <summary>Gets the name of the parameter that carries the result, or <see langword="null"/> for a task without result.</summary>
    public string? ResultParameter { get; init; }

    /// <summary>Gets the C# type of the task's result, or <see langword="null"/> for a task without result.</summary>
    public string? ResultType { get; init; }

    /// <summary>Gets whether the result is a managed copy built from a pointer to a raw structure.</summary>
    public bool ResultIsSnapshot { get; init; }

    /// <summary>Gets the name of the parameter that carries the failure message, or <see langword="null"/>.</summary>
    public string? MessageParameter { get; init; }
}
