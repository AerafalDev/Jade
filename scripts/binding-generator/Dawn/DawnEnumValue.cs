using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>A value of a <c>dawn.json</c> enum or bitmask.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record DawnEnumValue
{
    /// <summary>Gets the canonical name of the value.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the value as written. Dawn adds a fixed offset for some tags (<c>0x0005_0000</c> for
    /// <c>dawn</c>) when it generates <c>webgpu.h</c>.
    /// </summary>
    [JsonPropertyName("value")]
    public required ulong Value { get; init; }

    /// <summary>Gets the tags that select the header variants the value is in, or <see langword="null"/> for every variant.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Gets the JavaScript representation of the value in Emdawnwebgpu, when it differs from the name.</summary>
    [JsonPropertyName("jsrepr")]
    public string? JsRepresentation { get; init; }

    /// <summary>Gets whether Dawn's validation accepts the value.</summary>
    [JsonPropertyName("valid")]
    public bool Valid { get; init; } = true;

    /// <summary>Gets whether Emdawnwebgpu converts the value from a JavaScript string to an integer.</summary>
    [JsonPropertyName("emscripten_string_to_int")]
    public bool EmscriptenStringToInt { get; init; }

    /// <summary>Gets whether the value shares its number with another value of the enum.</summary>
    [JsonPropertyName("enum_value_conflict")]
    public bool EnumValueConflict { get; init; }

    /// <summary>Gets whether the value stands for another one.</summary>
    [JsonPropertyName("is_proxy")]
    public bool IsProxy { get; init; }

    /// <summary>Gets the comment Dawn attached to the value.</summary>
    [JsonPropertyName("_comment")]
    public string? Comment { get; init; }
}
