namespace Jade.BindingGenerator.Configuration;

/// <summary>The groups of <c>build/versions.json</c> that the generator reads.</summary>
/// <remarks>The <c>toolchains</c> group concerns the native build only and is not modelled.</remarks>
internal sealed record PinnedVersions
{
    /// <summary>Gets the native dependencies, by their key in the file.</summary>
    public required IReadOnlyDictionary<string, PinnedDependency> Dependencies { get; init; }

    /// <summary>Gets the minimum OS versions, which set the target triples of the C header parses.</summary>
    public required MinimumOsVersions MinimumOs { get; init; }

    /// <summary>Reads <c>build/versions.json</c>.</summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The pinned versions.</returns>
    /// <exception cref="InvalidDataException">The file does not match the expected format.</exception>
    public static PinnedVersions Load(string path)
    {
        return JsonFile.Read(path, ConfigurationJsonContext.Default.PinnedVersions);
    }

    /// <summary>Gets a pinned dependency by its key.</summary>
    /// <param name="name">The key of the dependency, such as <c>sdl</c>.</param>
    /// <returns>The pinned dependency.</returns>
    /// <exception cref="InvalidDataException">No dependency has this key.</exception>
    public PinnedDependency GetDependency(string name)
    {
        return Dependencies.TryGetValue(name, out var dependency)
            ? dependency
            : throw new InvalidDataException($"build/versions.json has no dependency named '{name}'.");
    }
}
