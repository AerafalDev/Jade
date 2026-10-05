using Jade.BindingGenerator.Clang;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Dawn;
using Jade.BindingGenerator.Sources;

namespace Jade.BindingGenerator;

/// <summary>Loads a generated library's inputs through the front-end its configuration selects.</summary>
/// <param name="versions">The pinned versions, which give each library's source commit.</param>
/// <param name="cache">Fetches the pinned sources.</param>
/// <param name="headerParser">Parses C headers for every target.</param>
internal sealed class LibraryLoader(PinnedVersions versions, SourceCache cache, HeaderParser headerParser)
{
    /// <summary>Reads the library's configuration, fetches its sources and loads them.</summary>
    /// <param name="library">The interop project to load.</param>
    /// <param name="cancellationToken">Cancels the download of the sources.</param>
    /// <returns>The library with its loaded inputs.</returns>
    /// <exception cref="InvalidDataException">The configuration or the sources are invalid.</exception>
    public async Task<LoadedLibrary> LoadAsync(GeneratedLibrary library, CancellationToken cancellationToken)
    {
        var configuration = LibraryConfiguration.Load(library.ConfigurationFile);
        var dependency = versions.GetDependency(configuration.Dependency);
        var sourceDirectory = await cache.GetAsync(configuration.Dependency, dependency, configuration.Sources, cancellationToken).ConfigureAwait(false);
        var loaded = new LoadedLibrary(library, configuration, dependency);

        return configuration switch
        {
            { DawnJson: { } dawnJson } => loaded with { Dawn = DawnApi.Load(Path.Combine(sourceDirectory, dawnJson)) },
            { Clang: { } clang } => loaded with { Headers = headerParser.Parse(configuration.Dependency, sourceDirectory, clang) },
            _ => throw new InvalidDataException($"'{library.ConfigurationFile}' selects no front-end."),
        };
    }
}
