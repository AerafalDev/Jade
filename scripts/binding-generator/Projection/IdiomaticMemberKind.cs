namespace Jade.BindingGenerator.Projection;

/// <summary>How a structure member of a mirror or a snapshot is exposed and converted.</summary>
internal enum IdiomaticMemberKind
{
    /// <summary>A number, an enum, a handle or a value structure, copied as is.</summary>
    Value,

    /// <summary>A <c>WGPUBool</c>, exposed as <see langword="bool"/>.</summary>
    Boolean,

    /// <summary>An untyped pointer, exposed as <see langword="nint"/>.</summary>
    Pointer,

    /// <summary>A function pointer, exposed with its unmanaged type.</summary>
    FunctionPointer,

    /// <summary>A string view, exposed as <c>Utf8Text</c> in a mirror and as <see langword="string"/> elsewhere.</summary>
    Text,

    /// <summary>A pointer and a count, or a fixed length, of blittable elements.</summary>
    Span,

    /// <summary>A pointer and a count of element mirrors, or of snapshots.</summary>
    StructureSpan,

    /// <summary>A pointer and a count of C strings, exposed as strings.</summary>
    StringSpan,

    /// <summary>A pointer to one value structure, exposed as a nullable value when optional.</summary>
    ValuePointer,

    /// <summary>A nested mirror or snapshot held by value.</summary>
    Nested,

    /// <summary>A pointer to one nested mirror, absent when the mirror is <see langword="default"/> and optional.</summary>
    NestedPointer,

    /// <summary>The count of a span member, set from the span.</summary>
    Count,

    /// <summary>The <c>nextInChain</c> pointer of a chain root, which the caller links.</summary>
    NextInChain,

    /// <summary>The chain header of an extension, which its lowering sets.</summary>
    ChainHeader,

    /// <summary>A member that a hand-written partial method lowers.</summary>
    HandWritten,
}
