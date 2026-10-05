using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A raw function, imported from the native library with its C name.</summary>
/// <param name="CName">The C name, which is also the C# name.</param>
/// <param name="ReturnType">The C# return type.</param>
/// <param name="Parameters">The parameters.</param>
/// <param name="Availability">The platform families the function is available on.</param>
internal sealed record ProjectedFunction(string CName, string ReturnType, IReadOnlyList<ProjectedParameter> Parameters, Platforms Availability);
