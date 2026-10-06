using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>The minimum OS versions of ADR 0024, which the builds use as compile targets.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record MinimumOsVersions
{
    /// <summary>Gets the minimum Windows version, such as <c>10.0.14393</c>.</summary>
    public required string Windows { get; init; }

    /// <summary>Gets the glibc the Linux libraries are built against, which the build environment provides.</summary>
    public required string LinuxGlibc { get; init; }

    /// <summary>Gets the macOS deployment target.</summary>
    public required string Macos { get; init; }

    /// <summary>Gets the iOS deployment target.</summary>
    public required string Ios { get; init; }

    /// <summary>Gets the Android API level the libraries are built for.</summary>
    public required int AndroidApiLevel { get; init; }

    /// <summary>Gets where the versions were decided.</summary>
    public required string Source { get; init; }
}
