namespace Jade.BindingGenerator.Projection;

/// <summary>A constant made public on the handle or structure the configuration places it on (ADR 0040).</summary>
/// <param name="CName">The C name of the macro.</param>
/// <param name="Name">The .NET name, the raw layer's.</param>
/// <param name="Type">The C# type.</param>
/// <param name="Value">The C# expression of the value.</param>
/// <param name="IsConst">Whether the value is a C# constant; otherwise it becomes a static property.</param>
internal sealed record IdiomaticConstant(string CName, string Name, string Type, string Value, bool IsConst);
