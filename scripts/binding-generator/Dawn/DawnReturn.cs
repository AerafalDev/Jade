using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>The return type of a <c>dawn.json</c> method.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record DawnReturn
{
    /// <summary>Gets the canonical name of the returned type.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>Gets whether the method may return null.</summary>
    [JsonPropertyName("optional")]
    public bool Optional { get; init; }
}
