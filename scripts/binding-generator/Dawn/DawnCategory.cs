using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>The category of a <c>dawn.json</c> entry, which selects the members it may have.</summary>
/// <remarks>An unknown category fails the generator, like any other schema change.</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<DawnCategory>))]
internal enum DawnCategory
{
    /// <summary>A flags type, <c>uint64_t</c>-based in <c>webgpu.h</c>.</summary>
    [JsonStringEnumMemberName("bitmask")]
    Bitmask,

    /// <summary>The signature of a callback that has a callback info.</summary>
    [JsonStringEnumMemberName("callback function")]
    CallbackFunction,

    /// <summary>The structure that carries a callback, its mode and its userdata.</summary>
    [JsonStringEnumMemberName("callback info")]
    CallbackInfo,

    /// <summary>A constant defined with a preprocessor macro.</summary>
    [JsonStringEnumMemberName("constant")]
    Constant,

    /// <summary>A <c>uint32_t</c>-based enum.</summary>
    [JsonStringEnumMemberName("enum")]
    Enum,

    /// <summary>A free function.</summary>
    [JsonStringEnumMemberName("function")]
    Function,

    /// <summary>A function pointer type.</summary>
    [JsonStringEnumMemberName("function pointer")]
    FunctionPointer,

    /// <summary>A C type referenced by name, such as <c>uint32_t</c> or <c>void *</c>.</summary>
    [JsonStringEnumMemberName("native")]
    Native,

    /// <summary>An object, which the API exposes as a handle with methods.</summary>
    [JsonStringEnumMemberName("object")]
    Object,

    /// <summary>A structure.</summary>
    [JsonStringEnumMemberName("structure")]
    Structure,

    /// <summary>An alias of another entry, used for gradual deprecations.</summary>
    [JsonStringEnumMemberName("typedef")]
    Typedef,
}
