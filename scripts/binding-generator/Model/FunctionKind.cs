namespace Jade.BindingGenerator.Model;

/// <summary>What a function is for, which the idiomatic layer uses to place it.</summary>
internal enum FunctionKind
{
    /// <summary>A function that belongs to no type.</summary>
    Free,

    /// <summary>A function whose first parameter is the handle it operates on.</summary>
    Method,

    /// <summary>The function that adds a reference to a handle.</summary>
    AddRef,

    /// <summary>The function that releases a reference to a handle.</summary>
    Release,

    /// <summary>The function that frees what the library allocated for an output structure's members.</summary>
    FreeMembers,
}
