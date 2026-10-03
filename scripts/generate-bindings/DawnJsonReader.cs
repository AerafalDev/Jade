using System.Globalization;
using System.Text.Json;

/// <summary>
/// Reads Dawn's <c>dawn.json</c> into a <see cref="LibraryModel"/> (ADR-0005). It follows the schema the way Dawn's own
/// generator reads it at the pinned revision (<c>generator/dawn_json_generator.py</c> and <c>generator/templates/api.h</c>):
/// the model holds what the staged <c>dawn/webgpu.h</c> declares, under the same C names, and
/// <see cref="DawnHeaderCheck"/> compares the two on every target.
/// </summary>
internal sealed class DawnJsonReader
{
    private const string Browser = "browser";
    private const string StringView = "string view";

    // validate_and_get_tags() asserts that every tag is one of these.
    private static readonly HashSet<string> s_knownTags = new(StringComparer.Ordinal) { "dawn", "emscripten", "native", "deprecated", "art", "art_experimental" };

    // The tags each consumer of dawn.json enables (MultiGeneratorFromDawnJSON.get_file_renders): dawn/webgpu.h declares
    // every implementation's items, emdawnwebgpu's webgpu.h only Emscripten's, and Dawn implements its own.
    private static readonly string[] s_headerTags = ["dawn", "emscripten", "native", "deprecated"];
    private static readonly string[] s_webTags = ["emscripten"];
    private static readonly string[] s_dawnTags = ["dawn", "native", "deprecated"];

    // Native types by dawn.json name. "bool" is WGPUBool, mapped through the config; the wire-only natives (ObjectId, ...)
    // never appear in the API.
    private static readonly Dictionary<string, TypeRef> s_natives = new(StringComparer.Ordinal)
    {
        ["char"] = TypeRef.Of(PrimitiveType.Char),
        ["int"] = TypeRef.Of(PrimitiveType.Int32),
        ["int32_t"] = TypeRef.Of(PrimitiveType.Int32),
        ["uint8_t"] = TypeRef.Of(PrimitiveType.Byte),
        ["uint16_t"] = TypeRef.Of(PrimitiveType.UInt16),
        ["uint32_t"] = TypeRef.Of(PrimitiveType.UInt32),
        ["uint64_t"] = TypeRef.Of(PrimitiveType.UInt64),
        ["size_t"] = TypeRef.Of(PrimitiveType.NUInt),
        ["float"] = TypeRef.Of(PrimitiveType.Single),
        ["double"] = TypeRef.Of(PrimitiveType.Double),
        ["void"] = TypeRef.Void,
        ["void *"] = TypeRef.PointerTo(TypeRef.Void, isConst: false),
        ["void const *"] = TypeRef.PointerTo(TypeRef.Void, isConst: true),
    };

    private readonly LibraryConfig _config;
    private readonly Dictionary<string, JsonElement> _items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeRef> _functionPointers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Documentation> _callbacks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (bool Web, bool Dawn)> _availability = new(StringComparer.Ordinal);
    private readonly HashSet<string> _usedConfigKeys = new(StringComparer.Ordinal);
    private readonly List<string> _errors = [];
    private string _prefix = string.Empty;

    private DawnJsonReader(LibraryConfig config)
    {
        _config = config;
    }

    /// <summary>Reads the API description.</summary>
    /// <param name="config">The library config: renames, exclusions, notes, typedef mappings.</param>
    /// <param name="path">The staged <c>dawn.json</c>.</param>
    /// <returns>The model, which is the same on every target, and the config entries the read used.</returns>
    /// <exception cref="InvalidOperationException">The file does not follow the schema, or the config does not cover it.</exception>
    public static (LibraryModel Model, IReadOnlySet<string> UsedConfigKeys) Read(LibraryConfig config, string path)
    {
        var reader = new DawnJsonReader(config);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var model = reader.Read(document.RootElement);
        return (model, reader._usedConfigKeys);
    }

    /// <summary>Returns the C names an item gives, following <c>Name</c> in dawn_json_generator.py: <c>texture view dimension</c> is <c>TextureViewDimension</c>.</summary>
    /// <param name="canonical">A dawn.json name: lower-case words separated by spaces.</param>
    /// <returns>The words, each with its first letter raised.</returns>
    public static string CamelCase(string canonical) => string.Concat(canonical.Split(' ').Select(Capitalize));

