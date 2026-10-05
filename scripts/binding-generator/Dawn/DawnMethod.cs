using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>A method of a <c>dawn.json</c> object.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record DawnMethod
{
    /// <summary>Gets the canonical name of the method.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Gets the arguments that follow the object itself.</summary>
    [JsonPropertyName("args")]
    public IReadOnlyList<DawnRecordMember>? Args { get; init; }

    /// <summary>Gets the return type, or <see langword="null"/> for <c>void</c>.</summary>
    [JsonPropertyName("returns")]
    [JsonConverter(typeof(DawnReturnConverter))]
    public DawnReturn? Returns { get; init; }

    /// <summary>Gets the tags that select the header variants the method is in, or <see langword="null"/> for every variant.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Gets whether the method takes a chain of its own.</summary>
    [JsonPropertyName("extensible")]
    [JsonConverter(typeof(DawnDirectionConverter))]
    public DawnDirection Extensible { get; init; }

    /// <summary>Gets whether Dawn skips its automatic locking for this method.</summary>
    [JsonPropertyName("no autolock")]
    public bool NoAutolock { get; init; }

    /// <summary>Gets the comment Dawn attached to the method.</summary>
    [JsonPropertyName("_comment")]
    public string? Comment { get; init; }
}
