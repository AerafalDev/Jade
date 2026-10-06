using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>A release archive of a pinned build tool, for one kind of host.</summary>
/// <remarks>
/// The hash pins the content: a release asset can be replaced under the same URL, and the version
/// the tool reports would not tell.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record ToolAsset
{
    /// <summary>Gets the URL the archive is downloaded from.</summary>
    public required Uri Url { get; init; }

    /// <summary>Gets the SHA-256 of the archive, in lowercase hexadecimal.</summary>
    public required string Sha256 { get; init; }
}
