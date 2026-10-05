namespace Jade.BindingGenerator.Clang;

/// <summary>The kinds of top-level C declaration the generator collects from headers.</summary>
internal enum HeaderDeclarationKind
{
    /// <summary>A function.</summary>
    Function,

    /// <summary>A structure or a union, complete or forward-declared.</summary>
    Record,

    /// <summary>An enumeration.</summary>
    Enum,

    /// <summary>A typedef.</summary>
    Typedef,

    /// <summary>A global variable.</summary>
    Variable,
}
