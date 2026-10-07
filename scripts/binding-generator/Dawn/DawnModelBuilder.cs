using System.Diagnostics;
using System.Globalization;
using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Dawn;

/// <summary>Builds the intermediate representation of the WebGPU API from <c>dawn.json</c> (ADR 0026).</summary>
/// <remarks>
/// The model reproduces the C header that Dawn generates with <c>generator/templates/api.h</c> at
/// the pinned commit: the members and functions the template adds (chain members, callback
/// userdata, reference counting, <c>FreeMembers</c>), the offsets that tags give to enum values,
/// and the defaults of the <c>*_INIT</c> macros.
/// </remarks>
internal sealed class DawnModelBuilder
{
    /// <summary>
    /// The header that declares the API in C, as an <c>#include</c> writes it: Dawn's build
    /// generates <c>dawn/webgpu.h</c> from <c>api.h</c> and installs it with
    /// <c>webgpu/webgpu.h</c>, which includes it (<c>dawn_headers</c> in <c>src/dawn/CMakeLists.txt</c>).
    /// The layout tests compile against it (ADR 0036).
    /// </summary>
    public const string Header = "webgpu/webgpu.h";

    /// <summary>The canonical name of the enum that identifies chained structures.</summary>
    private const string StructureTypeEnumName = "s type";

    /// <summary>The C name of the member of <c>WGPUChainedStruct</c> that identifies the structure.</summary>
    private const string ChainTypeMember = "sType";

    /// <summary>The canonical name of the structure that <c>dawn.json</c> uses for strings.</summary>
    private const string StringViewName = "string view";

    /// <summary>The canonical name of the enum value that stands for a default in WebGPU enums.</summary>
    private const string UndefinedValueName = "undefined";

    /// <summary>The canonical name of the empty bitmask value.</summary>
    private const string NoFlagsValueName = "none";

    /// <summary>The default of a structure member that is zeroed rather than initialized with its own defaults.</summary>
    private const string ZeroStructureDefault = "zero";

    /// <summary>The API read from <c>dawn.json</c>.</summary>
    private readonly DawnApi _api;

    /// <summary>The C prefix of the API, <c>WGPU</c>.</summary>
    private readonly string _prefix;

    /// <summary>The entries the bindings include, by canonical name, with the platforms each is available on.</summary>
    private readonly Dictionary<string, (DawnEntry Entry, Platforms Availability)> _entries = [with(StringComparer.Ordinal)];

    /// <summary>The enums and bitmasks, by canonical name, built before the structures whose defaults use them.</summary>
    private readonly Dictionary<string, EnumDeclaration> _enums = [with(StringComparer.Ordinal)];

    /// <summary>The constants, by canonical name, built before the structures whose defaults use them.</summary>
    private readonly Dictionary<string, ConstantDeclaration> _constants = [with(StringComparer.Ordinal)];

    /// <summary>The entries, values and methods left out, for the report.</summary>
    private readonly List<SkippedDeclaration> _skipped = [];

    /// <summary>Initializes a new instance of the <see cref="DawnModelBuilder"/> class and selects the entries to include.</summary>
    /// <param name="api">The API read from <c>dawn.json</c>.</param>
    /// <param name="exclude">The C names of the entries to leave out, with the reason.</param>
    /// <exception cref="InvalidDataException">An exclusion matches no entry.</exception>
    private DawnModelBuilder(DawnApi api, IReadOnlyDictionary<string, string> exclude)
    {
        _api = api;
        _prefix = api.Metadata.CPrefix;

        var unusedExclusions = new SortedSet<string>(exclude.Keys, StringComparer.Ordinal);

        foreach (var (name, entry) in api.Entries)
        {
            if (entry.Category == DawnCategory.Native)
            {
                continue;
            }

            var cName = GetEntryCName(name, entry.Category);
            var availability = DawnVariants.GetPlatforms(entry.Tags);

            if (availability == Platforms.None)
            {
                _skipped.Add(new SkippedDeclaration(cName, $"in no header variant (tags: {string.Join(", ", entry.Tags ?? [])})"));
            }
            else if (exclude.TryGetValue(cName, out var reason))
            {
                _ = unusedExclusions.Remove(cName);
                _skipped.Add(new SkippedDeclaration(cName, reason));
            }
            else
            {
                _entries.Add(name, (entry, availability));
            }
        }

        if (unusedExclusions.Count > 0)
        {
            throw new InvalidDataException($"'exclude' names entries that dawn.json does not include: {string.Join(", ", unusedExclusions)}.");
        }
    }

    /// <summary>Gets the C name of the boolean type, <c>WGPUBool</c>, which <c>api.h</c> declares itself.</summary>
    private string BooleanCName => _prefix + "Bool";

    /// <summary>Gets the C name of the chain header, <c>WGPUChainedStruct</c>, which <c>api.h</c> declares itself.</summary>
    private string ChainedStructCName => _prefix + "ChainedStruct";

