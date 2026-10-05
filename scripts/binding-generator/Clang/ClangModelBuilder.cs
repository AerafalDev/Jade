using System.Diagnostics;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Clang;

/// <summary>Merges the parses of a library's headers for every target into its intermediate representation (ADR 0026, ADR 0033).</summary>
/// <remarks>
/// A declaration is available on the platform families whose targets all declare it, and must be
/// the same on all of them: a divergence fails the generator unless the configuration makes the
/// declaration opaque or excludes it. The annotations of the configuration are applied after the
/// merge, and each one must match a declaration or a macro.
/// </remarks>
internal sealed class ClangModelBuilder
{
    /// <summary>The C types that a declaration cannot use, since their size differs between targets or C# has no equivalent (ADR 0027).</summary>
    private static readonly string[] _unsupportedTypes = ["va_list", "wchar_t", "long double"];

    /// <summary>The key of the library, for error messages.</summary>
    private readonly string _library;

    /// <summary>The parses of every target.</summary>
    private readonly ParsedHeaders _headers;

    /// <summary>The front-end settings and annotations.</summary>
    private readonly ClangConfiguration _configuration;

    /// <summary>The library's naming rules.</summary>
    private readonly CNames _names;

    /// <summary>The merged declarations, by C name.</summary>
    private readonly Dictionary<string, Declaration> _declarations = [with(StringComparer.Ordinal)];

    /// <summary>The header of each merged declaration, by C name.</summary>
    private readonly Dictionary<string, string> _headerOf = [with(StringComparer.Ordinal)];

    /// <summary>The constants, by C name.</summary>
    private readonly Dictionary<string, ConstantDeclaration> _constants = [with(StringComparer.Ordinal)];

    /// <summary>The declarations left out, by name.</summary>
    private readonly Dictionary<string, SkippedDeclaration> _skipped = [with(StringComparer.Ordinal)];

    /// <summary>Initializes a new instance of the <see cref="ClangModelBuilder"/> class.</summary>
    /// <param name="library">The key of the library.</param>
    /// <param name="headers">The parses of every target.</param>
    /// <param name="configuration">The front-end settings.</param>
    private ClangModelBuilder(string library, ParsedHeaders headers, ClangConfiguration configuration)
    {
        _library = library;
        _headers = headers;
        _configuration = configuration;
        _names = new CNames(configuration.Prefixes, configuration.MemberPrefixes);
    }

    /// <summary>Builds the intermediate representation of a library's headers.</summary>
    /// <param name="library">The key of the library, for error messages.</param>
    /// <param name="headers">The parses of every target.</param>
    /// <param name="configuration">The front-end settings and annotations.</param>
    /// <param name="exclude">The declarations the configuration leaves out, with the reason.</param>
    /// <returns>The model of the API.</returns>
    /// <exception cref="InvalidDataException">The targets disagree, a declaration cannot be bound, or an annotation matches nothing.</exception>
    public static ApiModel Build(string library, ParsedHeaders headers, ClangConfiguration configuration, IReadOnlyDictionary<string, string> exclude)
    {
        return new ClangModelBuilder(library, headers, configuration).Build(exclude);
    }

    /// <summary>Merges the targets and applies the annotations.</summary>
    /// <param name="exclude">The declarations the configuration leaves out.</param>
    /// <returns>The model of the API.</returns>
    private ApiModel Build(IReadOnlyDictionary<string, string> exclude)
    {
        foreach (var skipped in _headers.ByTarget.SelectMany(static target => target.Skipped.Values))
        {
            _ = _skipped.TryAdd(skipped.Name, skipped);
        }

        CheckUsed("exclude", exclude.Keys, _skipped.ContainsKey);
        MergeDeclarations();
        CheckUsed("opaque", _configuration.Opaque.Keys, name => _declarations.GetValueOrDefault(name) is HandleDeclaration);
        ApplyBooleans();

        var patterns = new MacroPatterns(_configuration);
        var claimedMacros = new HashSet<string>(StringComparer.Ordinal);

        ApplyEnums(patterns, claimedMacros);
        BuildConstants(patterns, claimedMacros);
        RewriteHandles();
        ExcludeHeaders();
        SkipUnsupportedDeclarations();
        CheckReferences();
        CheckAllocators();
        NameEnumValues();

        var declarations = _declarations.Values.ToList();

        return new ApiModel(
            _constants.Values,
            declarations.OfType<TypedefDeclaration>(),
            declarations.OfType<EnumDeclaration>(),
            declarations.OfType<HandleDeclaration>(),
            declarations.OfType<StructureDeclaration>(),
            declarations.OfType<FunctionPointerDeclaration>(),
            declarations.OfType<FunctionDeclaration>(),
            _skipped.Values);
    }

