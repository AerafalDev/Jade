using System.Diagnostics;
using System.Globalization;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Projection;

/// <summary>Projects the intermediate representation onto the idiomatic layer above the raw layer (ADR 0029, ADR 0040).</summary>
/// <remarks>
/// Structures are classified by how the functions use them: input structures with pointers become
/// mirrors, output structures with pointers become managed copies, and the others are the value
/// structures of the raw layer. Functions become members of the handle they belong to, with the
/// conversions their parameters need. Anything the rules cannot express fails the generator until
/// the configuration skips it or marks it written by hand, so that nothing is left out silently.
/// </remarks>
internal sealed class IdiomaticProjection
{
    /// <summary>The words of the string view structure.</summary>
    private static readonly string[] _stringViewWords = ["string", "view"];

    /// <summary>The words of the chain header structure.</summary>
    private static readonly string[] _chainHeaderWords = ["chained", "struct"];

    /// <summary>The words of the status enum that functions return.</summary>
    private static readonly string[] _statusWords = ["status"];

    /// <summary>The words of the enum value that a callback's status has on success.</summary>
    private static readonly string[] _successWords = ["success"];

    /// <summary>The members every handle declares, which no idiomatic member may take.</summary>
    private static readonly string[] _handleMembers = ["Handle", "Equals", "GetHashCode", "GetType", "ToString", "Dispose", "AddRef", "Release"];

    /// <summary>The model to project.</summary>
    private readonly ApiModel _model;

    /// <summary>The raw layer, whose names and types the idiomatic layer uses.</summary>
    private readonly ProjectedLibrary _raw;

    /// <summary>The .NET naming rules.</summary>
    private readonly DotNetNames _names;

    /// <summary>The settings of the idiomatic layer.</summary>
    private readonly IdiomaticConfiguration _configuration;

    /// <summary>The raw types, by C name; an inline array is keyed by <c>Structure.member</c>.</summary>
    private readonly Dictionary<string, ProjectedType> _rawTypes = [with(StringComparer.Ordinal)];

    /// <summary>The raw functions, by C name.</summary>
    private readonly Dictionary<string, ProjectedFunction> _rawFunctions = [with(StringComparer.Ordinal)];

    /// <summary>What each structure becomes, by C name.</summary>
    private readonly Dictionary<string, IdiomaticRole> _roles = [with(StringComparer.Ordinal)];

    /// <summary>The structures that input functions or input structures reach, by C name.</summary>
    private readonly HashSet<string> _inputs = [with(StringComparer.Ordinal)];

    /// <summary>The structures that output parameters or callbacks reach, by C name.</summary>
    private readonly HashSet<string> _outputs = [with(StringComparer.Ordinal)];

    /// <summary>The input structures used as array elements, by C name.</summary>
    private readonly HashSet<string> _elements = [with(StringComparer.Ordinal)];

    /// <summary>The mirrors that a member reaches through an optional pointer, by C name.</summary>
    private readonly HashSet<string> _presence = [with(StringComparer.Ordinal)];

    /// <summary>The extensions of each chain root, by C name of the root.</summary>
    private readonly Dictionary<string, List<StructureDeclaration>> _extensions = [with(StringComparer.Ordinal)];

    /// <summary>The mirrors and snapshots built so far, by C name.</summary>
    private readonly Dictionary<string, IdiomaticStructure> _structures = [with(StringComparer.Ordinal)];

    /// <summary>The slots of the nested chain roots, by C name of the root.</summary>
    private readonly Dictionary<string, IdiomaticSlots> _slots = [with(StringComparer.Ordinal)];

    /// <summary>The constants placed on each handle or structure, by C name of the owner.</summary>
    private readonly Dictionary<string, List<IdiomaticConstant>> _constants = [with(StringComparer.Ordinal)];

    /// <summary>The C# expressions of the placed constants that are C# constants, by C name of the constant.</summary>
    private readonly Dictionary<string, string> _constantReferences = [with(StringComparer.Ordinal)];

    /// <summary>The keys of <see cref="IdiomaticConfiguration.HandWritten"/> and <see cref="IdiomaticConfiguration.Skip"/> that matched.</summary>
    private readonly HashSet<string> _usedKeys = [with(StringComparer.Ordinal)];

    /// <summary>Initializes a new instance of the <see cref="IdiomaticProjection"/> class.</summary>
    /// <param name="model">The model to project.</param>
    /// <param name="raw">The raw layer.</param>
    /// <param name="names">The .NET naming rules.</param>
    /// <param name="configuration">The settings of the idiomatic layer.</param>
    private IdiomaticProjection(ApiModel model, ProjectedLibrary raw, DotNetNames names, IdiomaticConfiguration configuration)
    {
        _model = model;
        _raw = raw;
        _names = names;
        _configuration = configuration;
    }

    /// <summary>Projects a library's model onto its idiomatic layer.</summary>
    /// <param name="model">The model of the library's API.</param>
    /// <param name="raw">The raw layer projected from the same model.</param>
    /// <param name="names">The .NET naming rules.</param>
    /// <param name="configuration">The settings of the idiomatic layer.</param>
    /// <returns>The idiomatic layer.</returns>
    /// <exception cref="InvalidDataException">The model uses a construct the rules do not cover, or the configuration names nothing.</exception>
    public static IdiomaticLibrary Project(ApiModel model, ProjectedLibrary raw, DotNetNames names, IdiomaticConfiguration configuration)
    {
        return new IdiomaticProjection(model, raw, names, configuration).Project();
    }

    /// <summary>Projects the model.</summary>
    /// <returns>The idiomatic layer.</returns>
    private IdiomaticLibrary Project()
    {
        foreach (var type in _raw.Types)
        {
            _rawTypes.Add(type.CName, type);
        }

        foreach (var function in _raw.Functions)
        {
            _rawFunctions.Add(function.CName, function);
        }

        foreach (var structure in _model.Structures)
        {
            foreach (var root in structure.ChainRoots)
            {
                if (!_extensions.TryGetValue(root, out var extensions))
                {
                    _extensions.Add(root, extensions = []);
                }

                extensions.Add(structure);
            }
        }

        PlaceConstants();
        Classify();

        var structures = _model.Structures
            .Where(structure => _roles[structure.CName] is IdiomaticRole.Mirror or IdiomaticRole.ElementMirror or IdiomaticRole.Snapshot)
            .Select(GetStructure)
            .OrderBy(static structure => structure.Name, StringComparer.Ordinal)
            .ToList();
        var valueStructures = _model.Structures
            .Where(structure => _roles[structure.CName] == IdiomaticRole.Value)
            .Select(ProjectValueStructure)
            .OfType<IdiomaticValueStructure>()
            .OrderBy(static structure => structure.Name, StringComparer.Ordinal)
            .ToList();
        var freeFunctions = PlaceFreeFunctions();
        var handles = _model.Handles
            .Select(handle => ProjectHandle(handle, freeFunctions.GetValueOrDefault(handle.CName, [])))
            .OrderBy(static handle => handle.Name, StringComparer.Ordinal)
            .ToList();

        CheckConfigurationUsed();

        // The new public types share the library's namespace with the public types of the raw
        // layer, and the files of both share Generated/: no two may differ only in case.
        IEnumerable<string> publicNames = [.. _raw.Types.Where(static type => type.IsPublic).Select(static type => type.Name), .. structures.Select(static structure => structure.Name), .. _slots.Values.Select(static slots => slots.Name)];

        RawProjection.CheckUnique("public type", _raw.Namespace, publicNames, StringComparer.OrdinalIgnoreCase);

        var omitted = _configuration.Skip.Concat(_configuration.HandWritten)
            .Select(static entry => (entry.Key, entry.Value))
            .Concat(_model.Structures.Where(structure => _roles[structure.CName] == IdiomaticRole.Unused).Select(static structure => (structure.CName, "not reached by any function the idiomatic layer exposes")))
            .OrderBy(static entry => entry.Item1, StringComparer.Ordinal)
            .ToList();
        var internalConstants = _model.Constants
            .Where(constant => !_configuration.Constants.ContainsKey(constant.CName))
            .Select(static constant => constant.CName)
            .ToList();

        var chainHeader = (ProjectedStructure)_rawTypes[_model.Structures.Single(structure => _roles[structure.CName] == IdiomaticRole.ChainHeader).CName];

        return new IdiomaticLibrary(
            _raw.Namespace,
            handles,
            structures,
            valueStructures,
            [.. _slots.Values.OrderBy(static slots => slots.Name, StringComparer.Ordinal)],
            omitted,
            internalConstants,
            chainHeader.Fields[0].Name,
            chainHeader.Fields[1].Name);
    }

