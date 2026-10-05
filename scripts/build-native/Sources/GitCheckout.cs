using Jade.NativeBuild.Tools;

namespace Jade.NativeBuild.Sources;

/// <summary>Fetches one commit of a git repository, without its history.</summary>
/// <remarks>
/// A checkout rather than an archive: Dawn's build reads its own commit with <c>git rev-parse</c>
/// and puts it in the key of its pipeline cache, which would otherwise be the same for every
/// version (<c>generator/dawn_version_generator.py</c>).
/// </remarks>
internal static class GitCheckout
{
    /// <summary>Fetches a commit into an empty directory and checks what was checked out.</summary>
    /// <param name="repository">The URL of the repository.</param>
    /// <param name="commit">The full hash of the commit.</param>
    /// <param name="directory">The directory to check the commit out into; it must not exist or be empty.</param>
    /// <param name="cancellationToken">Stops git.</param>
    /// <returns>A task that completes when the commit is checked out.</returns>
    /// <exception cref="CommandFailedException">git cannot reach the repository or the commit.</exception>
    /// <exception cref="InvalidDataException">The checked-out commit is not the requested one.</exception>
    public static async Task FetchAsync(string repository, string commit, string directory, CancellationToken cancellationToken)
    {
        _ = Directory.CreateDirectory(directory);

        await Command.RunAsync("git", ["init", "--quiet", directory], environment: null, workingDirectory: null, cancellationToken).ConfigureAwait(false);

        // Both GitHub and googlesource serve a commit by its hash, so a single-commit fetch is
        // enough; the hash also pins the content.
        await Command.RunAsync("git", ["-C", directory, "fetch", "--quiet", "--depth", "1", repository, commit], environment: null, workingDirectory: null, cancellationToken).ConfigureAwait(false);

        // A user's core.autocrlf would rewrite the line endings of the sources, which the code
        // generators of Dawn and SDL3 read. Git for Windows refuses paths longer than MAX_PATH
        // without core.longpaths, and the longest path of the pinned DXC (133 characters) goes
        // past it from the source cache; other platforms ignore the setting.
        await Command.RunAsync("git", ["-C", directory, "-c", "core.autocrlf=false", "-c", "core.longpaths=true", "checkout", "--quiet", "--detach", "FETCH_HEAD"], environment: null, workingDirectory: null, cancellationToken).ConfigureAwait(false);

        var head = (await Command.ReadAsync("git", ["-C", directory, "rev-parse", "HEAD"], environment: null, cancellationToken).ConfigureAwait(false)).Trim();

        if (!string.Equals(head, commit, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{repository} checked out {head} instead of {commit}.");
        }
    }
}
