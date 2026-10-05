using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A constant of the raw layer, with its C name.</summary>
/// <param name="CName">The C name, which is also the C# name.</param>
/// <param name="Type">The C# type.</param>
/// <param name="Value">The C# expression of the value.</param>
/// <param name="IsConst">Whether the value is a C# constant; the maximum of <c>nuint</c> is not, and becomes a property.</param>
/// <param name="Availability">The platform families the constant is available on.</param>
internal sealed record ProjectedConstant(string CName, string Type, string Value, bool IsConst, Platforms Availability);
