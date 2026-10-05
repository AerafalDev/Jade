using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>The build tools of <c>build/versions.json</c>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PinnedToolchains
{
    /// <summary>Gets the workload's Emscripten, which builds the browser archives.</summary>
    public required PinnedEmscripten Emscripten { get; init; }

    /// <summary>Gets the Android NDK, which builds the Android libraries.</summary>
    public required PinnedAndroidNdk AndroidNdk { get; init; }

    /// <summary>Gets the pinned xmake, which runs the build definitions of <c>build/</c>.</summary>
    public required PinnedTool Xmake { get; init; }

    /// <summary>Gets the pinned CMake, which builds Dawn and SDL3.</summary>
    public required PinnedTool Cmake { get; init; }
}
