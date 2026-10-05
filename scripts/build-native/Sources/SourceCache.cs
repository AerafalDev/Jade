using Jade.NativeBuild.Configuration;

namespace Jade.NativeBuild.Sources;

/// <summary>
/// Fetches the native sources at the commits pinned in <c>build/versions.json</c> and keeps them in
/// <c>artifacts/native/sources/&lt;dependency&gt;/&lt;commit&gt;/</c>.
/// </summary>
/// <param name="directory">The root directory of the cache.</param>
/// <param name="output">Receives a line for each fetch.</param>
internal sealed class SourceCache(string directory, TextWriter output)
{
    /// <summary>The suffix of the directory a fetch is written to before it is complete.</summary>
    private const string StagingSuffix = ".partial";

    /// <summary>The number of characters of a commit hash shown in the progress lines.</summary>
    private const int ShortCommitLength = 12;

    /// <summary>Gets the source tree of a dependency, fetching it if needed.</summary>
    /// <param name="name">The key of the dependency in <c>build/versions.json</c>.</param>
    /// <param name="dependency">The pinned dependency.</param>
    /// <param name="depsPaths">
    /// The entries of the dependency's <c>DEPS</c> file to check out into the tree, at the commits that
    /// file pins; empty for a dependency without one.
    /// </param>
    /// <param name="cancellationToken">Stops git.</param>
    /// <returns>The directory of the source tree.</returns>
    /// <exception cref="InvalidDataException">A fetched commit or a <c>DEPS</c> entry is invalid.</exception>
    /// <exception cref="Tools.CommandFailedException">git fails.</exception>
    public async Task<string> GetAsync(string name, PinnedDependency dependency, IReadOnlyList<string> depsPaths, CancellationToken cancellationToken)
    {
        var target = Path.Combine(directory, name, dependency.Commit);

        if (Directory.Exists(target) && depsPaths.All(path => IsPopulated(Resolve(target, path))))
        {
            return target;
        }

        // A fetch is written aside and moved in place once complete, so an interrupted run never
        // leaves a tree that looks valid.
        var staging = target + StagingSuffix;

        DeleteDirectory(staging);

        await ReportAsync($"{name} {dependency.Tag}", dependency.Commit, cancellationToken).ConfigureAwait(false);
        await GitCheckout.FetchAsync(dependency.Repository, dependency.Commit, staging, cancellationToken).ConfigureAwait(false);

        if (depsPaths.Count > 0)
        {
            var deps = DepsFile.Load(Path.Combine(staging, "DEPS"));

            foreach (var path in depsPaths)
            {
                var entry = deps.GetEntry(path);

                await ReportAsync($"{name} {path}", entry.Commit, cancellationToken).ConfigureAwait(false);
                await GitCheckout.FetchAsync(entry.Repository, entry.Commit, Resolve(staging, path), cancellationToken).ConfigureAwait(false);
            }
        }

        DeleteDirectory(target);
        Directory.Move(staging, target);

        return target;
    }

    /// <summary>Resolves a path inside a source tree.</summary>
    /// <param name="root">The source tree.</param>
    /// <param name="path">The relative path.</param>
    /// <returns>The absolute path.</returns>
    /// <exception cref="InvalidDataException">The path escapes the tree.</exception>
    private static string Resolve(string root, string path)
    {
        var resolved = Path.GetFullPath(Path.Combine(root, path));

        return resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? resolved
            : throw new InvalidDataException($"'{path}' points outside the source tree.");
    }

    /// <summary>Tells whether a directory exists and is not empty.</summary>
    /// <param name="path">The directory.</param>
    /// <returns><see langword="true"/> when the directory has content.</returns>
    private static bool IsPopulated(string path)
    {
        return Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any();
    }

    /// <summary>Deletes a directory and its content if it exists.</summary>
    /// <param name="path">The directory to delete.</param>
    /// <remarks>git writes its object files read-only, which prevents their deletion on Windows.</remarks>
    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(path, recursive: true);
    }

    /// <summary>Writes the line that announces a fetch.</summary>
    /// <param name="what">What is fetched.</param>
    /// <param name="commit">The commit fetched.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the line is written.</returns>
    private async Task ReportAsync(string what, string commit, CancellationToken cancellationToken)
    {
        await output.WriteLineAsync($"Fetching {what} ({commit[..ShortCommitLength]})".AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
