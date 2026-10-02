using System.Text;
using System.Text.RegularExpressions;
using ClangSharp;
using ClangSharp.Interop;

/// <summary>
/// Reads a C library's staged headers through libclang into a <see cref="TargetModel"/>, for one target triple.
/// The headers are parsed freestanding (<c>-nostdinc</c> plus the stubs in <c>sysroot/</c>), so every triple
/// parses on any host without its SDK.
/// </summary>
internal sealed partial class ClangReader
{
    private const string MainFile = "jade_bindings.c";

    private readonly LibraryConfig config;
    private readonly Target target;
    private readonly string includeDirectory;
    private readonly string libraryDirectory;
    private readonly List<string> errors = [];
    private readonly List<FunctionDecl> functions = [];
    private readonly HashSet<string> functionNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RecordDecl> records = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EnumDecl> enums = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypedefDecl> typedefs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MacroDefinitionRecord> macros = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ulong> probes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> fileLines = new(StringComparer.Ordinal);
    private readonly HashSet<string> usedConfigKeys = new(StringComparer.Ordinal);

    private ClangReader(LibraryConfig config, Target target, string includeDirectory)
    {
        this.config = config;
        this.target = target;
        this.includeDirectory = Path.GetFullPath(includeDirectory);
        libraryDirectory = Path.Combine(this.includeDirectory, config.IncludeDirectory) + Path.DirectorySeparatorChar;
    }

    /// <summary>Parses the library for one target.</summary>
    /// <param name="index">The libclang index, shared by every parse.</param>
    /// <param name="config">The library config.</param>
    /// <param name="target">The target to parse for.</param>
    /// <param name="includeDirectory">The staged <c>include/</c> folder.</param>
    /// <param name="sysroot">The folder of stub system headers.</param>
    /// <param name="defines">Upstream build defines that shape the public API, from <c>versions.json</c>.</param>
    /// <returns>The model and the native facts it is checked against.</returns>
    /// <exception cref="InvalidOperationException">The headers do not parse, or the config does not cover a declaration.</exception>
    public static TargetModel Read(CXIndex index, LibraryConfig config, Target target, string includeDirectory, string sysroot, IReadOnlyList<string> defines)
    {
        var reader = new ClangReader(config, target, includeDirectory);
        return reader.Read(index, sysroot, defines);
    }

