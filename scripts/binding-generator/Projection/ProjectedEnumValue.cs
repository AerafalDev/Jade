using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A value of a C# enum.</summary>
/// <param name="CName">The C name of the value.</param>
/// <param name="Name">The .NET name of the value.</param>
/// <param name="Value">The numeric value.</param>
/// <param name="Availability">The platform families the value is available on.</param>
internal sealed record ProjectedEnumValue(string CName, string Name, ulong Value, Platforms Availability);