    /// <summary>Merges each declaration of the targets, checking that the targets that declare it agree.</summary>
    /// <exception cref="InvalidDataException">Targets disagree; every disagreement is listed, so that one run shows what to annotate.</exception>
    private void MergeDeclarations()
    {
        var names = _headers.ByTarget.SelectMany(static target => target.Declarations.Keys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var errors = new List<string>();

        foreach (var name in names)
        {
            try
            {
                MergeDeclaration(name);
            }
            catch (InvalidDataException exception)
            {
                errors.Add(exception.Message);
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        }
    }

    /// <summary>Merges one declaration of the targets.</summary>
    /// <param name="name">The C name of the declaration.</param>
    /// <exception cref="InvalidDataException">The targets that declare it disagree.</exception>
    private void MergeDeclaration(string name)
    {
        var entries = _headers.ByTarget
            .Where(target => target.Declarations.ContainsKey(name))
            .Select(target => (target.Target, target.Declarations[name].Declaration, target.Declarations[name].Header))
            .ToList();
        var availability = GetAvailability(name, entries.Select(static entry => entry.Target));
        var first = entries[0].Declaration;

        if (first is EnumDeclaration)
        {
            _declarations.Add(name, MergeEnum(name, [.. entries.Select(static entry => (entry.Target, entry.Declaration))], availability));
        }
        else
        {
            var signatures = entries.GroupBy(static entry => DeclarationSignature.Describe(entry.Declaration), StringComparer.Ordinal).ToList();

            if (signatures.Count > 1)
            {
                var descriptions = signatures.Select(static group => $"'{group.Key}' on {string.Join(", ", group.Select(static entry => entry.Target.RuntimeIdentifier))}");

                throw new InvalidDataException($"{_library}: '{name}' differs between targets ({string.Join("; ", descriptions)}): make it opaque or exclude it.");
            }

            _declarations.Add(name, first with { Availability = availability });
        }

        _headerOf.Add(name, entries[0].Header);
    }

    /// <summary>Merges the parses of an enum, whose values may depend on the target.</summary>
    /// <param name="name">The C name of the enum.</param>
    /// <param name="entries">The enum on each target that declares it.</param>
    /// <param name="availability">The platforms the enum is available on.</param>
    /// <returns>The enum, with the availability of each value.</returns>
    private EnumDeclaration MergeEnum(string name, IReadOnlyList<(Target Target, Declaration Declaration)> entries, Platforms availability)
    {
        var enums = entries.Select(static entry => (entry.Target, Enum: (EnumDeclaration)entry.Declaration)).ToList();

        if (enums.Select(static entry => entry.Enum.UnderlyingType).Distinct().Count() > 1)
        {
            throw new InvalidDataException($"{_library}: the enum '{name}' is stored in different types on different targets.");
        }

        var values = MergeValues(name, enums.Select(static entry => (entry.Target, entry.Enum.Values.Select(static value => (value.CName, value.Value)))));

        return enums[0].Enum with
        {
            Availability = availability,
            Values = [.. values.Select(value => new EnumValueDeclaration { CName = value.Name, Words = [], Availability = value.Availability, Value = value.Value })],
        };
    }

    /// <summary>Merges values that the targets define by name, checking that they agree.</summary>
    /// <param name="owner">The C name of the enum, for error messages.</param>
    /// <param name="targets">The values of each target, in order.</param>
    /// <returns>The values in the order they first appear, with their availability.</returns>
    private List<(string Name, ulong Value, Platforms Availability)> MergeValues(string owner, IEnumerable<(Target Target, IEnumerable<(string Name, ulong Value)> Values)> targets)
    {
        var order = new List<string>();
        var values = new Dictionary<string, (ulong Value, List<Target> Targets)>(StringComparer.Ordinal);

        foreach (var (target, targetValues) in targets)
        {
            foreach (var (valueName, value) in targetValues)
            {
                if (!values.TryGetValue(valueName, out var known))
                {
                    order.Add(valueName);
                    values.Add(valueName, (value, [target]));
                }
                else if (known.Value != value)
                {
                    throw new InvalidDataException($"{_library}: the value '{valueName}' of '{owner}' is {unchecked((long)known.Value)} on {known.Targets[0].RuntimeIdentifier} and {unchecked((long)value)} on {target.RuntimeIdentifier}.");
                }
                else
                {
                    known.Targets.Add(target);
                }
            }
        }

        return [.. order.Select(valueName => (valueName, values[valueName].Value, GetAvailability(valueName, values[valueName].Targets)))];
    }

    /// <summary>Gets the platforms of the targets that declare something, which must cover whole platform families.</summary>
    /// <param name="name">The name of what is declared, for error messages.</param>
    /// <param name="targets">The targets that declare it.</param>
    /// <returns>The platforms.</returns>
    /// <exception cref="InvalidDataException">Some targets of a family declare it and others do not, which a platform attribute cannot express.</exception>
    private Platforms GetAvailability(string name, IEnumerable<Target> targets)
    {
        var declaring = targets.ToHashSet();
        var availability = declaring.Aggregate(Platforms.None, static (platforms, target) => platforms | target.Platform);
        var missing = _headers.Targets.Where(target => availability.HasFlag(target.Platform) && !declaring.Contains(target)).ToList();

        return missing.Count == 0
            ? availability
            : throw new InvalidDataException($"{_library}: '{name}' is declared for {string.Join(", ", declaring.Select(static target => target.RuntimeIdentifier))} but not for {string.Join(", ", missing.Select(static target => target.RuntimeIdentifier))}: exclude it.");
    }

    /// <summary>Marks the integer typedefs that hold booleans (ADR 0027).</summary>
    private void ApplyBooleans()
    {
        foreach (var name in _configuration.Booleans)
        {
            if (_declarations.GetValueOrDefault(name) is not TypedefDeclaration typedef || Resolve(typedef.Target) is not BuiltinTypeReference)
            {
                throw new InvalidDataException($"{_library}: 'booleans' names '{name}', which is not an integer typedef.");
            }

            _declarations[name] = typedef with { IsBoolean = true };
        }
    }

    /// <summary>Applies the enum annotations: flags for C enums, and enums made of macros for integer typedefs (ADR 0027).</summary>
    /// <param name="patterns">The macro patterns of the configuration.</param>
    /// <param name="claimedMacros">Receives the macros bound as enum values, which no constant may bind again.</param>
    private void ApplyEnums(MacroPatterns patterns, HashSet<string> claimedMacros)
    {
        foreach (var (name, annotation) in _configuration.Enums.Where(static entry => entry.Value.Macros is null))
        {
            _declarations[name] = _declarations.GetValueOrDefault(name) is EnumDeclaration enumDeclaration && annotation.Flags
                ? enumDeclaration with { IsFlags = true }
                : throw new InvalidDataException($"{_library}: 'enums' names '{name}' without macros, which must be a C enum whose values are flags.");
        }

        foreach (var (name, pattern) in patterns.Enums)
        {
            if (_declarations.GetValueOrDefault(name) is not TypedefDeclaration typedef || Resolve(typedef.Target) is not BuiltinTypeReference integerType)
            {
                throw new InvalidDataException($"{_library}: 'enums' names '{name}', which is not an integer typedef.");
            }

            var macros = SelectMacros(pattern.IsMatch, claimedMacros, $"the enum '{name}'");
            var values = MergeValues(name, _headers.ByTarget.Select(target => (target.Target, macros
                .Where(target.MacroValues.ContainsKey)
                .Select(macro => (macro, GetInteger(target, macro, name))))));

            _declarations[name] = new EnumDeclaration
            {
                CName = name,
                Words = typedef.Words,
                Availability = typedef.Availability,
                UnderlyingType = integerType,
                IsFlags = _configuration.Enums[name].Flags,
                Values = [.. values.Select(value => new EnumValueDeclaration { CName = value.Name, Words = [], Availability = value.Availability, Value = value.Value })],
            };
        }
    }

    /// <summary>Binds the macros that the configuration selects as constants.</summary>
    /// <param name="patterns">The macro patterns of the configuration.</param>
    /// <param name="claimedMacros">The macros bound as enum values, which no constant may bind again.</param>
    private void BuildConstants(MacroPatterns patterns, HashSet<string> claimedMacros)
    {
        if (patterns.Constants.Count == 0)
        {
            return;
        }

        var unused = patterns.Constants.ToList();
        var macros = SelectMacros(name => patterns.Constants.Any(pattern => pattern.IsMatch(name)), claimedMacros, "a constant");

        foreach (var name in macros)
        {
            var values = _headers.ByTarget
                .Where(target => target.MacroValues.ContainsKey(name))
                .Select(target => (target.Target, Value: target.MacroValues[name]))
                .ToList();

            if (values.Select(static entry => entry.Value).Distinct().Count() > 1)
            {
                throw new InvalidDataException($"{_library}: the macro {name} has a different value or type on {string.Join(", ", values.Where(entry => entry.Value != values[0].Value).Select(static entry => entry.Target.RuntimeIdentifier))} than on {values[0].Target.RuntimeIdentifier}.");
            }

            var value = values[0].Value;

            _ = unused.RemoveAll(pattern => pattern.IsMatch(name));
            _constants.Add(name, new ConstantDeclaration
            {
                CName = name,
                Words = _names.GetWords(name),
                Availability = GetAvailability(name, values.Select(static entry => entry.Target)),
                Type = GetConstantType(name, value),
                Value = value.Kind switch
                {
                    MacroValueKind.SignedInteger or MacroValueKind.UnsignedInteger => new IntegerExpression(value.Bits),
                    MacroValueKind.Float => new FloatExpression(value.Number),
                    MacroValueKind.String => new StringExpression(value.Text!),
                    _ => throw new UnreachableException($"Unknown macro value kind {value.Kind}."),
                },
                Header = value.Header,
            });
        }

        if (unused.Count > 0)
        {
            throw new InvalidDataException($"{_library}: 'constants' has patterns that match no macro: {string.Join(", ", unused)}.");
        }
    }

    /// <summary>Selects the macros a pattern binds, in definition order, outside the excluded headers.</summary>
    /// <param name="isMatch">Tells whether the pattern matches a macro name.</param>
    /// <param name="claimedMacros">The macros already bound, which receives the selected ones.</param>
    /// <param name="purpose">What the macros are bound as, for error messages.</param>
    /// <returns>The names of the selected macros.</returns>
    /// <exception cref="InvalidDataException">The pattern matches nothing, or a macro another annotation already binds.</exception>
    private List<string> SelectMacros(Func<string, bool> isMatch, HashSet<string> claimedMacros, string purpose)
    {
        var macros = _headers.ByTarget
            .SelectMany(static target => target.Macros)
            .Where(macro => isMatch(macro.Name) && !_configuration.ExcludeHeaders.ContainsKey(macro.Header))
            .Select(static macro => macro.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return macros.Count == 0
            ? throw new InvalidDataException($"{_library}: the pattern of {purpose} matches no macro.")
            : macros.FirstOrDefault(macro => !claimedMacros.Add(macro)) is { } claimed
                ? throw new InvalidDataException($"{_library}: the macro {claimed} is matched by several patterns of 'enums' and 'constants'.")
                : macros;
    }

    /// <summary>Gets the integer value of a macro bound as an enum value.</summary>
    /// <param name="target">The target that defines the macro.</param>
    /// <param name="macro">The name of the macro.</param>
    /// <param name="owner">The C name of the enum, for error messages.</param>
    /// <returns>The value, as the bits of a 64-bit two's complement value.</returns>
    private ulong GetInteger(TargetDeclarations target, string macro, string owner)
    {
        var value = target.MacroValues[macro];

        return value.Kind is MacroValueKind.SignedInteger or MacroValueKind.UnsignedInteger
            ? value.Bits
            : throw new InvalidDataException($"{_library}: the macro {macro} of the enum '{owner}' is not an integer.");
    }

    /// <summary>Gets the type of a constant from the C type of its macro, by size and signedness.</summary>
    /// <param name="name">The name of the macro, for error messages.</param>
    /// <param name="value">Its value.</param>
    /// <returns>A fixed-width integer, <c>float</c>, <c>double</c>, or <c>const char*</c> for a string.</returns>
    private TypeReference GetConstantType(string name, MacroValue value)
    {
        return (value.Kind, value.Size) switch
        {
            (MacroValueKind.SignedInteger, 1 or 2 or 4 or 8) => new BuiltinTypeReference(FormattableString.Invariant($"int{value.Size * 8}_t")),
            (MacroValueKind.UnsignedInteger, 1 or 2 or 4 or 8) => new BuiltinTypeReference(FormattableString.Invariant($"uint{value.Size * 8}_t")),
            (MacroValueKind.Float, 4) => new BuiltinTypeReference("float"),
            (MacroValueKind.Float, 8) => new BuiltinTypeReference("double"),
            (MacroValueKind.String, _) => new PointerTypeReference(new BuiltinTypeReference("char"), IsConst: true),
            _ => throw new InvalidDataException($"{_library}: the macro {name} is a {value.Kind} of {value.Size} bytes, which no C# constant type matches."),
        };
    }

    /// <summary>Makes every pointer to a handle the handle itself, as for WebGPU handles (ADR 0027).</summary>
    /// <remarks>
    /// The rewrite resolves typedefs in the model as merged, so that a typedef of a pointer to a
    /// handle (<c>SDL_GLContext</c>) holds the handle, and a typedef that only renames a handle
    /// (<c>MSG</c> for <c>tagMSG</c>) is dropped once its pointers name the handle.
    /// </remarks>
    /// <exception cref="InvalidDataException">A declaration holds a handle by value.</exception>
    private void RewriteHandles()
    {
        CheckOpaqueClosure();

        var original = new Dictionary<string, Declaration>(_declarations, StringComparer.Ordinal);

        foreach (var (name, declaration) in original)
        {
            switch (declaration)
            {
                case StructureDeclaration structure:
                    _declarations[name] = structure with { Members = [.. structure.Members.Select(member => member with { Type = Rewrite(member.Type, $"{name}.{member.CName}", original) })] };
                    break;

                case FunctionDeclaration function:
                    _declarations[name] = function with
                    {
                        ReturnType = Rewrite(function.ReturnType, name, original),
                        Parameters = [.. function.Parameters.Select(parameter => parameter with { Type = Rewrite(parameter.Type, $"{name}({parameter.CName})", original) })],
                    };
                    break;

                case FunctionPointerDeclaration function:
                    _declarations[name] = function with
                    {
                        ReturnType = Rewrite(function.ReturnType, name, original),
                        Parameters = [.. function.Parameters.Select(parameter => parameter with { Type = Rewrite(parameter.Type, $"{name}({parameter.CName})", original) })],
                    };
                    break;

                case TypedefDeclaration typedef when GetHandle(typedef.Target, original) is { } handle:
                    _ = _declarations.Remove(name);
                    Skip(name, $"another name of the handle '{handle}', which the bindings use instead");
                    break;

                case TypedefDeclaration typedef:
                    _declarations[name] = typedef with { Target = Rewrite(typedef.Target, name, original) };
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>Checks that every structure that holds an opaque type by value is opaque too, listing all of them at once.</summary>
    /// <exception cref="InvalidDataException">Structures hold opaque types by value; the message names every one, transitively.</exception>
    private void CheckOpaqueClosure()
    {
        var opaque = _declarations.Values.OfType<HandleDeclaration>().Select(static handle => handle.CName).ToHashSet(StringComparer.Ordinal);
        var holders = new SortedSet<string>(StringComparer.Ordinal);
        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var structure in _declarations.Values.OfType<StructureDeclaration>())
            {
                if (!opaque.Contains(structure.CName) && structure.Members.Any(member => HoldsByValue(member.Type, opaque)))
                {
                    changed = opaque.Add(structure.CName) && holders.Add(structure.CName);
                }
            }
        }

        if (holders.Count > 0)
        {
            throw new InvalidDataException($"{_library}: these structures hold an opaque type by value, directly or through each other: make them opaque too or exclude them: {string.Join(", ", holders)}.");
        }
    }

    /// <summary>Tells whether a member type holds one of a set of types by value, through arrays and typedefs.</summary>
    /// <param name="type">The member type.</param>
    /// <param name="names">The C names of the types.</param>
    /// <returns><see langword="true"/> when the member embeds one of them.</returns>
    private bool HoldsByValue(TypeReference type, IReadOnlySet<string> names)
    {
        return type switch
        {
            ArrayTypeReference array => HoldsByValue(array.Element, names),
            NamedTypeReference named => names.Contains(named.CName)
                || (_declarations.GetValueOrDefault(named.CName) is TypedefDeclaration { IsBoolean: false } typedef && HoldsByValue(typedef.Target, names)),
            _ => false,
        };
    }

    /// <summary>Rewrites the pointers to handles in a type.</summary>
    /// <param name="type">The type.</param>
    /// <param name="referrer">The declaration or member that uses it, for error messages.</param>
    /// <param name="original">The declarations before the rewrite.</param>
    /// <returns>The type, with every pointer to a handle replaced by the handle.</returns>
    private TypeReference Rewrite(TypeReference type, string referrer, IReadOnlyDictionary<string, Declaration> original)
    {
        return type switch
        {
            PointerTypeReference pointer when GetHandle(pointer.Pointee, original) is { } handle => new NamedTypeReference(handle),
            PointerTypeReference pointer => pointer with { Pointee = Rewrite(pointer.Pointee, referrer, original) },
            NamedTypeReference named when GetHandle(named, original) is { } handle =>
                throw new InvalidDataException($"{_library}: '{referrer}' holds the opaque '{handle}' by value: make it opaque too or exclude it."),
            ArrayTypeReference array => array with { Element = Rewrite(array.Element, referrer, original) },
            FunctionPointerTypeReference function => new FunctionPointerTypeReference(
                Rewrite(function.ReturnType, referrer, original),
                [.. function.ParameterTypes.Select(parameter => Rewrite(parameter, referrer, original))]),
            _ => type,
        };
    }

    /// <summary>Gets the handle a type names, through typedefs.</summary>
    /// <param name="type">The type.</param>
    /// <param name="declarations">The declarations to resolve the names in.</param>
    /// <returns>The C name of the handle, or <see langword="null"/> when the type is not a handle.</returns>
    private static string? GetHandle(TypeReference type, IReadOnlyDictionary<string, Declaration> declarations)
    {
        return type is NamedTypeReference named
            ? declarations.GetValueOrDefault(named.CName) switch
            {
                HandleDeclaration handle => handle.CName,
                TypedefDeclaration { IsBoolean: false } typedef => GetHandle(typedef.Target, declarations),
                _ => null,
            }
            : null;
    }

    /// <summary>Leaves out the declarations of the excluded headers, except the types that the other headers use.</summary>
    private void ExcludeHeaders()
    {
        var excluded = _declarations.Keys.Where(name => _configuration.ExcludeHeaders.ContainsKey(_headerOf[name])).ToHashSet(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<Declaration>(_declarations.Values.Where(declaration => !excluded.Contains(declaration.CName)));

        while (pending.TryDequeue(out var declaration))
        {
            foreach (var name in GetReferences(declaration).Where(excluded.Contains))
            {
                if (used.Add(name))
                {
                    pending.Enqueue(_declarations[name]);
                }
            }
        }

        foreach (var name in excluded.Where(name => !used.Contains(name)))
        {
            var header = _headerOf[name];

            _ = _declarations.Remove(name);
            Skip(name, $"declared in {header}: {_configuration.ExcludeHeaders[header]}");
        }

        CheckUsed("excludeHeaders", _configuration.ExcludeHeaders.Keys, _headerOf.ContainsValue);
    }

    /// <summary>Leaves out the declarations that use a type with no portable C# equivalent (ADR 0027).</summary>
    private void SkipUnsupportedDeclarations()
    {
        foreach (var declaration in _declarations.Values.ToList())
        {
            if (GetTypes(declaration).Select(FindUnsupportedType).FirstOrDefault(static type => type is not null) is { } type)
            {
                _ = _declarations.Remove(declaration.CName);
                Skip(declaration.CName, $"uses {type}, whose size differs between targets or which C# cannot represent (ADR 0027)");
            }
        }
    }

    /// <summary>Finds a type a declaration cannot use, through pointers and typedefs.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The C spelling of the unsupported type, or <see langword="null"/>.</returns>
    private string? FindUnsupportedType(TypeReference type)
    {
        return type switch
        {
            BuiltinTypeReference builtin => _unsupportedTypes.Contains(builtin.Spelling, StringComparer.Ordinal) ? builtin.Spelling : null,
            PointerTypeReference pointer => FindUnsupportedType(pointer.Pointee),
            ArrayTypeReference array => FindUnsupportedType(array.Element),
            FunctionPointerTypeReference function => FindUnsupportedType(function.ReturnType) ?? function.ParameterTypes.Select(FindUnsupportedType).FirstOrDefault(static spelling => spelling is not null),
            NamedTypeReference named => _declarations.GetValueOrDefault(named.CName) switch
            {
                TypedefDeclaration typedef => FindUnsupportedType(typedef.Target),
                FunctionPointerDeclaration function => GetTypes(function).Select(FindUnsupportedType).FirstOrDefault(static spelling => spelling is not null),
                _ => null,
            },
            _ => null,
        };
    }

    /// <summary>Checks that every type a declaration names is bound.</summary>
    /// <exception cref="InvalidDataException">A declaration uses a type that is left out or not declared.</exception>
    private void CheckReferences()
    {
        foreach (var declaration in _declarations.Values)
        {
            if (GetReferences(declaration).FirstOrDefault(name => !_declarations.ContainsKey(name)) is { } missing)
            {
                var reason = _skipped.TryGetValue(missing, out var skipped) ? $"is left out ({skipped.Reason})" : "is not declared";

                throw new InvalidDataException($"{_library}: '{declaration.CName}' uses '{missing}', which {reason}.");
            }
        }
    }

    /// <summary>Checks that the shim allocates and frees every opaque type, as ADR 0006 requires.</summary>
    /// <exception cref="InvalidDataException">An opaque type lacks a function, or a function has the wrong signature.</exception>
    private void CheckAllocators()
    {
        if (_configuration.OpaqueAllocators is not { } allocators)
        {
            return;
        }

        foreach (var name in _configuration.Opaque.Keys.Order(StringComparer.Ordinal))
        {
            var allocate = allocators.Allocate.Replace(OpaqueAllocators.NamePlaceholder, name, StringComparison.Ordinal);
            var free = allocators.Free.Replace(OpaqueAllocators.NamePlaceholder, name, StringComparison.Ordinal);
            var handle = new NamedTypeReference(name);
            var hasAllocate = _declarations.GetValueOrDefault(allocate) is FunctionDeclaration { Parameters.Count: 0 } allocateFunction && allocateFunction.ReturnType == handle;
            var hasFree = _declarations.GetValueOrDefault(free) is FunctionDeclaration { Parameters: [{ } parameter] } freeFunction
                && parameter.Type == handle
                && freeFunction.ReturnType == BuiltinTypeReference.Void;

            if (!hasAllocate || !hasFree)
            {
                throw new InvalidDataException($"{_library}: '{name}' is opaque, so the shim must declare '{name}* {allocate}(void)' and 'void {free}({name}*)'.");
            }
        }
    }

    /// <summary>Names the values of every enum after the part of their C names they do not share (ADR 0033).</summary>
    private void NameEnumValues()
    {
        foreach (var enumDeclaration in _declarations.Values.OfType<EnumDeclaration>().ToList())
        {
            var prefix = CNames.GetCommonPrefix([.. enumDeclaration.Values.Select(static value => value.CName)]);

            _declarations[enumDeclaration.CName] = enumDeclaration with
            {
                Values = [.. enumDeclaration.Values.Select(value => value with { Words = CNames.Split(value.CName[prefix.Length..]) })],
            };
        }
    }

    /// <summary>Resolves a type through the typedefs of the model.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The type a chain of typedefs ends at.</returns>
    private TypeReference Resolve(TypeReference type)
    {
        return type is NamedTypeReference named && _declarations.GetValueOrDefault(named.CName) is TypedefDeclaration typedef
            ? Resolve(typedef.Target)
            : type;
    }

    /// <summary>Records a declaration left out.</summary>
    /// <param name="name">The name.</param>
    /// <param name="reason">Why it is left out.</param>
    private void Skip(string name, string reason)
    {
        _skipped[name] = new SkippedDeclaration(name, reason);
    }

    /// <summary>Checks that every entry of an annotation matches something.</summary>
    /// <param name="annotation">The name of the annotation, for the error message.</param>
    /// <param name="keys">The entries.</param>
    /// <param name="isUsed">Tells whether an entry matches something.</param>
    /// <exception cref="InvalidDataException">An entry matches nothing, so the configuration drifted from the headers.</exception>
    private void CheckUsed(string annotation, IEnumerable<string> keys, Func<string, bool> isUsed)
    {
        var unused = keys.Where(key => !isUsed(key)).Order(StringComparer.Ordinal).ToList();

        if (unused.Count > 0)
        {
            throw new InvalidDataException($"{_library}: '{annotation}' has entries that match nothing: {string.Join(", ", unused)}.");
        }
    }

    /// <summary>Gets the names of the declarations a declaration uses.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <returns>The C names, with repetitions.</returns>
    private static IEnumerable<string> GetReferences(Declaration declaration)
    {
        return GetTypes(declaration).SelectMany(GetNames);
    }

    /// <summary>Gets the types a declaration is made of.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <returns>The member, parameter, return and target types.</returns>
    private static IEnumerable<TypeReference> GetTypes(Declaration declaration)
    {
        return declaration switch
        {
            StructureDeclaration structure => structure.Members.Select(static member => member.Type),
            FunctionDeclaration function => function.Parameters.Select(static parameter => parameter.Type).Append(function.ReturnType),
            FunctionPointerDeclaration function => function.Parameters.Select(static parameter => parameter.Type).Append(function.ReturnType),
            TypedefDeclaration typedef => [typedef.Target],
            EnumDeclaration enumDeclaration => [enumDeclaration.UnderlyingType],
            _ => [],
        };
    }

    /// <summary>Gets the names of the declarations a type uses.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The C names.</returns>
    private static IEnumerable<string> GetNames(TypeReference type)
    {
        return type switch
        {
            NamedTypeReference named => [named.CName],
            PointerTypeReference pointer => GetNames(pointer.Pointee),
            ArrayTypeReference array => GetNames(array.Element),
            FunctionPointerTypeReference function => GetNames(function.ReturnType).Concat(function.ParameterTypes.SelectMany(GetNames)),
            _ => [],
        };
    }
}
