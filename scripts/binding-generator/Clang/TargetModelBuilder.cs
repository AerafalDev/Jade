using System.Collections.Frozen;
using ClangSharp;
using ClangSharp.Interop;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Targets;
using Type = ClangSharp.Type;

namespace Jade.BindingGenerator.Clang;

/// <summary>Collects what a library's headers declare for one target from a parsed translation unit (ADR 0033).</summary>
/// <remarks>
/// Each target is collected on its own, with its platform as availability; the merge compares the
/// targets afterwards. Opaque declarations are collected as handles without looking inside them,
/// since their members are what differs between targets.
/// </remarks>
internal sealed class TargetModelBuilder
{
    /// <summary>The C runtime types, mapped by name whatever declares them on a target (ADR 0027).</summary>
    /// <remarks>miniaudio declares <c>wchar_t</c> itself for MSVC, for instance.</remarks>
    private static readonly FrozenSet<string> _runtimeTypes = FrozenSet.Create(
        StringComparer.Ordinal,
        "int8_t", "int16_t", "int32_t", "int64_t", "uint8_t", "uint16_t", "uint32_t", "uint64_t",
        "intptr_t", "uintptr_t", "ptrdiff_t", "size_t", "va_list", "wchar_t");

    /// <summary>The C spellings of the builtin types, which the projection maps (ADR 0009 and 0027).</summary>
    private static readonly FrozenDictionary<CXTypeKind, string> _builtinSpellings = new Dictionary<CXTypeKind, string>
    {
        [CXTypeKind.CXType_Void] = "void",
        [CXTypeKind.CXType_Bool] = "_Bool",
        [CXTypeKind.CXType_Char_S] = "char",
        [CXTypeKind.CXType_Char_U] = "char",
        [CXTypeKind.CXType_SChar] = "signed char",
        [CXTypeKind.CXType_UChar] = "unsigned char",
        [CXTypeKind.CXType_Short] = "short",
        [CXTypeKind.CXType_UShort] = "unsigned short",
        [CXTypeKind.CXType_Int] = "int",
        [CXTypeKind.CXType_UInt] = "unsigned int",
        [CXTypeKind.CXType_Long] = "long",
        [CXTypeKind.CXType_ULong] = "unsigned long",
        [CXTypeKind.CXType_LongLong] = "long long",
        [CXTypeKind.CXType_ULongLong] = "unsigned long long",
        [CXTypeKind.CXType_Float] = "float",
        [CXTypeKind.CXType_Double] = "double",
        [CXTypeKind.CXType_LongDouble] = "long double",
    }.ToFrozenDictionary();

    /// <summary>The target the translation unit was parsed for.</summary>
    private readonly Target _target;

    /// <summary>The files that belong to the library.</summary>
    private readonly HeaderFiles _files;

    /// <summary>The front-end settings of the library.</summary>
    private readonly ClangConfiguration _configuration;

    /// <summary>The declarations the configuration leaves out, with the reason.</summary>
    private readonly IReadOnlyDictionary<string, string> _exclude;

    /// <summary>The library's naming rules.</summary>
    private readonly CNames _names;

    /// <summary>The declarations collected so far, by C name.</summary>
    private readonly Dictionary<string, TargetDeclaration> _declarations = [with(StringComparer.Ordinal)];

    /// <summary>The declarations left out, by C name.</summary>
    private readonly Dictionary<string, SkippedDeclaration> _skipped = [with(StringComparer.Ordinal)];

    /// <summary>The structures whose members are being collected, which a member may point back to.</summary>
    private readonly HashSet<string> _inProgress = [with(StringComparer.Ordinal)];

