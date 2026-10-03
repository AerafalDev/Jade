using System.Text;
using ClangSharp;
using ClangSharp.Interop;
using Type = ClangSharp.Type;

/// <summary>
/// Cross-checks a model read by <see cref="DawnJsonReader"/> against the header generated from the same description, for
/// one target: the header must declare the same functions, structs, enums, bitmasks, handles and constants, with the same
/// types and values. It also measures every struct's native layout on the target, which <see cref="VarianceCheck"/> and
/// <see cref="LayoutTestEmitter"/> compare with the generated definitions. The header is parsed freestanding, as
/// <see cref="ClangReader"/> parses C libraries.
/// </summary>
internal sealed class DawnHeaderCheck
{
    private const string MainFile = "jade_header_check.c";
    private const string MacroProbe = "jade_macro_";

    private readonly LibraryConfig _config;
    private readonly Target _target;
    private readonly LibraryModel _model;
    private readonly string _includeDirectory;
    private readonly string _libraryDirectory;
    private readonly HashSet<string> _named;
    private readonly HashSet<string> _usedConfigKeys;
    private readonly List<string> _errors = [];
    private readonly Dictionary<string, FunctionDecl> _functions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RecordDecl> _records = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EnumDecl> _enums = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypedefDecl> _typedefs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VarDecl> _variables = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VarDecl> _probes = new(StringComparer.Ordinal);

    private DawnHeaderCheck(LibraryConfig config, Target target, LibraryModel model, string includeDirectory, IReadOnlySet<string> usedConfigKeys)
    {
        _config = config;
        _target = target;
        _model = model;
        _includeDirectory = Path.GetFullPath(includeDirectory);
        _libraryDirectory = Path.Combine(_includeDirectory, config.IncludeDirectory) + Path.DirectorySeparatorChar;
        _named = model.Structs.Select(s => s.NativeName).Concat(model.Enums.Select(e => e.NativeName)).Concat(model.Handles.Select(h => h.NativeName)).ToHashSet(StringComparer.Ordinal);
        _usedConfigKeys = new HashSet<string>(usedConfigKeys, StringComparer.Ordinal);
    }

    /// <summary>Parses the header for one target and compares it with the model.</summary>
    /// <param name="index">The libclang index, shared by every parse.</param>
    /// <param name="config">The library config, whose <see cref="LibraryConfig.CrossCheckHeader"/> is parsed.</param>
    /// <param name="target">The target to parse for.</param>
    /// <param name="includeDirectory">The staged <c>include/</c> folder.</param>
    /// <param name="sysroot">The folder of stub system headers.</param>
    /// <param name="model">The model, the same on every target.</param>
    /// <param name="usedConfigKeys">The config entries the reader used.</param>
    /// <param name="report">Receives a summary and the header declarations left out on purpose, or <see langword="null"/>.</param>
    /// <returns>The model with this target's native layouts.</returns>
    /// <exception cref="InvalidOperationException">The header does not parse, or differs from the model.</exception>
    public static TargetModel Run(CXIndex index, LibraryConfig config, Target target, string includeDirectory, string sysroot, LibraryModel model, IReadOnlySet<string> usedConfigKeys, List<string>? report)
    {
        var check = new DawnHeaderCheck(config, target, model, includeDirectory, usedConfigKeys);
        return check.Run(index, sysroot, report);
    }

