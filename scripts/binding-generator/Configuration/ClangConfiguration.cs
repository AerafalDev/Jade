using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>The C header front-end settings of a library configuration.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record ClangConfiguration
{
    /// <summary>Gets the headers to parse, as written in an <c>#include</c> directive.</summary>
    public required IReadOnlyList<string> Headers { get; init; }

    /// <summary>Gets the include directories, relative to the dependency's sources.</summary>
    public required IReadOnlyList<string> IncludeDirectories { get; init; }

    /// <summary>
    /// Gets the files included before the headers, relative to the repository root. The native
    /// build force-includes the same files, so both see the same configuration macros.
    /// </summary>
    public IReadOnlyList<string> ForcedIncludes { get; init; } = [];
}