    /// <summary>Initializes a new instance of the <see cref="TargetModelBuilder"/> class.</summary>
    /// <param name="target">The target.</param>
    /// <param name="files">The files that belong to the library.</param>
    /// <param name="configuration">The front-end settings.</param>
    /// <param name="exclude">The declarations to leave out.</param>
    private TargetModelBuilder(Target target, HeaderFiles files, ClangConfiguration configuration, IReadOnlyDictionary<string, string> exclude)
    {
        _target = target;
        _files = files;
        _configuration = configuration;
        _exclude = exclude;
        _names = new CNames(configuration.Prefixes, configuration.MemberPrefixes);
    }

    /// <summary>Collects the declarations and macros of the library's headers.</summary>
    /// <param name="translationUnit">The translation unit, parsed with a detailed preprocessing record.</param>
    /// <param name="target">The target it was parsed for.</param>
    /// <param name="files">The files that belong to the library.</param>
    /// <param name="configuration">The front-end settings.</param>
    /// <param name="exclude">The declarations to leave out, with the reason.</param>
    /// <returns>The declarations, without macro values.</returns>
    /// <exception cref="InvalidDataException">A declaration uses a construct the bindings cannot reproduce.</exception>
    public static TargetDeclarations Collect(TranslationUnit translationUnit, Target target, HeaderFiles files, ClangConfiguration configuration, IReadOnlyDictionary<string, string> exclude)
    {
        var builder = new TargetModelBuilder(target, files, configuration, exclude);

        foreach (var declaration in translationUnit.TranslationUnitDecl.Decls)
        {
            if (builder.GetHeader(declaration) is { } header)
            {
                builder.Add(declaration, header);
            }
        }

        var macros = new List<MacroDefinition>();
        var functionLikeMacroCount = 0;

        foreach (var macro in translationUnit.TranslationUnitDecl.CursorChildren.OfType<MacroDefinitionRecord>())
        {
            if (macro.IsBuiltinMacro || builder.GetHeader(macro) is not { } header)
            {
                continue;
            }

            if (macro.IsFunctionLike)
            {
                functionLikeMacroCount++;
            }
            else
            {
                macros.Add(new MacroDefinition(macro.Name, header));
            }
        }

        return new TargetDeclarations(target, builder._declarations, builder._skipped, macros, functionLikeMacroCount, FrozenDictionary<string, MacroValue>.Empty);
    }

    /// <summary>Gets the library header a cursor comes from.</summary>
    /// <param name="cursor">A declaration or a macro definition.</param>
    /// <returns>The header, or <see langword="null"/> when the cursor is outside the library.</returns>
    private string? GetHeader(Cursor cursor)
    {
        cursor.Location.GetFileLocation(out var file, out _, out _, out _);
        using var fileName = file.Name;

        return _files.GetHeader(fileName.ToString());
    }

    /// <summary>Adds a top-level declaration.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="header">The header that declares it.</param>
    private void Add(Decl declaration, string header)
    {
        switch (declaration)
        {
            case FunctionDecl function:
                AddFunction(function, header);
                break;

            case RecordDecl record:
                AddRecord(record, header);
                break;

            case EnumDecl enumDecl:
                AddEnum(enumDecl, header);
                break;

            case TypedefNameDecl typedef:
                AddTypedef(typedef, header);
                break;

            case VarDecl variable:
                Skip(variable.Name, "a global variable, which LibraryImport cannot bind");
                break;

            default:
                // Static assertions and empty declarations declare nothing to bind.
                break;
        }
    }

    /// <summary>Adds a function, unless it has no exported symbol or cannot be called through <c>LibraryImport</c>.</summary>
    /// <param name="function">The function.</param>
    /// <param name="header">The header that declares it.</param>
    private void AddFunction(FunctionDecl function, string header)
    {
        var name = function.Name;

        if (IsKnown(name) || IsExcluded(name))
        {
            return;
        }

        if (function.StorageClass == CX_StorageClass.CX_SC_Static || function.IsInlined)
        {
            Skip(name, "an inline function, which the library does not export (ADR 0027)");
            return;
        }

        if (function.IsVariadic)
        {
            Skip(name, "a variadic function, which LibraryImport cannot call portably (ADR 0027)");
            return;
        }

        var parameters = function.Parameters
            .Select((parameter, index) => CreateParameter(parameter.Name, index, MapParameterType(parameter.Type, name)))
            .ToList();

        AddDeclaration(header, new FunctionDeclaration
        {
            CName = name,
            Words = _names.GetWords(name),
            Availability = _target.Platform,
            ReturnType = MapType(function.ReturnType, name, null),
            Parameters = parameters,
            Header = header,
        });
    }