    private TargetModel Read(CXIndex index, string sysroot, IReadOnlyList<string> defines)
    {
        string[] arguments =
        [
            "-x", "c", "-std=c11", $"--target={target.Triple}", "-nostdinc", "-isystem", Path.GetFullPath(sysroot), "-I", includeDirectory,
            .. defines.Select(d => "-D" + d),
        ];
        using var source = CXUnsavedFile.Create(MainFile, MainSource());
        var options = CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies | CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord;
        var result = CXTranslationUnit.TryParse(index, MainFile, arguments, [source], options, out var handle);
        if (result != CXErrorCode.CXError_Success)
        {
            throw new InvalidOperationException($"{config.Name} on {target.Rid}: libclang failed to parse ({result}).");
        }

        using var translationUnit = TranslationUnit.GetOrCreate(handle);
        CheckDiagnostics(handle);
        Index(translationUnit);

        var pointerSize = (int)Probe("jade_probe_pointer");
        var longSize = (int)Probe("jade_probe_long");
        var mapper = new TypeMapper(config, IsLibraryDeclaration, pointerSize, longSize);
        var functionModels = ReadFunctions(mapper);
        var (enumModels, structModels, handleModels, layouts) = ReadTypes(mapper);
        CheckConfigUse(mapper);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"{config.Name} on {target.Rid}:\n  {string.Join("\n  ", errors)}");
        }

        var model = new LibraryModel
        {
            Name = config.Name,
            Namespace = $"Jade.Interop.{config.Name}",
            FunctionsClass = config.FunctionsClass,
            Groups = [.. config.Headers.Select(h => KeyValuePair.Create(h.Group, h.Path))],
            Functions = functionModels,
            Enums = [.. enumModels.OrderBy(e => e.Name, StringComparer.Ordinal)],
            Structs = [.. structModels.OrderBy(s => s.Name, StringComparer.Ordinal)],
            Handles = [.. handleModels.OrderBy(h => h.Name, StringComparer.Ordinal)],
        };
        return new TargetModel { Target = target, Model = model, Layouts = layouts, PointerSize = pointerSize, LongSize = longSize };
    }

    // The bound headers, then probes: target sizes, and every object-like macro that may hold a flag value.
    // Each macro probe is guarded, so a macro missing on one target only drops that member there, which the
    // cross-target comparison then reports.
    private string MainSource()
    {
        var builder = new StringBuilder();
        foreach (var header in config.Headers)
        {
            builder.Append("#include <").Append(header.Path).Append(">\n");
        }

        builder.Append("static const unsigned long long jade_probe_pointer = sizeof(void *);\n");
        builder.Append("static const unsigned long long jade_probe_long = sizeof(long);\n");
        foreach (var macro in FlagMacroCandidates())
        {
            builder.Append("#ifdef ").Append(macro).Append('\n');
            builder.Append("static const unsigned long long jade_macro_").Append(macro).Append(" = (unsigned long long)(").Append(macro).Append(");\n");
            builder.Append("#endif\n");
        }

        return builder.ToString();
    }

    private SortedSet<string> FlagMacroCandidates()
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        if (config.FlagMacros.Count == 0)
        {
            return names;
        }

        foreach (var file in Directory.EnumerateFiles(libraryDirectory, "*.h"))
        {
            foreach (Match match in DefinePattern().Matches(File.ReadAllText(file)))
            {
                var name = match.Groups["name"].Value;
                if (config.FlagMacros.Values.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    private void CheckDiagnostics(CXTranslationUnit handle)
    {
        var messages = new List<string>();
        for (uint i = 0; i < handle.NumDiagnostics; i++)
        {
            using var diagnostic = handle.GetDiagnostic(i);
            if (diagnostic.Severity >= CXDiagnosticSeverity.CXDiagnostic_Error)
            {
                using var text = diagnostic.Format(CXDiagnosticDisplayOptions.CXDiagnostic_DisplaySourceLocation);
                messages.Add(text.ToString());
            }
        }

        if (messages.Count > 0)
        {
            throw new InvalidOperationException($"{config.Name} on {target.Rid}: the headers do not parse. Add what is missing to scripts/generate-bindings/sysroot/.\n  {string.Join("\n  ", messages)}");
        }
    }

    private void Index(TranslationUnit translationUnit)
    {
        foreach (var cursor in translationUnit.TranslationUnitDecl.CursorChildren)
        {
            switch (cursor)
            {
                case MacroDefinitionRecord macro when !macro.IsFunctionLike && IsLibraryCursor(cursor):
                    macros.TryAdd(macro.Name, macro);
                    break;

                case VarDecl variable when variable.Location.IsFromMainFile:
                    using (var evaluation = variable.Handle.Evaluate)
                    {
                        if (evaluation.Kind == CXEvalResultKind.CXEval_Int)
                        {
                            probes[variable.Name] = evaluation.AsUnsigned;
                        }
                    }

                    break;

                case FunctionDecl function when IsLibraryCursor(cursor):
                    if (functionNames.Add(function.Name))
                    {
                        functions.Add(function);
                    }

                    break;

                case RecordDecl record when record.IsCompleteDefinition && IsLibraryCursor(cursor):
                    records.TryAdd(TypeMapper.NameOf(record), record);
                    break;

                case EnumDecl enumDecl when enumDecl.IsCompleteDefinition && IsLibraryCursor(cursor):
                    enums.TryAdd(TypeMapper.NameOf(enumDecl), enumDecl);
                    break;

                case TypedefDecl typedef when IsLibraryCursor(cursor):
                    typedefs.TryAdd(typedef.Name, typedef);
                    break;
            }
        }
    }

    private List<FunctionModel> ReadFunctions(TypeMapper mapper)
    {
        var models = new List<FunctionModel>();
        foreach (var function in functions)
        {
            var header = config.Headers.FirstOrDefault(h => h.Path == RelativePath(function));
            if (header is null || header.Functions?.IsMatch(function.Name) == false)
            {
                continue;
            }

            // Inline and static functions have no exported symbol.
            if (function.HasBody || function.IsInlined || function.StorageClass == CX_StorageClass.CX_SC_Static)
            {
                continue;
            }

            if (config.Exclusions.ContainsKey(function.Name))
            {
                usedConfigKeys.Add(function.Name);
                continue;
            }

            if (function.IsVariadic)
            {
                errors.Add($"{function.Name} is variadic, which P/Invoke cannot call portably: add it to Exclusions with a reason.");
                continue;
            }

            try
            {
                models.Add(ReadFunction(function, header, mapper));
            }
            catch (MappingException e)
            {
                errors.Add($"{function.Name}: {e.Message}. Add it to Exclusions with a reason, or teach the generator.");
            }
        }

        return models;
    }

    private FunctionModel ReadFunction(FunctionDecl function, HeaderConfig header, TypeMapper mapper)
    {
        var functionType = function.Type;
        while (functionType is not FunctionType && functionType.IsSugared)
        {
            functionType = functionType.Desugar;
        }

        if (functionType is FunctionType { CallConv: not CXCallingConv.CXCallingConv_C } typed)
        {
            throw new MappingException($"calling convention {typed.CallConv} is not cdecl");
        }

        var returnType = mapper.Map(function.ReturnType);
        var types = function.Parameters.Select(p => mapper.Map(p.Type)).ToList();
        var nativeNames = function.Parameters.Select((p, i) => p.Name.Length > 0 ? p.Name : $"arg{i}").ToList();
        var kinds = new ParameterKind[types.Count];
        var pairs = new string?[types.Count];

        for (var i = 0; i < types.Count; i++)
        {
            var key = $"{function.Name}.{nativeNames[i]}";
            if (config.Parameters.TryGetValue(key, out var rule))
            {
                usedConfigKeys.Add(key);
                CheckRule(key, rule, types[i]);
                kinds[i] = rule.Kind;
                if (rule.Kind == ParameterKind.Span)
                {
                    var count = nativeNames.IndexOf(rule.Count!);
                    if (count < 0 || types[count].Kind != TypeKind.Primitive || types[count].Primitive is PrimitiveType.Single or PrimitiveType.Double)
                    {
                        throw new MappingException($"{key}: count parameter {rule.Count} is missing or not an integer");
                    }

                    kinds[count] = ParameterKind.Count;
                    pairs[i] = rule.Count;
                    pairs[count] = nativeNames[i];
                }
            }
            else if (kinds[i] == ParameterKind.None && config.Utf8Strings && types[i] is { IsPointer: true, IsConst: true } pointer && pointer.Element!.Is(PrimitiveType.Char))
            {
                kinds[i] = ParameterKind.Utf8String;
            }
        }

        var parameters = new List<ParameterModel>(types.Count);
        for (var i = 0; i < types.Count; i++)
        {
            parameters.Add(new ParameterModel
            {
                NativeName = nativeNames[i],
                Name = Rename($"{function.Name}.{nativeNames[i]}") ?? Naming.Camel(nativeNames[i], config.Words),
                Type = types[i],
                Kind = kinds[i],
                Pair = pairs[i] is { } pair ? Rename($"{function.Name}.{pair}") ?? Naming.Camel(pair, config.Words) : null,
            });
        }

        IReadOnlyList<string> platforms = [];
        if (config.UnsupportedPlatforms.TryGetValue(function.Name, out var unsupported))
        {
            usedConfigKeys.Add(function.Name);
            platforms = unsupported;
        }

        return new FunctionModel
        {
            NativeName = function.Name,
            Name = Rename(function.Name) ?? Naming.Pascal(Naming.StripPrefix(function.Name, config.Prefixes), config.Words),
            Return = returnType,
            Parameters = parameters,
            Group = header.Group,
            Documentation = DocumentationOf(function),
            UnsupportedPlatforms = platforms,
        };
    }

    private static void CheckRule(string key, ParameterRule rule, TypeRef type)
    {
        var valid = rule.Kind switch
        {
            ParameterKind.None => true,
            ParameterKind.In => type is { IsPointer: true, IsConst: true } && type.Element!.Kind != TypeKind.Void,
            ParameterKind.Out or ParameterKind.Ref => type is { IsPointer: true, IsConst: false } && type.Element!.Kind != TypeKind.Void,
            ParameterKind.Span => type.IsPointer && type.Element!.Kind != TypeKind.Void,
            _ => false,
        };
        if (!valid)
        {
            throw new MappingException($"{key}: rule {rule.Kind} does not fit its C type");
        }
    }

    private (List<EnumModel>, List<StructModel>, List<HandleModel>, Dictionary<string, RecordLayout>) ReadTypes(TypeMapper mapper)
    {
        var enumModels = new List<EnumModel>();
        var structModels = new List<StructModel>();
        var handleModels = new List<HandleModel>();
        var layouts = new Dictionary<string, RecordLayout>(StringComparer.Ordinal);

        // Reading a struct maps its fields, which can append references while this loop runs.
        for (var i = 0; i < mapper.References.Count; i++)
        {
            var reference = mapper.References[i];
            try
            {
                switch (reference.Kind)
                {
                    case ReferenceKind.Enum:
                        enumModels.Add(ReadEnum(enums[reference.Name]));
                        break;

                    case ReferenceKind.FlagMacros:
                        enumModels.Add(ReadFlagMacros(reference.Name, mapper));
                        break;

                    case ReferenceKind.Struct:
                        structModels.Add(ReadStruct(records[reference.Name], mapper, layouts));
                        break;

                    case ReferenceKind.Handle:
                        handleModels.Add(new HandleModel { NativeName = reference.Name, Name = TypeName(reference.Name), Documentation = TypeDocumentation(reference.Name) });
                        break;

                    case ReferenceKind.Opaque:
                        structModels.Add(new StructModel { NativeName = reference.Name, Name = TypeName(reference.Name), IsOpaque = true, Fields = [], Documentation = TypeDocumentation(reference.Name) });
                        break;
                }
            }
            catch (MappingException e)
            {
                errors.Add($"{reference.Name}: {e.Message}.");
            }
        }

        return (enumModels, structModels, handleModels, layouts);
    }

    private EnumModel ReadEnum(EnumDecl declaration)
    {
        var name = TypeMapper.NameOf(declaration);
        var constants = declaration.Enumerators;
        var prefix = Naming.CommonPrefix([.. constants.Select(c => c.Name)]);
        var members = constants.Select(c => new EnumMemberModel
        {
            NativeName = c.Name,
            Name = Rename($"{name}.{c.Name}") ?? Naming.Pascal(c.Name[prefix.Length..], config.Words),
            Value = c.IsNegative ? unchecked((ulong)c.InitVal) : c.UnsignedInitVal,
            Documentation = DocumentationOf(c),
        }).ToList();

        var isFlags = config.FlagEnums.Contains(name);
        if (isFlags)
        {
            usedConfigKeys.Add(name);
        }

        var size = (int)declaration.IntegerType.Handle.SizeOf;
        var negative = constants.Any(c => c.IsNegative);
        var max = constants.Where(c => !c.IsNegative).Select(c => c.UnsignedInitVal).DefaultIfEmpty(0UL).Max();
        var signedMax = (1UL << ((size * 8) - 1)) - 1;
        var underlying = (size, negative || (!isFlags && max <= signedMax)) switch
        {
            (1, true) => PrimitiveType.SByte,
            (1, false) => PrimitiveType.Byte,
            (2, true) => PrimitiveType.Int16,
            (2, false) => PrimitiveType.UInt16,
            (4, true) => PrimitiveType.Int32,
            (4, false) => PrimitiveType.UInt32,
            (8, true) => PrimitiveType.Int64,
            (8, false) => PrimitiveType.UInt64,
            _ => throw new MappingException($"enum size {size} is not supported"),
        };

        return new EnumModel { NativeName = name, Name = TypeName(name), Underlying = underlying, IsFlags = isFlags, Members = Unique(name, members), Documentation = TypeDocumentation(name) };
    }

    // A typedef such as `typedef Uint32 SDL_InitFlags;` followed by `#define SDL_INIT_VIDEO 0x20u` lines.
    private EnumModel ReadFlagMacros(string name, TypeMapper mapper)
    {
        usedConfigKeys.Add(name);
        var typedef = typedefs[name];
        var underlying = mapper.Map(typedef.UnderlyingType);
        if (underlying.Kind != TypeKind.Primitive || underlying.Primitive is PrimitiveType.Single or PrimitiveType.Double or PrimitiveType.Char)
        {
            throw new MappingException("a flags typedef must be an integer");
        }

        var prefix = config.FlagMacros[name];
        var header = FilePath(typedef);
        var members = new List<EnumMemberModel>();
        foreach (var (macroName, macro) in macros.Where(m => m.Key.StartsWith(prefix, StringComparison.Ordinal) && FilePath(m.Value) == header).OrderBy(m => Line(m.Value)))
        {
            if (config.Exclusions.ContainsKey(macroName))
            {
                usedConfigKeys.Add(macroName);
                continue;
            }

            if (!probes.TryGetValue("jade_macro_" + macroName, out var value))
            {
                throw new MappingException($"flag macro {macroName} does not evaluate to an integer: add it to Exclusions with a reason");
            }

            members.Add(new EnumMemberModel
            {
                NativeName = macroName,
                Name = Rename($"{name}.{macroName}") ?? Naming.Pascal(macroName[prefix.Length..], config.Words),
                Value = value,
                Documentation = TrailingComment(macro),
            });
        }

        if (members.Count == 0)
        {
            throw new MappingException($"no macro starting with {prefix} in {RelativePath(typedef)}");
        }

        return new EnumModel { NativeName = name, Name = TypeName(name), Underlying = underlying.Primitive, IsFlags = true, Members = Unique(name, members), Documentation = DocumentationOf(typedef) };
    }

    private StructModel ReadStruct(RecordDecl declaration, TypeMapper mapper, Dictionary<string, RecordLayout> layouts)
    {
        var name = TypeMapper.NameOf(declaration);
        IEnumerable<FieldDecl> fields = declaration.Fields;
        if (config.UnionMembers.TryGetValue(name, out var kept))
        {
            usedConfigKeys.Add(name);
            if (!declaration.IsUnion)
            {
                throw new MappingException("UnionMembers only applies to unions; dropping a struct field would break its layout");
            }

            var missing = kept.Where(k => declaration.Fields.All(f => f.Name != k)).ToList();
            if (missing.Count > 0)
            {
                throw new MappingException($"UnionMembers names unknown members {string.Join(", ", missing)}");
            }

            fields = declaration.Fields.Where(f => kept.Contains(f.Name));
        }

        var fieldModels = new List<FieldModel>();
        var offsets = new List<long>();
        foreach (var field in fields)
        {
            if (field.IsBitField || field.IsAnonymousField || field.Name.Length == 0)
            {
                throw new MappingException($"field {field.Name} is a bit-field or anonymous member, which the generator does not support yet");
            }

            var type = mapper.Map(field.Type);
            if (type.Kind == TypeKind.FixedArray && type.Element!.Kind != TypeKind.Primitive)
            {
                throw new MappingException($"field {field.Name} is an array of non-scalars, which needs an [InlineArray] type the generator does not emit yet");
            }

            fieldModels.Add(new FieldModel
            {
                NativeName = field.Name,
                Name = Rename($"{name}.{field.Name}") ?? Naming.Pascal(field.Name, config.Words),
                Type = type,
                Documentation = DocumentationOf(field),
            });
            offsets.Add(field.Handle.OffsetOfField / 8);
        }

        var recordType = declaration.TypeForDecl.Handle;
        layouts[name] = new RecordLayout(recordType.SizeOf, recordType.AlignOf, offsets);
        return new StructModel { NativeName = name, Name = TypeName(name), IsUnion = declaration.IsUnion, Fields = fieldModels, Documentation = TypeDocumentation(name) };
    }

    // A config entry that matches nothing is either stale or misspelled; both would silently change the output later.
    private void CheckConfigUse(TypeMapper mapper)
    {
        foreach (var reference in mapper.References)
        {
            usedConfigKeys.Add(reference.Name);
        }

        foreach (var (name, decision) in config.LayoutDecisions.Where(d => d.Value.Kind != LayoutDecisionKind.Opaque))
        {
            errors.Add($"layout decision {decision.Kind} for {name} is not implemented yet; only Opaque is.");
        }

        var keys = config.Exclusions.Keys
            .Concat(config.Parameters.Keys)
            .Concat(config.FlagMacros.Keys)
            .Concat(config.FlagEnums)
            .Concat(config.UnionMembers.Keys)
            .Concat(config.UnsupportedPlatforms.Keys)
            .Concat(config.Renames.Keys)
            .Concat(config.Handles)
            .Concat(config.OpaqueStructs)
            .Concat(config.LayoutDecisions.Keys);
        foreach (var key in keys.Where(k => !usedConfigKeys.Contains(k)).Order(StringComparer.Ordinal))
        {
            errors.Add($"config entry {key} matches nothing that is bound: remove it or fix its name.");
        }
    }

    private List<EnumMemberModel> Unique(string name, List<EnumMemberModel> members)
    {
        foreach (var duplicate in members.GroupBy(m => m.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            errors.Add($"{name}: {string.Join(", ", duplicate.Select(m => m.NativeName))} all map to {duplicate.Key}; add Renames.");
        }

        return members;
    }

    private string TypeName(string nativeName) => Rename(nativeName) ?? Naming.Pascal(Naming.StripPrefix(nativeName, config.Prefixes), config.Words);

    private string? Rename(string key)
    {
        if (config.Renames.TryGetValue(key, out var name))
        {
            usedConfigKeys.Add(key);
            return name;
        }

        return null;
    }

    // Records and enums declared through a typedef carry the comment on either the tag or the typedef.
    private Documentation TypeDocumentation(string name)
    {
        Cursor? tag = records.TryGetValue(name, out var record) ? record : enums.TryGetValue(name, out var enumDecl) ? enumDecl : null;
        var documentation = tag is null ? Documentation.None : DocumentationOf(tag);
        return documentation.Summary is null && typedefs.TryGetValue(name, out var typedef) ? DocumentationOf(typedef) : documentation;
    }

    private static Documentation DocumentationOf(Cursor cursor)
    {
        using var raw = cursor.Handle.RawCommentText;
        return DocCommentParser.Parse(raw.ToString());
    }

    // libclang attaches no comment to macros; SDL documents flag macros with a trailing /**< ... */.
    private Documentation TrailingComment(MacroDefinitionRecord macro)
    {
        var path = FilePath(macro);
        if (!fileLines.TryGetValue(path, out var lines))
        {
            lines = File.ReadAllLines(path);
            fileLines[path] = lines;
        }

        var match = TrailingCommentPattern().Match(lines[Line(macro) - 1]);
        return match.Success ? DocCommentParser.Parse(match.Value) : Documentation.None;
    }

    private ulong Probe(string name) =>
        probes.TryGetValue(name, out var value) ? value : throw new InvalidOperationException($"{config.Name} on {target.Rid}: probe {name} did not evaluate.");

    private bool IsLibraryDeclaration(Decl declaration) => IsLibraryCursor(declaration);

    private bool IsLibraryCursor(Cursor cursor) => FilePath(cursor).StartsWith(libraryDirectory, StringComparison.Ordinal);

    private static string FilePath(Cursor cursor)
    {
        cursor.Location.GetFileLocation(out var file, out _, out _, out _);
        using var name = file.Name;
        var path = name.ToString();
        return path.Length == 0 ? string.Empty : Path.GetFullPath(path);
    }

    private static int Line(Cursor cursor)
    {
        cursor.Location.GetFileLocation(out _, out var line, out _, out _);
        return (int)line;
    }

    private string RelativePath(Cursor cursor) => Path.GetRelativePath(includeDirectory, FilePath(cursor)).Replace('\\', '/');

    [GeneratedRegex(@"^[ \t]*#[ \t]*define[ \t]+(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]", RegexOptions.Multiline)]
    private static partial Regex DefinePattern();

    [GeneratedRegex(@"/\*\*<.*?\*/")]
    private static partial Regex TrailingCommentPattern();
}
