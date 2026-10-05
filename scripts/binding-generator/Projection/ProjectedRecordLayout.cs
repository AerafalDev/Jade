using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Projection;

/// <summary>A structure or a union whose C and C# layouts the tests compare (ADR 0036).</summary>
/// <param name="CName">The C name, or the name made for an anonymous record, which keys the C side's table.</param>
/// <param name="Type">The C# type, written with <c>global::</c>: a generated type, or the .NET type the configuration maps the record to.</param>
/// <param name="MethodName">The name of the C# method that measures the record.</param>
/// <param name="CRoot">How C code names the named record that holds an anonymous one, or the record itself.</param>
/// <param name="CPath">The member designators from <paramref name="CRoot"/> to an anonymous record, such as <c>input.axis</c>, or <see langword="null"/> for a named record.</param>
/// <param name="Availability">The platform families the record is available on; the C and C# sides only compare it there.</param>
/// <param name="Members">The members, in C layout order.</param>
internal sealed record ProjectedRecordLayout(
    string CName,
    string Type,
    string MethodName,
    string CRoot,
    string? CPath,
    Platforms Availability,
    IReadOnlyList<ProjectedMemberLayout> Members);
