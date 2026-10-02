using System.Text.Json.Serialization;

/// <summary>A bundled upstream package, in a <see cref="Manifest"/>.</summary>
internal sealed class ManifestPackage
{
    /// <summary>Gets the xmake package name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the package's install directory, which holds its stage fragment in <c>jade/</c>.</summary>
    [JsonPropertyName("installdir")]
    public required string InstallDirectory { get; init; }
}
