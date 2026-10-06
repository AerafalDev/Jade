using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>The Android NDK pinned in <c>build/versions.json</c>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PinnedAndroidNdk
{
    /// <summary>Gets the release name, such as <c>r28c</c>.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the revision the NDK's <c>source.properties</c> reports, which is also its directory name under the Android SDK's <c>ndk/</c>.</summary>
    public required string Revision { get; init; }

    /// <summary>Gets where the version was verified.</summary>
    public required string Source { get; init; }
}
