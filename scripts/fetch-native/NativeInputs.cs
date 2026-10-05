using Jade.NativeBuild.Tools;

namespace Jade.NativeFetch;

/// <summary>Compares the inputs of the native build between the checkout and the commit of a run.</summary>
/// <param name="root">The repository root.</param>
/// <remarks>
/// The paths are those the changes job of <c>.github/workflows/native.yml</c> compares: a change
/// to them is what makes the workflow build, so equal inputs mean equal libraries.
/// </remarks>
internal sealed class NativeInputs(string root)
{
    /// <summary>The inputs of the native build, relative to the repository root.</summary>
    private static readonly string[] _paths = ["build", "scripts/build-native.cs", "scripts/build-native", "global.json", ".github/workflows/native.yml"];

    /// <summary>Tells whether the checkout, uncommitted changes included, has the inputs of a commit.</summary>
    /// <param name="commit">The commit of a run.</param>
    /// <param name="cancellationToken">Stops git.</param>
    /// <returns>
    /// <see langword="true"/> when the inputs are the same, <see langword="false"/> when they differ
    /// or the commit cannot be fetched from <c>origin</c>.
    /// </returns>
    public async Task<bool> MatchAsync(string commit, CancellationToken cancellationToken)
    {
        if (!await HasCommitAsync(commit, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        var changed = await GitAsync(["diff", "--name-only", commit, "--", .. _paths], cancellationToken).ConfigureAwait(false);
        var untracked = await GitAsync(["ls-files", "--others", "--exclude-standard", "--", .. _paths], cancellationToken).ConfigureAwait(false);

        return changed.Length == 0 && untracked.Length == 0;
    }

    /// <summary>Makes a commit available locally, fetching it from <c>origin</c> when needed.</summary>
    /// <param name="commit">The commit.</param>
    /// <param name="cancellationToken">Stops git.</param>
    /// <returns><see langword="true"/> when the commit is available.</returns>
    private async Task<bool> HasCommitAsync(string commit, CancellationToken cancellationToken)
    {
        try
        {
            _ = await GitAsync(["cat-file", "-e", commit + "^{commit}"], cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (CommandFailedException)
        {
            try
            {
                // No --depth: it would make a complete clone shallow.
                _ = await GitAsync(["fetch", "--quiet", "origin", commit], cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (CommandFailedException)
            {
                return false;
            }
        }
    }

    /// <summary>Runs git in the repository.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <param name="cancellationToken">Stops git.</param>
    /// <returns>The trimmed standard output.</returns>
    private async Task<string> GitAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        return (await Command.ReadAsync("git", ["-C", root, .. arguments], environment: null, cancellationToken).ConfigureAwait(false)).Trim();
    }
}
