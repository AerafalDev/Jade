namespace Jade.NativeFetch;

/// <summary>The artifacts of the native workflow that hold a runtime identifier's libraries.</summary>
/// <param name="RuntimeIdentifier">The runtime identifier.</param>
/// <param name="Name">The part of the artifact names after <c>native-</c> or <c>native-test-</c>.</param>
/// <remarks>
/// macOS ships universal libraries, under <c>osx</c>, and iOS one xcframework per library for the
/// device and the simulators, under <c>ios</c> (<c>.github/workflows/native.yml</c>).
/// </remarks>
internal sealed record NativeArtifact(string RuntimeIdentifier, string Name)
{
    /// <summary>Gets the name of the artifact of the shipped libraries.</summary>
    public string LibrariesArtifact => "native-" + Name;

    /// <summary>Gets the name of the artifact of the layout libraries, which only the tests load.</summary>
    public string TestArtifact => "native-test-" + Name;

    /// <summary>Gets the artifacts of a runtime identifier that the workflow builds.</summary>
    /// <param name="runtimeIdentifier">The runtime identifier, one of <c>NativeTarget.All</c>.</param>
    /// <returns>Its artifacts.</returns>
    public static NativeArtifact For(string runtimeIdentifier)
    {
        return runtimeIdentifier.StartsWith("osx-", StringComparison.Ordinal) ? new(runtimeIdentifier, "osx")
            : runtimeIdentifier.StartsWith("ios", StringComparison.Ordinal) ? new(runtimeIdentifier, "ios")
            : new(runtimeIdentifier, runtimeIdentifier);
    }
}
