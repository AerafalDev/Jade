using System.Formats.Tar;
using System.IO.Compression;
using Jade.BindingGenerator.Configuration;

namespace Jade.BindingGenerator.Sources;

/// <summary>
/// Fetches the generator's inputs at the commits pinned in <c>build/versions.json</c> and keeps them
/// in <c>artifacts/binding-generator/sources/&lt;dependency&gt;/&lt;commit&gt;/</c>.
/// </summary>
/// <param name="directory">The root directory of the cache.</param>
/// <param name="http">The client that downloads from GitHub.</param>
internal sealed class SourceCache(string directory, HttpClient http)
{
    /// <summary>The suffix of the directory a fetch is written to before it is complete.</summary>
    private const string StagingSuffix = ".partial";

    /// <summary>Gets the directory that holds the requested paths of a dependency, fetching them if needed.</summary>
    /// <param name="name">The key of the dependency in <c>build/versions.json</c>.</param>
    /// <param name="dependency">The pinned dependency.</param>
    /// <param name="paths">The paths needed from the repository; a trailing <c>/</c> marks a directory.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>The directory that mirrors the repository for the requested paths.</returns>
    /// <exception cref="InvalidDataException">A requested path does not exist at the pinned commit.</exception>
    /// <exception cref="HttpRequestException">GitHub cannot be reached or refuses the request.</exception>
    public async Task<string> GetAsync(string name, PinnedDependency dependency, IReadOnlyList<string> paths, CancellationToken cancellationToken)
    {
        var target = Path.Combine(directory, name, dependency.Commit);

        if (paths.All(path => Contains(target, path)))
        {
            return target;
        }

        // A fetch is written aside and moved in place once complete, so an interrupted run never
        // leaves a cache entry that looks valid.
        var staging = target + StagingSuffix;

        DeleteDirectory(staging);
        _ = Directory.CreateDirectory(staging);

        await FetchAsync(GitHubRepository.Parse(dependency.Repository), dependency.Commit, paths, staging, cancellationToken).ConfigureAwait(false);

        if (paths.FirstOrDefault(path => !Contains(staging, path)) is { } missing)
        {
            throw new InvalidDataException($"'{missing}' does not exist in {dependency.Repository} at {dependency.Commit}.");
        }

        DeleteDirectory(target);
        Directory.Move(staging, target);

        return target;
    }

    /// <summary>Tells whether a requested path names a directory.</summary>
    /// <param name="path">A path from a library configuration.</param>
    /// <returns><see langword="true"/> when the path ends with <c>/</c>.</returns>
    private static bool IsDirectory(string path)
    {
        return path.EndsWith('/', StringComparison.Ordinal);
    }

    /// <summary>Tells whether a local directory already holds a requested path.</summary>
    /// <param name="root">The local mirror of the repository.</param>
    /// <param name="path">The requested path.</param>
    /// <returns><see langword="true"/> when the file exists, or the directory exists and is not empty.</returns>
    private static bool Contains(string root, string path)
    {
        var fullPath = Path.Combine(root, path);

        return IsDirectory(path)
            ? Directory.Exists(fullPath) && Directory.EnumerateFileSystemEntries(fullPath).Any()
            : File.Exists(fullPath);
    }

    /// <summary>Deletes a directory and its content if it exists.</summary>
    /// <param name="path">The directory to delete.</param>
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    /// <summary>Resolves a repository path inside a local mirror and creates its parent directory.</summary>
    /// <param name="root">The local mirror of the repository.</param>
    /// <param name="path">The path in the repository.</param>
    /// <returns>The absolute local path.</returns>
    /// <exception cref="InvalidDataException">The path escapes the mirror.</exception>
    private static string PrepareDestination(string root, string path)
    {
        var destination = Path.GetFullPath(Path.Combine(root, path));

        if (!destination.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"'{path}' points outside the source directory.");
        }

        _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        return destination;
    }

    /// <summary>Downloads the requested paths of a commit into a local mirror.</summary>
    /// <param name="repository">The repository to fetch from.</param>
    /// <param name="commit">The pinned commit.</param>
    /// <param name="paths">The requested paths.</param>
    /// <param name="root">The local mirror to write to.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>A task that completes when every path is written.</returns>
    private async Task FetchAsync(GitHubRepository repository, string commit, IReadOnlyList<string> paths, string root, CancellationToken cancellationToken)
    {
        var directories = paths.Where(IsDirectory).ToList();

        // Single files come from the raw content service, which avoids downloading a whole archive
        // (Dawn's is large) for one file; directories need the archive.
        if (directories.Count > 0)
        {
            await ExtractDirectoriesAsync(repository, commit, directories, root, cancellationToken).ConfigureAwait(false);
        }

        foreach (var file in paths.Where(static path => !IsDirectory(path)))
        {
            await DownloadFileAsync(repository, commit, file, root, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Downloads one file of a commit.</summary>
    /// <param name="repository">The repository to fetch from.</param>
    /// <param name="commit">The pinned commit.</param>
    /// <param name="path">The path of the file in the repository.</param>
    /// <param name="root">The local mirror to write to.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>A task that completes when the file is written.</returns>
    private async Task DownloadFileAsync(GitHubRepository repository, string commit, string path, string root, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(repository.GetFileUri(commit, path), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();

        using var file = File.Create(PrepareDestination(root, path));

        await response.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Extracts the requested directories from the archive of a commit.</summary>
    /// <param name="repository">The repository to fetch from.</param>
    /// <param name="commit">The pinned commit.</param>
    /// <param name="directories">The requested directories, each ending with <c>/</c>.</param>
    /// <param name="root">The local mirror to write to.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>A task that completes when the directories are extracted.</returns>
    private async Task ExtractDirectoriesAsync(GitHubRepository repository, string commit, IReadOnlyList<string> directories, string root, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(repository.GetArchiveUri(commit), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();

        using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var decompressed = new GZipStream(content, CompressionMode.Decompress);
        using var reader = new TarReader(decompressed);

        while (await reader.GetNextEntryAsync(copyData: false, cancellationToken).ConfigureAwait(false) is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile))
            {
                continue;
            }

            // GitHub archives wrap the tree in a single '<repository>-<commit>/' directory.
            var path = entry.Name[(entry.Name.IndexOf('/', StringComparison.Ordinal) + 1)..];

            if (directories.Any(directory => path.StartsWith(directory, StringComparison.Ordinal)))
            {
                await entry.ExtractToFileAsync(PrepareDestination(root, path), overwrite: false, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