    /// <summary>Adds a structure or a union, or a handle when it is opaque or never defined.</summary>
    /// <param name="record">A declaration of the record.</param>
    /// <param name="header">The header that declares it.</param>
    private void AddRecord(RecordDecl record, string header)
    {
        if (GetTagName(record) is not { } name || _configuration.Types.ContainsKey(name) || IsKnown(name) || IsExcluded(name))
        {
            return;
        }

        if (_configuration.Opaque.ContainsKey(name) || record.Definition is not { } definition)
        {
            AddHandle(name, header);
            return;
        }

        AddStructure(name, _names.GetWords(name), definition, header);
    }

    /// <summary>Adds a structure or a union with its members, after the anonymous records they use.</summary>
    /// <param name="cName">The C name, or the name made for an anonymous record.</param>
    /// <param name="words">The words of the name.</param>
    /// <param name="definition">The definition of the record.</param>
    /// <param name="header">The header that declares it.</param>
    private void AddStructure(string cName, IReadOnlyList<string> words, RecordDecl definition, string header)
    {
        var members = new List<StructureMember>();

        _ = _inProgress.Add(cName);

        foreach (var field in definition.Fields)
        {
            var referrer = $"{cName}.{field.Name}";

            if (field.IsBitField || field.IsAnonymousField)
            {
                throw new InvalidDataException($"{_target.Triple}: '{referrer}' is {(field.IsBitField ? "a bit-field" : "an anonymous member")}, which the bindings do not reproduce: make '{cName}' opaque or exclude it.");
            }

            var memberWords = _names.GetMemberWords(field.Name);
            var anonymous = new AnonymousRecordName($"{cName}_{field.Name}", [.. words, .. memberWords], header);

            members.Add(new StructureMember
            {
                CName = field.Name,
                Words = memberWords,
                Type = MapType(field.Type, referrer, anonymous),
                Default = ZeroExpression.Instance,
            });
        }

        CheckLayout(cName, definition);

        _ = _inProgress.Remove(cName);

        AddDeclaration(header, new StructureDeclaration
        {
            CName = cName,
            Words = words,
            Availability = _target.Platform,
            Members = members,
            IsUnion = definition.IsUnion,
        });
    }

    /// <summary>Adds an enum, with values stored in <c>int</c> when they all fit, in <c>unsigned int</c> otherwise (ADR 0027).</summary>
    /// <param name="enumDecl">A declaration of the enum.</param>
    /// <param name="header">The header that declares it.</param>
    private void AddEnum(EnumDecl enumDecl, string header)
    {
        var definition = enumDecl.Definition ?? enumDecl;

        if (GetTagName(enumDecl) is not { } name)
        {
            foreach (var value in definition.Enumerators)
            {
                Skip(value.Name, "a value of an anonymous enum, which has no type to bind it to");
            }

            return;
        }

        if (IsKnown(name) || IsExcluded(name))
        {
            return;
        }

        var integerType = definition.IntegerType.CanonicalType;

        if (integerType.Handle.SizeOf != sizeof(int))
        {
            throw new InvalidDataException($"{_target.Triple}: the enum '{name}' is stored in {integerType.Handle.SizeOf} bytes, where the bindings expect 4.");
        }

        var isSigned = IsSigned(integerType.Kind);
        var values = definition.Enumerators
            .Select(value => (value.Name, Value: isSigned ? value.InitVal : checked((long)value.UnsignedInitVal)))
            .ToList();
        var underlyingType = values.All(static value => value.Value is >= int.MinValue and <= int.MaxValue) ? "int"
            : values.All(static value => value.Value is >= 0 and <= uint.MaxValue) ? "unsigned int"
            : throw new InvalidDataException($"{_target.Triple}: the values of the enum '{name}' fit neither 'int' nor 'unsigned int'.");

        AddDeclaration(header, new EnumDeclaration
        {
            CName = name,
            Words = _names.GetWords(name),
            Availability = _target.Platform,
            UnderlyingType = new BuiltinTypeReference(underlyingType),
            Values = [.. values.Select(value => new EnumValueDeclaration
            {
                CName = value.Name,

                // The merge names the values once it knows all of them (ADR 0033).
                Words = [],
                Availability = _target.Platform,
                Value = unchecked((ulong)value.Value),
            })],
        });
    }

