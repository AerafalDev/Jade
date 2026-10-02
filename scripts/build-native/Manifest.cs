/// <summary>What one build produced: &lt;targetdir&gt;/jade_native.manifest.json, written by the jade.manifest rule (native/rules/bundle.lua).</summary>
internal sealed class Manifest
{
    /// <summary>Gets the absolute path of the built library.</summary>
    public required string Library { get; init; }

    /// <summary>Gets the public headers of jade_native itself.</summary>
    public required IReadOnlyList<ManifestHeader> Headers { get; init; }

    /// <summary>Gets the bundled upstream packages.</summary>
    public required IReadOnlyList<ManifestPackage> Packages { get; init; }
}
