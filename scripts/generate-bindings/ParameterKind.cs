/// <summary>How a parameter is exposed beyond the raw pointer signature (ADR-0006 friendly overloads).</summary>
internal enum ParameterKind
{
    /// <summary>Only the raw form.</summary>
    None,

    /// <summary><c>const char*</c> holding a NUL-terminated UTF-8 string: <c>ReadOnlySpan&lt;byte&gt;</c>.</summary>
    Utf8String,

    /// <summary><c>const T*</c> to a single value: <c>in T</c>.</summary>
    In,

    /// <summary><c>T*</c> written by the callee: <c>out T</c>.</summary>
    Out,

    /// <summary><c>T*</c> read and written by the callee: <c>ref T</c>.</summary>
    Ref,

    /// <summary><c>T*</c> to an array whose length is another parameter: <c>Span&lt;T&gt;</c> or <c>ReadOnlySpan&lt;T&gt;</c>.</summary>
    Span,

    /// <summary>The length of a <see cref="Span"/> parameter, filled from the span in the friendly overload.</summary>
    Count,
}
