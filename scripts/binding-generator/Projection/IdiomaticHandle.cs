using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>What the idiomatic layer adds to a handle of the raw layer: its methods, properties and constants (ADR 0040).</summary>
/// <param name="Name">The .NET name of the handle.</param>
/// <param name="CName">The C name of the handle.</param>
/// <param name="Availability">The platforms the handle is available on.</param>
/// <param name="Methods">The members that call the handle's functions, ordered by name.</param>
/// <param name="Constants">The constants the configuration places on the handle.</param>
internal sealed record IdiomaticHandle(string Name, string CName, Platforms Availability, IReadOnlyList<IdiomaticMethod> Methods, IReadOnlyList<IdiomaticConstant> Constants);