    private LibraryModel Read(JsonElement root)
    {
        _prefix = root.GetProperty("_metadata").GetProperty("c_prefix").GetString()!;
        foreach (var property in root.EnumerateObject().Where(p => !p.Name.StartsWith('_')))
        {
            CheckTags(property.Name, property.Value);
            if (IsEnabled(property.Value, s_headerTags))
            {
                _items.Add(property.Name, property.Value);
                _availability.Add(CName(property.Name), (IsEnabled(property.Value, s_webTags), IsEnabled(property.Value, s_dawnTags)));
            }
        }

        var enums = new List<EnumModel>();
        var structs = new List<StructModel> { ChainedStruct() };
        var handles = new List<HandleModel>();
        var constants = new List<ConstantModel>();
        foreach (var (name, item) in _items.OrderBy(i => SortKey(i.Key), StringComparer.Ordinal))
        {
            try
            {
                switch (Category(item))
                {
                    case "enum" or "bitmask":
                        enums.Add(ReadEnum(name, item));
                        break;

                    case "structure" or "callback info":
                        if (Exclude(CName(name)))
                        {
                            break;
                        }

                        structs.Add(ReadStruct(name, item));
                        break;

                    case "object":
                        handles.Add(ReadHandle(name, item));
                        break;

                    case "constant":
                        constants.Add(ReadConstant(name, item));
                        break;

                    case "function pointer" or "callback function":
                        FunctionPointer(name, item);
                        break;

                    case "function" or "native" or "typedef":
                        break;

                    case var category:
                        throw new MappingException($"category `{category}` is not in dawn_json_generator.py's parse_json()");
                }
            }
            catch (MappingException e)
            {
                _errors.Add($"{name}: {e.Message}.");
            }
        }

        var (groups, functions) = ReadFunctions();
        CheckAvailability(functions, structs);
        if (_errors.Count > 0)
        {
            throw new InvalidOperationException($"{_config.Name}: {_config.ApiDescription} does not map:\n  {string.Join("\n  ", _errors)}");
        }

        return new LibraryModel
        {
            Name = _config.Name,
            Namespace = $"Jade.Interop.{_config.Name}",
            FunctionsClass = _config.FunctionsClass,
            Groups = groups,
            Functions = functions,
            Enums = [.. enums.OrderBy(e => e.Name, StringComparer.Ordinal)],
            Structs = [.. structs.OrderBy(s => s.Name, StringComparer.Ordinal)],
            Handles = [.. handles.OrderBy(h => h.Name, StringComparer.Ordinal)],
            Constants = constants,
            Callbacks = _callbacks,
            StringView = new StringViewModel { Struct = CName(StringView), Data = "data", Length = "length" },
        };
    }

    // Functions in the order dawn/webgpu.h declares them: global functions by name with GetProcAddress last, then the
    // methods of objects and the FreeMembers of output structures, by parent name.
    private (List<KeyValuePair<string, string>> Groups, List<FunctionModel> Functions) ReadFunctions()
    {
        const string Global = "Global";
        var groups = new List<KeyValuePair<string, string>> { KeyValuePair.Create(Global, $"the functions and constants of {_config.ApiDescription}") };
        var functions = new List<FunctionModel>();
        var globals = _items.Where(i => Category(i.Value) == "function")
            .OrderBy(i => i.Key == "get proc address")
            .ThenBy(i => SortKey(i.Key), StringComparer.Ordinal);
        foreach (var (name, item) in globals)
        {
            Collect(functions, () => ReadFunction(null, name, item, Global, [item]));
        }

        var parents = _items.Where(i => Category(i.Value) == "object" || (Category(i.Value) == "structure" && HasFreeMembers(i.Value) && !Excluded(CName(i.Key))))
            .OrderBy(i => SortKey(i.Key), StringComparer.Ordinal);
        foreach (var (parent, item) in parents)
        {
            var group = CamelCase(parent);
            groups.Add(KeyValuePair.Create(group, $"the {(Category(item) == "object" ? "methods" : "FreeMembers function")} of `{CName(parent)}` in {_config.ApiDescription}"));
            if (Category(item) == "structure")
            {
                Collect(functions, () => ReadFunction(parent, "free members", default, group, [item]));
                continue;
            }

            var methods = item.TryGetProperty("methods", out var list) ? list.EnumerateArray().Where(m => IsEnabled(m, s_headerTags)) : [];
            foreach (var method in methods.OrderBy(m => SortKey(m.GetProperty("name").GetString()!), StringComparer.Ordinal))
            {
                Collect(functions, () => ReadFunction(parent, method.GetProperty("name").GetString()!, method, group, [item, method]));
            }

            // c_methods() adds reference counting to every object.
            Collect(functions, () => ReadFunction(parent, "add ref", default, group, [item]));
            Collect(functions, () => ReadFunction(parent, "release", default, group, [item]));
        }

        return (groups, functions);
    }

