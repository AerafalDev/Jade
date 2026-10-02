/// <summary>How a type whose layout differs between targets is exposed (ADR-0006).</summary>
internal enum LayoutDecisionKind
{
    /// <summary>Never exposed by value: the type becomes an opaque struct used through pointers.</summary>
    Opaque,

    /// <summary>Fields are reached through native accessor shims. Not implemented yet.</summary>
    AccessorShim,

    /// <summary>One definition per platform. Not implemented yet.</summary>
    PerPlatform,
}
