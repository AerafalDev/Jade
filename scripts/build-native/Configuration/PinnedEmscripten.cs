using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>The Emscripten toolchain of the SDK's <c>wasm-tools</c> workload, which builds the browser archives (ADR 0025).</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PinnedEmscripten
{
    /// <summary>Gets where the toolchain comes from, for the reader of the file.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the version the workload's <c>emcc --version</c> reports.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the version in the names of the workload's packs (<c>Microsoft.NET.Runtime.Emscripten.&lt;version&gt;.Sdk.*</c>).</summary>
    public required string PackVersion { get; init; }

    /// <summary>Gets the workload set that CI installs (<c>dotnet workload install wasm-tools --version</c>).</summary>
    public required string WorkloadVersion { get; init; }

    /// <summary>Gets where the versions were verified.</summary>
    public required string Source { get; init; }
}
