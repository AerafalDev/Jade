namespace Jade.BindingGenerator.Clang;

/// <summary>The kinds of value that a macro bound as a constant or an enum value evaluates to.</summary>
internal enum MacroValueKind
{
    /// <summary>A signed integer.</summary>
    SignedInteger,

    /// <summary>An unsigned integer.</summary>
    UnsignedInteger,

    /// <summary>A floating-point number.</summary>
    Float,

    /// <summary>A string literal.</summary>
    String,
}