    /// <summary>Builds the intermediate representation of <c>dawn.json</c>.</summary>
    /// <param name="api">The API read from <c>dawn.json</c>.</param>
    /// <param name="exclude">The C names of the entries to leave out, with the reason, from the library configuration.</param>
    /// <returns>The model of the API.</returns>
    /// <exception cref="InvalidDataException">The file uses a construct the model does not support, or refers to an entry it leaves out.</exception>
    public static ApiModel Build(DawnApi api, IReadOnlyDictionary<string, string> exclude)
    {
        return new DawnModelBuilder(api, exclude).Build();
    }

    /// <summary>Builds the model from the selected entries.</summary>
    /// <returns>The model of the API.</returns>
    private ApiModel Build()
    {
        var entries = _entries.OrderBy(static entry => entry.Key, StringComparer.Ordinal).ToList();

        if (entries.FirstOrDefault(static entry => entry.Value.Entry.Category == DawnCategory.Typedef) is { Key: { } typedef })
        {
            throw new InvalidDataException($"dawn.json: '{typedef}' is a typedef, which the generator does not support yet.");
        }

        foreach (var (name, (entry, availability)) in entries.Where(static entry => entry.Value.Entry.Category is DawnCategory.Enum or DawnCategory.Bitmask))
        {
            _enums.Add(name, BuildEnum(name, entry, availability));
        }

        foreach (var (name, (entry, availability)) in entries.Where(static entry => entry.Value.Entry.Category == DawnCategory.Constant))
        {
            _constants.Add(name, BuildConstant(name, entry, availability));
        }

        var handles = new List<HandleDeclaration>();
        var structures = new List<StructureDeclaration> { CreateChainedStruct() };
        var functionPointers = new List<FunctionPointerDeclaration>();
        var functions = new List<FunctionDeclaration>();

        foreach (var (name, (entry, availability)) in entries)
        {
            switch (entry.Category)
            {
                case DawnCategory.Object:
                    handles.Add(new HandleDeclaration { CName = GetTypeCName(name), Words = DawnNames.GetWords(name), Availability = availability });
                    functions.AddRange(BuildMethods(name, entry, availability));
                    break;

                case DawnCategory.Structure or DawnCategory.CallbackInfo:
                    var structure = BuildStructure(name, entry, availability);
                    structures.Add(structure);

                    if (HasFreeMembersFunction(structure, entry))
                    {
                        functions.Add(BuildFreeMembersFunction(name, structure));
                    }

                    break;

                case DawnCategory.CallbackFunction or DawnCategory.FunctionPointer:
                    functionPointers.Add(BuildFunctionPointer(name, entry, availability));
                    break;

                case DawnCategory.Function:
                    functions.Add(BuildFunction(name, entry, availability));
                    break;

                case DawnCategory.Bitmask or DawnCategory.Constant or DawnCategory.Enum or DawnCategory.Native or DawnCategory.Typedef:
                    // Built above, or not declarations.
                    break;

                default:
                    throw new InvalidDataException($"dawn.json: '{name}' has the unknown category {entry.Category}.");
            }
        }

        return new ApiModel([.. _constants.Values], [CreateBooleanTypedef()], [.. _enums.Values], handles, structures, functionPointers, functions, _skipped);
    }

    /// <summary>Gets the C name of an entry, whose form depends on its category.</summary>
    /// <param name="name">The canonical name of the entry.</param>
    /// <param name="category">The category of the entry.</param>
    /// <returns>The C name.</returns>
    private string GetEntryCName(string name, DawnCategory category)
    {
        return category switch
        {
            DawnCategory.Constant => DawnNames.GetConstantName(_prefix, name),
            DawnCategory.Function => DawnNames.GetFunctionName(_prefix, null, name),
            DawnCategory.Bitmask or DawnCategory.CallbackFunction or DawnCategory.CallbackInfo or DawnCategory.Enum or DawnCategory.FunctionPointer
                or DawnCategory.Native or DawnCategory.Object or DawnCategory.Structure or DawnCategory.Typedef => GetTypeCName(name),
            _ => throw new InvalidDataException($"dawn.json: '{name}' has the unknown category {category}."),
        };
    }

    /// <summary>Gets the C name of a type entry.</summary>
    /// <param name="name">The canonical name of the entry.</param>
    /// <returns>The C name, such as <c>WGPUDevice</c>.</returns>
    private string GetTypeCName(string name)
    {
        return DawnNames.GetTypeName(_prefix, name);
    }

    /// <summary>Creates <c>WGPUBool</c>, a 32-bit integer used as a boolean, which <c>api.h</c> declares itself.</summary>
    /// <returns>The typedef.</returns>
    private TypedefDeclaration CreateBooleanTypedef()
    {
        return new TypedefDeclaration
        {
            CName = BooleanCName,
            Words = ["bool"],
            Availability = Platforms.All,
            Target = new BuiltinTypeReference("uint32_t"),
            IsBoolean = true,
        };
    }

