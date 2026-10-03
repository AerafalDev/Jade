/// <summary>One bundled upstream: its jade/upstream.json (native/modules/stage.lua), and its entry in versions.json.</summary>
internal sealed class Upstream
{
    /// <summary>Gets the xmake package name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the pinned version.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the full upstream commit hash of that version.</summary>
    public required string Commit { get; init; }

    /// <summary>Gets the URL the sources were downloaded from.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the SHA-256 the download was verified against.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets the SPDX license expression.</summary>
    public required string License { get; init; }

    /// <summary>Gets the compile-time defines that shape the public API, which header parsers must reuse.</summary>
    public required IReadOnlyList<string> Defines { get; init; }

    /// <summary>Gets the other upstreams the package downloads and builds with its sources, sorted by name.</summary>
    public required IReadOnlyList<UpstreamResource> Resources { get; init; }
}
