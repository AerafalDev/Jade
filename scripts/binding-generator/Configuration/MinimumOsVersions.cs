using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>The minimum OS versions of <c>build/versions.json</c> (ADR 0024).</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record MinimumOsVersions
{
    /// <summary>Gets the minimum Windows version, as <c>major.minor.build</c>.</summary>
    public required string Windows { get; init; }

    /// <summary>Gets the minimum glibc version of Linux.</summary>
    public required string LinuxGlibc { get; init; }

    /// <summary>Gets the minimum macOS version.</summary>
    public required string Macos { get; init; }

    /// <summary>Gets the minimum iOS version.</summary>
    public required string Ios { get; init; }

    /// <summary>Gets the minimum Android API level.</summary>
    public required int AndroidApiLevel { get; init; }

    /// <summary>Gets where the minimum versions were decided.</summary>
    public required string Source { get; init; }
}
