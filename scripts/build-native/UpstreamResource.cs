/// <summary>An upstream that a bundled package downloads besides its sources (xmake's <c>add_resources</c>), recorded with it in versions.json.</summary>
internal sealed class UpstreamResource
{
    /// <summary>Gets the resource name in the package definition.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the pinned version.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the full upstream commit hash of that version.</summary>
    public required string Commit { get; init; }

    /// <summary>Gets the URL the resource was downloaded from.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the SHA-256 the download was verified against.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets the SPDX license expression.</summary>
    public required string License { get; init; }
}
