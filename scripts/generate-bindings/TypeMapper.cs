using ClangSharp;
using ClangSharp.Interop;
using Type = ClangSharp.Type;

/// <summary>
/// Maps libclang types to <see cref="TypeRef"/>s and records which library declarations they use. The mapping
/// follows typedef sugar so that fixed-width typedefs keep their width on every target (<c>uint64_t</c> is
/// <c>unsigned long</c> on Linux but <c>unsigned long long</c> on Windows, and both map to <c>ulong</c>).
/// </summary>
internal sealed class TypeMapper
{
    private static readonly Dictionary<string, PrimitiveType> s_fixedWidthTypedefs = new(StringComparer.Ordinal)
    {
        ["int8_t"] = PrimitiveType.SByte,
        ["uint8_t"] = PrimitiveType.Byte,
        ["int16_t"] = PrimitiveType.Int16,
        ["uint16_t"] = PrimitiveType.UInt16,
        ["int32_t"] = PrimitiveType.Int32,
        ["uint32_t"] = PrimitiveType.UInt32,
        ["int64_t"] = PrimitiveType.Int64,
        ["uint64_t"] = PrimitiveType.UInt64,
        ["intptr_t"] = PrimitiveType.NInt,
        ["uintptr_t"] = PrimitiveType.NUInt,
        ["ptrdiff_t"] = PrimitiveType.NInt,
        ["size_t"] = PrimitiveType.NUInt,
    };

    private readonly LibraryConfig _config;
    private readonly Func<Decl, bool> _isLibraryDeclaration;
    private readonly int _pointerSize;
    private readonly int _longSize;
    private readonly List<TypeReference> _references = [];
    private readonly HashSet<string> _referenced = new(StringComparer.Ordinal);

    /// <summary>Creates a mapper for one target.</summary>
    /// <param name="config">The library config: handles, opaque structs, flag typedefs, bool type.</param>
    /// <param name="isLibraryDeclaration">Whether a declaration lives in the library's own headers.</param>
    /// <param name="pointerSize">The target's <c>sizeof(void*)</c>.</param>
    /// <param name="longSize">The target's <c>sizeof(long)</c>.</param>
    public TypeMapper(LibraryConfig config, Func<Decl, bool> isLibraryDeclaration, int pointerSize, int longSize)
    {
        _config = config;
        _isLibraryDeclaration = isLibraryDeclaration;
        _pointerSize = pointerSize;
        _longSize = longSize;
    }

    /// <summary>Gets the declarations used so far, in first-use order. Mapping can append while a caller iterates by index.</summary>
    public IReadOnlyList<TypeReference> References => _references;

    /// <summary>Returns the name a record or enum is known by: its tag, or the typedef naming an anonymous one.</summary>
    /// <param name="declaration">The record or enum.</param>
    /// <returns>The C name, or an empty string.</returns>
    public static string NameOf(TagDecl declaration) =>
        declaration.Name.Length > 0 ? declaration.Name : declaration.TypedefNameForAnonDecl?.Name ?? string.Empty;

    /// <summary>Maps a type used by value: a parameter, a return value or a field.</summary>
    /// <param name="type">The libclang type.</param>
    /// <returns>The model type.</returns>
    /// <exception cref="MappingException">The type has no portable managed form, or the config does not cover it.</exception>
    public TypeRef Map(Type type)
    {
        var current = type;
        while (true)
        {
            switch (current)
            {
                case ElaboratedType elaborated:
                    current = elaborated.NamedType;
                    continue;

                case AttributedType attributed:
                    current = attributed.ModifiedType;
                    continue;

                case ParenType paren:
                    current = paren.InnerType;
                    continue;

                case DecayedType decayed:
                    current = decayed.GetDecayedType;
                    continue;

                case TypedefType typedef:
                    var name = typedef.Decl.Name;
                    if (s_fixedWidthTypedefs.TryGetValue(name, out var fixedWidth))
                    {
                        return TypeRef.Of(fixedWidth);
                    }

                    if (name is "va_list" or "__builtin_va_list" or "__gnuc_va_list")
                    {
                        throw new MappingException("va_list differs on every ABI and cannot be built from C#");
                    }

                    if (_config.FlagMacros.ContainsKey(name))
                    {
                        Reference(name, ReferenceKind.FlagMacros);
                        return TypeRef.Named(name);
                    }

                    current = typedef.Decl.UnderlyingType;
                    continue;

                case PointerType pointer:
                    return MapPointer(pointer.PointeeType);

                case BuiltinType builtin:
                    return MapBuiltin(builtin);

                case RecordType record:
                    return MapRecord(record.Decl);

                case EnumType enumType:
                    var enumName = NameOf(enumType.Decl);
                    RequireLibrary(enumType.Decl, enumName);
                    Reference(enumName, ReferenceKind.Enum);
                    return TypeRef.Named(enumName);

                case ConstantArrayType array:
                    return TypeRef.ArrayOf(Map(array.ElementType), checked((int)array.Size));

                case IncompleteArrayType:
                    throw new MappingException("flexible array members have no fixed layout");

                case FunctionType:
                    throw new MappingException("a function type is only bindable behind a pointer");

                default:
                    if (current.IsSugared)
                    {
                        current = current.Desugar;
                        continue;
                    }

                    throw new MappingException($"unsupported type `{current.AsString}` ({current.TypeClassSpelling})");
            }
        }
    }

