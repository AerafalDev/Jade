namespace Jade.NativeBuild.Configuration;

/// <summary>The build tools of <c>build/versions.json</c> that the host build runs.</summary>
/// <remarks>
/// Unknown members are accepted: the Emscripten and Android NDK entries concern the builds for the
/// browser and Android (roadmap task 10).
/// </remarks>
internal sealed record PinnedToolchains
{
    /// <summary>Gets the pinned xmake, which runs the build definitions of <c>build/</c>.</summary>
    public required PinnedTool Xmake { get; init; }

    /// <summary>Gets the pinned CMake, which builds Dawn and SDL3.</summary>
    public required PinnedTool Cmake { get; init; }
}
