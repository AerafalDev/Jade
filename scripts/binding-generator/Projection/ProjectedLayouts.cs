namespace Jade.BindingGenerator.Projection;

/// <summary>The layout tests of an interop library: the structures whose C and C# layouts are compared (ADR 0036).</summary>
/// <param name="Namespace">The namespace of the interop library, such as <c>Jade.Sdl</c>; its tests live in <c>{Namespace}.Tests</c>.</param>
/// <param name="Name">The short name of the library, such as <c>sdl</c>, which names the C source of its layout library.</param>
/// <param name="Headers">What the C side includes.</param>
/// <param name="Records">The records, ordered by C name.</param>
internal sealed record ProjectedLayouts(string Namespace, string Name, LayoutHeaders Headers, IReadOnlyList<ProjectedRecordLayout> Records)
{
    /// <summary>
    /// Gets the name of the layout library and of the function it exports, such as
    /// <c>jade_sdl_layout</c>; each library has its own, so that several layout libraries can be
    /// linked into one module.
    /// </summary>
    public string NativeName => $"jade_{Name}_layout";
}
