using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>A native dependency pinned in <c>build/versions.json</c>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PinnedDependency
{
    /// <summary>Gets the URL of the git repository the sources are fetched from.</summary>
    public required string Repository { get; init; }

    /// <summary>Gets the upstream tag of the pinned release.</summary>
    public required string Tag { get; init; }

    /// <summary>Gets the upstream version, when the project numbers its releases.</summary>
    public string? Version { get; init; }

    /// <summary>Gets the full hash of the pinned commit, which the sources are fetched at.</summary>
    public required string Commit { get; init; }

    /// <summary>Gets the SPDX license expression of the dependency.</summary>
    public required string License { get; init; }

    /// <summary>Gets where the pinned values were verified.</summary>
    public required string Source { get; init; }
}
