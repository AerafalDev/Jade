using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Projection;

/// <summary>Projects the intermediate representation onto the C# declarations of the raw layer (ADR 0026 to 0029, ADR 0034).</summary>
/// <remarks>
/// Every declaration gets a .NET name. Enums, flags, handles, booleans and value structures are
/// public, since they are identical in both layers; the other structures, the constants and the
/// functions are internal, in the <c>Raw</c> namespace, so that their .NET names stay free for the
/// idiomatic layer (ADR 0034). A value structure holds no pointer but its chain members (ADR 0029).
/// </remarks>
internal sealed class RawProjection
{
    /// <summary>The name of the internal class that holds the constants and the functions.</summary>
    public const string NativeMethodsClass = "NativeMethods";

    /// <summary>The namespace of the internal declarations, under the library's own (ADR 0034).</summary>
    public const string RawNamespace = "Raw";

    /// <summary>The C# types of the builtin C types, by C spelling (ADR 0009 and 0027).</summary>
    private static readonly FrozenDictionary<string, string> _builtinTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["void"] = "void",
        ["_Bool"] = "bool",
        ["bool"] = "bool",
        ["char"] = "byte",
        ["signed char"] = "sbyte",
        ["unsigned char"] = "byte",
        ["short"] = "short",
        ["unsigned short"] = "ushort",
        ["unsigned int"] = "uint",
        ["long"] = "global::System.Runtime.InteropServices.CLong",
        ["unsigned long"] = "global::System.Runtime.InteropServices.CULong",
        ["long long"] = "long",
        ["unsigned long long"] = "ulong",
        ["int8_t"] = "sbyte",
        ["uint8_t"] = "byte",
        ["int16_t"] = "short",
        ["uint16_t"] = "ushort",
        ["int"] = "int",
        ["int32_t"] = "int",
        ["uint32_t"] = "uint",
        ["int64_t"] = "long",
        ["uint64_t"] = "ulong",
        ["intptr_t"] = "nint",
        ["ptrdiff_t"] = "nint",
        ["uintptr_t"] = "nuint",
        ["size_t"] = "nuint",
        ["float"] = "float",
        ["double"] = "double",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The sizes of the integer types that enums and booleans can be stored in, by C# keyword.</summary>
    private static readonly FrozenDictionary<string, int> _integerSizes = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["sbyte"] = 1,
        ["byte"] = 1,
        ["short"] = 2,
        ["ushort"] = 2,
        ["int"] = 4,
        ["uint"] = 4,
        ["long"] = 8,
        ["ulong"] = 8,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The reserved C# keywords, which a C name used as an identifier must escape.</summary>
    private static readonly FrozenSet<string> _keywords = FrozenSet.Create(
        StringComparer.Ordinal,
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while");

    /// <summary>The model to project.</summary>
    private readonly ApiModel _model;

    /// <summary>The .NET naming rules, with the configuration's exceptions.</summary>
    private readonly DotNetNames _names;

    /// <summary>The C# names of the types, by C name.</summary>
    private readonly Dictionary<string, string> _typeNames = [with(StringComparer.Ordinal)];

    /// <summary>Whether each structure is a value structure, by C name.</summary>
    private readonly Dictionary<string, bool> _valueStructures = [with(StringComparer.Ordinal)];

    /// <summary>The projected enums, by C name.</summary>
    private readonly Dictionary<string, ProjectedEnum> _enums = [with(StringComparer.Ordinal)];

    /// <summary>The projected structures, by C name; a structure's defaults need those of the structures it holds.</summary>
    private readonly Dictionary<string, ProjectedStructure> _structures = [with(StringComparer.Ordinal)];

    /// <summary>The inline arrays of the array members of the structures.</summary>
    private readonly List<ProjectedInlineArray> _inlineArrays = [];

    /// <summary>The .NET names of the constants, by C name; structure defaults refer to them.</summary>
    private readonly Dictionary<string, string> _constantNames = [with(StringComparer.Ordinal)];

    /// <summary>Initializes a new instance of the <see cref="RawProjection"/> class.</summary>
    /// <param name="model">The model to project.</param>
    /// <param name="names">The .NET naming rules.</param>
    private RawProjection(ApiModel model, DotNetNames names)
    {
        _model = model;
        _names = names;
    }

    /// <summary>Projects a library's model onto its raw layer.</summary>
    /// <param name="model">The model of the library's API.</param>
    /// <param name="namespace">The namespace of the library, its project name.</param>
    /// <param name="configuration">The library configuration, which gives the native library name.</param>
    /// <param name="names">
    /// The naming rules with the configuration's exceptions, which the caller checks are all used
    /// once the layout tests are projected too.
    /// </param>
    /// <returns>The raw layer.</returns>
    /// <exception cref="InvalidDataException">The configuration lacks the library name, or two names collide.</exception>
    public static ProjectedLibrary Project(ApiModel model, string @namespace, LibraryConfiguration configuration, DotNetNames names)
    {
        var library = configuration.Library ?? throw new InvalidDataException($"The configuration of {@namespace} has no 'library', the name its functions are imported from.");

        return new RawProjection(model, names).Project(@namespace, library);
    }

    /// <summary>Projects the model.</summary>
    /// <param name="namespace">The namespace of the library.</param>
    /// <param name="library">The name the functions are imported from.</param>
    /// <returns>The raw layer.</returns>
    private ProjectedLibrary Project(string @namespace, string library)
    {
        // Other typedefs are aliases, which C# cannot declare: their uses name their target (ADR 0027).
        var booleans = _model.Typedefs.Where(static typedef => typedef.IsBoolean).ToList();

        IEnumerable<Declaration> types = [.. booleans, .. _model.Enums, .. _model.Handles, .. _model.Structures];

        foreach (var declaration in types)
        {
            _typeNames.Add(declaration.CName, _names.GetName(declaration.CName, declaration.Words));
        }

        foreach (var constant in _model.Constants)
        {
            _constantNames.Add(constant.CName, _names.GetName(constant.CName, constant.Words));
        }

        var projectedTypes = booleans.Select(ProjectBoolean)
            .Concat<ProjectedType>(_model.Enums.Select(ProjectEnum))
            .Concat(_model.Handles.Select(ProjectHandle))
            .Concat(_model.Structures.Select(ProjectStructure))
            .ToList()
            .Concat(_inlineArrays)
            .OrderBy(static type => type.Name, StringComparer.Ordinal)
            .ToList();

        // Public and internal types live in two namespaces, but one name must never mean both:
        // the internal declarations see the public ones through their enclosing namespace.
        CheckUnique("type", @namespace, projectedTypes.Select(static type => type.Name).Append(NativeMethodsClass).Append(RawNamespace), StringComparer.OrdinalIgnoreCase);

        var constants = _model.Constants.Select(ProjectConstant).ToList();
        var functions = _model.Functions.Select(ProjectFunction).ToList();

        CheckUnique("member", NativeMethodsClass, constants.Select(static constant => constant.Name).Concat(functions.Select(static function => function.Name)).Append("LibraryName"), StringComparer.Ordinal);

        return new ProjectedLibrary(@namespace, library, projectedTypes, constants, functions);
    }

    /// <summary>Projects a boolean typedef onto a boolean structure of the same size.</summary>
    /// <param name="typedef">The typedef.</param>
    /// <returns>The boolean structure.</returns>
    private ProjectedBoolean ProjectBoolean(TypedefDeclaration typedef)
    {
        return new ProjectedBoolean
        {
            CName = typedef.CName,
            Name = _typeNames[typedef.CName],
            IsPublic = true,
            Availability = typedef.Availability,
            UnderlyingType = GetIntegerType(typedef.Target, typedef.CName),
        };
    }

    /// <summary>Projects an enum or a set of flags.</summary>
    /// <param name="declaration">The enum.</param>
    /// <returns>The C# enum.</returns>
    private ProjectedEnum ProjectEnum(EnumDeclaration declaration)
    {
        if (_enums.TryGetValue(declaration.CName, out var projected))
        {
            return projected;
        }

        var values = declaration.Values
            .Select(value => new ProjectedEnumValue(value.CName, _names.GetEnumValueName(value.CName, value.Words, declaration.Words), value.Value, value.Availability))
            .ToList();
        var underlyingType = GetIntegerType(declaration.UnderlyingType, declaration.CName);

        CheckUnique("value", declaration.CName, values.Select(static value => value.Name), StringComparer.Ordinal);

        projected = new ProjectedEnum
        {
            CName = declaration.CName,
            Name = _typeNames[declaration.CName],
            IsPublic = true,
            Availability = declaration.Availability,
            UnderlyingType = underlyingType,
            IsFlags = declaration.IsFlags,
            Size = _integerSizes[underlyingType],
            Values = values,
        };

        _enums.Add(declaration.CName, projected);

        return projected;
    }

    /// <summary>Projects a handle.</summary>
    /// <param name="declaration">The handle.</param>
    /// <returns>The C# handle.</returns>
    private ProjectedHandle ProjectHandle(HandleDeclaration declaration)
    {
        return new ProjectedHandle
        {
            CName = declaration.CName,
            Name = _typeNames[declaration.CName],
            IsPublic = true,
            Availability = declaration.Availability,
        };
    }

    /// <summary>Projects a structure, public with .NET names when it is a value structure, internal with C names otherwise.</summary>
    /// <param name="declaration">The structure.</param>
    /// <returns>The C# structure.</returns>
    private ProjectedStructure ProjectStructure(StructureDeclaration declaration)
    {
        if (_structures.TryGetValue(declaration.CName, out var projected))
        {
            return projected;
        }

        var isPublic = IsValueStructure(declaration);
        var name = _typeNames[declaration.CName];
        var fields = new List<ProjectedField>();

        foreach (var member in declaration.Members)
        {
            var fieldName = _names.GetName($"{declaration.CName}.{member.CName}", member.Words);
            var type = member.Type is ArrayTypeReference array
                ? ProjectInlineArray(declaration, member, array, $"{name}{fieldName}", isPublic)
                : GetTypeName(member.Type, isPublic);

            fields.Add(new ProjectedField(member.CName, fieldName, type, isPublic && member.Role == MemberRole.Value));
        }

        // A member cannot have the name of its enclosing type (CS0542).
        CheckUnique("member", declaration.CName, fields.Select(static field => field.Name).Append(name), StringComparer.Ordinal);

        projected = new ProjectedStructure
        {
            CName = declaration.CName,
            Name = name,
            IsPublic = isPublic,
            Availability = declaration.Availability,
            Fields = fields,
            IsUnion = declaration.IsUnion,
            InitializerCName = declaration.InitializerCName,
            Defaults = [.. declaration.Members.Zip(fields).SelectMany(pair => GetDefaults(pair.First, pair.Second))],
        };

        _structures.Add(declaration.CName, projected);

        return projected;
    }

    /// <summary>Projects the inline array of an array member.</summary>
    /// <param name="structure">The structure that holds the member.</param>
    /// <param name="member">The member.</param>
    /// <param name="array">The type of the member.</param>
    /// <param name="name">The C# name of the inline array.</param>
    /// <param name="isPublic">Whether the structure, and so the inline array, is public.</param>
    /// <returns>The C# name of the inline array.</returns>
    private string ProjectInlineArray(StructureDeclaration structure, StructureMember member, ArrayTypeReference array, string name, bool isPublic)
    {
        if (array.Element is ArrayTypeReference)
        {
            throw new InvalidDataException($"'{structure.CName}.{member.CName}' is an array of arrays, which the raw layer does not support yet.");
        }

        var elementType = GetTypeName(array.Element);

        if (elementType.EndsWith('*', StringComparison.Ordinal) || elementType.StartsWith("delegate*", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"'{structure.CName}.{member.CName}' is an array of pointers, which an inline array cannot hold.");
        }

        _inlineArrays.Add(new ProjectedInlineArray
        {
            CName = $"{structure.CName}.{member.CName}",
            Name = name,
            IsPublic = isPublic,
            Availability = structure.Availability,
            ElementType = elementType,
            Length = array.Length,
        });

        return name;
    }

    /// <summary>Gets the assignments that give a field its default, if it is not zero.</summary>
    /// <param name="member">The member.</param>
    /// <param name="field">The projected field.</param>
    /// <returns>No assignment for a zero default, one otherwise.</returns>
    private IEnumerable<ProjectedAssignment> GetDefaults(StructureMember member, ProjectedField field)
    {
        switch (member.Default)
        {
            case ChainHeaderExpression header:
                var chainedStruct = ProjectStructure(_model.Get<StructureDeclaration>(((NamedTypeReference)member.Type).CName));
                var typeField = chainedStruct.Fields.Single(chainField => chainField.CName == header.TypeMember);

                if (GetEnumValue(header.StructureType) is { } structureType)
                {
                    yield return new ProjectedAssignment($"{field.Name}.{typeField.Name}", structureType);
                }

                break;

            case StructureDefaultsExpression:
                var nested = ProjectStructure(_model.Get<StructureDeclaration>(((NamedTypeReference)member.Type).CName));

                if (nested.Defaults.Count > 0)
                {
                    yield return new ProjectedAssignment(field.Name, $"new {nested.Name}()");
                }

                break;

            default:
                if (FormatValue(member.Default, member.Type, member.CName) is { } value)
                {
                    yield return new ProjectedAssignment(field.Name, value);
                }

                break;
        }
    }

    /// <summary>Projects a constant.</summary>
    /// <param name="constant">The constant.</param>
    /// <returns>The C# constant.</returns>
    private ProjectedConstant ProjectConstant(ConstantDeclaration constant)
    {
        var header = GetHeaderName(constant.Header);
        var name = _constantNames[constant.CName];

        // A UTF-8 literal is NUL-terminated in memory, so the span's address is the C string.
        if (constant.Value is StringExpression text)
        {
            return new ProjectedConstant(constant.CName, name, "global::System.ReadOnlySpan<byte>", $"{FormatString(text.Value)}u8", IsConst: false, constant.Availability, header);
        }

        var type = GetTypeName(constant.Type);

        return constant.Value is TypeMaximumExpression
            ? new ProjectedConstant(constant.CName, name, type, $"{type}.MaxValue", type is not ("nint" or "nuint"), constant.Availability, header)
            : new ProjectedConstant(constant.CName, name, type, FormatValue(constant.Value, constant.Type, constant.CName) ?? "0", IsConst: true, constant.Availability, header);
    }

    /// <summary>Projects a function.</summary>
    /// <param name="function">The function.</param>
    /// <returns>The C# function.</returns>
    private ProjectedFunction ProjectFunction(FunctionDeclaration function)
    {
        // A method is named after its owner too, as its C name is: wgpuDeviceCreateBuffer gives DeviceCreateBuffer.
        IReadOnlyList<string> words = function.Owner is { } owner ? [.. _model.Get<Declaration>(owner).Words, .. function.Words] : function.Words;
        var parameters = function.Parameters
            .Select(parameter => new ProjectedParameter(Escape(_names.GetParameterName($"{function.CName}.{parameter.CName}", parameter.Words)), GetTypeName(parameter.Type)))
            .ToList();

        CheckUnique("parameter", function.CName, parameters.Select(static parameter => parameter.Name), StringComparer.Ordinal);

        return new ProjectedFunction(function.CName, _names.GetName(function.CName, words), GetTypeName(function.ReturnType), parameters, function.Availability, GetHeaderName(function.Header));
    }

    /// <summary>Gets the name of a header that names the partial file of its declarations (ADR 0027).</summary>
    /// <param name="header">The header, such as <c>SDL3/SDL_video.h</c>, or <see langword="null"/>.</param>
    /// <returns>The file name without directory and extension, such as <c>SDL_video</c>.</returns>
    private static string? GetHeaderName(string? header)
    {
        return header is null ? null : Path.GetFileNameWithoutExtension(header);
    }

    /// <summary>Tells whether a structure is a value structure: no pointer but its chain members (ADR 0029).</summary>
    /// <param name="structure">The structure.</param>
    /// <returns><see langword="true"/> when the structure serves both layers as is.</returns>
    private bool IsValueStructure(StructureDeclaration structure)
    {
        if (!_valueStructures.TryGetValue(structure.CName, out var isValue))
        {
            isValue = structure.Members.All(member => member.Role is MemberRole.NextInChain or MemberRole.ChainHeader || IsValueType(member.Type));
            _valueStructures.Add(structure.CName, isValue);
        }

        return isValue;
    }

    /// <summary>Tells whether a type holds no pointer.</summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> for numbers, enums, booleans, handles and value structures.</returns>
    /// <remarks>Handles count as values: they are public types of their own, not raw pointers (ADR 0009).</remarks>
    private bool IsValueType(TypeReference type)
    {
        return type switch
        {
            BuiltinTypeReference builtin => builtin.Spelling != "void",
            DotNetTypeReference => true,
            ArrayTypeReference array => IsValueType(array.Element),
            NamedTypeReference named => _model.Find(named.CName) switch
            {
                EnumDeclaration or HandleDeclaration => true,
                TypedefDeclaration typedef => typedef.IsBoolean || IsValueType(typedef.Target),
                StructureDeclaration structure => IsValueStructure(structure),
                _ => false,
            },
            _ => false,
        };
    }

    /// <summary>Gets the C# spelling of a type.</summary>
    /// <param name="type">The type.</param>
    /// <param name="fromPublic">Whether a public type uses it, from whose namespace an internal type is reached through <c>Raw.</c>.</param>
    /// <returns>The C# type; a function pointer type is spelled inline, since C# has no public type alias (ADR 0027).</returns>
    private string GetTypeName(TypeReference type, bool fromPublic = false)
    {
        return type switch
        {
            BuiltinTypeReference builtin => GetBuiltinTypeName(builtin.Spelling),
            PointerTypeReference pointer => GetTypeName(pointer.Pointee, fromPublic) + "*",
            DotNetTypeReference dotNet => $"global::{dotNet.FullName}",
            FunctionPointerTypeReference function => GetFunctionPointerName(function.ParameterTypes, function.ReturnType, fromPublic),
            NamedTypeReference named => _model.Find(named.CName) switch
            {
                FunctionPointerDeclaration function => GetFunctionPointerName(function.Parameters.Select(static parameter => parameter.Type), function.ReturnType, fromPublic),
                TypedefDeclaration { IsBoolean: false } typedef => GetTypeName(typedef.Target, fromPublic),
                StructureDeclaration structure when fromPublic && !IsValueStructure(structure) => $"{RawNamespace}.{_typeNames[named.CName]}",
                null => throw new InvalidDataException($"'{named.CName}' is not declared."),
                _ => _typeNames[named.CName],
            },
            ArrayTypeReference => throw new InvalidDataException($"An array of {type} is used outside a structure member."),
            _ => throw new UnreachableException($"Unknown type reference {type}."),
        };
    }

    /// <summary>Spells a function pointer type.</summary>
    /// <param name="parameterTypes">The types of the parameters.</param>
    /// <param name="returnType">The return type.</param>
    /// <param name="fromPublic">Whether a public type uses it.</param>
    /// <returns>The <c>delegate* unmanaged[Cdecl]</c> type.</returns>
    private string GetFunctionPointerName(IEnumerable<TypeReference> parameterTypes, TypeReference returnType, bool fromPublic)
    {
        return $"delegate* unmanaged[Cdecl]<{string.Join(", ", parameterTypes.Select(type => GetTypeName(type, fromPublic)).Append(GetTypeName(returnType, fromPublic)))}>";
    }

    /// <summary>Gets the C# keyword of an integer type that an enum or a boolean is stored in.</summary>
    /// <param name="type">The type.</param>
    /// <param name="cName">The C name of the enum or boolean, for error messages.</param>
    /// <returns>The C# keyword.</returns>
    private string GetIntegerType(TypeReference type, string cName)
    {
        var name = GetTypeName(type);

        return _integerSizes.ContainsKey(name) ? name : throw new InvalidDataException($"'{cName}' is stored in '{name}', which is not an integer type.");
    }

    /// <summary>Formats a value as a C# expression.</summary>
    /// <param name="value">The value.</param>
    /// <param name="type">The type of the member or constant that holds it.</param>
    /// <param name="cName">The C name of the member or constant, for error messages.</param>
    /// <returns>The expression, or <see langword="null"/> for a zero value, which needs no assignment.</returns>
    private string? FormatValue(ValueExpression value, TypeReference type, string cName)
    {
        // A zero value needs no type, and an array member, which only has zero defaults, has no C# type of its own.
        if (value is ZeroExpression)
        {
            return null;
        }

        var typeName = GetTypeName(type);

        return value switch
        {
            IntegerExpression { Value: 0 } or BooleanExpression { Value: false } => null,
            FloatExpression { Value: 0 } number when !double.IsNegative(number.Value) => null,
            IntegerExpression integer when IsSigned(typeName) => unchecked((long)integer.Value).ToString(CultureInfo.InvariantCulture),
            IntegerExpression integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            FloatExpression number when double.IsNaN(number.Value) => $"{typeName}.NaN",
            // A float is written with the shortest digits that give back its value, not those of its double.
            FloatExpression number when typeName == "float" => ((float)number.Value).ToString("R", CultureInfo.InvariantCulture) + "f",
            FloatExpression number => number.Value.ToString("R", CultureInfo.InvariantCulture) + "d",
            BooleanExpression => "true",
            ConstantReferenceExpression constant => $"{RawNamespace}.{NativeMethodsClass}.{_constantNames[constant.CName]}",
            EnumValueReferenceExpression enumValue => GetEnumValue(enumValue),
            _ => throw new InvalidDataException($"'{cName}' has the value {value}, which a structure member cannot have."),
        };
    }

    /// <summary>Gets the C# type of a builtin C type (ADR 0009 and 0027).</summary>
    /// <param name="spelling">The C spelling, such as <c>uint32_t</c>.</param>
    /// <returns>The C# keyword or type.</returns>
    /// <exception cref="InvalidDataException">The C type has no mapping.</exception>
    public static string GetBuiltinTypeName(string spelling)
    {
        return _builtinTypes.TryGetValue(spelling, out var keyword)
            ? keyword
            : throw new InvalidDataException($"The C type '{spelling}' has no mapping.");
    }

    /// <summary>Tells whether a C# integer type is signed.</summary>
    /// <param name="typeName">The C# type.</param>
    /// <returns><see langword="true"/> for the signed integer keywords.</returns>
    public static bool IsSigned(string typeName)
    {
        return typeName is "sbyte" or "short" or "int" or "long" or "nint";
    }

    /// <summary>Formats a string as a C# literal.</summary>
    /// <param name="value">The string.</param>
    /// <returns>The quoted literal, with the characters C# requires escaped.</returns>
    private static string FormatString(string value)
    {
        var builder = new System.Text.StringBuilder("\"", value.Length + 2);

        foreach (var character in value)
        {
            _ = character switch
            {
                '"' => builder.Append("\\\""),
                '\\' => builder.Append("\\\\"),
                _ when char.IsControl(character) => builder.Append(CultureInfo.InvariantCulture, $"\\u{(int)character:X4}"),
                _ => builder.Append(character),
            };
        }

        return builder.Append('"').ToString();
    }

    /// <summary>Formats an enum value as a C# expression.</summary>
    /// <param name="reference">The enum value.</param>
    /// <returns>The qualified value, or <see langword="null"/> when it is zero.</returns>
    private string? GetEnumValue(EnumValueReferenceExpression reference)
    {
        var projected = ProjectEnum(_model.Get<EnumDeclaration>(reference.EnumCName));
        var value = projected.Values.Single(value => value.CName == reference.ValueCName);

        return value.Value == 0 ? null : $"{projected.Name}.{value.Name}";
    }

    /// <summary>Escapes a C name that is a reserved C# keyword.</summary>
    /// <param name="name">The C name.</param>
    /// <returns>The name, prefixed with <c>@</c> when it is a keyword.</returns>
    public static string Escape(string name)
    {
        return _keywords.Contains(name) ? "@" + name : name;
    }

    /// <summary>Checks that names do not collide.</summary>
    /// <param name="kind">The kind of the names, for error messages.</param>
    /// <param name="scope">Where the names are declared, for error messages.</param>
    /// <param name="names">The names.</param>
    /// <param name="comparer">The comparison: type names must also differ in more than case, since they name files.</param>
    /// <exception cref="InvalidDataException">Two names collide.</exception>
    public static void CheckUnique(string kind, string scope, IEnumerable<string> names, StringComparer comparer)
    {
        var duplicates = names.GroupBy(static name => name, comparer).Where(static group => group.Count() > 1).Select(static group => group.Key).ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidDataException($"Several {kind}s of {scope} are named {string.Join(", ", duplicates)}: add a name to 'names'.");
        }
    }
}
