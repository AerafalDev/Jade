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
    /// <summary>The runtime identifiers of ADR 0012, which name their own artifacts unless they share an Apple one.</summary>
    private static readonly string[] _runtimeIdentifiers =
    [
        "linux-x64", "linux-arm64", "win-x64", "win-arm64", "osx-arm64", "osx-x64",
        "ios-arm64", "iossimulator-arm64", "iossimulator-x64", "android-arm64", "android-x64", "browser-wasm",
    ];

    /// <summary>Gets the name of the artifact of the shipped libraries.</summary>
    public string LibrariesArtifact => "native-" + Name;

    /// <summary>Gets the name of the artifact of the layout libraries, which only the tests load.</summary>
    public string TestArtifact => "native-test-" + Name;

    /// <summary>Gets the artifacts of a runtime identifier.</summary>
    /// <param name="runtimeIdentifier">The runtime identifier.</param>
    /// <returns>Its artifacts, or <see langword="null"/> when the workflow does not build it.</returns>
    public static NativeArtifact? Find(string runtimeIdentifier)
    {
        return !_runtimeIdentifiers.Contains(runtimeIdentifier, StringComparer.Ordinal)
            ? null
            : runtimeIdentifier.StartsWith("osx-", StringComparison.Ordinal) ? new(runtimeIdentifier, "osx")
            : runtimeIdentifier.StartsWith("ios", StringComparison.Ordinal) ? new(runtimeIdentifier, "ios")
            : new(runtimeIdentifier, runtimeIdentifier);
    }

    /// <summary>Gets the runtime identifiers the workflow builds.</summary>
    public static IReadOnlyList<string> RuntimeIdentifiers => _runtimeIdentifiers;
}