    /// <summary>Adds a typedef: a function pointer type, a handle when it is opaque, or an alias kept by name (ADR 0026).</summary>
    /// <param name="typedef">The typedef.</param>
    /// <param name="header">The header that declares it.</param>
    /// <remarks>
    /// A typedef that only names a structure or an enum, as in <c>typedef struct SDL_Rect SDL_Rect;</c>
    /// or <c>typedef enum { … } ma_format;</c>, adds nothing: the tag is collected under that name.
    /// </remarks>
    private void AddTypedef(TypedefNameDecl typedef, string header)
    {
        var name = typedef.Name;

        if (_runtimeTypes.Contains(name) || _configuration.Types.ContainsKey(name) || IsKnown(name) || IsExcluded(name))
        {
            return;
        }

        var underlying = typedef.UnderlyingType;

        if (StripSugar(underlying) is TagType tag && GetTagName(tag.Decl) == name)
        {
            return;
        }

        if (_configuration.Opaque.ContainsKey(name))
        {
            AddHandle(name, header);
            return;
        }

        if (StripSugar(underlying) is PointerType pointer && StripSugar(pointer.PointeeType) is FunctionProtoType function)
        {
            if (function.IsVariadic)
            {
                throw new InvalidDataException($"{_target.Triple}: the function pointer type '{name}' is variadic: exclude it.");
            }

            AddDeclaration(header, new FunctionPointerDeclaration
            {
                CName = name,
                Words = _names.GetWords(name),
                Availability = _target.Platform,
                ReturnType = MapType(function.ReturnType, name, null),
                Parameters = [.. function.ParamTypes.Select((type, index) => CreateParameter(string.Empty, index, MapParameterType(type, name)))],
            });

            return;
        }

        AddDeclaration(header, new TypedefDeclaration
        {
            CName = name,
            Words = _names.GetWords(name),
            Availability = _target.Platform,
            Target = MapType(underlying, name, null),
        });
    }

    /// <summary>Adds a handle: a record never defined, or a declaration the configuration makes opaque.</summary>
    /// <param name="name">The C name.</param>
    /// <param name="header">The header that declares it.</param>
    private void AddHandle(string name, string header)
    {
        AddDeclaration(header, new HandleDeclaration { CName = name, Words = _names.GetWords(name), Availability = _target.Platform });
    }

    /// <summary>Records a declaration.</summary>
    /// <param name="header">The header that declares it.</param>
    /// <param name="declaration">The declaration.</param>
    private void AddDeclaration(string header, Declaration declaration)
    {
        _declarations.Add(declaration.CName, new TargetDeclaration(declaration, header));
    }

    /// <summary>Tells whether a name was already collected, left out or is being collected, as happens for every redeclaration.</summary>
    /// <param name="name">The C name.</param>
    /// <returns><see langword="true"/> when the name was seen before.</returns>
    private bool IsKnown(string name)
    {
        return _declarations.ContainsKey(name) || _skipped.ContainsKey(name) || _inProgress.Contains(name);
    }

