/// <summary>The shape of a <see cref="TypeRef"/>.</summary>
internal enum TypeKind
{
    /// <summary><c>void</c>, only valid as a return type or pointee.</summary>
    Void,

    /// <summary>A <see cref="PrimitiveType"/>.</summary>
    Primitive,

    /// <summary>A data pointer to <see cref="TypeRef.Element"/>.</summary>
    Pointer,

    /// <summary>A declaration of the library (enum, struct or handle), by native name.</summary>
    Named,

    /// <summary>A pointer to a C function, emitted as <c>delegate* unmanaged[Cdecl]</c>.</summary>
    FunctionPointer,

    /// <summary>A fixed-size array of <see cref="TypeRef.Element"/>, only valid as a struct field.</summary>
    FixedArray,
}
