namespace Jade.BindingGenerator.Projection;

/// <summary>The raw layer of an interop library, ready to be emitted.</summary>
/// <param name="Namespace">The namespace of the library, which is also its project name (ADR 0027).</param>
/// <param name="LibraryName">The name the functions are imported from.</param>
/// <param name="Types">The types, ordered by C# name.</param>
/// <param name="Constants">The constants, ordered by C name.</param>
/// <param name="Functions">The functions, ordered by C name.</param>
internal sealed record ProjectedLibrary(
    string Namespace,
    string LibraryName,
    IReadOnlyList<ProjectedType> Types,
    IReadOnlyList<ProjectedConstant> Constants,
    IReadOnlyList<ProjectedFunction> Functions);