    /// <summary>Creates <c>WGPUChainedStruct</c>, the header that links chained structures, which <c>api.h</c> declares itself.</summary>
    /// <returns>The structure.</returns>
    /// <remarks>Its <c>next</c> member is an ordinary pointer, so that the header itself is never a value structure (ADR 0029).</remarks>
    private StructureDeclaration CreateChainedStruct()
    {
        return new StructureDeclaration
        {
            CName = ChainedStructCName,
            Words = ["chained", "struct"],
            Availability = Platforms.All,
            CTypeName = ChainedStructCName,
            Members =
            [
                new StructureMember
                {
                    CName = "next",
                    Words = ["next"],
                    Type = new PointerTypeReference(new NamedTypeReference(ChainedStructCName), IsConst: false),
                    Default = ZeroExpression.Instance,
                },
                new StructureMember
                {
                    CName = ChainTypeMember,
                    Words = ["s", "type"],
                    Type = GetBaseType(StructureTypeEnumName, ChainedStructCName, Platforms.All),
                    Default = ZeroExpression.Instance,
                },
            ],
        };
    }

    /// <summary>Builds an enum or a bitmask.</summary>
    /// <param name="name">The canonical name of the entry.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="availability">The platforms the entry is available on.</param>
    /// <returns>The enum.</returns>
    private EnumDeclaration BuildEnum(string name, DawnEntry entry, Platforms availability)
    {
        var isFlags = entry.Category == DawnCategory.Bitmask;
        var values = new List<EnumValueDeclaration>();

        foreach (var value in entry.Values ?? [])
        {
            var cName = DawnNames.GetEnumValueName(_prefix, name, value.Name);
            var valueAvailability = DawnVariants.GetPlatforms(value.Tags) & availability;

            if (valueAvailability == Platforms.None)
            {
                _skipped.Add(new SkippedDeclaration(cName, $"in no header variant of its enum (tags: {string.Join(", ", value.Tags ?? [])})"));
                continue;
            }

            values.Add(new EnumValueDeclaration
            {
                CName = cName,
                Words = DawnNames.GetWords(value.Name),
                Availability = valueAvailability,
                Value = isFlags ? value.Value : value.Value + GetEnumValueOffset(value.Tags),
            });
        }

        return new EnumDeclaration
        {
            CName = GetTypeCName(name),
            Words = DawnNames.GetWords(name),
            Availability = availability,
            UnderlyingType = new BuiltinTypeReference(isFlags ? "uint64_t" : "uint32_t"),
            IsFlags = isFlags,
            Values = values,
        };
    }

    /// <summary>Gets the offset that <c>dawn_json_generator.py</c> adds to an enum value for its tags.</summary>
    /// <param name="tags">The tags of the value.</param>
    /// <returns>The offset: the value's own tags give it, whatever the header variant.</returns>
    /// <remarks>
    /// Dawn's extensions live at <c>0x0005_0000</c>, Emdawnwebgpu's at <c>0x0004_0000</c> and other
    /// native values at <c>0x0001_0000</c>, so that the variants never give one number two meanings.
    /// </remarks>
    private static ulong GetEnumValueOffset(IReadOnlyList<string>? tags)
    {
        var offset = 0UL;

        if (tags is null)
        {
            return offset;
        }

        if (tags.Contains("dawn"))
        {
            offset = 0x0005_0000;
        }
        else if (tags.Contains("emscripten"))
        {
            offset = 0x0004_0000;
        }

        if (offset == 0 && tags.Contains("native"))
        {
            offset = 0x0001_0000;
        }

        return offset;
    }

    /// <summary>Builds a constant.</summary>
    /// <param name="name">The canonical name of the entry.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="availability">The platforms the entry is available on.</param>
    /// <returns>The constant.</returns>
    private ConstantDeclaration BuildConstant(string name, DawnEntry entry, Platforms availability)
    {
        var cName = DawnNames.GetConstantName(_prefix, name);
        var type = GetBaseType(entry.Type ?? throw new InvalidDataException($"dawn.json: '{name}' has no type."), cName, availability) as BuiltinTypeReference
            ?? throw new InvalidDataException($"dawn.json: '{name}' is not of a C builtin type.");
        var value = entry.Value ?? throw new InvalidDataException($"dawn.json: '{name}' has no value.");

        return new ConstantDeclaration
        {
            CName = cName,
            Words = DawnNames.GetWords(name),
            Availability = availability,
            Type = type,
            Value = (value, type.Spelling) switch
            {
                ("UINT32_MAX", "uint32_t") or ("UINT64_MAX", "uint64_t") or ("SIZE_MAX", "size_t") => TypeMaximumExpression.Instance,
                ("NAN", "float" or "double") => new FloatExpression(double.NaN),
                _ => ParseLiteral(value, type, cName),
            },
        };
    }

