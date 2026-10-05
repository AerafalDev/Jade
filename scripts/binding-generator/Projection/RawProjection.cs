using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Projection;

/// <summary>Projects the intermediate representation onto the C# declarations of the raw layer (ADR 0026 to 0029).</summary>
/// <remarks>
/// Enums, flags, handles, booleans and value structures are public with .NET names, since they are
/// identical in both layers; the other structures, the constants and the functions are internal
/// and keep their C names (ADR 0028). A value structure holds no pointer but its chain members
/// (ADR 0029).
/// </remarks>
internal sealed class RawProjection
{
    /// <summary>The name of the internal class that holds the constants and the functions.</summary>
    public const string NativeMethodsClass = "NativeMethods";

    /// <summary>The C# types of the builtin C types, by C spelling (ADR 0009 and 0027).</summary>
    private static readonly FrozenDictionary<string, string> _builtinTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["void"] = "void",
        ["_Bool"] = "bool",
        ["bool"] = "bool",
        ["char"] = "byte",
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
    /// <param name="configuration">The library configuration, which gives the native library name and the naming exceptions.</param>
    /// <returns>The raw layer.</returns>
    /// <exception cref="InvalidDataException">The configuration lacks the library name or has unused exceptions, or two names collide.</exception>
    public static ProjectedLibrary Project(ApiModel model, string @namespace, LibraryConfiguration configuration)
    {
        var library = configuration.Library ?? throw new InvalidDataException($"The configuration of {@namespace} has no 'library', the name its functions are imported from.");

        return new RawProjection(model, new DotNetNames(configuration.Names, configuration.Words)).Project(@namespace, library);
    }

    /// <summary>Projects the model.</summary>
    /// <param name="namespace">The namespace of the library.</param>
    /// <param name="library">The name the functions are imported from.</param>
    /// <returns>The raw layer.</returns>
    private ProjectedLibrary Project(string @namespace, string library)
    {
        if (_model.Typedefs.FirstOrDefault(static typedef => !typedef.IsBoolean) is { } typedef)
        {
            throw new InvalidDataException($"'{typedef.CName}' is a typedef that is not a boolean, which the raw layer does not support yet.");
        }

        IEnumerable<Declaration> publicTypes = [.. _model.Typedefs, .. _model.Enums, .. _model.Handles, .. _model.Structures.Where(IsValueStructure)];

        foreach (var declaration in publicTypes)
        {
            _typeNames.Add(declaration.CName, _names.GetName(declaration.CName, declaration.Words));
        }

        foreach (var structure in _model.Structures.Where(structure => !IsValueStructure(structure)))
        {
            _typeNames.Add(structure.CName, structure.CName);
        }

        var types = _model.Typedefs.Select(ProjectBoolean)
            .Concat<ProjectedType>(_model.Enums.Select(ProjectEnum))
            .Concat(_model.Handles.Select(ProjectHandle))
            .Concat(_model.Structures.Select(ProjectStructure))
            .OrderBy(static type => type.Name, StringComparer.Ordinal)
            .ToList();

        CheckUnique("type", @namespace, types.Select(static type => type.Name).Append(NativeMethodsClass), StringComparer.OrdinalIgnoreCase);

        var constants = _model.Constants.Select(ProjectConstant).ToList();
        var functions = _model.Functions.Select(ProjectFunction).ToList();

        _names.CheckAllUsed();

        return new ProjectedLibrary(@namespace, library, types, constants, functions);
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
        var fields = declaration.Members
            .Select(member => new ProjectedField(
                member.CName,
                isPublic ? _names.GetName($"{declaration.CName}.{member.CName}", member.Words) : Escape(member.CName),
                GetTypeName(member.Type),
                isPublic && member.Role == MemberRole.Value))
            .ToList();

        // A member cannot have the name of its enclosing type (CS0542).
        CheckUnique("member", declaration.CName, fields.Select(static field => field.Name).Append(name), StringComparer.Ordinal);

        projected = new ProjectedStructure
        {
            CName = declaration.CName,
            Name = name,
            IsPublic = isPublic,
            Availability = declaration.Availability,
            Fields = fields,
            InitializerCName = declaration.InitializerCName,
            Defaults = [.. declaration.Members.Zip(fields).SelectMany(pair => GetDefaults(pair.First, pair.Second))],
        };

        _structures.Add(declaration.CName, projected);

        return projected;
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
        var type = GetTypeName(constant.Type);

        return constant.Value is TypeMaximumExpression
            ? new ProjectedConstant(constant.CName, type, $"{type}.MaxValue", type is not ("nint" or "nuint"), constant.Availability)
            : new ProjectedConstant(constant.CName, type, FormatValue(constant.Value, constant.Type, constant.CName) ?? "0", IsConst: true, constant.Availability);
    }

