using Jade.BindingGenerator.Configuration;

namespace Jade.BindingGenerator.Projection;

/// <summary>What the C side of the layout tests includes, so that it sees the declarations the bindings were generated from (ADR 0036).</summary>
/// <param name="Defines">The macros defined first, as <c>NAME</c> or <c>NAME=value</c>.</param>
/// <param name="ForcedIncludes">The files included before the headers, relative to the repository root.</param>
/// <param name="Headers">The headers, as an <c>#include</c> directive writes them between angle brackets.</param>
/// <param name="Shims">The repository's shim headers, relative to the repository root, included last.</param>
internal sealed record LayoutHeaders(IReadOnlyList<string> Defines, IReadOnlyList<string> ForcedIncludes, IReadOnlyList<string> Headers, IReadOnlyList<string> Shims)
{
    /// <summary>Gets the headers of a library parsed by the C header front-end: those the generator parsed, in the same order.</summary>
    /// <param name="configuration">The library's C header settings.</param>
    /// <returns>The headers and macros of the parse.</returns>
    public static LayoutHeaders FromClang(ClangConfiguration configuration)
    {
        return new LayoutHeaders(configuration.Defines, configuration.ForcedIncludes, configuration.Headers, configuration.Shims);
    }

    /// <summary>Gets the headers of a library read from <c>dawn.json</c>: the one header Dawn's build generates from it.</summary>
    /// <param name="header">The header, as an <c>#include</c> directive writes it.</param>
    /// <returns>The header alone.</returns>
    public static LayoutHeaders FromHeader(string header)
    {
        return new LayoutHeaders([], [], [header], []);
    }
}
