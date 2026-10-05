using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>The <c>_metadata</c> object of <c>dawn.json</c>, which Dawn's templates read.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record DawnMetadata
{
    /// <summary>Gets the name of the web API, <c>WebGPU</c>.</summary>
    [JsonPropertyName("api")]
    public required string Api { get; init; }

    /// <summary>Gets the prefix of the C types and functions, <c>WGPU</c>.</summary>
    [JsonPropertyName("c_prefix")]
    public required string CPrefix { get; init; }

    /// <summary>Gets the namespace of Dawn's C++ wrapper.</summary>
    [JsonPropertyName("namespace")]
    public required string Namespace { get; init; }

    /// <summary>Gets the prefix of Dawn's proc table.</summary>
    [JsonPropertyName("proc_table_prefix")]
    public required string ProcTablePrefix { get; init; }

    /// <summary>Gets the directory of Dawn's implementation.</summary>
    [JsonPropertyName("impl_dir")]
    public required string ImplementationDirectory { get; init; }

    /// <summary>Gets the namespace of Dawn's native implementation.</summary>
    [JsonPropertyName("native_namespace")]
    public required string NativeNamespace { get; init; }

    /// <summary>Gets the copyright year of the generated files.</summary>
    [JsonPropertyName("copyright_year")]
    public required string CopyrightYear { get; init; }
}