    /// <summary>Projects a function.</summary>
    /// <param name="function">The function.</param>
    /// <returns>The C# function.</returns>
    private ProjectedFunction ProjectFunction(FunctionDeclaration function)
    {
        var parameters = function.Parameters.Select(parameter => new ProjectedParameter(Escape(parameter.CName), GetTypeName(parameter.Type))).ToList();

        CheckUnique("parameter", function.CName, parameters.Select(static parameter => parameter.Name), StringComparer.Ordinal);

        return new ProjectedFunction(function.CName, GetTypeName(function.ReturnType), parameters, function.Availability);
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
    /// <returns>The C# type; a function pointer type is spelled inline, since C# has no public type alias (ADR 0027).</returns>
    private string GetTypeName(TypeReference type)
    {
        return type switch
        {
            BuiltinTypeReference builtin => _builtinTypes.TryGetValue(builtin.Spelling, out var keyword)
                ? keyword
                : throw new InvalidDataException($"The C type '{builtin.Spelling}' has no mapping."),
            PointerTypeReference pointer => GetTypeName(pointer.Pointee) + "*",
            NamedTypeReference named => _model.Find(named.CName) switch
            {
                FunctionPointerDeclaration function =>
                    $"delegate* unmanaged[Cdecl]<{string.Join(", ", function.Parameters.Select(parameter => GetTypeName(parameter.Type)).Append(GetTypeName(function.ReturnType)))}>",
                null => throw new InvalidDataException($"'{named.CName}' is not declared."),
                _ => _typeNames[named.CName],
            },
            _ => throw new UnreachableException($"Unknown type reference {type}."),
        };
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
        var typeName = GetTypeName(type);

        return value switch
        {
            ZeroExpression or IntegerExpression { Value: 0 } or BooleanExpression { Value: false } => null,
            FloatExpression { Value: 0 } number when !double.IsNegative(number.Value) => null,
            IntegerExpression integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            FloatExpression number when double.IsNaN(number.Value) => $"{typeName}.NaN",
            FloatExpression number => number.Value.ToString("R", CultureInfo.InvariantCulture) + (typeName == "float" ? "f" : "d"),
            BooleanExpression => "true",
            ConstantReferenceExpression constant => $"{NativeMethodsClass}.{constant.CName}",
            EnumValueReferenceExpression enumValue => GetEnumValue(enumValue),
            _ => throw new InvalidDataException($"'{cName}' has the value {value}, which a structure member cannot have."),
        };
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
    private static string Escape(string name)
    {
        return _keywords.Contains(name) ? "@" + name : name;
    }

    /// <summary>Checks that names do not collide.</summary>
    /// <param name="kind">The kind of the names, for error messages.</param>
    /// <param name="scope">Where the names are declared, for error messages.</param>
    /// <param name="names">The names.</param>
    /// <param name="comparer">The comparison: type names must also differ in more than case, since they name files.</param>
    /// <exception cref="InvalidDataException">Two names collide.</exception>
    private static void CheckUnique(string kind, string scope, IEnumerable<string> names, StringComparer comparer)
    {
        var duplicates = names.GroupBy(static name => name, comparer).Where(static group => group.Count() > 1).Select(static group => group.Key).ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidDataException($"Several {kind}s of {scope} are named {string.Join(", ", duplicates)}: add a name to 'names'.");
        }
    }
}