    /// <summary>Builds the methods of an object, with the reference counting functions that <c>api.h</c> adds to every object.</summary>
    /// <param name="name">The canonical name of the object.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="availability">The platforms the object is available on.</param>
    /// <returns>The functions.</returns>
    private List<FunctionDeclaration> BuildMethods(string name, DawnEntry entry, Platforms availability)
    {
        var owner = GetTypeCName(name);
        var self = new Parameter { CName = DawnNames.GetVariableName(name), Words = DawnNames.GetWords(name), Type = new NamedTypeReference(owner) };
        var methods = new List<FunctionDeclaration>();

        foreach (var method in entry.Methods ?? [])
        {
            var cName = DawnNames.GetFunctionName(_prefix, name, method.Name);
            var methodAvailability = DawnVariants.GetPlatforms(method.Tags) & availability;

            if (methodAvailability == Platforms.None)
            {
                _skipped.Add(new SkippedDeclaration(cName, $"in no header variant of its object (tags: {string.Join(", ", method.Tags ?? [])})"));
                continue;
            }

            methods.Add(new FunctionDeclaration
            {
                CName = cName,
                Words = DawnNames.GetWords(method.Name),
                Availability = methodAvailability,
                ReturnType = method.Returns is { } returns ? GetBaseType(returns.Type, cName, methodAvailability) : BuiltinTypeReference.Void,
                ReturnIsOptional = method.Returns?.Optional ?? false,
                Parameters = [self, .. BuildParameters(method.Args, cName, methodAvailability)],
                Kind = FunctionKind.Method,
                Owner = owner,
            });
        }

        foreach (var (methodName, kind) in new[] { ("add ref", FunctionKind.AddRef), ("release", FunctionKind.Release) })
        {
            methods.Add(new FunctionDeclaration
            {
                CName = DawnNames.GetFunctionName(_prefix, name, methodName),
                Words = DawnNames.GetWords(methodName),
                Availability = availability,
                ReturnType = BuiltinTypeReference.Void,
                Parameters = [self],
                Kind = kind,
                Owner = owner,
            });
        }

        return methods;
    }

    /// <summary>Builds a structure or a callback info, with the members that <c>api.h</c> adds.</summary>
    /// <param name="name">The canonical name of the entry.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="availability">The platforms the entry is available on.</param>
    /// <returns>The structure.</returns>
    private StructureDeclaration BuildStructure(string name, DawnEntry entry, Platforms availability)
    {
        var cName = GetTypeCName(name);
        var isCallbackInfo = entry.Category == DawnCategory.CallbackInfo;

        // api.h gives every callback info a nextInChain, whatever dawn.json says.
        var extensible = isCallbackInfo ? ChainDirection.In : ToChainDirection(entry.Extensible);
        var chained = ToChainDirection(entry.Chained);
        var members = new List<StructureMember>();

        if (extensible != ChainDirection.None && chained != ChainDirection.None)
        {
            throw new InvalidDataException($"dawn.json: '{name}' is both extensible and chained.");
        }

        if (extensible != ChainDirection.None)
        {
            members.Add(new StructureMember
            {
                CName = "nextInChain",
                Words = ["next", "in", "chain"],
                Type = new PointerTypeReference(new NamedTypeReference(ChainedStructCName), IsConst: false),
                Role = MemberRole.NextInChain,
                Default = ZeroExpression.Instance,
            });
        }

        if (chained != ChainDirection.None)
        {
            members.Add(new StructureMember
            {
                CName = "chain",
                Words = ["chain"],
                Type = new NamedTypeReference(ChainedStructCName),
                Role = MemberRole.ChainHeader,
                Default = new ChainHeaderExpression(ChainTypeMember, GetEnumValue(StructureTypeEnumName, name, cName, availability)),
            });
        }

        members.AddRange((entry.Members ?? []).Select(member => BuildMember(member, cName, availability)));

        if (isCallbackInfo)
        {
            members.AddRange(CreateUserdata().Select(static parameter => new StructureMember
            {
                CName = parameter.CName,
                Words = parameter.Words,
                Type = parameter.Type,
                Role = MemberRole.Userdata,
                Default = ZeroExpression.Instance,
                IsOptional = true,
            }));
        }

        CheckLengths(cName, members.Select(static member => (member.CName, member.Length)));

        return new StructureDeclaration
        {
            CName = cName,
            Words = DawnNames.GetWords(name),
            Availability = availability,
            Members = members,
            CTypeName = cName,
            InitializerCName = DawnNames.GetInitializerName(_prefix, name),
            IsOutput = entry.Out || extensible == ChainDirection.Out || chained == ChainDirection.Out,
            IsCallbackInfo = isCallbackInfo,
            Extensible = extensible,
            Chained = chained,
            ChainRoots = [.. (entry.ChainRoots ?? []).Select(root => ((NamedTypeReference)GetBaseType(root, cName, availability)).CName)],
        };
    }

