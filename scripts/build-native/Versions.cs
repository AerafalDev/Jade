/// <summary>metadata/versions.json: what a staged jade_native was built from.</summary>
internal sealed class Versions
{
    /// <summary>Gets the RID that was built.</summary>
    public required string Rid { get; init; }

    /// <summary>Gets the configuration that was built (<c>release</c> or <c>debug</c>).</summary>
    public required string Config { get; init; }

    /// <summary>Gets the toolchain that built it, sorted by role.</summary>
    public required IReadOnlyDictionary<string, string> Toolchain { get; init; }

    /// <summary>Gets the bundled upstreams, sorted by name.</summary>
    public required IReadOnlyList<Upstream> Upstreams { get; init; }
}
