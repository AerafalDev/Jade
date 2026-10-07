namespace Jade.BindingGenerator.Projection;

/// <summary>What a structure of the model becomes in the idiomatic layer (ADR 0029, ADR 0040).</summary>
internal enum IdiomaticRole
{
    /// <summary>A structure that nothing exposed by the idiomatic layer uses.</summary>
    Unused,

    /// <summary>A value structure, public in the raw layer and used as is by both layers.</summary>
    Value,

    /// <summary>An input structure with pointers, exposed as a public <c>ref struct</c> mirror.</summary>
    Mirror,

    /// <summary>An input structure used as an array element, exposed as a public struct mirror with <c>ReadOnlyMemory&lt;T&gt;</c> and <c>string</c> members.</summary>
    ElementMirror,

    /// <summary>An output structure with pointers, exposed as an immutable managed copy.</summary>
    Snapshot,

    /// <summary><c>WGPUStringView</c>, exposed as text.</summary>
    StringView,

    /// <summary><c>WGPUChainedStruct</c>, which the idiomatic layer links itself.</summary>
    ChainHeader,

    /// <summary>A callback info, which an asynchronous method or a hand-written member fills in.</summary>
    CallbackInfo,

    /// <summary>A structure the configuration leaves out of the idiomatic layer.</summary>
    Skipped,
}
