using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>A member of a <c>dawn.json</c> record: a structure member or a function argument.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record DawnRecordMember
{
    /// <summary>The annotation of a member passed by value.</summary>
    public const string ValueAnnotation = "value";

    /// <summary>Gets the canonical name of the member.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Gets the canonical name of the member's base type.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>Gets how the base type is referenced: by value, <c>*</c>, <c>const*</c> or <c>const*const*</c>.</summary>
    [JsonPropertyName("annotation")]
    public string Annotation { get; init; } = ValueAnnotation;

    /// <summary>
    /// Gets the length of the array a pointer member points to: the name of the member that holds
    /// it, or a literal length. <see langword="null"/> means a single element.
    /// </summary>
    [JsonPropertyName("length")]
    [JsonConverter(typeof(DawnLiteralConverter))]
    public string? Length { get; init; }

    /// <summary>Gets whether the member may be null or omitted.</summary>
    [JsonPropertyName("optional")]
    public bool Optional { get; init; }

    /// <summary>Gets the default value: a number, or the name of a constant or of an enum or bitmask value.</summary>
    [JsonPropertyName("default")]
    [JsonConverter(typeof(DawnLiteralConverter))]
    public string? Default { get; init; }

    /// <summary>Gets whether an optional member has no default, unlike the null default of optional pointers.</summary>
    [JsonPropertyName("no_default")]
    public bool NoDefault { get; init; }

    /// <summary>Gets whether the elements of an array member may be null.</summary>
    [JsonPropertyName("array_element_optional")]
    public bool ArrayElementOptional { get; init; }

    /// <summary>Gets the comment Dawn attached to the member.</summary>
    [JsonPropertyName("_comment")]
    public string? Comment { get; init; }
}
