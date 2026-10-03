/// <summary>What a type referenced by a bound declaration turns into.</summary>
internal enum ReferenceKind
{
    /// <summary>A struct or union exposed by value.</summary>
    Struct,

    /// <summary>A C enum.</summary>
    Enum,

    /// <summary>An integer typedef whose values are macros (<see cref="LibraryConfig.MacroEnums"/>).</summary>
    MacroEnum,

    /// <summary>An integer typedef identifying objects (<see cref="LibraryConfig.IdTypedefs"/>): an enum without members.</summary>
    IdEnum,

    /// <summary>A handle struct.</summary>
    Handle,

    /// <summary>An opaque struct used through pointers.</summary>
    Opaque,
}
