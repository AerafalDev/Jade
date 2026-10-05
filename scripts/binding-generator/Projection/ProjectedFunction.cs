using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A raw function, imported from the native library by its C name.</summary>
/// <param name="CName">The C name, the entry point of the import.</param>
/// <param name="Name">The .NET name (ADR 0034).</param>
/// <param name="ReturnType">The C# return type.</param>
/// <param name="Parameters">The parameters.</param>
/// <param name="Availability">The platform families the function is available on.</param>
/// <param name="Header">The name of the C header that declares the function, without directory and extension, or <see langword="null"/>.</param>
internal sealed record ProjectedFunction(string CName, string Name, string ReturnType, IReadOnlyList<ProjectedParameter> Parameters, Platforms Availability, string? Header);
