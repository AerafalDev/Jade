namespace Jade.BindingGenerator.Emission;

/// <summary>One public overload of an idiomatic method (ADR 0040).</summary>
/// <param name="Text">How the overload takes text.</param>
/// <param name="IsOptionalPresent">Whether the overload takes the method's optional pointer parameter, if it has one.</param>
/// <param name="InputExtensions">The number of extensions chained to the input root, from 0 to 2 (ADR 0029).</param>
/// <param name="OutputExtensions">The number of extensions chained to the output, from 0 to 2.</param>
internal sealed record MethodOverload(TextForm Text, bool IsOptionalPresent, int InputExtensions, int OutputExtensions);
