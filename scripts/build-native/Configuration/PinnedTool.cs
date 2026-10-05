using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>A build tool pinned to an exact version in <c>build/versions.json</c>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PinnedTool
{
    /// <summary>Gets the version the tool must report.</summary>
    public required string Version { get; init; }

    /// <summary>Gets where the version was verified.</summary>
    public required string Source { get; init; }
}
