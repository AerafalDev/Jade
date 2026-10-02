/// <summary>
/// A type in the intermediate model. It is target-independent: the same C declaration must map to the same
/// <see cref="TypeRef"/> on every target triple, and sizes are only computed per target (<see cref="LayoutCalculator"/>).
/// </summary>
internal sealed class TypeRef
{
    private TypeRef(TypeKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the <c>void</c> type.</summary>
    public static TypeRef Void { get; } = new(TypeKind.Void);

    /// <summary>Gets the shape of the type.</summary>
    public TypeKind Kind { get; }

    /// <summary>Gets the scalar type, for <see cref="TypeKind.Primitive"/>.</summary>
    public PrimitiveType Primitive { get; private init; }

    /// <summary>Gets the pointee or array element, for <see cref="TypeKind.Pointer"/> and <see cref="TypeKind.FixedArray"/>.</summary>
    public TypeRef? Element { get; private init; }

    /// <summary>Gets whether the pointee is <c>const</c>, for <see cref="TypeKind.Pointer"/>.</summary>
    public bool IsConst { get; private init; }

    /// <summary>Gets the native name of the declaration, for <see cref="TypeKind.Named"/>.</summary>
    public string? Name { get; private init; }

    /// <summary>Gets the element count, for <see cref="TypeKind.FixedArray"/>.</summary>
    public int Length { get; private init; }

    /// <summary>Gets the return type, for <see cref="TypeKind.FunctionPointer"/>.</summary>
    public TypeRef? Return { get; private init; }

    /// <summary>Gets the parameter types, for <see cref="TypeKind.FunctionPointer"/>.</summary>
    public IReadOnlyList<TypeRef> Parameters { get; private init; } = [];

    /// <summary>Gets whether this is a data pointer.</summary>
    public bool IsPointer => Kind == TypeKind.Pointer;

    /// <summary>Creates a scalar type.</summary>
    /// <param name="primitive">The scalar.</param>
    /// <returns>The type.</returns>
    public static TypeRef Of(PrimitiveType primitive) => new(TypeKind.Primitive) { Primitive = primitive };

    /// <summary>Creates a data pointer.</summary>
    /// <param name="element">The pointee.</param>
    /// <param name="isConst">Whether the pointee is <c>const</c>.</param>
    /// <returns>The type.</returns>
    public static TypeRef PointerTo(TypeRef element, bool isConst) => new(TypeKind.Pointer) { Element = element, IsConst = isConst };

    /// <summary>Creates a reference to a declaration of the library.</summary>
    /// <param name="nativeName">The C name of the enum, struct or handle.</param>
    /// <returns>The type.</returns>
    public static TypeRef Named(string nativeName) => new(TypeKind.Named) { Name = nativeName };

    /// <summary>Creates a fixed-size array.</summary>
    /// <param name="element">The element type.</param>
    /// <param name="length">The element count.</param>
    /// <returns>The type.</returns>
    public static TypeRef ArrayOf(TypeRef element, int length) => new(TypeKind.FixedArray) { Element = element, Length = length };

    /// <summary>Creates a C function pointer type.</summary>
    /// <param name="returnType">The return type.</param>
    /// <param name="parameters">The parameter types.</param>
    /// <returns>The type.</returns>
    public static TypeRef FunctionPointer(TypeRef returnType, IReadOnlyList<TypeRef> parameters) =>
        new(TypeKind.FunctionPointer) { Return = returnType, Parameters = parameters };

    /// <summary>Gets whether this is a scalar of the given type.</summary>
    /// <param name="primitive">The scalar to test for.</param>
    /// <returns><see langword="true"/> for a <see cref="TypeKind.Primitive"/> of <paramref name="primitive"/>.</returns>
    public bool Is(PrimitiveType primitive) => Kind == TypeKind.Primitive && Primitive == primitive;
}