    /// <summary>Builds a structure member, with the default its <c>*_INIT</c> macro gives it.</summary>
    /// <param name="member">The member.</param>
    /// <param name="structure">The C name of the structure, for error messages.</param>
    /// <param name="availability">The platforms the structure is available on.</param>
    /// <returns>The member.</returns>
    private StructureMember BuildMember(DawnRecordMember member, string structure, Platforms availability)
    {
        var cName = DawnNames.GetVariableName(member.Name);
        var referrer = $"{structure}.{cName}";

        return new StructureMember
        {
            CName = cName,
            Words = DawnNames.GetWords(member.Name),
            Type = GetType(member.Type, member.Annotation, referrer, availability),
            Default = GetDefault(member, referrer, availability),
            Length = GetLength(member.Length),
            IsOptional = member.Optional,
        };
    }

    /// <summary>Tells whether <c>api.h</c> declares a <c>FreeMembers</c> function for a structure.</summary>
    /// <param name="structure">The structure.</param>
    /// <param name="entry">Its entry, whose own members decide.</param>
    /// <returns><see langword="true"/> for an output structure with a pointer or string member, which the library allocates.</returns>
    private static bool HasFreeMembersFunction(StructureDeclaration structure, DawnEntry entry)
    {
        return !structure.IsCallbackInfo
            && structure.IsOutput
            && (entry.Members ?? []).Any(static member => member.Annotation != DawnRecordMember.ValueAnnotation || member.Type == StringViewName);
    }

    /// <summary>Builds the <c>FreeMembers</c> function of an output structure, which takes the structure by value.</summary>
    /// <param name="name">The canonical name of the structure.</param>
    /// <param name="structure">The structure.</param>
    /// <returns>The function.</returns>
    private FunctionDeclaration BuildFreeMembersFunction(string name, StructureDeclaration structure)
    {
        const string MethodName = "free members";

        return new FunctionDeclaration
        {
            CName = DawnNames.GetFunctionName(_prefix, name, MethodName),
            Words = DawnNames.GetWords(MethodName),
            Availability = structure.Availability,
            ReturnType = BuiltinTypeReference.Void,
            Parameters = [new Parameter { CName = DawnNames.GetVariableName(name), Words = structure.Words, Type = new NamedTypeReference(structure.CName) }],
            Kind = FunctionKind.FreeMembers,
            Owner = structure.CName,
        };
    }

    /// <summary>Builds a callback function or a function pointer type; callbacks get the two userdata parameters <c>api.h</c> adds.</summary>
    /// <param name="name">The canonical name of the entry.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="availability">The platforms the entry is available on.</param>
    /// <returns>The function pointer type.</returns>
    private FunctionPointerDeclaration BuildFunctionPointer(string name, DawnEntry entry, Platforms availability)
    {
        var cName = GetTypeCName(name);
        var parameters = BuildParameters(entry.Args, cName, availability);

        if (entry.Category == DawnCategory.CallbackFunction)
        {
            parameters.AddRange(CreateUserdata());
        }

        return new FunctionPointerDeclaration
        {
            CName = cName,
            Words = DawnNames.GetWords(name),
            Availability = availability,
            ReturnType = entry.Returns is { } returns ? GetBaseType(returns, cName, availability) : BuiltinTypeReference.Void,
            Parameters = parameters,
        };
    }

    /// <summary>Builds a free function.</summary>
    /// <param name="name">The canonical name of the entry.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="availability">The platforms the entry is available on.</param>
    /// <returns>The function.</returns>
    private FunctionDeclaration BuildFunction(string name, DawnEntry entry, Platforms availability)
    {
        var cName = DawnNames.GetFunctionName(_prefix, null, name);

        return new FunctionDeclaration
        {
            CName = cName,
            Words = DawnNames.GetWords(name),
            Availability = availability,
            ReturnType = entry.Returns is { } returns ? GetBaseType(returns, cName, availability) : BuiltinTypeReference.Void,
            Parameters = BuildParameters(entry.Args, cName, availability),
            Kind = FunctionKind.Free,
        };
    }

    /// <summary>Builds the parameters of a function or a function pointer type.</summary>
    /// <param name="args">The arguments in <c>dawn.json</c>.</param>
    /// <param name="function">The C name of the function, for error messages.</param>
    /// <param name="availability">The platforms the function is available on.</param>
    /// <returns>The parameters.</returns>
    private List<Parameter> BuildParameters(IReadOnlyList<DawnRecordMember>? args, string function, Platforms availability)
    {
        var parameters = (args ?? []).Select(arg =>
        {
            var cName = DawnNames.GetVariableName(arg.Name);
            var referrer = $"{function}({cName})";

            return new Parameter
            {
                CName = cName,
                Words = DawnNames.GetWords(arg.Name),
                Type = GetType(arg.Type, arg.Annotation, referrer, availability),
                Length = GetLength(arg.Length),
                IsOptional = arg.Optional,
                Default = GetArgumentDefault(arg, referrer, availability),
            };
        }).ToList();

        CheckLengths(function, parameters.Select(static parameter => (parameter.CName, parameter.Length)));

        return parameters;
    }

