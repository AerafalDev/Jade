namespace Jade.NativeFetch;

/// <summary>A run of the native workflow.</summary>
/// <param name="Id">The run's identifier.</param>
/// <param name="HeadSha">The commit the run built, which its attestations name as their source.</param>
internal sealed record WorkflowRun(long Id, string HeadSha);
