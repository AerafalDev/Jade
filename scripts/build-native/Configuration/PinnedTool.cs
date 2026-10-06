using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>A build tool pinned to an exact version in <c>build/versions.json</c>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PinnedTool
{
    /// <summary>Gets the version the tool must report.</summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the release archives that <c>--install-tools</c> installs, by host: <c>&lt;os&gt;-&lt;arch&gt;</c>
    /// (<c>windows-arm64</c>), or <c>&lt;os&gt;</c> alone for an archive that serves every architecture.
    /// </summary>
    public required IReadOnlyDictionary<string, ToolAsset> Assets { get; init; }

    /// <summary>Gets where the version and the assets were verified.</summary>
    public required string Source { get; init; }
}
