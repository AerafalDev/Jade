using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Jade.NativeBuild.Build;

/// <summary>Checks the installed libraries of a target and reports them.</summary>
internal static class OutputCheck
{
    /// <summary>Checks that an output directory holds exactly the expected libraries, with their exports.</summary>
    /// <param name="directory">The output directory.</param>
    /// <param name="libraries">The libraries of the target.</param>
    /// <param name="symbols">
    /// Reads the symbols of libraries the build process cannot load, or <see langword="null"/> to
    /// load them into this process instead.
    /// </param>
    /// <param name="output">Receives one line per library, with its size and SHA-256.</param>
    /// <param name="cancellationToken">Cancels the checks and the report.</param>
    /// <returns>A task that completes when every library is checked.</returns>
    /// <exception cref="InvalidDataException">A library is missing, unexpected, not loadable or lacks an export.</exception>
    public static async Task VerifyAsync(string directory, IReadOnlyList<ExpectedLibrary> libraries, SymbolReader? symbols, TextWriter output, CancellationToken cancellationToken)
    {
        var expected = libraries.Select(static library => library.FileName).ToHashSet(StringComparer.Ordinal);
        var unexpected = Directory.EnumerateFileSystemEntries(directory)
            .Select(static entry => Path.GetFileName(entry))
            .Where(name => !expected.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (unexpected.Count > 0)
        {
            throw new InvalidDataException($"'{directory}' contains unexpected files: {string.Join(", ", unexpected)}.");
        }

        foreach (var library in libraries)
        {
            var path = Path.Combine(directory, library.FileName);

            if (!File.Exists(path))
            {
                throw new InvalidDataException($"The build did not produce '{path}'.");
            }

            if (library.Exports.Count > 0)
            {
                var missing = symbols is null
                    ? LoadAndFindMissingExports(path, library)
                    : [.. library.Exports.Except(await symbols.ReadAsync(path, cancellationToken).ConfigureAwait(false), StringComparer.Ordinal)];

                if (missing.Count > 0)
                {
                    throw new InvalidDataException($"'{path}' does not export {string.Join(", ", missing)}.");
                }
            }

            var stream = File.OpenRead(path);

            await using (stream.ConfigureAwait(false))
            {
                var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
                var line = string.Create(CultureInfo.InvariantCulture, $"{library.FileName}: {stream.Length} bytes, SHA-256 {hash}");

                await output.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Loads a library and resolves its expected exports.</summary>
    /// <param name="path">The path of the library.</param>
    /// <param name="library">The expected library.</param>
    /// <returns>The expected exports the library lacks.</returns>
    /// <exception cref="InvalidDataException">The library cannot be loaded.</exception>
    /// <remarks>
    /// The library stays loaded until the process exits: unloading Dawn's C++ runtime state is not
    /// something the check needs to exercise.
    /// </remarks>
    private static List<string> LoadAndFindMissingExports(string path, ExpectedLibrary library)
    {
        return NativeLibrary.TryLoad(path, out var handle)
            ? [.. library.Exports.Where(export => !NativeLibrary.TryGetExport(handle, export, out _))]
            : throw new InvalidDataException($"'{path}' cannot be loaded.");
    }
}