    /// <summary>Places the configured constants on their handle or structure.</summary>
    /// <exception cref="InvalidDataException">A constant or its owner does not exist.</exception>
    private void PlaceConstants()
    {
        foreach (var (cName, ownerCName) in _configuration.Constants.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            var constant = _raw.Constants.FirstOrDefault(constant => constant.CName == cName)
                ?? throw new InvalidDataException($"'idiomatic.constants' names '{cName}', which is not a constant.");
            var owner = _model.Find(ownerCName) is HandleDeclaration or StructureDeclaration && _rawTypes.TryGetValue(ownerCName, out var ownerType)
                ? ownerType
                : throw new InvalidDataException($"'idiomatic.constants' places '{cName}' on '{ownerCName}', which is not a handle or a structure.");

            if (!_constants.TryGetValue(ownerCName, out var placed))
            {
                _constants.Add(ownerCName, placed = []);
            }

            placed.Add(new IdiomaticConstant(cName, constant.Name, constant.Type, constant.Value, constant.IsConst));

            if (constant.IsConst)
            {
                _constantReferences.Add(cName, $"{owner.Name}.{constant.Name}");
            }
        }
    }

    /// <summary>Classifies every structure from how the functions use it.</summary>
    /// <exception cref="InvalidDataException">A structure with pointers is both an input and an output.</exception>
    private void Classify()
    {
        foreach (var function in _model.Functions)
        {
            if (function.Kind is FunctionKind.FreeMembers or FunctionKind.AddRef or FunctionKind.Release || IsOmitted(function.CName))
            {
                continue;
            }

            foreach (var parameter in function.Parameters)
            {
                switch (parameter.Type)
                {
                    case PointerTypeReference { Pointee: NamedTypeReference named, IsConst: true } when _model.Find(named.CName) is StructureDeclaration:
                        MarkInput(named.CName, parameter.Length is not null);
                        break;

                    case PointerTypeReference { Pointee: NamedTypeReference named, IsConst: false } when parameter.Length is null && _model.Find(named.CName) is StructureDeclaration:
                        MarkOutput(named.CName);
                        break;

                    case NamedTypeReference named when _model.Find(named.CName) is StructureDeclaration { IsCallbackInfo: true } callbackInfo:
                        foreach (var callbackParameter in GetCallback(callbackInfo).Parameters)
                        {
                            if (callbackParameter.Type is PointerTypeReference { Pointee: NamedTypeReference pointee } && _model.Find(pointee.CName) is StructureDeclaration)
                            {
                                MarkOutput(pointee.CName);
                            }
                        }

                        break;

                    default:
                        break;
                }
            }
        }

        // An extension is reached through its roots, which may themselves be extensions' roots.
        bool changed;

        do
        {
            changed = false;

            foreach (var extension in _model.Structures.Where(structure => structure.Chained != ChainDirection.None && !IsSkipped(structure.CName)))
            {
                var roots = extension.ChainRoots;

                if (extension.Chained == ChainDirection.In && !_inputs.Contains(extension.CName) && roots.Any(_inputs.Contains))
                {
                    MarkInput(extension.CName, asElement: false);
                    changed = true;
                }
                else if (extension.Chained == ChainDirection.Out && !_outputs.Contains(extension.CName) && roots.Any(_outputs.Contains))
                {
                    MarkOutput(extension.CName);
                    changed = true;
                }
            }
        }
        while (changed);

        foreach (var structure in _model.Structures)
        {
            _roles.Add(structure.CName, GetRole(structure));
        }

        foreach (var structure in _model.Structures.Where(structure => _roles[structure.CName] is IdiomaticRole.Mirror or IdiomaticRole.ElementMirror))
        {
            foreach (var member in structure.Members)
            {
                if (member is { Type: PointerTypeReference { Pointee: NamedTypeReference pointee }, Length: null, IsOptional: true } && _roles.GetValueOrDefault(pointee.CName) == IdiomaticRole.Mirror)
                {
                    _ = _presence.Add(pointee.CName);
                }
            }
        }
    }

    /// <summary>Gets what a structure becomes.</summary>
    /// <param name="structure">The structure.</param>
    /// <returns>Its role.</returns>
    /// <exception cref="InvalidDataException">A structure with pointers is both an input and an output.</exception>
    private IdiomaticRole GetRole(StructureDeclaration structure)
    {
        return structure.Words.SequenceEqual(_stringViewWords, StringComparer.Ordinal) ? IdiomaticRole.StringView
            : structure.Words.SequenceEqual(_chainHeaderWords, StringComparer.Ordinal) ? IdiomaticRole.ChainHeader
            : structure.IsCallbackInfo ? IdiomaticRole.CallbackInfo
            : IsSkipped(structure.CName) ? IdiomaticRole.Skipped
            : IsValue(structure) ? IdiomaticRole.Value
            : (_inputs.Contains(structure.CName), _outputs.Contains(structure.CName)) switch
            {
                (true, true) => throw new InvalidDataException($"'{structure.CName}' is both an input and an output of the API, which the idiomatic layer does not support."),
                (true, false) => _elements.Contains(structure.CName) ? IdiomaticRole.ElementMirror : IdiomaticRole.Mirror,
                (false, true) => IdiomaticRole.Snapshot,
                (false, false) => IdiomaticRole.Unused,
            };
    }