    private TargetModel Run(CXIndex index, string sysroot, List<string>? report)
    {
        string[] arguments =
        [
            "-x", "c", "-std=c11", $"--target={_target.Triple}", "-nostdinc", "-isystem", Path.GetFullPath(sysroot), "-I", _includeDirectory,
            "-ferror-limit=0",
        ];
        using var file = CXUnsavedFile.Create(MainFile, MainSource());
        var options = CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies | CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord;
        if (CXTranslationUnit.TryParse(index, MainFile, arguments, [file], options, out var handle) is not CXErrorCode.CXError_Success and var result)
        {
            throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: libclang failed to parse {_config.CrossCheckHeader} ({result}).");
        }

        using var translationUnit = TranslationUnit.GetOrCreate(handle);
        CheckDiagnostics(handle);
        Index(translationUnit);

        var excluded = new List<string>();
        CheckFunctions(excluded);
        var layouts = CheckStructs(excluded);
        CheckEnums();
        CheckHandles();
        var pointerSize = (int)Integer("jade_probe_pointer");
        CheckConstants(pointerSize);
        if (_errors.Count > 0)
        {
            throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: {_config.CrossCheckHeader} differs from {_config.ApiDescription}:\n  {string.Join("\n  ", _errors)}");
        }

        if (report is not null)
        {
            report.Add($"{_config.Name}: header cross-check: {_config.CrossCheckHeader} declares the same {_model.Functions.Count} functions, {_model.Structs.Count} structs, " +
                $"{_model.Enums.Count(e => !e.IsFlags)} enums, {_model.Enums.Count(e => e.IsFlags)} bitmasks, {_model.Handles.Count} handles and {_model.Constants.Count} constants " +
                $"as {_config.ApiDescription}, with the same types and values, except:");
            report.AddRange(excluded);
        }

        return new TargetModel
        {
            Target = _target,
            Model = _model,
            Layouts = layouts,
            PointerSize = pointerSize,
            LongSize = (int)Integer("jade_probe_long"),
            ExcludedFunctions = new Dictionary<string, string>(),
            UsedConfigKeys = _usedConfigKeys,
        };
    }

