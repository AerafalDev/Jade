namespace Jade.BindingGenerator.Sources;

/// <summary>A GitHub repository, and the URLs that serve its content at a given commit.</summary>
/// <param name="Owner">The account that owns the repository.</param>
/// <param name="Name">The name of the repository.</param>
/// <remarks>GitHub serves a commit's content by hash, so these URLs pin what they return.</remarks>
internal sealed record GitHubRepository(string Owner, string Name)
{
    /// <summary>The prefix of the repository URLs that <c>build/versions.json</c> holds.</summary>
    private const string UrlPrefix = "https://github.com/";

    /// <summary>Parses a repository URL such as <c>https://github.com/libsdl-org/SDL</c>.</summary>
    /// <param name="url">The repository URL.</param>
    /// <returns>The repository.</returns>
    /// <exception cref="InvalidDataException">The URL is not a GitHub repository URL.</exception>
    public static GitHubRepository Parse(string url)
    {
        var segments = url.StartsWith(UrlPrefix, StringComparison.Ordinal) ? url[UrlPrefix.Length..].Split('/') : [];

        return segments is [{ Length: > 0 } owner, { Length: > 0 } name]
            ? new GitHubRepository(owner, name)
            : throw new InvalidDataException($"'{url}' is not a GitHub repository URL.");
    }

    /// <summary>Gets the URL of one file at a commit.</summary>
    /// <param name="commit">The full commit hash.</param>
    /// <param name="path">The path of the file in the repository.</param>
    /// <returns>The raw content URL.</returns>
    public Uri GetFileUri(string commit, string path)
    {
        return new Uri($"https://raw.githubusercontent.com/{Owner}/{Name}/{commit}/{path}");
    }

    /// <summary>Gets the URL of the gzip-compressed tar archive of a commit.</summary>
    /// <param name="commit">The full commit hash.</param>
    /// <returns>The archive URL.</returns>
    public Uri GetArchiveUri(string commit)
    {
        return new Uri($"https://codeload.github.com/{Owner}/{Name}/tar.gz/{commit}");
    }
}