    private void Collect(List<FunctionModel> functions, Func<FunctionModel?> read)
    {
        try
        {
            if (read() is { } function)
            {
                functions.Add(function);
            }
        }
        catch (MappingException e)
        {
            _errors.Add(e.Message);
        }
    }

    // A method of `parent` (an object, or a structure for FreeMembers), or a global function when `parent` is null.
    private FunctionModel? ReadFunction(string? parent, string name, JsonElement json, string group, JsonElement[] owners)
    {
        var nativeName = $"{_prefix.ToLowerInvariant()}{(parent is null ? string.Empty : CamelCase(parent))}{CamelCase(name)}";
        if (Exclude(nativeName))
        {
            return null;
        }

        try
        {
            var names = new List<string>();
            var types = new List<TypeRef>();
            var arguments = new List<JsonElement?>();
            if (parent is not null)
            {
                // render_c_method_args(): the object, or the structure by value, comes first.
                names.Add(LowerCamelCase(parent));
                types.Add(TypeRef.Named(CName(parent)));
                arguments.Add(null);
            }

            if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("args", out var args))
            {
                foreach (var argument in args.EnumerateArray())
                {
                    names.Add(LowerCamelCase(argument.GetProperty("name").GetString()!));
                    types.Add(Map(argument));
                    arguments.Add(argument);
                }
            }

            var kinds = new ParameterKind[types.Count];
            var pairs = new string?[types.Count];
            for (var i = 0; i < types.Count; i++)
            {
                if (arguments[i] is not { } argument || kinds[i] != ParameterKind.None)
                {
                    continue;
                }

                var key = $"{nativeName}.{names[i]}";
                if (_config.Parameters.TryGetValue(key, out var rule))
                {
                    // Only Raw: dawn.json's own metadata decides every other rule.
                    _usedConfigKeys.Add(key);
                    if (rule.Kind != ParameterKind.None)
                    {
                        throw new MappingException($"{key}: only ParameterRule.Raw applies to {_config.ApiDescription}, whose metadata gives the other rules");
                    }

                    continue;
                }

                kinds[i] = Kind(argument, types[i]);
                if (kinds[i] == ParameterKind.Span)
                {
                    var count = names.IndexOf(LowerCamelCase(argument.GetProperty("length").GetString()!));
                    if (count < 0 || !types[count].IsInteger() || kinds[count] != ParameterKind.None)
                    {
                        throw new MappingException($"{key}: its length {argument.GetProperty("length")} is not an integer argument of its own");
                    }

                    kinds[count] = ParameterKind.Count;
                    pairs[i] = names[count];
                    pairs[count] = names[i];
                }
            }

            var parameters = new List<ParameterModel>(types.Count);
            for (var i = 0; i < types.Count; i++)
            {
                parameters.Add(new ParameterModel
                {
                    NativeName = names[i],
                    Name = ParameterName(nativeName, names[i]),
                    Type = types[i],
                    Kind = kinds[i],
                    Pair = pairs[i] is { } pair ? ParameterName(nativeName, pair) : null,
                });
            }

            var returns = json.ValueKind == JsonValueKind.Object ? Returns(json) : TypeRef.Void;
            var (supported, unsupported) = Platforms(owners);
            if (_config.UnsupportedPlatforms.TryGetValue(nativeName, out var configured))
            {
                // Declared by emdawnwebgpu's header too, but known not to work there.
                _usedConfigKeys.Add(nativeName);
                unsupported = [.. unsupported.Union(configured, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            }

            return new FunctionModel
            {
                NativeName = nativeName,
                Name = Rename(nativeName) ?? Naming.StripPrefix(nativeName, _config.Prefixes),
                Return = returns,
                Parameters = parameters,
                Group = group,
                Documentation = Noted(nativeName, Documentation.None),
                SupportedPlatforms = supported,
                UnsupportedPlatforms = unsupported,
            };
        }
        catch (MappingException e)
        {
            throw new MappingException($"{nativeName}: {e.Message}.");
        }
    }

    // How dawn.json's annotations prove a friendly overload (ADR-0006): a `length` naming another argument is a span, a
    // const pointer to one struct is `in`, a pointer to one struct is written by the callee, and a string view is UTF-8.
    private ParameterKind Kind(JsonElement argument, TypeRef type)
    {
        var annotation = Annotation(argument);
        var typeName = argument.GetProperty("type").GetString()!;
        if (annotation == "value")
        {
            return typeName == StringView ? ParameterKind.StringView : ParameterKind.None;
        }

        if (argument.TryGetProperty("length", out var length))
        {
            // A constant length (only on struct members) leaves a fixed-size array behind a raw pointer.
            return length.ValueKind == JsonValueKind.String && type.Element!.Kind != TypeKind.Pointer ? ParameterKind.Span : ParameterKind.None;
        }

        if (!_items.TryGetValue(typeName, out var item) || Category(item) is not ("structure" or "callback info"))
        {
            return ParameterKind.None;
        }

        // An extensible output struct carries its own nextInChain in, for the callee to fill the chained structs too: the
        // caller sets it up first, so it is `ref`, not an `out` that would clear it.
        return annotation switch
        {
            "const*" => ParameterKind.In,
            "*" => IsExtensible(item) ? ParameterKind.Ref : ParameterKind.Out,
            _ => ParameterKind.None,
        };
    }

    private string ParameterName(string function, string nativeName) => Rename($"{function}.{nativeName}") ?? Naming.Escape(nativeName);

    private EnumModel ReadEnum(string name, JsonElement item)
    {
        var isBitmask = Category(item) == "bitmask";
        var nativeName = CName(name);
        var members = new List<EnumMemberModel>();
        foreach (var value in item.GetProperty("values").EnumerateArray().Where(v => IsEnabled(v, s_headerTags)))
        {
            var valueName = value.GetProperty("name").GetString()!;
            var number = value.GetProperty("value").GetUInt64();
            if (!isBitmask)
            {
                // EnumType.__init__: each implementation's own values live in a range of their own, chosen by the raw tags.
                var tags = Tags(value);
                var range = tags.Contains("dawn") ? 0x0005_0000UL : tags.Contains("emscripten") ? 0x0004_0000UL : 0;
                number += range == 0 && tags.Contains("native") ? 0x0001_0000UL : range;
            }

            var memberName = $"{nativeName}_{CamelCase(valueName)}";
            var (supported, unsupported) = Platforms([item, value]);
            members.Add(new EnumMemberModel
            {
                NativeName = memberName,
                Name = Rename($"{nativeName}.{memberName}") ?? Identifier(CamelCase(valueName)),
                Value = number,
                Documentation = MemberDocumentation(memberName),
                SupportedPlatforms = supported,
                UnsupportedPlatforms = unsupported,
            });
        }

        foreach (var duplicate in members.GroupBy(m => m.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            throw new MappingException($"{string.Join(", ", duplicate.Select(m => m.NativeName))} all map to {duplicate.Key}; add Renames");
        }

        var (enumSupported, enumUnsupported) = Platforms([item]);
        return new EnumModel
        {
            NativeName = nativeName,
            Name = TypeName(name),
            // api.h: enums are 32-bit (they end with Force32 = 0x7FFFFFFF), bitmasks are typedefs of the 64-bit WGPUFlags.
            Underlying = isBitmask ? PrimitiveType.UInt64 : PrimitiveType.UInt32,
            IsFlags = isBitmask,
            Members = members,
            Documentation = Noted(nativeName, Documentation.None),
            SupportedPlatforms = enumSupported,
            UnsupportedPlatforms = enumUnsupported,
        };
    }

    private StructModel ReadStruct(string name, JsonElement item)
    {
        var nativeName = CName(name);
        var isCallbackInfo = Category(item) == "callback info";
        var chained = Direction(item, "chained");
        var fields = new List<FieldModel>();
        var chainedStruct = TypeRef.Named(_prefix + "ChainedStruct");

        // render_c_struct_definition() and the callback info template: what api.h adds around the members.
        if (isCallbackInfo || IsExtensible(item))
        {
            fields.Add(Field(nativeName, "nextInChain", TypeRef.PointerTo(chainedStruct, isConst: false)));
        }

        if (chained is not null)
        {
            fields.Add(Field(nativeName, "chain", chainedStruct));
        }

        foreach (var member in item.GetProperty("members").EnumerateArray().Where(m => IsEnabled(m, s_headerTags)))
        {
            // A member only some implementations have would give the struct a different layout in emdawnwebgpu.
            if (!IsEnabled(member, s_webTags) || !IsEnabled(member, s_dawnTags))
            {
                throw new MappingException($"member `{member.GetProperty("name").GetString()}` is tagged, so the struct's layout would differ between Dawn and emdawnwebgpu");
            }

            fields.Add(Field(nativeName, LowerCamelCase(member.GetProperty("name").GetString()!), Map(member)));
        }

        if (isCallbackInfo)
        {
            fields.Add(Field(nativeName, "userdata1", TypeRef.PointerTo(TypeRef.Void, isConst: false)));
            fields.Add(Field(nativeName, "userdata2", TypeRef.PointerTo(TypeRef.Void, isConst: false)));
        }

        var remarks = new List<DocBlock>();
        if (chained is not null)
        {
            var roots = item.GetProperty("chain roots").EnumerateArray().Select(r => $"`{TypeName(r.GetString()!)}`");
            remarks.Add(Paragraph($"Chains into the `NextInChain` of {string.Join(", ", roots)}, as `&value.Chain`. `new {TypeName(name)}()` sets its `Chain.SType`."));
        }

        var extensions = _items.Where(i => Direction(i.Value, "chained") is not null && i.Value.GetProperty("chain roots").EnumerateArray().Any(r => r.GetString() == name))
            .Select(i => $"`{TypeName(i.Key)}`")
            .Order(StringComparer.Ordinal)
            .ToList();
        if (extensions.Count > 0)
        {
            remarks.Add(Paragraph($"Its `NextInChain` accepts the `Chain` of {string.Join(", ", extensions)}."));
        }

        if (HasFreeMembers(item))
        {
            remarks.Add(Paragraph($"Release what the callee allocates in it with `{_prefix.ToLowerInvariant()}{CamelCase(name)}FreeMembers`."));
        }

        var (supported, unsupported) = Platforms([item]);
        return new StructModel
        {
            NativeName = nativeName,
            Name = TypeName(name),
            Fields = fields,
            Initializers = chained is null ? [] : [new FieldInitializer { Path = ["chain", "sType"], Member = $"{_prefix}SType_{CamelCase(name)}" }],
            Documentation = Noted(nativeName, new Documentation { Remarks = remarks }),
            SupportedPlatforms = supported,
            UnsupportedPlatforms = unsupported,
        };
    }

    // WGPUChainedStruct is written by api.h itself, not described in dawn.json.
    private StructModel ChainedStruct()
    {
        var nativeName = _prefix + "ChainedStruct";
        return new StructModel
        {
            NativeName = nativeName,
            Name = "ChainedStruct",
            Fields =
            [
                Field(nativeName, "next", TypeRef.PointerTo(TypeRef.Named(nativeName), isConst: false)),
                Field(nativeName, "sType", TypeRef.Named(_prefix + "SType")),
            ],
            Documentation = new Documentation
            {
                Remarks = [Paragraph("The first field of every chained struct: `SType` says which struct it is, `Next` points to the next one in the chain.")],
            },
        };
    }

    private HandleModel ReadHandle(string name, JsonElement item)
    {
        var (supported, unsupported) = Platforms([item]);
        return new HandleModel
        {
            NativeName = CName(name),
            Name = TypeName(name),
            Stem = CamelCase(name),
            IsPointerTypedef = true,
            Documentation = Noted(CName(name), Documentation.None),
            SupportedPlatforms = supported,
            UnsupportedPlatforms = unsupported,
        };
    }

    private ConstantModel ReadConstant(string name, JsonElement item)
    {
        var nativeName = $"{_prefix}_{SnakeCase(name)}";
        var type = MapBase(item.GetProperty("type").GetString()!);
        var value = item.GetProperty("value").GetString()!;
        var (supported, unsupported) = Platforms([item]);
        var constant = new ConstantModel
        {
            NativeName = nativeName,
            Name = Rename(nativeName) ?? Identifier(CamelCase(name)),
            Group = "Global",
            Type = type,
            Documentation = Noted(nativeName, Documentation.None),
            SupportedPlatforms = supported,
            UnsupportedPlatforms = unsupported,
        };

        // api.h pastes the value into a macro; these are the C library limits dawn.json uses. DawnHeaderCheck evaluates the
        // macros on every target.
        return (value, type.Primitive) switch
        {
            ("UINT32_MAX", PrimitiveType.UInt32) => With(constant, uint.MaxValue),
            ("UINT64_MAX", PrimitiveType.UInt64) => With(constant, ulong.MaxValue),
            ("SIZE_MAX", PrimitiveType.NUInt) => With(constant, ulong.MaxValue),
            ("NAN", PrimitiveType.Single) => new ConstantModel
            {
                NativeName = constant.NativeName,
                Name = constant.Name,
                Group = constant.Group,
                Type = type,
                Float = double.NaN,
                Documentation = constant.Documentation,
                SupportedPlatforms = supported,
                UnsupportedPlatforms = unsupported,
            },
            _ when type.IsInteger() && ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) => With(constant, number),
            _ => throw new MappingException($"value `{value}` of type {type.Describe()} is not one the generator knows"),
        };
    }

    private static ConstantModel With(ConstantModel constant, ulong value) => new()
    {
        NativeName = constant.NativeName,
        Name = constant.Name,
        Group = constant.Group,
        Type = constant.Type,
        Integer = value,
        Documentation = constant.Documentation,
        SupportedPlatforms = constant.SupportedPlatforms,
        UnsupportedPlatforms = constant.UnsupportedPlatforms,
    };

    private FieldModel Field(string owner, string nativeName, TypeRef type) => new()
    {
        NativeName = nativeName,
        Name = Rename($"{owner}.{nativeName}") ?? Identifier(Capitalize(nativeName)),
        Type = type,
        Documentation = Documentation.None,
    };

    // A record member or argument: its base type decorated by its annotation (decorate() in dawn_json_generator.py).
    private TypeRef Map(JsonElement member)
    {
        var element = MapBase(member.GetProperty("type").GetString()!);
        return Annotation(member) switch
        {
            "value" => element,
            "*" => TypeRef.PointerTo(element, isConst: false),
            "const*" => TypeRef.PointerTo(element, isConst: true),
            "const*const*" => TypeRef.PointerTo(TypeRef.PointerTo(element, isConst: true), isConst: true),
            var annotation => throw new MappingException($"annotation `{annotation}` is not one decorate() knows"),
        };
    }

    private TypeRef MapBase(string type)
    {
        if (type == "bool")
        {
            // as_cType(): dawn.json's bool is the WGPUBool typedef, a 32-bit integer (ADR-0012).
            var typedef = _prefix + "Bool";
            if (!_config.TypedefMappings.TryGetValue(typedef, out var mapped))
            {
                throw new MappingException($"{typedef} needs an entry in TypedefMappings");
            }

            _usedConfigKeys.Add(typedef);
            return TypeRef.Of(mapped);
        }

        if (s_natives.TryGetValue(type, out var native))
        {
            return native;
        }

        if (!_items.TryGetValue(type, out var item))
        {
            throw new MappingException($"type `{type}` is not an API type of dawn/webgpu.h");
        }

        if (Excluded(CName(type)))
        {
            throw new MappingException($"type `{type}` is excluded, but a bound declaration uses it");
        }

        return Category(item) switch
        {
            "enum" or "bitmask" or "structure" or "callback info" or "object" => TypeRef.Named(CName(type)),
            "function pointer" or "callback function" => FunctionPointer(type, item),
            "typedef" => MapBase(item.GetProperty("type").GetString()!),
            var category => throw new MappingException($"type `{type}` is a {category}, which has no C type"),
        };
    }

    // A callback typedef: api.h appends the two userdata pointers to every "callback function".
    private TypeRef FunctionPointer(string name, JsonElement item)
    {
        if (_functionPointers.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var parameters = new List<TypeRef>();
        var documentation = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var argument in item.GetProperty("args").EnumerateArray())
        {
            parameters.Add(Map(argument));
            var optional = argument.TryGetProperty("optional", out var flag) && flag.GetBoolean() ? ", may be null" : string.Empty;
            documentation.Add(LowerCamelCase(argument.GetProperty("name").GetString()!), $"`{CSpelling(argument)}`{optional}.");
        }

        if (Category(item) == "callback function")
        {
            foreach (var userdata in new[] { "userdata1", "userdata2" })
            {
                parameters.Add(TypeRef.PointerTo(TypeRef.Void, isConst: false));
                documentation.Add(userdata, $"`void*`, the `{userdata}` of the callback info.");
            }
        }

        var returns = Returns(item);
        var type = TypeRef.FunctionPointer(returns, parameters, CName(name));
        _functionPointers.Add(name, type);
        _callbacks.Add(CName(name), new Documentation
        {
            Summary = "a function pointer whose parameters are, in order:",
            Parameters = documentation,
            Returns = returns.Kind == TypeKind.Void ? null : $"`{ReturnSpelling(item)}`.",
        });
        return type;
    }

    // link_function(): "returns" is a type name, or a member-like object with its own annotation.
    private TypeRef Returns(JsonElement item)
    {
        if (!item.TryGetProperty("returns", out var returns))
        {
            return TypeRef.Void;
        }

        return returns.ValueKind == JsonValueKind.String ? MapBase(returns.GetString()!) : Map(returns);
    }

    private string ReturnSpelling(JsonElement item)
    {
        var returns = item.GetProperty("returns");
        return returns.ValueKind == JsonValueKind.String ? CSpelling(returns.GetString()!, "value") : CSpelling(returns);
    }

    // The C spelling of a member's type, for docs.
    private string CSpelling(JsonElement member) => CSpelling(member.GetProperty("type").GetString()!, Annotation(member));

    private string CSpelling(string type, string annotation)
    {
        var name = type == "bool" ? _prefix + "Bool" : s_natives.ContainsKey(type) ? type : CName(type);
        return annotation switch
        {
            "*" => name + " *",
            "const*" => name + " const *",
            "const*const*" => $"const {name}* const *",
            _ => name,
        };
    }

    // Every type a declaration uses exists wherever the declaration does; dawn_json_generator.py guarantees it for each tag
    // set, and this guards the availability read here.
    private void CheckAvailability(List<FunctionModel> functions, List<StructModel> structs)
    {
        void Check(string owner, bool web, bool dawn, TypeRef type)
        {
            switch (type.Kind)
            {
                case TypeKind.Pointer or TypeKind.FixedArray:
                    Check(owner, web, dawn, type.Element!);
                    break;

                case TypeKind.FunctionPointer:
                    Check(owner, web, dawn, type.Return!);
                    type.Parameters.ToList().ForEach(p => Check(owner, web, dawn, p));
                    break;

                case TypeKind.Named when _availability.TryGetValue(type.Name!, out var used) && ((web && !used.Web) || (dawn && !used.Dawn)):
                    _errors.Add($"{owner} uses {type.Name}, which does not exist everywhere {owner} does.");
                    break;
            }
        }

        foreach (var function in functions)
        {
            var (web, dawn) = (!function.UnsupportedPlatforms.Contains(Browser), function.SupportedPlatforms.Count == 0);
            Check(function.NativeName, web, dawn, function.Return);
            function.Parameters.ToList().ForEach(p => Check(function.NativeName, web, dawn, p.Type));
        }

        foreach (var structModel in structs)
        {
            var (web, dawn) = (!structModel.UnsupportedPlatforms.Contains(Browser), structModel.SupportedPlatforms.Count == 0);
            structModel.Fields.ToList().ForEach(f => Check(structModel.NativeName, web, dawn, f.Type));
        }
    }

    private static (IReadOnlyList<string> Supported, IReadOnlyList<string> Unsupported) Platforms(JsonElement[] owners)
    {
        var web = owners.All(o => IsEnabled(o, s_webTags));
        var dawn = owners.All(o => IsEnabled(o, s_dawnTags));
        return (web, dawn) switch
        {
            (true, true) => ([], []),
            (false, true) => ([], [Browser]),
            (true, false) => ([Browser], []),
            _ => throw new MappingException("neither Dawn nor emdawnwebgpu has it, yet dawn/webgpu.h declares it"),
        };
    }

    // item_is_enabled(). Its `tag not in ('art_experimental')` is a substring test, so it drops "art" as well as
    // "art_experimental" before matching; neither is ever enabled here.
    private static bool IsEnabled(JsonElement item, string[] enabled)
    {
        var tags = Tags(item);
        if (tags.Count == 0)
        {
            return true;
        }

        var kept = tags.Where(t => !"art_experimental".Contains(t, StringComparison.Ordinal)).ToList();
        return kept.Count > 0 && kept.Any(enabled.Contains);
    }

    private static List<string> Tags(JsonElement item) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty("tags", out var tags) ? [.. tags.EnumerateArray().Select(t => t.GetString()!)] : [];

    // An unknown tag means dawn.json changed meaning; Dawn's generator would assert on it too.
    private void CheckTags(string name, JsonElement item)
    {
        var tagged = new List<JsonElement> { item };
        foreach (var list in new[] { "members", "values", "methods" })
        {
            if (item.TryGetProperty(list, out var children))
            {
                tagged.AddRange(children.EnumerateArray());
            }
        }

        foreach (var tag in tagged.SelectMany(Tags).Where(t => !s_knownTags.Contains(t)).Distinct(StringComparer.Ordinal))
        {
            _errors.Add($"{name}: unknown tag `{tag}`.");
        }
    }

    private static string Category(JsonElement item) => item.GetProperty("category").GetString()!;

    private static string Annotation(JsonElement member) => member.TryGetProperty("annotation", out var annotation) ? annotation.GetString()! : "value";

    private static string? Direction(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.False)
        {
            return null;
        }

        return value.GetString() is "in" or "out" ? value.GetString() : throw new MappingException($"`{property}` must be \"in\" or \"out\"");
    }

