/// <summary>What one build produced: &lt;targetdir&gt;/jade_native.manifest.json, written by the jade.manifest rule (native/rules/bundle.lua).</summary>
internal sealed class Manifest
{
    /// <summary>Gets the absolute path of the built library.</summary>
    public required string Library { get; init; }

    /// <summary>Gets the absolute path of the library's separate debug symbols (a file, or a dSYM bundle on Apple), if the build has them.</summary>
    public string? Symbols { get; init; }

    /// <summary>Gets the public headers of jade_native itself.</summary>
    public required IReadOnlyList<ManifestHeader> Headers { get; init; }

    /// <summary>Gets the bundled upstream packages.</summary>
    public required IReadOnlyList<ManifestPackage> Packages { get; init; }

    /// <summary>Gets the toolchain that built the library and the packages, as display strings by role (<c>cc</c>, <c>cmake</c>, <c>libc</c>, ...).</summary>
    public required IReadOnlyDictionary<string, string> Toolchain { get; init; }
}