    private static Type Peel(Type type)
    {
        var current = type;
        while (true)
        {
            switch (current)
            {
                case ElaboratedType elaborated:
                    current = elaborated.NamedType;
                    break;

                case AttributedType attributed:
                    current = attributed.ModifiedType;
                    break;

                case ParenType paren:
                    current = paren.InnerType;
                    break;

                case TypedefType typedef when !s_fixedWidthTypedefs.ContainsKey(typedef.Decl.Name):
                    current = typedef.Decl.UnderlyingType;
                    break;

                default:
                    return current;
            }
        }
    }

    private TypeRef MapPointer(Type pointee)
    {
        var isConst = pointee.IsLocalConstQualified;
        var bare = Peel(pointee);
        if (bare is FunctionProtoType function)
        {
            return MapFunction(function);
        }

        if (bare is FunctionNoProtoType)
        {
            throw new MappingException("a function pointer without a prototype cannot be typed");
        }

        if (bare is RecordType record)
        {
            var name = NameOf(record.Decl);
            if (_config.Handles.Contains(name))
            {
                RequireLibrary(record.Decl, name);
                Reference(name, ReferenceKind.Handle);
                return TypeRef.Named(name);
            }

            if (IsOpaque(record.Decl, name))
            {
                RequireLibrary(record.Decl, name);
                Reference(name, ReferenceKind.Opaque);
                return TypeRef.PointerTo(TypeRef.Named(name), isConst);
            }
        }

        return TypeRef.PointerTo(Map(pointee), isConst);
    }

    private TypeRef MapFunction(FunctionProtoType function)
    {
        if (function.CallConv != CXCallingConv.CXCallingConv_C)
        {
            throw new MappingException($"callback calling convention {function.CallConv} is not cdecl");
        }

        if (function.IsVariadic)
        {
            throw new MappingException("variadic callbacks cannot be implemented in C#");
        }

        var returnType = Map(function.ReturnType);
        var parameters = function.ParamTypes.Select(Map).ToList();
        return TypeRef.FunctionPointer(returnType, parameters);
    }

    private TypeRef MapRecord(RecordDecl declaration)
    {
        var name = NameOf(declaration);
        if (_config.Handles.Contains(name))
        {
            throw new MappingException($"{name} is a handle, only valid behind a pointer");
        }

        if (IsOpaque(declaration, name))
        {
            throw new MappingException($"{name} is opaque, only valid behind a pointer");
        }

        RequireLibrary(declaration, name);
        Reference(name, ReferenceKind.Struct);
        return TypeRef.Named(name);
    }

    private TypeRef MapBuiltin(BuiltinType builtin)
    {
        var primitive = builtin.Kind switch
        {
            CXTypeKind.CXType_Void => (PrimitiveType?)null,
            CXTypeKind.CXType_Bool => _config.Bool,
            CXTypeKind.CXType_Char_S or CXTypeKind.CXType_Char_U => PrimitiveType.Char,
            CXTypeKind.CXType_SChar => PrimitiveType.SByte,
            CXTypeKind.CXType_UChar => PrimitiveType.Byte,
            CXTypeKind.CXType_Short => PrimitiveType.Int16,
            CXTypeKind.CXType_UShort => PrimitiveType.UInt16,
            CXTypeKind.CXType_Int => PrimitiveType.Int32,
            CXTypeKind.CXType_UInt => PrimitiveType.UInt32,
            CXTypeKind.CXType_Long => PrimitiveType.CLong,
            CXTypeKind.CXType_ULong => PrimitiveType.CULong,
            CXTypeKind.CXType_LongLong => PrimitiveType.Int64,
            CXTypeKind.CXType_ULongLong => PrimitiveType.UInt64,
            CXTypeKind.CXType_Float => PrimitiveType.Single,
            CXTypeKind.CXType_Double => PrimitiveType.Double,
            CXTypeKind.CXType_WChar => throw new MappingException("wchar_t is 2 bytes on Windows and 4 elsewhere"),
            _ => throw new MappingException($"unsupported builtin type `{builtin.AsString}`"),
        };

        if (primitive is not { } value)
        {
            return TypeRef.Void;
        }

        // The managed type must have the C type's size on this target; this catches e.g. a 4-byte C bool.
        var nativeSize = builtin.Handle.SizeOf;
        var managedSize = LayoutCalculator.SizeOf(value, _pointerSize, _longSize);
        if (nativeSize != managedSize)
        {
            throw new MappingException($"`{builtin.AsString}` is {nativeSize} bytes but maps to {value} ({managedSize} bytes)");
        }

        return TypeRef.Of(value);
    }

    private bool IsOpaque(RecordDecl declaration, string name)
    {
        if (declaration.Definition is null)
        {
            if (!_config.OpaqueStructs.Contains(name))
            {
                throw new MappingException($"{name} has no definition: list it in Handles or OpaqueStructs");
            }

            return true;
        }

        return _config.LayoutDecisions.TryGetValue(name, out var decision) && decision.Kind == LayoutDecisionKind.Opaque;
    }

    private void RequireLibrary(Decl declaration, string name)
    {
        if (!_isLibraryDeclaration(declaration))
        {
            throw new MappingException($"{name} is not declared in the library's headers");
        }
    }

    private void Reference(string name, ReferenceKind kind)
    {
        if (_referenced.Add(name))
        {
            _references.Add(new TypeReference(name, kind));
        }
    }
}
