namespace Jade.BindingGenerator.Projection;

/// <summary>A parameter of a raw function.</summary>
/// <param name="Name">The camel-case .NET name, escaped when it is a C# keyword.</param>
/// <param name="Type">The C# type.</param>
internal sealed record ProjectedParameter(string Name, string Type);