    // Dawn's header refuses Emscripten (which uses emdawnwebgpu's header, generated from the same dawn.json). Layouts only
    // depend on the target's ABI, and DawnJsonReader rejects struct members only one implementation has, so wasm32 parses
    // Dawn's header with the guard lifted.
    private string MainSource()
    {
        var builder = new StringBuilder();
        builder.Append("#undef __EMSCRIPTEN__\n");
        builder.Append("#include <").Append(_config.CrossCheckHeader).Append(">\n");
        builder.Append("static const unsigned long long jade_probe_pointer = sizeof(void *);\n");
        builder.Append("static const unsigned long long jade_probe_long = sizeof(long);\n");
        foreach (var constant in _model.Constants)
        {
            builder.Append("static const __typeof__(").Append(constant.NativeName).Append(") ").Append(MacroProbe).Append(constant.NativeName)
                .Append(" = ").Append(constant.NativeName).Append(";\n");
        }

        return builder.ToString();
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
            throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: {_config.CrossCheckHeader} does not parse. Add what is missing to scripts/generate-bindings/sysroot/.\n  {string.Join("\n  ", messages)}");
        }
    }

    private void Index(TranslationUnit translationUnit)
    {
        foreach (var cursor in translationUnit.TranslationUnitDecl.CursorChildren)
        {
            if (cursor is VarDecl probe && probe.Location.IsFromMainFile)
            {
                _probes[probe.Name] = probe;
                continue;
            }

            if (!IsLibraryCursor(cursor))
            {
                continue;
            }

            switch (cursor)
            {
                case FunctionDecl function:
                    _functions.TryAdd(function.Name, function);
                    break;

                case RecordDecl record when record.IsCompleteDefinition:
                    _records.TryAdd(TypeMapper.NameOf(record), record);
                    break;

                case EnumDecl enumDecl when enumDecl.IsCompleteDefinition:
                    _enums.TryAdd(TypeMapper.NameOf(enumDecl), enumDecl);
                    break;

                case TypedefDecl typedef:
                    _typedefs.TryAdd(typedef.Name, typedef);
                    break;

                case VarDecl variable:
                    _variables.TryAdd(variable.Name, variable);
                    break;
            }
        }
    }

    private void CheckFunctions(List<string> excluded)
    {
        var functions = _model.Functions.ToDictionary(f => f.NativeName, StringComparer.Ordinal);
        foreach (var (name, declaration) in _functions.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            if (!functions.TryGetValue(name, out var function))
            {
                Excluded(name, "function", excluded);
                continue;
            }

            Compare(name, "return type", Map(declaration.ReturnType), function.Return);
            var parameters = declaration.Parameters;
            if (parameters.Count != function.Parameters.Count)
            {
                _errors.Add($"{name}: {parameters.Count} parameters in the header, {function.Parameters.Count} in the model.");
                continue;
            }

            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].Name != function.Parameters[i].NativeName)
                {
                    _errors.Add($"{name}: parameter {i} is `{parameters[i].Name}` in the header, `{function.Parameters[i].NativeName}` in the model.");
                }

                Compare($"{name}.{parameters[i].Name}", "type", Map(parameters[i].Type), function.Parameters[i].Type);
            }
        }

        foreach (var name in functions.Keys.Where(f => !_functions.ContainsKey(f)).Order(StringComparer.Ordinal))
        {
            _errors.Add($"{name} is in the model but not declared in the header.");
        }
    }

    private Dictionary<string, RecordLayout> CheckStructs(List<string> excluded)
    {
        var layouts = new Dictionary<string, RecordLayout>(StringComparer.Ordinal);
        foreach (var structModel in _model.Structs.Where(s => !s.IsOpaque))
        {
            if (!_records.TryGetValue(structModel.NativeName, out var record))
            {
                _errors.Add($"{structModel.NativeName} is in the model but not defined in the header.");
                continue;
            }

            var fields = record.Fields;
            if (fields.Count != structModel.Fields.Count)
            {
                _errors.Add($"{structModel.NativeName}: {fields.Count} fields in the header, {structModel.Fields.Count} in the model.");
                continue;
            }

            var offsets = new List<long>(fields.Count);
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].IsBitField || fields[i].Name != structModel.Fields[i].NativeName)
                {
                    _errors.Add($"{structModel.NativeName}: field {i} is `{fields[i].Name}` in the header, `{structModel.Fields[i].NativeName}` in the model.");
                }

                Compare($"{structModel.NativeName}.{fields[i].Name}", "type", Map(fields[i].Type), structModel.Fields[i].Type);
                offsets.Add(fields[i].Handle.OffsetOfField / 8);
            }

            var recordType = record.TypeForDecl.Handle;
            layouts[structModel.NativeName] = new RecordLayout(recordType.SizeOf, recordType.AlignOf, offsets);
        }

        var modelled = _model.Structs.Select(s => s.NativeName).ToHashSet(StringComparer.Ordinal);
        foreach (var name in _records.Keys.Where(r => !modelled.Contains(r)).Order(StringComparer.Ordinal))
        {
            Excluded(name, "struct", excluded);
        }

        return layouts;
    }

    // Enums end with a Force32 member that only fixes their size; bitmasks are a typedef plus static const values.
    private void CheckEnums()
    {
        var bitmaskValues = new HashSet<string>(StringComparer.Ordinal);
        foreach (var enumModel in _model.Enums)
        {
            var header = new Dictionary<string, ulong>(StringComparer.Ordinal);
            if (!enumModel.IsFlags)
            {
                if (!_enums.TryGetValue(enumModel.NativeName, out var declaration))
                {
                    _errors.Add($"{enumModel.NativeName} is an enum in the model but not in the header.");
                    continue;
                }

                if (declaration.IntegerType.Handle.SizeOf != LayoutCalculator.SizeOf(enumModel.Underlying, 8, 8))
                {
                    _errors.Add($"{enumModel.NativeName} is {declaration.IntegerType.Handle.SizeOf} bytes in the header, but its model type is {enumModel.Underlying}.");
                }

                foreach (var constant in declaration.Enumerators.Where(c => c.Name != enumModel.NativeName + "_Force32"))
                {
                    header[constant.Name] = constant.IsNegative ? unchecked((ulong)constant.InitVal) : constant.UnsignedInitVal;
                }
            }
            else
            {
                if (!_typedefs.TryGetValue(enumModel.NativeName, out var typedef))
                {
                    _errors.Add($"{enumModel.NativeName} is a bitmask in the model but not a typedef in the header.");
                    continue;
                }

                Compare(enumModel.NativeName, "underlying type", Map(typedef.UnderlyingType), TypeRef.Of(enumModel.Underlying));
                foreach (var (name, variable) in _variables.Where(v => TypedefName(v.Value.Type) == enumModel.NativeName))
                {
                    using var evaluation = variable.Handle.Evaluate;
                    header[name] = evaluation.Kind == CXEvalResultKind.CXEval_Int ? evaluation.AsUnsigned : throw new InvalidOperationException($"{name} does not evaluate to an integer.");
                    bitmaskValues.Add(name);
                }
            }

            var members = enumModel.Members.ToDictionary(m => m.NativeName, m => m.Value, StringComparer.Ordinal);
            foreach (var name in header.Keys.Union(members.Keys).Order(StringComparer.Ordinal))
            {
                var inHeader = header.TryGetValue(name, out var headerValue);
                var inModel = members.TryGetValue(name, out var modelValue);
                if (inHeader != inModel || headerValue != modelValue)
                {
                    _errors.Add($"{name}: {(inHeader ? $"0x{headerValue:X}" : "absent")} in the header, {(inModel ? $"0x{modelValue:X}" : "absent")} in the model.");
                }
            }
        }

        var modelled = _model.Enums.Where(e => !e.IsFlags).Select(e => e.NativeName).ToHashSet(StringComparer.Ordinal);
        foreach (var name in _enums.Keys.Where(e => !modelled.Contains(e)).Order(StringComparer.Ordinal))
        {
            _errors.Add($"{name} is an enum in the header but not in the model.");
        }

        foreach (var name in _variables.Keys.Where(v => !bitmaskValues.Contains(v)).Order(StringComparer.Ordinal))
        {
            _errors.Add($"{name} is a constant variable of the header that no bitmask of the model holds.");
        }
    }

    // A handle is a typedef of a pointer to an incomplete struct, as `typedef struct WGPUBufferImpl* WGPUBuffer`.
    private void CheckHandles()
    {
        var handles = _model.Handles.Select(h => h.NativeName).ToHashSet(StringComparer.Ordinal);
        foreach (var (name, typedef) in _typedefs.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            var isHandle = Peel(typedef.UnderlyingType) is PointerType { PointeeType: var pointee } && Peel(pointee) is RecordType { Decl.Definition: null };
            if (isHandle != handles.Contains(name))
            {
                _errors.Add(isHandle ? $"{name} is a handle in the header but not in the model." : $"{name} is a handle in the model but not in the header.");
            }
        }

        foreach (var name in handles.Where(h => !_typedefs.ContainsKey(h)).Order(StringComparer.Ordinal))
        {
            _errors.Add($"{name} is a handle in the model but not declared in the header.");
        }

        foreach (var name in _model.Callbacks.Keys.Where(c => !_typedefs.ContainsKey(c)).Order(StringComparer.Ordinal))
        {
            _errors.Add($"{name} is a callback type in the model but not declared in the header.");
        }
    }

    // The value of each constant macro on this target. A native-sized all-ones value is SIZE_MAX of this target.
    private void CheckConstants(int pointerSize)
    {
        foreach (var constant in _model.Constants)
        {
            if (!_probes.TryGetValue(MacroProbe + constant.NativeName, out var probe))
            {
                _errors.Add($"{constant.NativeName} is in the model but not defined in the header.");
                continue;
            }

            using var evaluation = probe.Handle.Evaluate;
            if (constant.Type.Is(PrimitiveType.Single) || constant.Type.Is(PrimitiveType.Double))
            {
                var value = evaluation.Kind == CXEvalResultKind.CXEval_Float ? evaluation.AsDouble : double.PositiveInfinity;
                if (!(double.IsNaN(value) ? double.IsNaN(constant.Float) : value == constant.Float))
                {
                    _errors.Add($"{constant.NativeName}: {value} in the header, {constant.Float} in the model.");
                }

                continue;
            }

            var expected = constant.Type.Is(PrimitiveType.NUInt) && constant.Integer == ulong.MaxValue && pointerSize == 4 ? uint.MaxValue : constant.Integer;
            if (evaluation.Kind != CXEvalResultKind.CXEval_Int || evaluation.AsUnsigned != expected)
            {
                _errors.Add($"{constant.NativeName}: {(evaluation.Kind == CXEvalResultKind.CXEval_Int ? $"0x{evaluation.AsUnsigned:X}" : evaluation.Kind.ToString())} in the header, 0x{expected:X} in the model.");
            }
        }
    }

    private void Excluded(string name, string kind, List<string> excluded)
    {
        if (_config.Exclusions.TryGetValue(name, out var reason))
        {
            _usedConfigKeys.Add(name);
            excluded.Add($"  {kind} {name}: {reason}");
        }
        else
        {
            _errors.Add($"{name} is a {kind} of the header but not of the model: bind it, or add it to Exclusions with a reason.");
        }
    }

    private void Compare(string owner, string what, TypeRef header, TypeRef model)
    {
        if (header.Describe() != model.Describe())
        {
            _errors.Add($"{owner}: {what} is `{header.Describe()}` in the header, `{model.Describe()}` in the model.");
        }
    }

    // The header's types in the model's terms: named types are the model's structs, enums, bitmasks and handles,
    // function pointer typedefs keep their name, and other typedefs stand for what they alias.
    private TypeRef Map(Type type)
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

                case TypedefType typedef:
                    var name = typedef.Decl.Name;
                    if (TypeMapper.TryMapFixedWidth(name, out var primitive))
                    {
                        return TypeRef.Of(primitive);
                    }

                    if (_named.Contains(name))
                    {
                        return TypeRef.Named(name);
                    }

                    if (Peel(typedef.Decl.UnderlyingType) is PointerType { PointeeType: var target } && Peel(target) is FunctionProtoType callback)
                    {
                        return MapFunction(callback, name);
                    }

                    current = typedef.Decl.UnderlyingType;
                    continue;

                case PointerType pointer:
                    return Peel(pointer.PointeeType) is FunctionProtoType function
                        ? MapFunction(function, null)
                        : TypeRef.PointerTo(Map(pointer.PointeeType), pointer.PointeeType.IsLocalConstQualified);

                case BuiltinType builtin:
                    return builtin.Kind switch
                    {
                        CXTypeKind.CXType_Void => TypeRef.Void,
                        CXTypeKind.CXType_Char_S or CXTypeKind.CXType_Char_U => TypeRef.Of(PrimitiveType.Char),
                        CXTypeKind.CXType_Int => TypeRef.Of(PrimitiveType.Int32),
                        CXTypeKind.CXType_UInt => TypeRef.Of(PrimitiveType.UInt32),
                        CXTypeKind.CXType_Float => TypeRef.Of(PrimitiveType.Single),
                        CXTypeKind.CXType_Double => TypeRef.Of(PrimitiveType.Double),
                        _ => throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: the header uses `{builtin.AsString}`, which the cross-check does not map."),
                    };

                case RecordType record:
                    return TypeRef.Named(TypeMapper.NameOf(record.Decl));

                case EnumType enumType:
                    return TypeRef.Named(TypeMapper.NameOf(enumType.Decl));

                default:
                    if (current.IsSugared)
                    {
                        current = current.Desugar;
                        continue;
                    }

                    throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: the header uses `{current.AsString}` ({current.TypeClassSpelling}), which the cross-check does not map.");
            }
        }
    }

    private TypeRef MapFunction(FunctionProtoType function, string? alias)
    {
        if (function.CallConv != CXCallingConv.CXCallingConv_C)
        {
            _errors.Add($"{alias ?? function.AsString}: calling convention {function.CallConv} is not cdecl.");
        }

        return TypeRef.FunctionPointer(Map(function.ReturnType), [.. function.ParamTypes.Select(Map)], alias);
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

                case TypedefType typedef when !TypeMapper.TryMapFixedWidth(typedef.Decl.Name, out _):
                    current = typedef.Decl.UnderlyingType;
                    break;

                default:
                    return current;
            }
        }
    }

    // The typedef a type names, under the sugar clang wraps it in (ElaboratedType since clang 16).
    private static string? TypedefName(Type type)
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

                case TypedefType typedef:
                    return typedef.Decl.Name;

                default:
                    return null;
            }
        }
    }

    private ulong Integer(string probe)
    {
        if (_probes.TryGetValue(probe, out var variable))
        {
            using var evaluation = variable.Handle.Evaluate;
            if (evaluation.Kind == CXEvalResultKind.CXEval_Int)
            {
                return evaluation.AsUnsigned;
            }
        }

        throw new InvalidOperationException($"{_config.Name} on {_target.Rid}: probe {probe} did not evaluate.");
    }

    private bool IsLibraryCursor(Cursor cursor)
    {
        cursor.Location.GetFileLocation(out var file, out _, out _, out _);
        using var name = file.Name;
        var path = name.ToString();
        return path.Length > 0 && Path.GetFullPath(path).StartsWith(_libraryDirectory, StringComparison.Ordinal);
    }
}