    private static bool IsExtensible(JsonElement item) => Category(item) == "callback info" || Direction(item, "extensible") is not null;

    // StructureType.has_free_members_function.
    private static bool HasFreeMembers(JsonElement item)
    {
        var output = Direction(item, "chained") == "out" || Direction(item, "extensible") == "out" || (item.TryGetProperty("out", out var flag) && flag.GetBoolean());
        return output && item.GetProperty("members").EnumerateArray()
            .Where(m => IsEnabled(m, s_headerTags))
            .Any(m => Annotation(m) != "value" || m.GetProperty("type").GetString() == StringView);
    }

    private string CName(string name) => _prefix + CamelCase(name);

    private string TypeName(string name) => Rename(CName(name)) ?? Identifier(CamelCase(name));

    private string Identifier(string name) => char.IsDigit(name[0]) ? _config.LeadingDigitPrefix + name : name;

    private bool Excluded(string nativeName) => _config.Exclusions.ContainsKey(nativeName);

    private bool Exclude(string nativeName)
    {
        if (!Excluded(nativeName))
        {
            return false;
        }

        _usedConfigKeys.Add(nativeName);
        return true;
    }

    private string? Rename(string key)
    {
        if (_config.Renames.TryGetValue(key, out var name))
        {
            _usedConfigKeys.Add(key);
            return name;
        }

        return null;
    }

