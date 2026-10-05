using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A constant of the raw layer.</summary>
/// <param name="CName">The C name of the macro.</param>
/// <param name="Name">The .NET name.</param>
/// <param name="Type">The C# type.</param>
/// <param name="Value">The C# expression of the value.</param>
/// <param name="IsConst">Whether the value is a C# constant; the maximum of <c>nuint</c> and strings are not, and become properties.</param>
/// <param name="Availability">The platform families the constant is available on.</param>
/// <param name="Header">The name of the C header that defines the constant, without directory and extension, or <see langword="null"/>.</param>
internal sealed record ProjectedConstant(string CName, string Name, string Type, string Value, bool IsConst, Platforms Availability, string? Header);
