using Jade.BindingGenerator.Clang;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Dawn;

namespace Jade.BindingGenerator;

/// <summary>A generated library with the inputs its front-end loaded.</summary>
/// <param name="Library">The interop project the library is generated into.</param>
/// <param name="Configuration">The library's <c>bindings.json</c>.</param>
/// <param name="Dependency">The pinned source of the library.</param>
/// <remarks>Exactly one of <see cref="Dawn"/> and <see cref="Headers"/> is set, as in the configuration.</remarks>
internal sealed record LoadedLibrary(GeneratedLibrary Library, LibraryConfiguration Configuration, PinnedDependency Dependency)
{
    /// <summary>Gets the WebGPU API read from <c>dawn.json</c>, for a library configured with <c>dawnJson</c>.</summary>
    public DawnApi? Dawn { get; init; }

    /// <summary>Gets the declarations parsed from the C headers, for a library configured with <c>clang</c>.</summary>
    public ParsedHeaders? Headers { get; init; }
}