    /// <summary>Leaves out a declaration that the configuration excludes.</summary>
    /// <param name="name">The C name.</param>
    /// <returns><see langword="true"/> when the declaration is excluded.</returns>
    private bool IsExcluded(string name)
    {
        if (!_exclude.TryGetValue(name, out var reason))
        {
            return false;
        }

        Skip(name, reason);

        return true;
    }

    /// <summary>Records a declaration left out.</summary>
    /// <param name="name">The C name.</param>
    /// <param name="reason">Why it is left out.</param>
    private void Skip(string name, string reason)
    {
        _ = _skipped.TryAdd(name, new SkippedDeclaration(name, reason));
    }

    /// <summary>Maps a clang type onto a type reference of the intermediate representation.</summary>
    /// <param name="type">The clang type.</param>
    /// <param name="referrer">The declaration or member that uses the type, for error messages.</param>
    /// <param name="anonymous">The name an anonymous record gets in this position, or <see langword="null"/> where none may appear.</param>
    /// <returns>The type reference; typedef names are kept (ADR 0026).</returns>
    /// <exception cref="InvalidDataException">The type cannot be reproduced in C#.</exception>
    private TypeReference MapType(Type type, string referrer, AnonymousRecordName? anonymous)
    {
        return type switch
        {
            ElaboratedType elaborated => MapType(elaborated.NamedType, referrer, anonymous),
            ParenType paren => MapType(paren.InnerType, referrer, anonymous),
            AttributedType attributed => MapType(attributed.ModifiedType, referrer, anonymous),
            MacroQualifiedType qualified => MapType(qualified.ModifiedType, referrer, anonymous),
            DecayedType decayed => MapType(decayed.GetDecayedType, referrer, anonymous),
            TypedefType typedef => MapTypedefName(typedef.Decl),
            BuiltinType builtin => new BuiltinTypeReference(GetBuiltinSpelling(builtin, referrer)),
            PointerType pointer when StripSugar(pointer.PointeeType) is FunctionProtoType function => function.IsVariadic
                ? throw new InvalidDataException($"{_target.Triple}: '{referrer}' points to a variadic function: exclude it.")
                : new FunctionPointerTypeReference(MapType(function.ReturnType, referrer, null), [.. function.ParamTypes.Select(parameter => MapParameterType(parameter, referrer))]),
            PointerType pointer => new PointerTypeReference(MapType(pointer.PointeeType, referrer, null), pointer.PointeeType.IsLocalConstQualified),
            ConstantArrayType array => new ArrayTypeReference(MapType(array.ElementType, referrer, anonymous?.ForElement()), checked((int)array.Size)),
            TagType tag => MapTag(tag.Decl, referrer, anonymous),
            _ => throw new InvalidDataException($"{_target.Triple}: '{referrer}' uses the type '{type.AsString}' ({type.TypeClassSpelling}), which the bindings do not reproduce: make it opaque or exclude it."),
        };
    }

    /// <summary>Maps the type of a parameter, which C adjusts from an array to a pointer to its first element.</summary>
    /// <param name="type">The type as declared, such as <c>const ma_backend backends[]</c>.</param>
    /// <param name="referrer">The function that declares the parameter, for error messages.</param>
    /// <returns>The type reference.</returns>
    private TypeReference MapParameterType(Type type, string referrer)
    {
        return StripSugar(type) is ArrayType array
            ? new PointerTypeReference(MapType(array.ElementType, referrer, null), array.ElementType.IsLocalConstQualified)
            : MapType(type, referrer, null);
    }

    /// <summary>Maps a typedef name: a C runtime type or a configured type by name, a library typedef as a reference.</summary>
    /// <param name="typedef">The typedef.</param>
    /// <returns>The type reference.</returns>
    private TypeReference MapTypedefName(TypedefNameDecl typedef)
    {
        var name = typedef.Name;

        return _configuration.Types.TryGetValue(name, out var mapped)
            ? MapConfiguredType(mapped)
            : _runtimeTypes.Contains(name) || GetHeader(typedef) is not { } header
                ? new BuiltinTypeReference(name)
                : EnsureCollected(typedef, header, name);
    }