    /// <summary>Marks a structure, and the structures it holds, as inputs.</summary>
    /// <param name="cName">The C name of the structure.</param>
    /// <param name="asElement">Whether the structure is used as an array element.</param>
    private void MarkInput(string cName, bool asElement)
    {
        if (_model.Find(cName) is not StructureDeclaration structure || structure.IsCallbackInfo || IsSkipped(cName))
        {
            return;
        }

        if (asElement)
        {
            _ = _elements.Add(cName);
        }

        if (!_inputs.Add(cName))
        {
            return;
        }

        foreach (var member in structure.Members.Where(static member => member.Role == MemberRole.Value))
        {
            switch (member.Type)
            {
                case NamedTypeReference named:
                    MarkInput(named.CName, asElement: false);
                    break;

                case PointerTypeReference { Pointee: NamedTypeReference named }:
                    MarkInput(named.CName, member.Length is not null);
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>Marks a structure, and the structures it holds, as outputs.</summary>
    /// <param name="cName">The C name of the structure.</param>
    private void MarkOutput(string cName)
    {
        if (_model.Find(cName) is not StructureDeclaration structure || IsSkipped(cName) || !_outputs.Add(cName))
        {
            return;
        }

        foreach (var member in structure.Members.Where(static member => member.Role == MemberRole.Value))
        {
            var named = member.Type switch
            {
                NamedTypeReference type => type,
                PointerTypeReference { Pointee: NamedTypeReference pointee } => pointee,
                _ => null,
            };

            if (named is not null)
            {
                MarkOutput(named.CName);
            }
        }
    }

    /// <summary>Gets the mirror or snapshot of a structure, building it the first time.</summary>
    /// <param name="structure">The structure.</param>
    /// <returns>The idiomatic structure.</returns>
    private IdiomaticStructure GetStructure(StructureDeclaration structure)
    {
        if (_structures.TryGetValue(structure.CName, out var built))
        {
            return built;
        }

        var role = _roles[structure.CName];
        var raw = (ProjectedStructure)_rawTypes[structure.CName];
        var counts = structure.Members.Select(static member => member.Length).OfType<MemberArrayLength>().Select(static length => length.CName).ToHashSet(StringComparer.Ordinal);
        var members = new List<IdiomaticMember>();

        for (var i = 0; i < structure.Members.Count; i++)
        {
            var member = structure.Members[i];
            var field = raw.Fields[i];

            members.Add(role == IdiomaticRole.Snapshot
                ? ProjectSnapshotMember(structure, member, field, counts)
                : ProjectMirrorMember(structure, role, member, field, counts));
        }

        members = [.. members.Select(member => member.Member.Length is MemberArrayLength length
            ? member with { CountField = members.Single(count => count.Member.CName == length.CName).Name, CountType = members.Single(count => count.Member.CName == length.CName).RawType }
            : member)];

        var visible = members.Where(static member => member.Type.Length > 0).Select(static member => member.Name)
            .Concat(members.Select(static member => member.Slots?.Name).OfType<string>())
            .Concat(_constants.GetValueOrDefault(structure.CName, []).Select(static constant => constant.Name))
            .Append(raw.Name);

        RawProjection.CheckUnique("member", structure.CName, visible, StringComparer.Ordinal);

        var isMirror = role is IdiomaticRole.Mirror or IdiomaticRole.ElementMirror;

        built = new IdiomaticStructure
        {
            Declaration = structure,
            Name = raw.Name,
            RawType = $"{RawProjection.RawNamespace}.{raw.Name}",
            Role = role,
            Availability = structure.Availability,
            Members = members,
            Defaults = isMirror ? [.. raw.Defaults.Where(assignment => members.Any(member => member.Name == assignment.Target && member.Kind is IdiomaticMemberKind.Value or IdiomaticMemberKind.Boolean or IdiomaticMemberKind.Nested))] : [],
            HasPresence = _presence.Contains(structure.CName),
            FreeMembers = role == IdiomaticRole.Snapshot ? GetFreeMembers(structure) : null,
            InputRoots = isMirror && structure.Chained == ChainDirection.In ? GetRootNames(structure, input: true) : [],
            OutputRoots = role == IdiomaticRole.Snapshot && structure.Chained == ChainDirection.Out ? GetRootNames(structure, input: false) : [],
            StructureType = structure.Chained != ChainDirection.None ? GetStructureType(structure) : null,
            Constants = _constants.GetValueOrDefault(structure.CName, []),
        };

        _structures.Add(structure.CName, built);

        return built;
    }

    /// <summary>Projects a member of a mirror or an element mirror.</summary>
    /// <param name="structure">The structure that holds the member.</param>
    /// <param name="role">Whether the structure is a mirror or an element mirror.</param>
    /// <param name="member">The member.</param>
    /// <param name="field">The raw field of the member.</param>
    /// <param name="counts">The C names of the members that hold the count of another.</param>
    /// <returns>The idiomatic member.</returns>
    /// <exception cref="InvalidDataException">The member's type has no idiomatic form.</exception>
    private IdiomaticMember ProjectMirrorMember(StructureDeclaration structure, IdiomaticRole role, StructureMember member, ProjectedField field, HashSet<string> counts)
    {
        var referrer = $"{structure.CName}.{member.CName}";
        var isRef = role == IdiomaticRole.Mirror;
        var result = new IdiomaticMember { Member = member, Name = field.Name, Kind = IdiomaticMemberKind.Value, Type = string.Empty, RawType = SpellRaw(member.Type), IsOptional = member.IsOptional };

        if (IsHandWritten(referrer))
        {
            return ProjectHandWrittenMember(result, member, referrer, isRef);
        }

        switch (member.Role)
        {
            case MemberRole.NextInChain:
                return result with { Kind = IdiomaticMemberKind.NextInChain };

            case MemberRole.ChainHeader:
                return result with { Kind = IdiomaticMemberKind.ChainHeader, StructureType = GetStructureType(structure) };

            case MemberRole.Userdata:
                throw new InvalidDataException($"'{referrer}' is userdata, which only a callback info holds.");

            case MemberRole.Value:
            default:
                break;
        }

        if (counts.Contains(member.CName))
        {
            return result with { Kind = IdiomaticMemberKind.Count };
        }

        switch (member.Type)
        {
            case BuiltinTypeReference builtin when builtin.Spelling != "void":
                return result with { Type = result.RawType };

            case NamedTypeReference named:
                switch (_model.Find(named.CName))
                {
                    case EnumDeclaration:
                        return result with { Type = result.RawType };

                    case HandleDeclaration:
                        return result with { Type = result.RawType, IsHandle = true };

                    case TypedefDeclaration { IsBoolean: true }:
                        return result with { Kind = IdiomaticMemberKind.Boolean, Type = "bool" };

                    case FunctionPointerDeclaration:
                        return result with { Kind = IdiomaticMemberKind.FunctionPointer, Type = result.RawType };

                    case StructureDeclaration nested:
                        return _roles[nested.CName] switch
                        {
                            IdiomaticRole.StringView => result with { Kind = IdiomaticMemberKind.Text, Type = isRef ? "Utf8Text" : "string?" },
                            IdiomaticRole.Value => result with { Type = result.RawType },
                            IdiomaticRole.Mirror when isRef => result with { Kind = IdiomaticMemberKind.Nested, Type = GetName(nested), RawNestedType = result.RawType },
                            IdiomaticRole.ElementMirror => result with { Kind = IdiomaticMemberKind.Nested, Type = GetName(nested), RawNestedType = result.RawType },
                            IdiomaticRole.CallbackInfo => throw new InvalidDataException($"'{referrer}' is a callback info: add it to 'idiomatic.handWritten', or the structure to 'idiomatic.skip'."),
                            IdiomaticRole.Unused or IdiomaticRole.Mirror or IdiomaticRole.Snapshot or IdiomaticRole.ChainHeader or IdiomaticRole.Skipped => throw new InvalidDataException($"'{referrer}' holds '{nested.CName}', a {_roles[nested.CName]} structure, which a {role} cannot hold."),
                            _ => throw new UnreachableException("Unknown structure role."),
                        };

                    default:
                        break;
                }

                break;

            case PointerTypeReference pointer when member.Length is not null:
                return ProjectSpanMember(result, pointer, member.Length, isRef, referrer);

            case PointerTypeReference { Pointee: BuiltinTypeReference { Spelling: "void" } }:
                return result with { Kind = IdiomaticMemberKind.Pointer, Type = "nint" };

            case PointerTypeReference { Pointee: NamedTypeReference pointee, IsConst: true } when _model.Find(pointee.CName) is StructureDeclaration nested:
                var rawNested = SpellRaw(pointee);

                return _roles[nested.CName] switch
                {
                    IdiomaticRole.Value => result with
                    {
                        Kind = IdiomaticMemberKind.ValuePointer,
                        Type = member.IsOptional ? $"{rawNested}?" : rawNested,
                        RawNestedType = rawNested,
                        Slots = GetSlots(nested),
                    },
                    IdiomaticRole.Mirror when isRef => result with { Kind = IdiomaticMemberKind.NestedPointer, Type = GetName(nested), RawNestedType = rawNested },
                    IdiomaticRole.Unused or IdiomaticRole.Mirror or IdiomaticRole.ElementMirror or IdiomaticRole.Snapshot or IdiomaticRole.StringView or IdiomaticRole.ChainHeader or IdiomaticRole.CallbackInfo or IdiomaticRole.Skipped => throw new InvalidDataException($"'{referrer}' points to '{nested.CName}', a {_roles[nested.CName]} structure, which a {role} cannot point to."),
                    _ => throw new UnreachableException("Unknown structure role."),
                };

            default:
                break;
        }

        throw new InvalidDataException($"'{referrer}' has a type the idiomatic layer does not support: add it to 'idiomatic.handWritten', or the structure to 'idiomatic.skip'.");
    }

    /// <summary>Projects a member that a hand-written partial method lowers.</summary>
    /// <param name="result">The member as projected so far.</param>
    /// <param name="member">The member.</param>
    /// <param name="referrer">The C name of the member, for error messages.</param>
    /// <param name="isRef">Whether the member belongs to a <c>ref struct</c> mirror.</param>
    /// <returns>The idiomatic member.</returns>
    /// <exception cref="InvalidDataException">The member is not a callback info of a <c>ref struct</c> mirror.</exception>
    /// <remarks>
    /// A callback info becomes a field of the delegate type named after its callback, declared here
    /// because the fields of a struct must all be in one partial declaration (CS0282); the
    /// delegate type and the lowering are written by hand.
    /// </remarks>
    private IdiomaticMember ProjectHandWrittenMember(IdiomaticMember result, StructureMember member, string referrer, bool isRef)
    {
        if (!isRef || member.Type is not NamedTypeReference named || _model.Find(named.CName) is not StructureDeclaration { IsCallbackInfo: true } callbackInfo || member.Words.Count < 3 || member.Words[^2] != "callback" || member.Words[^1] != "info")
        {
            throw new InvalidDataException($"'{referrer}' is marked hand-written, which only a callback info of a descriptor can be.");
        }

        var callback = GetCallback(callbackInfo);

        return result with
        {
            Kind = IdiomaticMemberKind.HandWritten,
            Name = _names.GetMemberName(referrer, [.. member.Words.Take(member.Words.Count - 2)]),
            Type = _names.GetName(callback.CName, callback.Words) + "?",
        };
    }

    /// <summary>Projects a member that points to an array.</summary>
    /// <param name="result">The member as projected so far.</param>
    /// <param name="pointer">The type of the member.</param>
    /// <param name="length">The length of the array.</param>
    /// <param name="isRef">Whether the member belongs to a <c>ref struct</c> mirror, which takes spans rather than memory.</param>
    /// <param name="referrer">The C name of the member, for error messages.</param>
    /// <returns>The idiomatic member.</returns>
    private IdiomaticMember ProjectSpanMember(IdiomaticMember result, PointerTypeReference pointer, ArrayLength length, bool isRef, string referrer)
    {
        var projected = ProjectSpanElements(result, pointer, length, isRef, referrer);

        return projected.Slots is not null && !isRef
            ? throw new InvalidDataException($"'{referrer}' holds chain roots with extensions in an element mirror, whose slots the idiomatic layer does not support.")
            : projected;
    }

    /// <summary>Projects the elements of a member that points to an array.</summary>
    /// <param name="result">The member as projected so far.</param>
    /// <param name="pointer">The type of the member.</param>
    /// <param name="length">The length of the array.</param>
    /// <param name="isRef">Whether the member belongs to a <c>ref struct</c> mirror, which takes spans rather than memory.</param>
    /// <param name="referrer">The C name of the member, for error messages.</param>
    /// <returns>The idiomatic member.</returns>
    private IdiomaticMember ProjectSpanElements(IdiomaticMember result, PointerTypeReference pointer, ArrayLength length, bool isRef, string referrer)
    {
        var fixedLength = length is FixedArrayLength fixedArray ? fixedArray.Count : (int?)null;
        var container = isRef ? "global::System.ReadOnlySpan" : "global::System.ReadOnlyMemory";

        if (!pointer.IsConst)
        {
            throw new InvalidDataException($"'{referrer}' is an array the library writes, in an input structure.");
        }

        switch (pointer.Pointee)
        {
            case PointerTypeReference { Pointee: BuiltinTypeReference { Spelling: "char" } }:
                return result with { Kind = IdiomaticMemberKind.StringSpan, Type = $"{container}<string>", ElementType = "string", RawElementType = "byte*", FixedLength = fixedLength };

            case NamedTypeReference named when _model.Find(named.CName) is StructureDeclaration element && _roles[element.CName] is IdiomaticRole.ElementMirror:
                return result with
                {
                    Kind = IdiomaticMemberKind.StructureSpan,
                    Type = $"{container}<{GetName(element)}>",
                    ElementType = GetName(element),
                    RawElementType = SpellRaw(named),
                    FixedLength = fixedLength,
                    Slots = GetSlots(element),
                };

            case var element when IsBlittable(element):
                var elementType = SpellRaw(element);

                return result with
                {
                    Kind = IdiomaticMemberKind.Span,
                    Type = $"{container}<{elementType}>",
                    ElementType = elementType,
                    RawElementType = elementType,
                    FixedLength = fixedLength,
                    IsHandle = element is NamedTypeReference handle && _model.Find(handle.CName) is HandleDeclaration,
                    Slots = element is NamedTypeReference root && _model.Find(root.CName) is StructureDeclaration rootStructure ? GetSlots(rootStructure) : null,
                };

            default:
                throw new InvalidDataException($"'{referrer}' is an array whose elements the idiomatic layer does not support.");
        }
    }

    /// <summary>Projects a member of a snapshot.</summary>
    /// <param name="structure">The structure that holds the member.</param>
    /// <param name="member">The member.</param>
    /// <param name="field">The raw field of the member.</param>
    /// <param name="counts">The C names of the members that hold the count of another.</param>
    /// <returns>The idiomatic member.</returns>
    /// <exception cref="InvalidDataException">The member's type has no idiomatic form.</exception>
    private IdiomaticMember ProjectSnapshotMember(StructureDeclaration structure, StructureMember member, ProjectedField field, HashSet<string> counts)
    {
        var referrer = $"{structure.CName}.{member.CName}";
        var result = new IdiomaticMember { Member = member, Name = field.Name, Kind = IdiomaticMemberKind.Value, Type = string.Empty, RawType = SpellRaw(member.Type), IsOptional = member.IsOptional };

        switch (member.Role)
        {
            case MemberRole.NextInChain:
                return result with { Kind = IdiomaticMemberKind.NextInChain };

            case MemberRole.ChainHeader:
                return result with { Kind = IdiomaticMemberKind.ChainHeader, StructureType = GetStructureType(structure) };

            case MemberRole.Userdata:
                throw new InvalidDataException($"'{referrer}' is userdata, which only a callback info holds.");

            case MemberRole.Value:
            default:
                break;
        }

        if (counts.Contains(member.CName))
        {
            return result with { Kind = IdiomaticMemberKind.Count };
        }

        switch (member.Type)
        {
            case BuiltinTypeReference builtin when builtin.Spelling != "void":
                return result with { Type = result.RawType };

            case NamedTypeReference named:
                switch (_model.Find(named.CName))
                {
                    case EnumDeclaration:
                        return result with { Type = result.RawType };

                    case HandleDeclaration:
                        return result with { Type = result.RawType, IsHandle = true };

                    case TypedefDeclaration { IsBoolean: true }:
                        return result with { Kind = IdiomaticMemberKind.Boolean, Type = "bool" };

                    case StructureDeclaration nested:
                        return _roles[nested.CName] switch
                        {
                            IdiomaticRole.StringView => result with { Kind = IdiomaticMemberKind.Text, Type = "string?" },
                            IdiomaticRole.Value => result with { Type = result.RawType },
                            IdiomaticRole.Snapshot => result with { Kind = IdiomaticMemberKind.Nested, Type = GetName(nested), RawNestedType = result.RawType },
                            IdiomaticRole.Unused or IdiomaticRole.Mirror or IdiomaticRole.ElementMirror or IdiomaticRole.ChainHeader or IdiomaticRole.CallbackInfo or IdiomaticRole.Skipped => throw new InvalidDataException($"'{referrer}' holds '{nested.CName}', a {_roles[nested.CName]} structure, which a snapshot cannot hold."),
                            _ => throw new UnreachableException("Unknown structure role."),
                        };

                    default:
                        break;
                }

                break;

            case PointerTypeReference { Pointee: BuiltinTypeReference { Spelling: "void" } } when member.Length is null:
                return result with { Kind = IdiomaticMemberKind.Pointer, Type = "nint" };

            case PointerTypeReference { Pointee: NamedTypeReference named } when member.Length is MemberArrayLength && _model.Find(named.CName) is StructureDeclaration element && _roles[element.CName] == IdiomaticRole.Snapshot:
                return result with
                {
                    Kind = IdiomaticMemberKind.StructureSpan,
                    Type = $"global::System.Collections.Immutable.ImmutableArray<{GetName(element)}>",
                    ElementType = GetName(element),
                    RawElementType = SpellRaw(named),
                };

            case PointerTypeReference pointer when member.Length is MemberArrayLength && IsBlittable(pointer.Pointee):
                var elementType = SpellRaw(pointer.Pointee);

                return result with
                {
                    Kind = IdiomaticMemberKind.Span,
                    Type = $"global::System.Collections.Immutable.ImmutableArray<{elementType}>",
                    ElementType = elementType,
                    RawElementType = elementType,
                    IsHandle = pointer.Pointee is NamedTypeReference handle && _model.Find(handle.CName) is HandleDeclaration,
                };

            default:
                break;
        }

        throw new InvalidDataException($"'{referrer}' has a type the idiomatic layer cannot copy: add the structure to 'idiomatic.skip'.");
    }

    /// <summary>Gets the slots of the extensions of a chain root reached through a member, building them the first time.</summary>
    /// <param name="root">The root.</param>
    /// <returns>The slots, or <see langword="null"/> when nothing extends the root.</returns>
    /// <exception cref="InvalidDataException">An extension of the root holds pointers.</exception>
    private IdiomaticSlots? GetSlots(StructureDeclaration root)
    {
        if (_slots.TryGetValue(root.CName, out var slots))
        {
            return slots;
        }

        var extensions = _extensions.GetValueOrDefault(root.CName, []).Where(extension => !IsSkipped(extension.CName)).ToList();

        if (extensions.Count == 0)
        {
            return null;
        }

        if (extensions.FirstOrDefault(extension => _roles[extension.CName] != IdiomaticRole.Value) is { } withPointers)
        {
            throw new InvalidDataException($"'{withPointers.CName}' extends '{root.CName}', which a member holds, and has pointers: the slots of nested roots only hold value structures.");
        }

        var rootName = GetName(root);

        slots = new IdiomaticSlots(
            $"{rootName}Extensions",
            rootName,
            root.CName,
            GetNextInChainField(root) ?? throw new InvalidDataException($"'{root.CName}' has extensions but no nextInChain member."),
            root.Availability,
            [.. extensions.Select(extension => new IdiomaticExtensionSlot(GetName(extension), GetChainField(extension), GetStructureType(extension), extension.Availability)).OrderBy(static slot => slot.Name, StringComparer.Ordinal)]);
        _slots.Add(root.CName, slots);

        return slots;
    }

    /// <summary>Projects what the idiomatic layer adds to a value structure.</summary>
    /// <param name="structure">The value structure.</param>
    /// <returns>The additions, or <see langword="null"/> when there are none.</returns>
    private IdiomaticValueStructure? ProjectValueStructure(StructureDeclaration structure)
    {
        var inputRoots = structure.Chained == ChainDirection.In ? GetRootNames(structure, input: true) : [];
        var outputRoots = structure.Chained == ChainDirection.Out ? GetRootNames(structure, input: false) : [];
        var constants = _constants.GetValueOrDefault(structure.CName, []);

        return inputRoots.Count == 0 && outputRoots.Count == 0 && constants.Count == 0
            ? null
            : new IdiomaticValueStructure
            {
                Name = GetName(structure),
                Availability = structure.Availability,
                InputRoots = inputRoots,
                OutputRoots = outputRoots,
                ChainField = structure.Chained != ChainDirection.None ? GetChainField(structure) : null,
                StructureType = structure.Chained != ChainDirection.None ? GetStructureType(structure) : null,
                Constants = constants,
            };
    }

    /// <summary>Gets the .NET names of the roots an extension can be chained to in the idiomatic layer.</summary>
    /// <param name="extension">The extension.</param>
    /// <param name="input">Whether the extension is an input; an output extension's roots are outputs.</param>
    /// <returns>The names, ordered.</returns>
    private List<string> GetRootNames(StructureDeclaration extension, bool input)
    {
        return [.. extension.ChainRoots
            .Where(root => _roles.GetValueOrDefault(root) is var role && (input ? role is IdiomaticRole.Mirror or IdiomaticRole.ElementMirror or IdiomaticRole.Value : role is IdiomaticRole.Snapshot or IdiomaticRole.Value))
            .Select(root => _rawTypes[root].Name)
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Places each free function on the handle whose words its name contains.</summary>
    /// <returns>The free functions by C name of their handle, with the words left once the handle's are removed.</returns>
    /// <exception cref="InvalidDataException">A free function names no handle, or several.</exception>
    private Dictionary<string, List<(FunctionDeclaration Function, IReadOnlyList<string> Words)>> PlaceFreeFunctions()
    {
        var placed = new Dictionary<string, List<(FunctionDeclaration, IReadOnlyList<string>)>>(StringComparer.Ordinal);

        foreach (var function in _model.Functions.Where(function => function.Kind == FunctionKind.Free && !IsOmitted(function.CName)))
        {
            var owners = _model.Handles.Select(handle => (Handle: handle, Index: IndexOf(function.Words, handle.Words))).Where(static owner => owner.Index >= 0).ToList();

            if (owners.Count != 1)
            {
                throw new InvalidDataException($"The free function '{function.CName}' names {owners.Count} handles: add it to 'idiomatic.skip' or 'idiomatic.handWritten'.");
            }

            var (handle, index) = owners[0];

            if (!placed.TryGetValue(handle.CName, out var functions))
            {
                placed.Add(handle.CName, functions = []);
            }

            functions.Add((function, [.. function.Words.Take(index), .. function.Words.Skip(index + handle.Words.Count)]));
        }

        return placed;
    }

    /// <summary>Finds a run of words in a name.</summary>
    /// <param name="words">The words of the name.</param>
    /// <param name="run">The words to find.</param>
    /// <returns>The index of the first word of the run, or -1.</returns>
    private static int IndexOf(IReadOnlyList<string> words, IReadOnlyList<string> run)
    {
        for (var i = 0; i + run.Count <= words.Count; i++)
        {
            if (words.Skip(i).Take(run.Count).SequenceEqual(run, StringComparer.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Projects a handle and the functions placed on it.</summary>
    /// <param name="handle">The handle.</param>
    /// <param name="statics">The free functions placed on the handle, with the words of their member names.</param>
    /// <returns>The idiomatic handle.</returns>
    private IdiomaticHandle ProjectHandle(HandleDeclaration handle, List<(FunctionDeclaration Function, IReadOnlyList<string> Words)> statics)
    {
        var name = _rawTypes[handle.CName].Name;
        var methods = _model.Functions
            .Where(function => function.Owner == handle.CName && function.Kind is FunctionKind.Method or FunctionKind.AddRef or FunctionKind.Release && !IsOmitted(function.CName))
            .Select(function => ProjectMethod(function, function.Words, isStatic: false))
            .Concat(statics.Select(placed => ProjectMethod(placed.Function, placed.Words, isStatic: true)))
            .OrderBy(static method => method.Name, StringComparer.Ordinal)
            .ThenBy(static method => method.Function.CName, StringComparer.Ordinal)
            .ToList();
        var constants = _constants.GetValueOrDefault(handle.CName, []);
        var reserved = _handleMembers.Except(methods.Where(static method => method.Kind is IdiomaticMethodKind.AddRef or IdiomaticMethodKind.Release).Select(static method => method.Name), StringComparer.Ordinal);

        RawProjection.CheckUnique("member", handle.CName, methods.Select(static method => method.Name).Concat(constants.Select(static constant => constant.Name)).Concat(reserved).Append(name), StringComparer.Ordinal);

        return new IdiomaticHandle(name, handle.CName, handle.Availability, methods, constants);
    }

    /// <summary>Projects a function onto a member of its handle.</summary>
    /// <param name="function">The function.</param>
    /// <param name="words">The words of the member's name.</param>
    /// <param name="isStatic">Whether the function is a free function placed on the handle, which takes no handle.</param>
    /// <returns>The idiomatic method.</returns>
    /// <exception cref="InvalidDataException">A parameter or the result has no idiomatic form.</exception>
    private IdiomaticMethod ProjectMethod(FunctionDeclaration function, IReadOnlyList<string> words, bool isStatic)
    {
        var raw = _rawFunctions[function.CName];

        if (function.Kind is FunctionKind.AddRef or FunctionKind.Release)
        {
            return new IdiomaticMethod
            {
                Function = function,
                RawName = raw.Name,
                Name = function.Kind == FunctionKind.AddRef ? "AddRef" : "Release",
                Kind = function.Kind == FunctionKind.AddRef ? IdiomaticMethodKind.AddRef : IdiomaticMethodKind.Release,
                Parameters = [],
                ReturnKind = IdiomaticReturnKind.Void,
                ReturnType = "void",
                Availability = function.Availability,
            };
        }

        var skip = isStatic ? 0 : 1;
        var parameters = function.Parameters.Skip(skip).ToList();
        var arrays = parameters.Where(static parameter => parameter.Length is MemberArrayLength).ToDictionary(static parameter => ((MemberArrayLength)parameter.Length!).CName, StringComparer.Ordinal);
        var rawNames = function.Parameters.Select((parameter, index) => (parameter.CName, raw.Parameters[index].Name)).ToDictionary(static pair => pair.CName, static pair => pair.Name, StringComparer.Ordinal);
        var projected = parameters.Select(parameter => ProjectParameter(function, parameter, rawNames, arrays)).ToList();
        var outputs = projected.Where(static parameter => parameter.Kind == IdiomaticParameterKind.Output).ToList();
        var callbacks = projected.Where(static parameter => parameter.Kind == IdiomaticParameterKind.Callback).ToList();

        if (outputs.Count > 1 || callbacks.Count > 1 || (outputs.Count > 0 && callbacks.Count > 0))
        {
            throw new InvalidDataException($"'{function.CName}' has several outputs or callbacks: add it to 'idiomatic.handWritten'.");
        }

        if (projected.Count(static parameter => parameter.IsOptional) > 1)
        {
            throw new InvalidDataException($"'{function.CName}' has several optional pointers: add it to 'idiomatic.handWritten'.");
        }

        var returnsStatus = function.ReturnType is NamedTypeReference returned && _model.Find(returned.CName) is EnumDeclaration { Words: var statusWords } && statusWords.SequenceEqual(_statusWords, StringComparer.Ordinal);
        var name = _names.GetMemberName(function.CName, words);
        IdiomaticAsync? async = null;
        IdiomaticReturnKind returnKind;
        string returnType;

        if (callbacks.Count == 1)
        {
            async = ProjectAsync(function, _model.Get<StructureDeclaration>(((NamedTypeReference)callbacks[0].Parameter.Type).CName), name);
            returnKind = IdiomaticReturnKind.Task;
            returnType = async.ResultType is { } resultType ? $"global::System.Threading.Tasks.Task<{resultType}>" : "global::System.Threading.Tasks.Task";
            name = name.EndsWith("Async", StringComparison.Ordinal) ? name : name + "Async";
        }
        else if (outputs.Count == 1)
        {
            if (function.ReturnType != BuiltinTypeReference.Void && !returnsStatus)
            {
                throw new InvalidDataException($"'{function.CName}' has an output and a result: add it to 'idiomatic.handWritten'.");
            }

            returnKind = IdiomaticReturnKind.Output;
            returnType = outputs[0].Type;
        }
        else
        {
            (returnKind, returnType) = returnsStatus ? (IdiomaticReturnKind.Status, "void") : ProjectResult(function);
        }

        var visible = projected.Where(static parameter => IsVisible(parameter)).ToList();
        var kind = isStatic
            ? IdiomaticMethodKind.Static
            : visible.Count == 0 && returnKind == IdiomaticReturnKind.Value && words is ["get" or "is", ..] && !(function.ReturnType is NamedTypeReference handle && _model.Find(handle.CName) is HandleDeclaration)
                ? IdiomaticMethodKind.Property
                : IdiomaticMethodKind.Method;

        if (kind == IdiomaticMethodKind.Property && words[0] == "get")
        {
            name = _names.GetMemberName(function.CName, [.. words.Skip(1)]);
        }

        return new IdiomaticMethod
        {
            Function = function,
            RawName = raw.Name,
            Name = name,
            Kind = kind,
            Parameters = ApplyDefaults(projected),
            ReturnKind = returnKind,
            ReturnType = returnType,
            ChecksStatus = returnsStatus,
            Availability = function.Availability,
            Async = async,
        };
    }

    /// <summary>Gets the result of a function that returns a value, as is.</summary>
    /// <param name="function">The function.</param>
    /// <returns>The return kind and the C# type.</returns>
    /// <exception cref="InvalidDataException">The result is a pointer or a function pointer.</exception>
    private (IdiomaticReturnKind Kind, string Type) ProjectResult(FunctionDeclaration function)
    {
        return function.ReturnType switch
        {
            BuiltinTypeReference { Spelling: "void" } => (IdiomaticReturnKind.Void, "void"),
            BuiltinTypeReference builtin => (IdiomaticReturnKind.Value, RawProjection.GetBuiltinTypeName(builtin.Spelling)),
            NamedTypeReference named => _model.Find(named.CName) switch
            {
                TypedefDeclaration { IsBoolean: true } => (IdiomaticReturnKind.Value, "bool"),
                EnumDeclaration or HandleDeclaration => (IdiomaticReturnKind.Value, SpellRaw(named)),
                StructureDeclaration structure when _roles[structure.CName] == IdiomaticRole.Value => (IdiomaticReturnKind.Value, SpellRaw(named)),
                _ => throw new InvalidDataException($"'{function.CName}' returns '{named.CName}', which the idiomatic layer does not support: add it to 'idiomatic.handWritten'."),
            },
            _ => throw new InvalidDataException($"'{function.CName}' returns a pointer: add it to 'idiomatic.handWritten'."),
        };
    }

    /// <summary>Projects a parameter of a function.</summary>
    /// <param name="function">The function.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="names">The .NET names of the function's parameters, the raw layer's, by C name.</param>
    /// <param name="arrays">The array parameters, by C name of the parameter that holds their length.</param>
    /// <returns>The idiomatic parameter.</returns>
    /// <exception cref="InvalidDataException">The parameter's type has no idiomatic form.</exception>
    private IdiomaticParameter ProjectParameter(FunctionDeclaration function, Parameter parameter, Dictionary<string, string> names, Dictionary<string, Parameter> arrays)
    {
        var referrer = $"{function.CName}({parameter.CName})";
        var rawType = SpellRaw(parameter.Type);
        var result = new IdiomaticParameter { Parameter = parameter, Name = names[parameter.CName], Kind = IdiomaticParameterKind.Value, Type = rawType, RawType = rawType };

        if (arrays.TryGetValue(parameter.CName, out var array))
        {
            return result with
            {
                Kind = IdiomaticParameterKind.Count,
                CountOf = names[array.CName],
                CountsBytes = array.Type is PointerTypeReference { Pointee: BuiltinTypeReference { Spelling: "void" } },
            };
        }

        switch (parameter.Type)
        {
            case BuiltinTypeReference builtin when builtin.Spelling != "void":
                return result;

            case NamedTypeReference named:
                switch (_model.Find(named.CName))
                {
                    case EnumDeclaration or HandleDeclaration:
                        return result;

                    case TypedefDeclaration { IsBoolean: true }:
                        return result with { Kind = IdiomaticParameterKind.Boolean, Type = "bool" };

                    case StructureDeclaration structure:
                        return _roles[structure.CName] switch
                        {
                            IdiomaticRole.StringView => result with { Kind = IdiomaticParameterKind.Text, Type = "global::System.ReadOnlySpan<byte>" },
                            IdiomaticRole.CallbackInfo => result with { Kind = IdiomaticParameterKind.Callback },
                            IdiomaticRole.Value => result,
                            IdiomaticRole.Unused or IdiomaticRole.Mirror or IdiomaticRole.ElementMirror or IdiomaticRole.Snapshot or IdiomaticRole.ChainHeader or IdiomaticRole.Skipped => throw new InvalidDataException($"'{referrer}' takes '{structure.CName}', a {_roles[structure.CName]} structure, by value."),
                            _ => throw new UnreachableException("Unknown structure role."),
                        };

                    default:
                        break;
                }

                break;

            case PointerTypeReference { Pointee: BuiltinTypeReference { Spelling: "void" } } pointer:
                return parameter.Length is null
                    ? result with { Kind = IdiomaticParameterKind.Pointer, Type = "nint" }
                    : pointer.IsConst
                        ? result with { Kind = IdiomaticParameterKind.Data, Type = "global::System.ReadOnlySpan<TData>", ElementType = "TData" }
                        : result with { Kind = IdiomaticParameterKind.MutableData, Type = "global::System.Span<TData>", ElementType = "TData" };

            case PointerTypeReference pointer when parameter.Length is not null && IsBlittable(pointer.Pointee):
                var elementType = SpellRaw(pointer.Pointee);

                return pointer.IsConst
                    ? result with { Kind = IdiomaticParameterKind.Span, Type = $"global::System.ReadOnlySpan<{elementType}>", ElementType = elementType }
                    : result with { Kind = IdiomaticParameterKind.MutableSpan, Type = $"global::System.Span<{elementType}>", ElementType = elementType };

            case PointerTypeReference { Pointee: NamedTypeReference pointee } pointer when parameter.Length is null && _model.Find(pointee.CName) is StructureDeclaration structure:
                var role = _roles[structure.CName];
                var structureType = SpellRaw(pointee);

                if (pointer.IsConst && role is IdiomaticRole.Value)
                {
                    return result with
                    {
                        Kind = IdiomaticParameterKind.InValue,
                        Type = structureType,
                        IsOptional = parameter.IsOptional,
                        RawStructure = structureType,
                        StructureCName = structure.CName,
                        HasExtensions = HasExtensions(structure, ChainDirection.In),
                        NextInChainField = GetNextInChainField(structure),
                    };
                }

                if (pointer.IsConst && role is IdiomaticRole.Mirror or IdiomaticRole.ElementMirror)
                {
                    var mirror = GetStructure(structure);

                    return result with
                    {
                        Kind = IdiomaticParameterKind.Descriptor,
                        Type = mirror.Name,
                        IsOptional = parameter.IsOptional,
                        Mirror = mirror,
                        RawStructure = mirror.RawType,
                        StructureCName = structure.CName,
                        HasExtensions = HasExtensions(structure, ChainDirection.In),
                        NextInChainField = GetNextInChainField(structure),
                    };
                }

                if (!pointer.IsConst && role is IdiomaticRole.Value or IdiomaticRole.Snapshot)
                {
                    var snapshot = role == IdiomaticRole.Snapshot ? GetStructure(structure) : null;

                    return result with
                    {
                        Kind = IdiomaticParameterKind.Output,
                        Type = snapshot?.Name ?? structureType,
                        RawStructure = snapshot?.RawType ?? structureType,
                        StructureCName = structure.CName,
                        HasExtensions = HasExtensions(structure, ChainDirection.Out),
                        NextInChainField = GetNextInChainField(structure),
                        Snapshot = snapshot,
                    };
                }

                throw new InvalidDataException($"'{referrer}' points to '{structure.CName}', a {role} structure, in a direction the idiomatic layer does not support.");

            default:
                break;
        }

        throw new InvalidDataException($"'{referrer}' has a type the idiomatic layer does not support: add the function to 'idiomatic.handWritten'.");
    }

    /// <summary>Gives the trailing parameters their documented defaults, and the last span parameter <see langword="params"/>.</summary>
    /// <param name="parameters">The parameters of a function.</param>
    /// <returns>The parameters with their defaults.</returns>
    /// <remarks>
    /// C# only allows optional parameters after the required ones, so a default is kept only when
    /// every visible parameter after it has one too; a final argument list comes after them all.
    /// </remarks>
    private List<IdiomaticParameter> ApplyDefaults(List<IdiomaticParameter> parameters)
    {
        var result = parameters.ToList();
        var visible = Enumerable.Range(0, result.Count).Where(index => IsVisible(result[index])).ToList();

        if (visible.Count > 0 && result[visible[^1]].Kind == IdiomaticParameterKind.Span)
        {
            result[visible[^1]] = result[visible[^1]] with { IsParams = true };
            visible.RemoveAt(visible.Count - 1);
        }

        for (var i = visible.Count - 1; i >= 0; i--)
        {
            var parameter = result[visible[i]];

            if (parameter.Kind is not (IdiomaticParameterKind.Value or IdiomaticParameterKind.Boolean) || parameter.Parameter.Default is not { } value || FormatDefault(value, parameter.Type) is not { } formatted)
            {
                break;
            }

            result[visible[i]] = parameter with { Default = formatted };
        }

        return result;
    }

    /// <summary>Formats the default of an optional argument.</summary>
    /// <param name="value">The default.</param>
    /// <param name="type">The C# type of the parameter.</param>
    /// <returns>The C# constant expression, or <see langword="null"/> when the default is not a C# constant.</returns>
    private string? FormatDefault(ValueExpression value, string type)
    {
        return value switch
        {
            IntegerExpression integer when RawProjection.IsSigned(type) => unchecked((long)integer.Value).ToString(CultureInfo.InvariantCulture),
            IntegerExpression integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            BooleanExpression boolean => boolean.Value ? "true" : "false",
            ConstantReferenceExpression constant => _constantReferences.GetValueOrDefault(constant.CName),
            EnumValueReferenceExpression enumValue => GetEnumValue(enumValue),
            _ => null,
        };
    }

    /// <summary>Projects how an asynchronous function's callback completes a task.</summary>
    /// <param name="function">The function.</param>
    /// <param name="callbackInfo">The callback info it takes.</param>
    /// <param name="name">The name of the method, which names the trampoline.</param>
    /// <returns>The asynchronous completion.</returns>
    /// <exception cref="InvalidDataException">The function does not return a future, or its callback carries several results.</exception>
    private IdiomaticAsync ProjectAsync(FunctionDeclaration function, StructureDeclaration callbackInfo, string name)
    {
        if (function.ReturnType is not NamedTypeReference future || _model.Find(future.CName) is not StructureDeclaration)
        {
            throw new InvalidDataException($"'{function.CName}' takes a callback info but returns no future: add it to 'idiomatic.skip' or 'idiomatic.handWritten'.");
        }

        var callback = GetCallback(callbackInfo);
        var status = callback.Parameters.Count > 0 && callback.Parameters[0].Type is NamedTypeReference statusType && _model.Find(statusType.CName) is EnumDeclaration statusEnum
            ? statusEnum
            : throw new InvalidDataException($"The callback of '{function.CName}' does not start with a status: add it to 'idiomatic.handWritten'.");
        var success = status.Values.FirstOrDefault(value => value.Words.SequenceEqual(_successWords, StringComparer.Ordinal))
            ?? throw new InvalidDataException($"'{status.CName}' has no success value.");
        var parameters = callback.Parameters
            .Select(parameter => (Parameter: parameter, Name: RawProjection.Escape(_names.GetParameterName($"{callback.CName}.{parameter.CName}", parameter.Words))))
            .ToList();
        var message = parameters.FirstOrDefault(parameter => parameter.Parameter.CName == "message" && parameter.Parameter.Type is NamedTypeReference text && _roles.GetValueOrDefault(text.CName) == IdiomaticRole.StringView);
        var results = parameters.Skip(1).Where(parameter => parameter != message && parameter.Parameter.Type is not PointerTypeReference { Pointee: BuiltinTypeReference { Spelling: "void" } }).ToList();

        if (results.Count > 1)
        {
            throw new InvalidDataException($"The callback of '{function.CName}' carries several results: add it to 'idiomatic.handWritten'.");
        }

        var result = results.Count == 1 ? results[0] : default;
        var resultType = result.Parameter?.Type switch
        {
            null => null,
            NamedTypeReference handle when _model.Find(handle.CName) is HandleDeclaration => SpellRaw(handle),
            PointerTypeReference { Pointee: NamedTypeReference pointee, IsConst: true } when _model.Find(pointee.CName) is StructureDeclaration snapshot && _roles[snapshot.CName] == IdiomaticRole.Snapshot => GetStructure(snapshot).Name,
            _ => throw new InvalidDataException($"The callback of '{function.CName}' carries a result the idiomatic layer does not support: add it to 'idiomatic.handWritten'."),
        };

        var rawCallbackInfo = (ProjectedStructure)_rawTypes[callbackInfo.CName];
        var fields = callbackInfo.Members.Zip(rawCallbackInfo.Fields).ToDictionary(static pair => pair.First.CName, static pair => pair.Second.Name, StringComparer.Ordinal);
        var mode = callbackInfo.Members.FirstOrDefault(static member => member.CName == "mode");
        string? modeValue = null;

        if (mode?.Type is NamedTypeReference modeType)
        {
            var modeEnum = _model.Get<EnumDeclaration>(modeType.CName);
            var processEvents = modeEnum.Values.FirstOrDefault(static value => value.Words.SequenceEqual(["allow", "process", "events"], StringComparer.Ordinal))
                ?? throw new InvalidDataException($"'{modeEnum.CName}' has no value that delivers callbacks when the instance processes events.");

            modeValue = GetEnumValue(new EnumValueReferenceExpression(modeEnum.CName, processEvents.CName));
        }

        return new IdiomaticAsync
        {
            CallbackInfoType = SpellRaw(new NamedTypeReference(callbackInfo.CName)),
            ModeField = mode is null ? null : fields[mode.CName],
            ModeValue = modeValue,
            CallbackField = fields["callback"],
            UserdataField = fields["userdata1"],
            UserdataParameter = parameters.Single(static parameter => parameter.Parameter.CName == "userdata1").Name,
            Trampoline = $"On{name}Completed",
            TrampolineParameters = [.. parameters.Select(parameter => (SpellRaw(parameter.Parameter.Type), parameter.Name))],
            StatusType = _rawTypes[status.CName].Name,
            SuccessValue = GetEnumValue(new EnumValueReferenceExpression(status.CName, success.CName))!,
            ResultParameter = result.Name,
            ResultType = resultType,
            ResultIsSnapshot = result.Parameter?.Type is PointerTypeReference,
            MessageParameter = message.Name,
        };
    }

    /// <summary>Gets the callback function of a callback info.</summary>
    /// <param name="callbackInfo">The callback info.</param>
    /// <returns>The function pointer type of its <c>callback</c> member.</returns>
    private FunctionPointerDeclaration GetCallback(StructureDeclaration callbackInfo)
    {
        return callbackInfo.Members.SingleOrDefault(static member => member.CName == "callback")?.Type is NamedTypeReference callback
            ? _model.Get<FunctionPointerDeclaration>(callback.CName)
            : throw new InvalidDataException($"'{callbackInfo.CName}' has no callback member.");
    }

    /// <summary>Tells whether a chain root has extensions of a direction that the idiomatic layer exposes.</summary>
    /// <param name="root">The root.</param>
    /// <param name="direction">The direction of the extensions.</param>
    /// <returns><see langword="true"/> when at least one extension can be chained to the root.</returns>
    private bool HasExtensions(StructureDeclaration root, ChainDirection direction)
    {
        return _extensions.GetValueOrDefault(root.CName, []).Any(extension => extension.Chained == direction && _roles[extension.CName] is not (IdiomaticRole.Skipped or IdiomaticRole.Unused));
    }

    /// <summary>Gets the name of the <c>FreeMembers</c> function of a structure, in the raw layer.</summary>
    /// <param name="structure">The structure.</param>
    /// <returns>The raw name, or <see langword="null"/> when the library allocates nothing for it.</returns>
    private string? GetFreeMembers(StructureDeclaration structure)
    {
        return _model.Functions.FirstOrDefault(function => function.Kind == FunctionKind.FreeMembers && function.Owner == structure.CName) is { } freeMembers
            ? _rawFunctions[freeMembers.CName].Name
            : null;
    }

    /// <summary>Gets the name of the raw field that points to a chain root's first extension.</summary>
    /// <param name="structure">The structure.</param>
    /// <returns>The field name, or <see langword="null"/> when the structure is not a chain root.</returns>
    private string? GetNextInChainField(StructureDeclaration structure)
    {
        var raw = (ProjectedStructure)_rawTypes[structure.CName];

        return raw.Fields.Zip(structure.Members).FirstOrDefault(static pair => pair.Second.Role == MemberRole.NextInChain).First?.Name;
    }

    /// <summary>Gets the name of the raw field that holds an extension's chain header.</summary>
    /// <param name="structure">The extension.</param>
    /// <returns>The field name.</returns>
    private string GetChainField(StructureDeclaration structure)
    {
        var raw = (ProjectedStructure)_rawTypes[structure.CName];

        return raw.Fields.Zip(structure.Members).Single(static pair => pair.Second.Role == MemberRole.ChainHeader).First.Name;
    }

    /// <summary>Gets the C# expression of the structure type that identifies an extension.</summary>
    /// <param name="structure">The extension.</param>
    /// <returns>The enum value.</returns>
    private string GetStructureType(StructureDeclaration structure)
    {
        return structure.Members.FirstOrDefault(static member => member.Role == MemberRole.ChainHeader)?.Default is ChainHeaderExpression header && GetEnumValue(header.StructureType) is { } value
            ? value
            : throw new InvalidDataException($"'{structure.CName}' has no structure type.");
    }

    /// <summary>Formats an enum value.</summary>
    /// <param name="reference">The enum value.</param>
    /// <returns>The qualified value, or <see langword="null"/> when the enum is unknown.</returns>
    private string? GetEnumValue(EnumValueReferenceExpression reference)
    {
        return _rawTypes.GetValueOrDefault(reference.EnumCName) is ProjectedEnum projected && projected.Values.FirstOrDefault(value => value.CName == reference.ValueCName) is { } value
            ? $"{projected.Name}.{value.Name}"
            : null;
    }

    /// <summary>Spells a type of the model as the raw layer declares it, from the library's namespace.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The C# type; an internal raw structure is qualified with <c>Raw.</c>.</returns>
    private string SpellRaw(TypeReference type)
    {
        return type switch
        {
            BuiltinTypeReference builtin => RawProjection.GetBuiltinTypeName(builtin.Spelling),
            PointerTypeReference pointer => SpellRaw(pointer.Pointee) + "*",
            FunctionPointerTypeReference function => SpellFunctionPointer(function.ParameterTypes, function.ReturnType),
            NamedTypeReference named => _model.Find(named.CName) switch
            {
                FunctionPointerDeclaration function => SpellFunctionPointer(function.Parameters.Select(static parameter => parameter.Type), function.ReturnType),
                TypedefDeclaration { IsBoolean: false } typedef => SpellRaw(typedef.Target),
                StructureDeclaration structure when !IsValue(structure) => $"{RawProjection.RawNamespace}.{_rawTypes[structure.CName].Name}",
                null => throw new InvalidDataException($"'{named.CName}' is not declared."),
                var declaration => _rawTypes[declaration.CName].Name,
            },
            _ => throw new InvalidDataException($"The idiomatic layer cannot spell {type}."),
        };
    }

    /// <summary>Spells a function pointer type.</summary>
    /// <param name="parameterTypes">The types of the parameters.</param>
    /// <param name="returnType">The return type.</param>
    /// <returns>The <c>delegate* unmanaged[Cdecl]</c> type.</returns>
    private string SpellFunctionPointer(IEnumerable<TypeReference> parameterTypes, TypeReference returnType)
    {
        return $"delegate* unmanaged[Cdecl]<{string.Join(", ", parameterTypes.Select(SpellRaw).Append(SpellRaw(returnType)))}>";
    }

    /// <summary>Tells whether a type is blittable as is: a number, an enum, a handle or a value structure.</summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> when an array of it can be passed without conversion.</returns>
    private bool IsBlittable(TypeReference type)
    {
        return type switch
        {
            BuiltinTypeReference builtin => builtin.Spelling != "void",
            NamedTypeReference named => _model.Find(named.CName) switch
            {
                EnumDeclaration or HandleDeclaration => true,
                StructureDeclaration structure => IsValue(structure),
                _ => false,
            },
            _ => false,
        };
    }

    /// <summary>Tells whether a structure is a value structure of the raw layer.</summary>
    /// <param name="structure">The structure.</param>
    /// <returns><see langword="true"/> when the raw layer declares it public.</returns>
    private bool IsValue(StructureDeclaration structure)
    {
        return _rawTypes[structure.CName] is ProjectedStructure { IsPublic: true };
    }

    /// <summary>Gets the .NET name of a declaration, the raw layer's.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <returns>The name.</returns>
    private string GetName(Declaration declaration)
    {
        return _rawTypes[declaration.CName].Name;
    }

    /// <summary>Tells whether an idiomatic method takes a parameter itself.</summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="false"/> for counts, outputs and callbacks, which the method handles.</returns>
    private static bool IsVisible(IdiomaticParameter parameter)
    {
        return parameter.Kind is not (IdiomaticParameterKind.Count or IdiomaticParameterKind.Output or IdiomaticParameterKind.Callback);
    }

    /// <summary>Tells whether the configuration leaves a function out or writes it by hand, and records the match.</summary>
    /// <param name="cName">The C name of the function.</param>
    /// <returns><see langword="true"/> when the generator must not write it.</returns>
    private bool IsOmitted(string cName)
    {
        return IsSkipped(cName) || IsHandWritten(cName);
    }

    /// <summary>Tells whether the configuration leaves a declaration out, and records the match.</summary>
    /// <param name="cName">The C name.</param>
    /// <returns><see langword="true"/> when it is in <see cref="IdiomaticConfiguration.Skip"/>.</returns>
    private bool IsSkipped(string cName)
    {
        if (!_configuration.Skip.ContainsKey(cName))
        {
            return false;
        }

        _ = _usedKeys.Add(cName);

        return true;
    }

    /// <summary>Tells whether the configuration writes a function or a member by hand, and records the match.</summary>
    /// <param name="key">The C name of the function, or <c>Structure.member</c>.</param>
    /// <returns><see langword="true"/> when it is in <see cref="IdiomaticConfiguration.HandWritten"/>.</returns>
    private bool IsHandWritten(string key)
    {
        if (!_configuration.HandWritten.ContainsKey(key))
        {
            return false;
        }

        _ = _usedKeys.Add(key);

        return true;
    }

    /// <summary>Checks that every entry of the configuration matched a declaration.</summary>
    /// <exception cref="InvalidDataException">An entry matches nothing.</exception>
    private void CheckConfigurationUsed()
    {
        var unused = _configuration.Skip.Keys.Concat(_configuration.HandWritten.Keys).Where(key => !_usedKeys.Contains(key)).Order(StringComparer.Ordinal).ToList();

        if (unused.Count > 0)
        {
            throw new InvalidDataException($"'idiomatic' has entries that match nothing: {string.Join(", ", unused)}.");
        }
    }
}
