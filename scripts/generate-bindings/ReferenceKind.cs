/// <summary>What a type referenced by a bound declaration turns into.</summary>
internal enum ReferenceKind
{
    /// <summary>A struct or union exposed by value.</summary>
    Struct,

    /// <summary>A C enum.</summary>
    Enum,

    /// <summary>An integer typedef whose values are flag macros.</summary>
    FlagMacros,

    /// <summary>A handle struct.</summary>
    Handle,

    /// <summary>An opaque struct used through pointers.</summary>
    Opaque,
}
