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
    private const string MacroProbe = "jade_macro_";

    private readonly LibraryConfig _config;
    private readonly Target _target;
    private readonly string _includeDirectory;
    private readonly string _libraryDirectory;
    private readonly Dictionary<string, HeaderConfig> _boundHeaders;
    private readonly List<string> _errors = [];
    private readonly List<FunctionDecl> _functions = [];
    private readonly HashSet<string> _functionNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RecordDecl> _records = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EnumDecl> _enums = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypedefDecl> _typedefs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MacroDefinitionRecord> _macros = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ulong> _probes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VarDecl> _macroProbes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _fileLines = new(StringComparer.Ordinal);
    private readonly HashSet<string> _usedConfigKeys = new(StringComparer.Ordinal);

    private ClangReader(LibraryConfig config, Target target, string includeDirectory)
    {
        _config = config;
        _target = target;
        _includeDirectory = Path.GetFullPath(includeDirectory);
        _libraryDirectory = Path.Combine(_includeDirectory, config.IncludeDirectory) + Path.DirectorySeparatorChar;
        _boundHeaders = config.Headers.ToDictionary(h => h.Path, StringComparer.Ordinal);
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
        CheckHeaders();
        string[] arguments =
        [
            "-x", "c", "-std=c11", $"--target={_target.Triple}", "-nostdinc", "-isystem", Path.GetFullPath(sysroot), "-I", _includeDirectory,
            "-ferror-limit=0",
            .. defines.Select(d => "-D" + d),
        ];

        // A macro is a constant if `__typeof__(M) probe = M;` compiles and evaluates. The first parse probes every
        // object-like macro of the bound headers and ignores the errors of those that are not constants; the second
        // only probes the valid ones, so any error left is a real one.
        var constants = ProbeMacros(index, arguments, MacroCandidates());

        using var translationUnit = Parse(index, arguments, MainSource(constants));
        CheckDiagnostics(translationUnit.Handle);
        Index(translationUnit);

        var pointerSize = (int)Probe("jade_probe_pointer");
        var longSize = (int)Probe("jade_probe_long");
        var mapper = new TypeMapper(_config, IsLibraryDeclaration, pointerSize, longSize);
        var (functionModels, excludedFunctions) = ReadFunctions(mapper);
        var consumed = new HashSet<string>(StringComparer.Ordinal);
        var constantModels = ReadConstants(mapper, functionModels, consumed);
        var (enumModels, structModels, handleModels, layouts) = ReadTypes(mapper);
        var callbacks = mapper.Callbacks.ToDictionary(c => c.Key, c => DocumentationOf(c.Value), StringComparer.Ordinal);
        RecordConfigUse(mapper);

        if (_errors.Count > 0)
        {
            throw new InvalidOperationException($"{_config.Name} on {_target.Rid}:\n  {string.Join("\n  ", _errors)}");
        }

        var model = new LibraryModel
        {
            Name = _config.Name,
            Namespace = $"Jade.Interop.{_config.Name}",
            FunctionsClass = _config.FunctionsClass,
            Groups = [.. _config.Headers.Select(h => KeyValuePair.Create(h.Group, h.Path))],
            Functions = functionModels,
            Enums = [.. enumModels.OrderBy(e => e.Name, StringComparer.Ordinal)],
            Structs = [.. structModels.OrderBy(s => s.Name, StringComparer.Ordinal)],
            Handles = [.. handleModels.OrderBy(h => h.Name, StringComparer.Ordinal)],
            Constants = constantModels,
            Callbacks = callbacks,
        };
        return new TargetModel
        {
            Target = _target,
            Model = model,
            Layouts = layouts,
            PointerSize = pointerSize,
            LongSize = longSize,
            ExcludedFunctions = excludedFunctions,
            UsedConfigKeys = _usedConfigKeys,
        };
    }

    // Every header of the library is either bound or excluded with a reason, so a new upstream header cannot go unnoticed.
    private void CheckHeaders()
    {
        var files = Directory.EnumerateFiles(_libraryDirectory, "*.h").Select(f => $"{_config.IncludeDirectory}/{Path.GetFileName(f)}").ToHashSet(StringComparer.Ordinal);
        var configured = _config.Headers.Select(h => h.Path).Concat(_config.ExcludedHeaders.Keys).ToList();
        foreach (var file in files.Where(f => !configured.Contains(f)).Order(StringComparer.Ordinal))
        {
            _errors.Add($"header {file} is neither bound nor excluded: add it to Headers or ExcludedHeaders.");
        }

        foreach (var header in configured.Where(h => !files.Contains(h)).Order(StringComparer.Ordinal))
        {
            _errors.Add($"configured header {header} does not exist.");
        }

        foreach (var duplicate in configured.GroupBy(h => h, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            _errors.Add($"header {duplicate.Key} is configured twice.");
        }

        if (_errors.Count > 0)
        {
            throw new InvalidOperationException($"{_config.Name}:\n  {string.Join("\n  ", _errors)}");
        }
    }

    private TranslationUnit Parse(CXIndex index, string[] arguments, string source)
    {
        using var file = CXUnsavedFile.Create(MainFile, source);
        var options = CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies | CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord;
        var result = CXTranslationUnit.TryParse(index, MainFile, arguments, [file], options, out var handle);
        if (result != CXErrorCode.CXError_Success)
        {
            throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: libclang failed to parse ({result}).");
        }

        return TranslationUnit.GetOrCreate(handle);
    }

    private List<string> ProbeMacros(CXIndex index, string[] arguments, IReadOnlyCollection<string> candidates)
    {
        using var translationUnit = Parse(index, arguments, MainSource(candidates));
        var valid = new List<string>();
        foreach (var cursor in translationUnit.TranslationUnitDecl.CursorChildren)
        {
            if (cursor is VarDecl variable && variable.Location.IsFromMainFile && variable.Name.StartsWith(MacroProbe, StringComparison.Ordinal)
                && !variable.Handle.IsInvalidDeclaration)
            {
                using var evaluation = variable.Handle.Evaluate;
                if (evaluation.Kind is CXEvalResultKind.CXEval_Int or CXEvalResultKind.CXEval_Float or CXEvalResultKind.CXEval_StrLiteral)
                {
                    valid.Add(variable.Name[MacroProbe.Length..]);
                }
            }
        }

        return valid;
    }

    // The bound and excluded headers, then probes: target sizes, and the macros to evaluate. Each macro probe is
    // guarded, so a macro missing on one target only drops it there, which the cross-target comparison then reports.
    private string MainSource(IEnumerable<string> macros)
    {
        var builder = new StringBuilder();
        foreach (var header in _config.Headers.Select(h => h.Path).Concat(_config.ExcludedHeaders.Keys))
        {
            builder.Append("#include <").Append(header).Append(">\n");
        }

        builder.Append("static const unsigned long long jade_probe_pointer = sizeof(void *);\n");
        builder.Append("static const unsigned long long jade_probe_long = sizeof(long);\n");
        foreach (var macro in macros)
        {
            builder.Append("#ifdef ").Append(macro).Append('\n');
            builder.Append("static const __typeof__(").Append(macro).Append(") ").Append(MacroProbe).Append(macro).Append(" = ").Append(macro).Append(";\n");
            builder.Append("#endif\n");
        }

        return builder.ToString();
    }

    private SortedSet<string> MacroCandidates()
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var header in _config.Headers)
        {
            foreach (Match match in DefinePattern().Matches(File.ReadAllText(Path.Combine(_includeDirectory, header.Path))))
            {
                names.Add(match.Groups["name"].Value);
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
            throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: the headers do not parse. Add what is missing to scripts/generate-bindings/sysroot/.\n  {string.Join("\n  ", messages)}");
        }
    }

    private void Index(TranslationUnit translationUnit)
    {
        foreach (var cursor in translationUnit.TranslationUnitDecl.CursorChildren)
        {
            switch (cursor)
            {
                case MacroDefinitionRecord macro when !macro.IsFunctionLike && IsLibraryCursor(cursor):
                    _macros[macro.Name] = macro;
                    break;

                case VarDecl variable when variable.Location.IsFromMainFile && variable.Name.StartsWith(MacroProbe, StringComparison.Ordinal):
                    _macroProbes[variable.Name[MacroProbe.Length..]] = variable;
                    break;

                case VarDecl variable when variable.Location.IsFromMainFile:
                    using (var evaluation = variable.Handle.Evaluate)
                    {
                        if (evaluation.Kind == CXEvalResultKind.CXEval_Int)
                        {
                            _probes[variable.Name] = evaluation.AsUnsigned;
                        }
                    }

                    break;

                case FunctionDecl function when IsLibraryCursor(cursor):
                    if (_functionNames.Add(function.Name))
                    {
                        _functions.Add(function);
                    }

                    break;

                case RecordDecl record when record.IsCompleteDefinition && IsLibraryCursor(cursor):
                    _records.TryAdd(TypeMapper.NameOf(record), record);
                    break;

                case EnumDecl enumDecl when enumDecl.IsCompleteDefinition && IsLibraryCursor(cursor):
                    _enums.TryAdd(TypeMapper.NameOf(enumDecl), enumDecl);
                    break;

                case TypedefDecl typedef when IsLibraryCursor(cursor):
                    _typedefs.TryAdd(typedef.Name, typedef);
                    break;
            }
        }
    }

    private (List<FunctionModel> Bound, Dictionary<string, string> Excluded) ReadFunctions(TypeMapper mapper)
    {
        var models = new List<FunctionModel>();
        var excluded = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var function in _functions)
        {
            // Inline and static functions have no exported symbol.
            if (function.HasBody || function.IsInlined || function.StorageClass == CX_StorageClass.CX_SC_Static)
            {
                continue;
            }

            var path = RelativePath(function);
            if (_config.ExcludedHeaders.ContainsKey(path))
            {
                excluded[function.Name] = path;
                continue;
            }

            if (!_boundHeaders.TryGetValue(path, out var header) || header.Functions?.IsMatch(function.Name) == false)
            {
                continue;
            }

            if (_config.Exclusions.ContainsKey(function.Name))
            {
                _usedConfigKeys.Add(function.Name);
                continue;
            }

            if (function.IsVariadic)
            {
                _errors.Add($"{function.Name} is variadic, which P/Invoke cannot call portably: add it to Exclusions with a reason.");
                continue;
            }

            try
            {
                models.Add(ReadFunction(function, header, mapper));
            }
            catch (MappingException e)
            {
                _errors.Add($"{function.Name}: {e.Message}. Add it to Exclusions with a reason, or teach the generator.");
            }
        }

        return (models, excluded);
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

        var documentation = DocumentationOf(function);
        var returnType = mapper.Map(function.ReturnType);
        var types = function.Parameters.Select(p => mapper.Map(p.Type)).ToList();
        var nativeNames = function.Parameters.Select((p, i) => p.Name.Length > 0 ? p.Name : $"arg{i}").ToList();
        var kinds = new ParameterKind[types.Count];
        var pairs = new string?[types.Count];

        for (var i = 0; i < types.Count; i++)
        {
            if (kinds[i] != ParameterKind.None)
            {
                continue;
            }

            var key = $"{function.Name}.{nativeNames[i]}";
            ParameterRule? rule = null;
            if (_config.Parameters.TryGetValue(key, out var configured))
            {
                _usedConfigKeys.Add(key);
                CheckRule(key, configured, types[i]);
                rule = configured;
            }
            else if (_config.InferParameters)
            {
                rule = ParameterInference.Infer(i, nativeNames, types, documentation, name => mapper.KindOf(name) == ReferenceKind.Struct);
            }

            if (rule is not null && rule.Kind == ParameterKind.Span)
            {
                var count = nativeNames.IndexOf(rule.Count!);
                if (count < 0 || !types[count].IsInteger())
                {
                    throw new MappingException($"{key}: count parameter {rule.Count} is missing or not an integer");
                }

                if (kinds[count] != ParameterKind.None)
                {
                    throw new MappingException($"{key}: count parameter {rule.Count} already has a rule");
                }

                kinds[count] = ParameterKind.Count;
                pairs[i] = rule.Count;
                pairs[count] = nativeNames[i];
            }

            if (rule is not null)
            {
                kinds[i] = rule.Kind;
            }
            else if (_config.Utf8Strings && types[i] is { IsPointer: true, IsConst: true } pointer && pointer.Element!.Is(PrimitiveType.Char))
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
                Name = Rename($"{function.Name}.{nativeNames[i]}") ?? Naming.Camel(nativeNames[i], _config.Words),
                Type = types[i],
                Kind = kinds[i],
                Pair = pairs[i] is { } pair ? Rename($"{function.Name}.{pair}") ?? Naming.Camel(pair, _config.Words) : null,
            });
        }

        return new FunctionModel
        {
            NativeName = function.Name,
            Name = Rename(function.Name) ?? Pascal(Naming.StripPrefix(function.Name, _config.Prefixes)),
            Return = returnType,
            Parameters = parameters,
            Group = header.Group,
            Documentation = documentation,
            SupportedPlatforms = Platforms(_config.SupportedPlatforms, function.Name),
            UnsupportedPlatforms = Platforms(_config.UnsupportedPlatforms, function.Name),
        };
    }

    private IReadOnlyList<string> Platforms(IReadOnlyDictionary<string, IReadOnlyList<string>> platforms, string function)
    {
        if (!platforms.TryGetValue(function, out var list))
        {
            return [];
        }

        _usedConfigKeys.Add(function);
        return list;
    }

    private static void CheckRule(string key, ParameterRule rule, TypeRef type)
    {
        var valid = rule.Kind switch
        {
            ParameterKind.None => true,
            ParameterKind.In => type is { IsPointer: true, IsConst: true } && type.Element!.Kind != TypeKind.Void,
            ParameterKind.Out or ParameterKind.Ref => type is { IsPointer: true, IsConst: false } && type.Element!.Kind != TypeKind.Void,
            ParameterKind.Span => type.IsPointer && type.Element!.Kind is not (TypeKind.Void or TypeKind.Pointer or TypeKind.FunctionPointer),
            _ => false,
        };
        if (!valid)
        {
            throw new MappingException($"{key}: rule {rule.Kind} does not fit its C type");
        }
    }

    private List<ConstantModel> ReadConstants(TypeMapper mapper, IReadOnlyList<FunctionModel> functions, HashSet<string> consumed)
    {
        // Macros of a macro enum are its members, not constants.
        foreach (var (typedefName, macroEnum) in _config.MacroEnums)
        {
            if (_typedefs.TryGetValue(typedefName, out var typedef))
            {
                var header = FilePath(typedef);
                consumed.UnionWith(_macros.Where(m => macroEnum.Matches(m.Key) && FilePath(m.Value) == header).Select(m => m.Key));
            }
        }

        // SDL documents most property names only in the list of the function that takes them: "`SDL_PROP_X`: text".
        var listed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var function in functions)
        {
            foreach (var item in function.Documentation.Remarks.Where(b => b.Kind == DocBlockKind.List).SelectMany(b => b.Lines))
            {
                var match = ListedMacroPattern().Match(item);
                if (match.Success)
                {
                    var text = match.Groups["text"].Value.TrimEnd();
                    listed.TryAdd(match.Groups["name"].Value, $"{text}{(text.EndsWith('.') ? string.Empty : ".")} See `{function.NativeName}`.");
                }
            }
        }

        var constants = new List<(int Header, int Line, ConstantModel Model)>();
        var headerOrder = _config.Headers.Select((h, i) => (h.Path, i)).ToDictionary(p => p.Path, p => p.i, StringComparer.Ordinal);
        foreach (var (name, probe) in _macroProbes)
        {
            if (consumed.Contains(name) || !_macros.TryGetValue(name, out var macro) || !_boundHeaders.TryGetValue(RelativePath(macro), out var header))
            {
                continue;
            }

            if (_config.Exclusions.ContainsKey(name))
            {
                _usedConfigKeys.Add(name);
                continue;
            }

            try
            {
                var documentation = MacroDocumentation(macro);
                if (documentation.Summary is null && listed.TryGetValue(name, out var text))
                {
                    documentation = new Documentation { Summary = text, Remarks = documentation.Remarks };
                }

                var model = ReadConstant(name, probe, header, documentation, mapper);
                constants.Add((headerOrder[header.Path], Line(macro), model));
            }
            catch (MappingException e)
            {
                _errors.Add($"{name}: {e.Message}. Add it to Exclusions with a reason, or teach the generator.");
            }
        }

        return [.. constants.OrderBy(c => c.Header).ThenBy(c => c.Line).ThenBy(c => c.Model.NativeName, StringComparer.Ordinal).Select(c => c.Model)];
    }

    private ConstantModel ReadConstant(string name, VarDecl probe, HeaderConfig header, Documentation documentation, TypeMapper mapper)
    {
        using var evaluation = probe.Handle.Evaluate;
        var type = mapper.Map(probe.Type);
        var csharpName = Rename(name) ?? Pascal(Naming.StripPrefix(name, _config.Prefixes));
        switch (evaluation.Kind)
        {
            case CXEvalResultKind.CXEval_StrLiteral when type is { Kind: TypeKind.FixedArray } array && array.Element!.Is(PrimitiveType.Char):
                var text = evaluation.AsStr;
                if (Encoding.UTF8.GetByteCount(text) + 1 != array.Length)
                {
                    throw new MappingException("the string holds a NUL or is not valid UTF-8");
                }

                return new ConstantModel { NativeName = name, Name = csharpName, Group = header.Group, Type = array, Text = text, Documentation = documentation };

            case CXEvalResultKind.CXEval_Float when type.Is(PrimitiveType.Single) || type.Is(PrimitiveType.Double):
                return new ConstantModel { NativeName = name, Name = csharpName, Group = header.Group, Type = type, Float = evaluation.AsDouble, Documentation = documentation };

            case CXEvalResultKind.CXEval_Int:
                // `long` constants come from INT64_C-style macros, which pick `long` where it is 64-bit and `long long`
                // elsewhere: the width is what the macro means. A genuine 32-bit `long` constant then differs between
                // targets, which the cross-target comparison reports.
                if (type.Is(PrimitiveType.CLong) || type.Is(PrimitiveType.CULong))
                {
                    var is64 = (int)Probe("jade_probe_long") == 8;
                    type = TypeRef.Of(type.Is(PrimitiveType.CLong) ? (is64 ? PrimitiveType.Int64 : PrimitiveType.Int32) : (is64 ? PrimitiveType.UInt64 : PrimitiveType.UInt32));
                }

                var isEnum = type.Kind == TypeKind.Named && mapper.KindOf(type.Name!) is ReferenceKind.Enum or ReferenceKind.IdEnum or ReferenceKind.MacroEnum;
                if (!type.IsInteger() && !type.Is(PrimitiveType.Bool) && !isEnum)
                {
                    throw new MappingException($"constant of type {type.Describe()} is not supported");
                }

                var bits = evaluation.IsUnsignedInt ? evaluation.AsUnsigned : unchecked((ulong)evaluation.AsLongLong);
                return new ConstantModel { NativeName = name, Name = csharpName, Group = header.Group, Type = type, Integer = bits, Documentation = documentation };

            default:
                throw new MappingException($"constant of type {type.Describe()} evaluates to {evaluation.Kind}");
        }
    }

    private (List<EnumModel>, List<StructModel>, List<HandleModel>, Dictionary<string, RecordLayout>) ReadTypes(TypeMapper mapper)
    {
        var enumModels = new List<EnumModel>();
        var structModels = new List<StructModel>();
        var handleModels = new List<HandleModel>();
        var layouts = new Dictionary<string, RecordLayout>(StringComparer.Ordinal);

        // Enums and macro enums of bound headers are API even when no bound function names them, for example the
        // indices of SDL_MessageBoxColorScheme.colors.
        foreach (var (name, declaration) in _enums.Where(e => e.Key.Length > 0 && _boundHeaders.ContainsKey(RelativePath(e.Value))).OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            if (_config.Exclusions.ContainsKey(name))
            {
                _usedConfigKeys.Add(name);
                continue;
            }

            mapper.Reference(name, ReferenceKind.Enum);
        }

        foreach (var name in _config.MacroEnums.Keys.Where(_typedefs.ContainsKey).Order(StringComparer.Ordinal))
        {
            mapper.Reference(name, ReferenceKind.MacroEnum);
        }

        // Reading a struct maps its fields, which can append references while this loop runs.
        for (var i = 0; i < mapper.References.Count; i++)
        {
            var reference = mapper.References[i];
            try
            {
                switch (reference.Kind)
                {
                    case ReferenceKind.Enum:
                        enumModels.Add(ReadEnum(_enums[reference.Name]));
                        break;

                    case ReferenceKind.MacroEnum:
                        enumModels.Add(ReadMacroEnum(reference.Name, mapper));
                        break;

                    case ReferenceKind.IdEnum:
                        enumModels.Add(ReadIdEnum(reference.Name, mapper));
                        break;

                    case ReferenceKind.Struct:
                        var record = mapper.AnonymousRecords.TryGetValue(reference.Name, out var anonymous) ? anonymous : _records[reference.Name];
                        structModels.Add(ReadStruct(reference.Name, record, mapper, layouts));
                        break;

                    case ReferenceKind.Handle:
                        handleModels.Add(new HandleModel
                        {
                            NativeName = reference.Name,
                            Name = TypeName(reference.Name),
                            Stem = MethodStem(reference.Name),
                            Documentation = TypeDocumentation(reference.Name),
                            IsForeign = _config.ForeignHandles.Contains(reference.Name),
                        });
                        break;

                    case ReferenceKind.Opaque:
                        structModels.Add(new StructModel { NativeName = reference.Name, Name = TypeName(reference.Name), IsOpaque = true, Fields = [], Documentation = TypeDocumentation(reference.Name) });
                        break;
                }
            }
            catch (MappingException e)
            {
                _errors.Add($"{reference.Name}: {e.Message}.");
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
            Name = Rename($"{name}.{c.Name}") ?? Pascal(c.Name[prefix.Length..]),
            Value = c.IsNegative ? unchecked((ulong)c.InitVal) : c.UnsignedInitVal,
            Documentation = DocumentationOf(c),
        }).ToList();

        var isFlags = _config.FlagEnums.Contains(name);
        if (isFlags)
        {
            _usedConfigKeys.Add(name);
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
    private EnumModel ReadMacroEnum(string name, TypeMapper mapper)
    {
        _usedConfigKeys.Add(name);
        var typedef = _typedefs[name];
        var underlying = mapper.Map(typedef.UnderlyingType);
        if (!underlying.IsInteger())
        {
            throw new MappingException("a macro enum typedef must be an integer");
        }

        var macroEnum = _config.MacroEnums[name];
        var header = FilePath(typedef);
        var members = new List<EnumMemberModel>();
        foreach (var (macroName, macro) in _macros.Where(m => macroEnum.Matches(m.Key) && FilePath(m.Value) == header).OrderBy(m => Line(m.Value)))
        {
            if (_config.Exclusions.ContainsKey(macroName))
            {
                _usedConfigKeys.Add(macroName);
                continue;
            }

            if (!_macroProbes.TryGetValue(macroName, out var probe))
            {
                throw new MappingException($"macro {macroName} does not evaluate to a constant: add it to Exclusions with a reason");
            }

            using var evaluation = probe.Handle.Evaluate;
            if (evaluation.Kind != CXEvalResultKind.CXEval_Int)
            {
                throw new MappingException($"macro {macroName} is not an integer: add it to Exclusions with a reason");
            }

            members.Add(new EnumMemberModel
            {
                NativeName = macroName,
                Name = Rename($"{name}.{macroName}") ?? Pascal(macroName[macroEnum.Prefix.Length..]),
                Value = evaluation.IsUnsignedInt ? evaluation.AsUnsigned : unchecked((ulong)evaluation.AsLongLong),
                Documentation = MacroDocumentation(macro),
            });
        }

        if (members.Count == 0)
        {
            throw new MappingException($"no macro starting with {macroEnum.Prefix} in {RelativePath(typedef)}");
        }

        return new EnumModel
        {
            NativeName = name,
            Name = TypeName(name),
            Underlying = underlying.Primitive,
            IsFlags = macroEnum.IsFlags,
            Members = Unique(name, members),
            Documentation = DocumentationOf(typedef),
        };
    }

    private EnumModel ReadIdEnum(string name, TypeMapper mapper)
    {
        _usedConfigKeys.Add(name);
        var typedef = _typedefs[name];
        var underlying = mapper.Map(typedef.UnderlyingType);
        if (!underlying.IsInteger())
        {
            throw new MappingException("an ID typedef must be an integer");
        }

        return new EnumModel { NativeName = name, Name = TypeName(name), Underlying = underlying.Primitive, Members = [], Documentation = DocumentationOf(typedef) };
    }

    private StructModel ReadStruct(string name, RecordDecl declaration, TypeMapper mapper, Dictionary<string, RecordLayout> layouts)
    {
        IEnumerable<FieldDecl> fields = declaration.Fields;
        if (_config.UnionMembers.TryGetValue(name, out var kept))
        {
            _usedConfigKeys.Add(name);
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

            var type = mapper.MapField(field.Type, name, field.Name);
            if (type.Kind == TypeKind.FixedArray && type.Element!.Kind is TypeKind.FixedArray or TypeKind.FunctionPointer)
            {
                throw new MappingException($"field {field.Name} is an array of arrays or function pointers, which the generator does not support yet");
            }

            fieldModels.Add(new FieldModel
            {
                NativeName = field.Name,
                Name = Rename($"{name}.{field.Name}") ?? Pascal(field.Name),
                Type = type,
                Documentation = DocumentationOf(field),
            });
            offsets.Add(field.Handle.OffsetOfField / 8);
        }

        var recordType = declaration.TypeForDecl.Handle;
        layouts[name] = new RecordLayout(recordType.SizeOf, recordType.AlignOf, offsets);
        var documentation = mapper.AnonymousRecords.ContainsKey(name) ? Documentation.None : TypeDocumentation(name);
        return new StructModel { NativeName = name, Name = TypeName(name), IsUnion = declaration.IsUnion, Fields = fieldModels, Documentation = documentation };
    }

    // Config entries used by this target's parse; generate-bindings.cs reports those no target used.
    private void RecordConfigUse(TypeMapper mapper)
    {
        foreach (var reference in mapper.References)
        {
            _usedConfigKeys.Add(reference.Name);
        }

        _usedConfigKeys.UnionWith(mapper.UsedMappings);
        foreach (var (name, decision) in _config.LayoutDecisions.Where(d => d.Value.Kind != LayoutDecisionKind.Opaque))
        {
            _errors.Add($"layout decision {decision.Kind} for {name} is not implemented yet; only Opaque is.");
        }
    }

    private List<EnumMemberModel> Unique(string name, List<EnumMemberModel> members)
    {
        foreach (var duplicate in members.GroupBy(m => m.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            _errors.Add($"{name}: {string.Join(", ", duplicate.Select(m => m.NativeName))} all map to {duplicate.Key}; add Renames.");
        }

        return members;
    }

    private string Pascal(string name) => Naming.Pascal(name, _config.Words, _config.LeadingDigitPrefix);

    private string MethodStem(string handle)
    {
        if (_config.MethodStems.TryGetValue(handle, out var stem))
        {
            _usedConfigKeys.Add(handle);
            return stem;
        }

        return Pascal(Naming.StripPrefix(handle, _config.Prefixes));
    }

    // An anonymous struct or union is named after its field: SDL_GamepadBinding.input becomes GamepadBindingInput.
    private string TypeName(string nativeName)
    {
        if (Rename(nativeName) is { } renamed)
        {
            return renamed;
        }

        var dot = nativeName.LastIndexOf('.');
        return dot < 0
            ? Pascal(Naming.StripPrefix(nativeName, _config.Prefixes))
            : TypeName(nativeName[..dot]) + Pascal(nativeName[(dot + 1)..]);
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

    // Records and enums declared through a typedef carry the comment on either the tag or the typedef.
    private Documentation TypeDocumentation(string name)
    {
        Cursor? tag = _records.TryGetValue(name, out var record) ? record : _enums.TryGetValue(name, out var enumDecl) ? enumDecl : null;
        var documentation = tag is null ? Documentation.None : DocumentationOf(tag);
        return documentation.Summary is null && _typedefs.TryGetValue(name, out var typedef) ? DocumentationOf(typedef) : documentation;
    }

    private static Documentation DocumentationOf(Cursor cursor)
    {
        using var raw = cursor.Handle.RawCommentText;
        return DocCommentParser.Parse(raw.ToString());
    }

    // libclang attaches no comment to macros: SDL documents them with a trailing /**< ... */ or a /** ... */ block
    // that ends on the line right above.
    private Documentation MacroDocumentation(MacroDefinitionRecord macro)
    {
        var lines = FileLines(FilePath(macro));
        var line = Line(macro) - 1;
        var trailing = TrailingCommentPattern().Match(lines[line]);
        if (trailing.Success)
        {
            return DocCommentParser.Parse(trailing.Value);
        }

        if (line == 0 || !lines[line - 1].TrimEnd().EndsWith("*/", StringComparison.Ordinal) || lines[line - 1].Contains("#define", StringComparison.Ordinal))
        {
            return Documentation.None;
        }

        for (var start = line - 1; start >= 0; start--)
        {
            var trimmed = lines[start].TrimStart();
            if (trimmed.StartsWith("/**", StringComparison.Ordinal) && !trimmed.StartsWith("/**<", StringComparison.Ordinal))
            {
                return DocCommentParser.Parse(string.Join('\n', lines[start..line]));
            }

            if (start < line - 1 && trimmed.Contains("*/", StringComparison.Ordinal))
            {
                break;
            }
        }

        return Documentation.None;
    }

    private string[] FileLines(string path)
    {
        if (!_fileLines.TryGetValue(path, out var lines))
        {
            lines = File.ReadAllLines(path);
            _fileLines[path] = lines;
        }

        return lines;
    }

    private ulong Probe(string name) =>
        _probes.TryGetValue(name, out var value) ? value : throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: probe {name} did not evaluate.");

    private bool IsLibraryDeclaration(Decl declaration) => IsLibraryCursor(declaration);

    private bool IsLibraryCursor(Cursor cursor) => FilePath(cursor).StartsWith(_libraryDirectory, StringComparison.Ordinal);

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

    private string RelativePath(Cursor cursor) => Path.GetRelativePath(_includeDirectory, FilePath(cursor)).Replace('\\', '/');

    [GeneratedRegex(@"^[ \t]*#[ \t]*define[ \t]+(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]", RegexOptions.Multiline)]
    private static partial Regex DefinePattern();

    [GeneratedRegex(@"/\*\*<.*?\*/")]
    private static partial Regex TrailingCommentPattern();

    [GeneratedRegex(@"^`(?<name>[A-Za-z_][A-Za-z0-9_]*)`: (?<text>.+)$")]
    private static partial Regex ListedMacroPattern();
}