    private Documentation Noted(string nativeName, Documentation documentation)
    {
        if (!_config.Notes.TryGetValue(nativeName, out var note))
        {
            return documentation;
        }

        _usedConfigKeys.Add(nativeName);
        return new Documentation { Summary = documentation.Summary, Remarks = [.. documentation.Remarks, Paragraph(note)], Parameters = documentation.Parameters, Returns = documentation.Returns };
    }

    // An enum member's documentation is its summary alone.
    private Documentation MemberDocumentation(string nativeName)
    {
        if (!_config.Notes.TryGetValue(nativeName, out var note))
        {
            return Documentation.None;
        }

        _usedConfigKeys.Add(nativeName);
        return new Documentation { Summary = $"Binds `{nativeName}`. {note}" };
    }

    private static DocBlock Paragraph(string text) => new(DocBlockKind.Paragraph, [text]);

    private static string Capitalize(string chunk) => chunk.Length == 0 ? chunk : char.ToUpperInvariant(chunk[0]) + chunk[1..];

    private static string LowerCamelCase(string canonical)
    {
        var chunks = canonical.Split(' ');
        return chunks[0] + string.Concat(chunks.Skip(1).Select(Capitalize));
    }

    private static string SnakeCase(string canonical) => string.Join('_', canonical.Split(' ').Select(c => c.ToUpperInvariant()));

    // Name.__lt__: concatenated words, lower-cased, compared by code point.
    private static string SortKey(string canonical) => canonical.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
}