    /// <summary>Creates the two userdata pointers that <c>api.h</c> adds to callbacks and callback infos.</summary>
    /// <returns>The parameters <c>userdata1</c> and <c>userdata2</c>.</returns>
    private static Parameter[] CreateUserdata()
    {
        return
        [
            new Parameter { CName = "userdata1", Words = ["userdata1"], Type = new PointerTypeReference(BuiltinTypeReference.Void, IsConst: false), IsOptional = true },
            new Parameter { CName = "userdata2", Words = ["userdata2"], Type = new PointerTypeReference(BuiltinTypeReference.Void, IsConst: false), IsOptional = true },
        ];
    }

    /// <summary>Checks that the lengths held by other members name members of the same record.</summary>
    /// <param name="record">The C name of the structure or function, for error messages.</param>
    /// <param name="members">The members or parameters, with their lengths.</param>
    /// <exception cref="InvalidDataException">A length names no member of the record.</exception>
    private static void CheckLengths(string record, IEnumerable<(string CName, ArrayLength? Length)> members)
    {
        var list = members.ToList();
        var names = list.Select(static member => member.CName).ToHashSet(StringComparer.Ordinal);

        if (list.FirstOrDefault(member => member.Length is MemberArrayLength length && !names.Contains(length.CName)) is { Length: MemberArrayLength missing } member)
        {
            throw new InvalidDataException($"dawn.json: the length of '{record}.{member.CName}' is '{missing.CName}', which '{record}' does not have.");
        }
    }

