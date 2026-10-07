namespace Jade.Wgpu;

/// <summary>An error a device reports, through an error scope or as an uncaptured error.</summary>
/// <param name="Type">The type of the error; <see cref="ErrorType.NoError"/> when an error scope captured none.</param>
/// <param name="Message">The message, or <see langword="null"/> when there is none.</param>
public readonly record struct GpuError(ErrorType Type, string? Message);
