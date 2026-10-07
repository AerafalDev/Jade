namespace Jade.BindingGenerator.Projection;

/// <summary>How a parameter of a C function is exposed by an idiomatic method (ADR 0040).</summary>
internal enum IdiomaticParameterKind
{
    /// <summary>A number, an enum or a handle, passed as is.</summary>
    Value,

    /// <summary>A <c>WGPUBool</c>, taken as <see langword="bool"/>.</summary>
    Boolean,

    /// <summary>An untyped pointer, taken as <see langword="nint"/>.</summary>
    Pointer,

    /// <summary>A string view, taken as a UTF-8 span and, in another overload, as a string.</summary>
    Text,

    /// <summary>A pointer to one value structure, taken by <see langword="in"/> reference.</summary>
    InValue,

    /// <summary>A pointer to one input structure with pointers, taken as its mirror by <see langword="in"/> reference.</summary>
    Descriptor,

    /// <summary>A constant pointer and a count of blittable elements, taken as a read-only span.</summary>
    Span,

    /// <summary>A pointer and a count of blittable elements that the function writes, taken as a span.</summary>
    MutableSpan,

    /// <summary>A constant untyped pointer and a size in bytes, taken as a read-only span of any unmanaged type.</summary>
    Data,

    /// <summary>An untyped pointer and a size in bytes that the function writes, taken as a span of any unmanaged type.</summary>
    MutableData,

    /// <summary>The count or size of a span parameter, computed from the span.</summary>
    Count,

    /// <summary>A pointer to a structure the function fills in, returned by the method.</summary>
    Output,

    /// <summary>The callback info of an asynchronous function, filled in by the method, which returns a task.</summary>
    Callback,
}
