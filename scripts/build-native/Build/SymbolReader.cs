using Jade.NativeBuild.Tools;

namespace Jade.NativeBuild.Build;

/// <summary>
/// Lists the symbols a library defines with an <c>nm</c> tool, for the targets whose libraries the
/// build process cannot load (another platform or architecture, or a static archive).
/// </summary>
/// <param name="Program">The <c>nm</c> program.</param>
/// <param name="Arguments">The arguments that come before the library's path and select its defined, external symbols, names only.</param>
/// <param name="Environment">The environment of the program, or <see langword="null"/>.</param>
/// <param name="Prefixed">Whether the object format prefixes C symbols with an underscore (Mach-O).</param>
internal sealed record SymbolReader(string Program, IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string?>? Environment, bool Prefixed)
{
    /// <summary>Lists the external symbols a library defines.</summary>
    /// <param name="path">The library: a shared library, or an archive whose members are all read.</param>
    /// <param name="cancellationToken">Stops the program.</param>
    /// <returns>The C names of the symbols.</returns>
    public async Task<HashSet<string>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var output = await Command.ReadAsync(Program, [.. Arguments, path], Environment, cancellationToken).ConfigureAwait(false);
        var symbols = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // An archive is listed member by member, each under a "name:" or "archive(name):" line.
            if (line[^1] == ':')
            {
                continue;
            }

            // ELF symbol versions follow the name (SDL_Init@@SDL3_0.0.0).
            var version = line.IndexOf('@', StringComparison.Ordinal);
            var name = version < 0 ? line : line[..version];

            _ = symbols.Add(Prefixed && name[0] == '_' ? name[1..] : name);
        }

        return symbols;
    }
}