    /// <summary>Converts the length of a <c>dawn.json</c> member.</summary>
    /// <param name="length">A literal length, the canonical name of the member that holds it, or <see langword="null"/>.</param>
    /// <returns>The length, or <see langword="null"/> for a single element.</returns>
    private static ArrayLength? GetLength(string? length)
    {
        return length is null
            ? null
            : int.TryParse(length, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
                ? new FixedArrayLength(count)
                : new MemberArrayLength(DawnNames.GetVariableName(length));
    }

    /// <summary>Gets the type of a member, argument or result, with its pointer annotation.</summary>
    /// <param name="typeName">The canonical name of the base type.</param>
    /// <param name="annotation">The annotation: <c>value</c>, <c>*</c>, <c>const*</c> or <c>const*const*</c>.</param>
    /// <param name="referrer">The C name of what uses the type, for error messages.</param>
    /// <param name="availability">The platforms where the referrer is available, which the type must cover.</param>
    /// <returns>The type.</returns>
    private TypeReference GetType(string typeName, string annotation, string referrer, Platforms availability)
    {
        var baseType = GetBaseType(typeName, referrer, availability);

        return annotation switch
        {
            DawnRecordMember.ValueAnnotation => baseType,
            "*" => new PointerTypeReference(baseType, IsConst: false),
            "const*" => new PointerTypeReference(baseType, IsConst: true),
            "const*const*" => new PointerTypeReference(new PointerTypeReference(baseType, IsConst: true), IsConst: true),
            _ => throw new InvalidDataException($"dawn.json: '{referrer}' has the unknown annotation '{annotation}'."),
        };
    }

    /// <summary>Gets a type by canonical name.</summary>
    /// <param name="typeName">The canonical name.</param>
    /// <param name="referrer">The C name of what uses the type, for error messages.</param>
    /// <param name="availability">The platforms where the referrer is available, which the type must cover.</param>
    /// <returns>A builtin type for the natives of the C API, a reference to its declaration otherwise.</returns>
    /// <exception cref="InvalidDataException">The type is left out, is not available everywhere its referrer is, or only serves Dawn's wire.</exception>
    private TypeReference GetBaseType(string typeName, string referrer, Platforms availability)
    {
        var entry = _api.Entries.GetValueOrDefault(typeName);

        return entry?.Category == DawnCategory.Native
            ? GetNativeType(typeName, referrer)
            : !_entries.TryGetValue(typeName, out var included)
                ? throw new InvalidDataException($"dawn.json: '{referrer}' uses '{typeName}', which {(entry is null ? "does not exist" : "the bindings leave out")}.")
                : (included.Availability & availability) == availability
                    ? new NamedTypeReference(GetTypeCName(typeName))
                    : throw new InvalidDataException($"dawn.json: '{referrer}' is available on {availability} but uses '{typeName}', which is only available on {included.Availability}.");
    }

    /// <summary>Gets the type of a native of <c>dawn.json</c>, as <c>api.h</c> spells it.</summary>
    /// <param name="typeName">The name of the native.</param>
    /// <param name="referrer">The C name of what uses the type, for error messages.</param>
    /// <returns>A builtin type, a pointer to <c>void</c>, or <c>WGPUBool</c> for <c>bool</c>.</returns>
    /// <exception cref="InvalidDataException">The native only serves Dawn's wire or C++ API.</exception>
    private TypeReference GetNativeType(string typeName, string referrer)
    {
        return typeName switch
        {
            "bool" => new NamedTypeReference(BooleanCName),
            "char" or "float" or "double" or "int" or "int32_t" or "size_t" or "uint8_t" or "uint16_t" or "uint32_t" or "uint64_t" or "void" => new BuiltinTypeReference(typeName),
            "void *" => new PointerTypeReference(BuiltinTypeReference.Void, IsConst: false),
            "void const *" => new PointerTypeReference(BuiltinTypeReference.Void, IsConst: true),
            _ => throw new InvalidDataException($"dawn.json: '{referrer}' uses the native type '{typeName}', which only Dawn's wire and C++ API use."),
        };
    }

    /// <summary>Gets the default that the <c>*_INIT</c> macro of a structure gives a member.</summary>
    /// <param name="member">The member.</param>
    /// <param name="referrer">The C name of the member, for error messages.</param>
    /// <param name="availability">The platforms the structure is available on.</param>
    /// <returns>The default.</returns>
    /// <remarks>
    /// This follows <c>render_c_default_value</c> in <c>api.h</c>, which differs from the
    /// <c>default</c> of <c>dawn.json</c> in places: an enum that has an <c>undefined</c> value
    /// defaults to it, so that the implementation picks the default.
    /// </remarks>
    private ValueExpression GetDefault(DawnRecordMember member, string referrer, Platforms availability)
    {
        if (member.Annotation != DawnRecordMember.ValueAnnotation)
        {
            return ZeroExpression.Instance;
        }

        var type = _api.Entries[member.Type];

        switch (type.Category)
        {
            case DawnCategory.Object or DawnCategory.CallbackFunction or DawnCategory.FunctionPointer:
                return ZeroExpression.Instance;

            case DawnCategory.Enum:
                var hasUndefined = _enums[member.Type].Values.Any(static value => value.Words is [UndefinedValueName]);

                return hasUndefined
                    ? GetEnumValue(member.Type, UndefinedValueName, referrer, availability)
                    : member.Default is { } enumDefault ? GetEnumValue(member.Type, enumDefault, referrer, availability) : ZeroExpression.Instance;

            case DawnCategory.Bitmask:
                return GetEnumValue(member.Type, member.Default ?? NoFlagsValueName, referrer, availability);

            case DawnCategory.Structure or DawnCategory.CallbackInfo:
                return member.Default switch
                {
                    null => StructureDefaultsExpression.Instance,
                    ZeroStructureDefault => ZeroExpression.Instance,
                    _ => throw new InvalidDataException($"dawn.json: '{referrer}' has the structure default '{member.Default}', which api.h does not support."),
                };

            case DawnCategory.Native:
                return GetNativeDefault(member, referrer, availability);

            case DawnCategory.Constant or DawnCategory.Function or DawnCategory.Typedef:
            default:
                throw new InvalidDataException($"dawn.json: '{referrer}' has a type of category {type.Category}, which a member cannot have.");
        }
    }

    /// <summary>Gets the value <c>dawn.json</c> documents for an argument the caller leaves out.</summary>
    /// <param name="arg">The argument.</param>
    /// <param name="referrer">The C name of the argument, for error messages.</param>
    /// <param name="availability">The platforms the function is available on.</param>
    /// <returns>The default, or <see langword="null"/> when the argument has none.</returns>
    /// <remarks>
    /// Dawn's C++ wrapper turns these into default arguments (<c>render_cpp_default_value</c> in
    /// <c>generator/templates/api_cpp.h</c>); <c>nullptr</c> marks a pointer the caller may omit.
    /// </remarks>
    private ValueExpression? GetArgumentDefault(DawnRecordMember arg, string referrer, Platforms availability)
    {
        return arg.Default is not { } value
            ? null
            : arg.Annotation != DawnRecordMember.ValueAnnotation
            ? value == "nullptr" ? ZeroExpression.Instance : throw new InvalidDataException($"dawn.json: '{referrer}' is a pointer with the default '{value}'.")
            : _api.Entries[arg.Type].Category switch
            {
                DawnCategory.Native => GetNativeDefault(arg, referrer, availability),
                DawnCategory.Enum or DawnCategory.Bitmask => GetEnumValue(arg.Type, value, referrer, availability),
                DawnCategory.CallbackFunction or DawnCategory.CallbackInfo or DawnCategory.Constant or DawnCategory.Function or DawnCategory.FunctionPointer
                    or DawnCategory.Object or DawnCategory.Structure or DawnCategory.Typedef => throw new InvalidDataException($"dawn.json: '{referrer}' has a default, which an argument of category {_api.Entries[arg.Type].Category} cannot have."),
                _ => throw new InvalidDataException($"dawn.json: '{referrer}' has a type of unknown category."),
            };
    }

    /// <summary>Gets the default of a member of a native type.</summary>
    /// <param name="member">The member.</param>
    /// <param name="referrer">The C name of the member, for error messages.</param>
    /// <param name="availability">The platforms the structure is available on.</param>
    /// <returns>The default: a constant, a literal, a boolean or zero.</returns>
    private ValueExpression GetNativeDefault(DawnRecordMember member, string referrer, Platforms availability)
    {
        return (member.Type, member.Default) switch
        {
            ("bool", null or "false") => new BooleanExpression(false),
            ("bool", "true") => new BooleanExpression(true),
            ("bool", _) => throw new InvalidDataException($"dawn.json: '{referrer}' has the boolean default '{member.Default}'."),
            (_, null) => ZeroExpression.Instance,
            (_, { } name) when _constants.TryGetValue(name, out var constant) => (constant.Availability & availability) == availability
                ? new ConstantReferenceExpression(constant.CName)
                : throw new InvalidDataException($"dawn.json: '{referrer}' defaults to '{constant.CName}', which is only available on {constant.Availability}."),
            (_, { } literal) => GetBaseType(member.Type, referrer, availability) is BuiltinTypeReference builtin
                ? ParseLiteral(literal, builtin, referrer)
                : throw new InvalidDataException($"dawn.json: '{referrer}' has the default '{literal}', which only an integer or floating-point member can have."),
        };
    }

    /// <summary>Gets a value of an enum or a bitmask by canonical name.</summary>
    /// <param name="typeName">The canonical name of the enum or bitmask.</param>
    /// <param name="valueName">The canonical name of the value.</param>
    /// <param name="referrer">The C name of what uses the value, for error messages.</param>
    /// <param name="availability">The platforms where the referrer is available, which the value must cover.</param>
    /// <returns>The reference to the value.</returns>
    private EnumValueReferenceExpression GetEnumValue(string typeName, string valueName, string referrer, Platforms availability)
    {
        var declaration = _enums.TryGetValue(typeName, out var found)
            ? found
            : throw new InvalidDataException($"dawn.json: '{referrer}' uses '{typeName}', which is not an enum the bindings include.");
        var value = declaration.Values.FirstOrDefault(value => string.Join(' ', value.Words) == valueName)
            ?? throw new InvalidDataException($"dawn.json: '{referrer}' uses '{valueName}', which '{declaration.CName}' does not have.");

        return (value.Availability & availability) == availability
            ? new EnumValueReferenceExpression(declaration.CName, value.CName)
            : throw new InvalidDataException($"dawn.json: '{referrer}' uses '{value.CName}', which is only available on {value.Availability}.");
    }

    /// <summary>Parses a C literal as <c>api.h</c> writes it verbatim, such as <c>0xFFFFFFFF</c> or <c>32.f</c>.</summary>
    /// <param name="literal">The literal.</param>
    /// <param name="type">The type of the member or constant.</param>
    /// <param name="referrer">The C name of the member or constant, for error messages.</param>
    /// <returns>An integer or a floating-point number.</returns>
    /// <exception cref="InvalidDataException">The literal is not a number of the type.</exception>
    private static ValueExpression ParseLiteral(string literal, BuiltinTypeReference type, string referrer)
    {
        if (type.Spelling is "float" or "double")
        {
            return double.TryParse(literal.TrimEnd('f', 'F'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
                ? new FloatExpression(number)
                : throw new InvalidDataException($"dawn.json: '{referrer}' has the value '{literal}', which is not a floating-point number.");
        }

        var parsed = literal.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.TryParse(literal.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var integer)
            : ulong.TryParse(literal, NumberStyles.None, CultureInfo.InvariantCulture, out integer);

        return parsed
            ? new IntegerExpression(integer)
            : throw new InvalidDataException($"dawn.json: '{referrer}' has the value '{literal}', which is not a non-negative integer.");
    }

    /// <summary>Converts a chain direction of <c>dawn.json</c>.</summary>
    /// <param name="direction">The direction.</param>
    /// <returns>The direction in the model.</returns>
    private static ChainDirection ToChainDirection(DawnDirection direction)
    {
        return direction switch
        {
            DawnDirection.None => ChainDirection.None,
            DawnDirection.In => ChainDirection.In,
            DawnDirection.Out => ChainDirection.Out,
            _ => throw new UnreachableException($"Unknown chain direction {direction}."),
        };
    }
}