    /// <summary>Maps a C type the configuration maps by name.</summary>
    /// <param name="mapped">A C builtin type, a pointer to one such as <c>void*</c>, or the full name of a .NET type.</param>
    /// <returns>The type reference.</returns>
    private static TypeReference MapConfiguredType(string mapped)
    {
        return mapped.Contains('.', StringComparison.Ordinal)
            ? new DotNetTypeReference(mapped)
            : mapped.EndsWith('*', StringComparison.Ordinal)
                ? new PointerTypeReference(new BuiltinTypeReference(mapped[..^1].TrimEnd()), IsConst: false)
                : new BuiltinTypeReference(mapped);
    }

    /// <summary>Maps a structure, union or enum, collecting an anonymous record under the name of its position.</summary>
    /// <param name="tag">The declaration of the tag.</param>
    /// <param name="referrer">The declaration or member that uses it, for error messages.</param>
    /// <param name="anonymous">The name an anonymous record gets in this position.</param>
    /// <returns>A reference to the tag.</returns>
    private TypeReference MapTag(TagDecl tag, string referrer, AnonymousRecordName? anonymous)
    {
        var name = GetTagName(tag);

        if (name is not null && _configuration.Types.TryGetValue(name, out var mapped))
        {
            return MapConfiguredType(mapped);
        }

        if (name is not null)
        {
            return GetHeader(tag) is { } header ? EnsureCollected(tag, header, name) : throw new InvalidDataException($"{_target.Triple}: '{referrer}' uses '{name}', which the library does not declare.");
        }

        if (tag is not RecordDecl record || anonymous is null)
        {
            throw new InvalidDataException($"{_target.Triple}: '{referrer}' uses an anonymous {(tag is RecordDecl ? "record" : "enum")} outside a structure member.");
        }

        if (!IsKnown(anonymous.CName))
        {
            AddStructure(anonymous.CName, anonymous.Words, record.Definition ?? record, anonymous.Header);
        }

        return new NamedTypeReference(anonymous.CName);
    }

    /// <summary>Collects a declaration that is used before, or without, appearing at the top level, such as a structure defined inside another.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="header">The header that declares it.</param>
    /// <param name="name">Its C name.</param>
    /// <returns>A reference to it.</returns>
    private NamedTypeReference EnsureCollected(Decl declaration, string header, string name)
    {
        if (!IsKnown(name))
        {
            Add(declaration, header);
        }

        return new NamedTypeReference(name);
    }

    /// <summary>Checks that the members of a record lie where a sequential layout of their types puts them (ADR 0033).</summary>
    /// <param name="cName">The C name of the record.</param>
    /// <param name="definition">The definition.</param>
    /// <exception cref="InvalidDataException">An alignment attribute, a packing pragma or a member without a size makes the C layout differ.</exception>
    /// <remarks>
    /// C# lays the members out by their natural alignment, which only matches the C layout when no
    /// attribute changes it, as miniaudio's <c>MA_ATOMIC</c> could. The sizes come from clang and
    /// only serve this check: the emitted layout follows from the member types (ADR 0026).
    /// </remarks>
    private void CheckLayout(string cName, RecordDecl definition)
    {
        var end = 0L;
        var alignment = 1L;

        foreach (var field in definition.Fields)
        {
            var type = field.Type.CanonicalType.Handle;
            var (size, fieldAlignment) = (type.SizeOf, type.AlignOf);

            if (size < 0 || fieldAlignment <= 0)
            {
                throw new InvalidDataException($"{_target.Triple}: '{cName}.{field.Name}' has no complete type: make '{cName}' opaque or exclude it.");
            }

            var offset = definition.IsUnion ? 0 : AlignUp(end, fieldAlignment);

            if (field.Handle.OffsetOfField != offset * 8)
            {
                throw new InvalidDataException($"{_target.Triple}: '{cName}.{field.Name}' is at byte {field.Handle.OffsetOfField / 8}, where a sequential layout puts it at byte {offset}: make '{cName}' opaque.");
            }

            end = definition.IsUnion ? Math.Max(end, size) : offset + size;
            alignment = Math.Max(alignment, fieldAlignment);
        }

        var recordType = definition.TypeForDecl.CanonicalType.Handle;

        if (recordType.SizeOf != AlignUp(end, alignment) || recordType.AlignOf != alignment)
        {
            throw new InvalidDataException($"{_target.Triple}: '{cName}' takes {recordType.SizeOf} bytes aligned to {recordType.AlignOf}, where a sequential layout gives {AlignUp(end, alignment)} aligned to {alignment}: make it opaque.");
        }
    }

