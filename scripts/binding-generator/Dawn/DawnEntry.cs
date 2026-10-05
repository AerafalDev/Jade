using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>A top-level entry of <c>dawn.json</c>; its <see cref="Category"/> says which members are set.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record DawnEntry
{
    /// <summary>Gets the category of the entry.</summary>
    [JsonPropertyName("category")]
    public required DawnCategory Category { get; init; }

    /// <summary>Gets the tags that select the header variants the entry is in, or <see langword="null"/> for every variant.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Gets the members of a structure or a callback info.</summary>
    [JsonPropertyName("members")]
    public IReadOnlyList<DawnRecordMember>? Members { get; init; }

    /// <summary>Gets the arguments of a function, a function pointer or a callback function.</summary>
    [JsonPropertyName("args")]
    public IReadOnlyList<DawnRecordMember>? Args { get; init; }

    /// <summary>Gets the return type of a function or a function pointer, or <see langword="null"/> for <c>void</c>.</summary>
    [JsonPropertyName("returns")]
    public string? Returns { get; init; }

    /// <summary>Gets the methods of an object; reference counting methods are implicit.</summary>
    [JsonPropertyName("methods")]
    public IReadOnlyList<DawnMethod>? Methods { get; init; }

    /// <summary>Gets the values of an enum or a bitmask.</summary>
    [JsonPropertyName("values")]
    public IReadOnlyList<DawnEnumValue>? Values { get; init; }

    /// <summary>Gets whether a structure has a <c>nextInChain</c> member, and the direction of its chain.</summary>
    [JsonPropertyName("extensible")]
    [JsonConverter(typeof(DawnDirectionConverter))]
    public DawnDirection Extensible { get; init; }

    /// <summary>Gets whether a structure extends others through their chain, and in which direction.</summary>
    [JsonPropertyName("chained")]
    [JsonConverter(typeof(DawnDirectionConverter))]
    public DawnDirection Chained { get; init; }

    /// <summary>Gets the structures that a chained structure can extend.</summary>
    [JsonPropertyName("chain roots")]
    public IReadOnlyList<string>? ChainRoots { get; init; }

    /// <summary>Gets whether a structure is filled in by the API rather than by the caller.</summary>
    [JsonPropertyName("out")]
    public bool Out { get; init; }

    /// <summary>Gets the type of a constant, or the target of a typedef.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Gets the C value of a constant, such as <c>UINT32_MAX</c>.</summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>Gets the C++ value of a constant, when it differs from the C one.</summary>
    [JsonPropertyName("cpp_value")]
    public string? CppValue { get; init; }

    /// <summary>Gets whether a native pointer type may be null.</summary>
    [JsonPropertyName("is nullable pointer")]
    public bool IsNullablePointer { get; init; }

    /// <summary>Gets whether Dawn skips its automatic locking for an object's methods.</summary>
    [JsonPropertyName("no autolock")]
    public bool NoAutolock { get; init; }

    /// <summary>Gets the WebAssembly signature character of a native type.</summary>
    [JsonPropertyName("wasm type")]
    public string? WasmType { get; init; }

    /// <summary>Gets whether Dawn's wire serializes a native type as is.</summary>
    [JsonPropertyName("wire transparent")]
    public bool? WireTransparent { get; init; }

    /// <summary>Gets whether Emdawnwebgpu omits the JavaScript table of an enum.</summary>
    [JsonPropertyName("emscripten_no_enum_table")]
    public bool EmscriptenNoEnumTable { get; init; }

    /// <summary>Gets whether Emdawnwebgpu converts an enum from a JavaScript string to an integer.</summary>
    [JsonPropertyName("emscripten_string_to_int")]
    public bool EmscriptenStringToInt { get; init; }

    /// <summary>Gets the comment Dawn attached to the entry.</summary>
    [JsonPropertyName("_comment")]
    public string? Comment { get; init; }
}
