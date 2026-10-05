namespace Jade.BindingGenerator.Targets;

/// <summary>A supported runtime identifier and the clang target triple its headers are parsed for.</summary>
/// <param name="RuntimeIdentifier">The .NET runtime identifier, such as <c>linux-x64</c>.</param>
/// <param name="Triple">The clang target triple, with the minimum OS version where the triple carries one.</param>
/// <param name="Platform">The platform family of the target, a single flag.</param>
internal sealed record Target(string RuntimeIdentifier, string Triple, Platforms Platform);