    /// <summary>Gets the spelling of a builtin type, as the projection maps it.</summary>
    /// <param name="builtin">The builtin type.</param>
    /// <param name="referrer">The declaration or member that uses it, for error messages.</param>
    /// <returns>The C spelling.</returns>
    private string GetBuiltinSpelling(BuiltinType builtin, string referrer)
    {
        return _builtinSpellings.TryGetValue(builtin.Kind, out var spelling)
            ? spelling
            : throw new InvalidDataException($"{_target.Triple}: '{referrer}' uses the builtin type '{builtin.AsString}', which the bindings do not reproduce: exclude it.");
    }

    /// <summary>Gets the name of a structure, union or enum: its tag, or the typedef that names it when it is anonymous.</summary>
    /// <param name="tag">The declaration.</param>
    /// <returns>The name, or <see langword="null"/> for an anonymous tag without typedef.</returns>
    private static string? GetTagName(TagDecl tag)
    {
        return tag.Handle.IsAnonymous ? tag.TypedefNameForAnonDecl?.Name : tag.Name;
    }

    /// <summary>Removes the sugar that does not name a type: elaboration, parentheses and attributes.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The type under the sugar; a typedef stays.</returns>
    private static Type StripSugar(Type type)
    {
        return type switch
        {
            ElaboratedType elaborated => StripSugar(elaborated.NamedType),
            ParenType paren => StripSugar(paren.InnerType),
            AttributedType attributed => StripSugar(attributed.ModifiedType),
            MacroQualifiedType qualified => StripSugar(qualified.ModifiedType),
            _ => type,
        };
    }

    /// <summary>Tells whether a builtin integer type is signed.</summary>
    /// <param name="kind">The kind of the type.</param>
    /// <returns><see langword="true"/> for the signed integer types.</returns>
    internal static bool IsSigned(CXTypeKind kind)
    {
        return kind is CXTypeKind.CXType_Char_S or CXTypeKind.CXType_SChar or CXTypeKind.CXType_Short or CXTypeKind.CXType_Int
            or CXTypeKind.CXType_Long or CXTypeKind.CXType_LongLong;
    }

    /// <summary>Rounds an offset up to an alignment.</summary>
    /// <param name="offset">The offset.</param>
    /// <param name="alignment">The alignment, a power of two.</param>
    /// <returns>The aligned offset.</returns>
    private static long AlignUp(long offset, long alignment)
    {
        return (offset + alignment - 1) / alignment * alignment;
    }

    /// <summary>Creates a parameter of a function or a function pointer type.</summary>
    /// <param name="name">The C name, empty when the declaration names none.</param>
    /// <param name="index">The position of the parameter, which names an unnamed one.</param>
    /// <param name="type">The type.</param>
    /// <returns>The parameter.</returns>
    private Parameter CreateParameter(string name, int index, TypeReference type)
    {
        var cName = name.Length > 0 ? name : FormattableString.Invariant($"arg{index}");

        return new Parameter { CName = cName, Words = _names.GetMemberWords(cName), Type = type };
    }
}
