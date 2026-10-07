using System.Globalization;
using Jade.BindingGenerator.Projection;

namespace Jade.BindingGenerator.Emission;

/// <summary>Writes the mirrors, element mirrors and snapshots of the idiomatic layer (ADR 0029, ADR 0040).</summary>
internal static class MirrorEmitter
{
    /// <summary>The interface of input extensions.</summary>
    private const string InputInterface = "global::Jade.Wgpu.IChainedExtension";

    /// <summary>The interface of output extensions.</summary>
    private const string OutputInterface = "global::Jade.Wgpu.IChainedOutputExtension";

    /// <summary>The prefix of the raw functions.</summary>
    private const string NativeMethods = "Raw.NativeMethods";

    /// <summary>The name of the field that records that a mirror's constructor ran.</summary>
    private const string PresenceField = "_isSet";

    /// <summary>Gets the members of a <c>ref struct</c> mirror that a call can pin rather than copy.</summary>
    /// <param name="mirror">The mirror.</param>
    /// <returns>The text members and the members that are spans of blittable elements, in order.</returns>
    public static IEnumerable<IdiomaticMember> GetPinnedMembers(IdiomaticStructure mirror)
    {
        return mirror.Role == IdiomaticRole.Mirror
            ? mirror.Members.Where(static member => member.Kind is IdiomaticMemberKind.Text or IdiomaticMemberKind.Span)
            : [];
    }

    /// <summary>Writes a mirror or an element mirror.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="library">The projected idiomatic layer.</param>
    /// <param name="mirror">The mirror.</param>
    public static void Emit(CSharpWriter writer, IdiomaticLibrary library, IdiomaticStructure mirror)
    {
        var isRef = mirror.Role == IdiomaticRole.Mirror;
        var interfaces = mirror.InputRoots.Select(root => $"{InputInterface}<{mirror.Name}, {root}>").ToList();

        writer.Line(isRef
            ? $"/// <summary>The idiomatic form of <c>{mirror.Declaration.CName}</c>, lowered to its raw form for each call that takes it.</summary>"
            : $"/// <summary>The idiomatic form of <c>{mirror.Declaration.CName}</c> as an array element, lowered to its raw form for each call that takes it.</summary>");
        writer.Line(RawLayerEmitter.GeneratedCodeAttribute);
        RawLayerEmitter.EmitPlatformAttributes(writer, mirror.Availability);
        writer.Line($"public unsafe {(isRef ? "ref " : string.Empty)}partial struct {mirror.Name}{(interfaces.Count > 0 ? " : " + string.Join(", ", interfaces) : string.Empty)}");
        writer.OpenBlock();

        var first = true;

        foreach (var constant in mirror.Constants)
        {
            Separate(writer, ref first);
            writer.Line($"/// <summary>Maps <c>{constant.CName}</c>.</summary>");
            writer.Line(constant.IsConst
                ? $"public const {constant.Type} {constant.Name} = {constant.Value};"
                : $"public static {constant.Type} {constant.Name} => {constant.Value};");
        }

        foreach (var member in mirror.Members.Where(static member => member.Type.Length > 0))
        {
            Separate(writer, ref first);
            writer.Line($"/// <summary>{GetMemberDocumentation(mirror, member)}</summary>");
            writer.Line($"public {member.Type} {member.Name};");

            if (member.Slots is { } slots)
            {
                writer.Line();
                writer.Line(member.Kind == IdiomaticMemberKind.ValuePointer
                    ? $"/// <summary>The extensions chained to <see cref=\"{member.Name}\"/> when it has a value.</summary>"
                    : $"/// <summary>The extensions chained to each element of <see cref=\"{member.Name}\"/>: empty, or one per element.</summary>");
                writer.Line(member.Kind == IdiomaticMemberKind.ValuePointer
                    ? $"public {slots.Name} {slots.Name};"
                    : $"public global::System.ReadOnlySpan<{slots.Name}> {slots.Name};");
            }
        }

        if (mirror.HasPresence)
        {
            Separate(writer, ref first);
            writer.Line("/// <summary>Whether the constructor ran: a <see langword=\"default\"/> mirror stands for a null pointer.</summary>");
            writer.Line($"private readonly bool {PresenceField};");
        }

        if (mirror.Defaults.Count > 0 || mirror.HasPresence)
        {
            Separate(writer, ref first);
            writer.Line(mirror.Declaration.InitializerCName is { } initializer
                ? $"/// <summary>Initializes a new instance of the <see cref=\"{mirror.Name}\"/> struct with the defaults of <c>{initializer}</c>.</summary>"
                : $"/// <summary>Initializes a new instance of the <see cref=\"{mirror.Name}\"/> struct.</summary>");
            writer.Line($"public {mirror.Name}()");
            writer.OpenBlock();

            foreach (var assignment in mirror.Defaults)
            {
                writer.Line($"{assignment.Target} = {assignment.Value};");
            }

            if (mirror.HasPresence)
            {
                writer.Line($"{PresenceField} = true;");
            }

            writer.CloseBlock();

            if (mirror.HasPresence)
            {
                writer.Line();
                writer.Line("/// <summary>Gets whether the constructor ran; a member that points to the mirror passes a null pointer otherwise.</summary>");
                writer.Line($"internal readonly bool IsSet => {PresenceField};");
            }
        }

        Separate(writer, ref first);
        EmitLower(writer, library, mirror);

        if (mirror.Members.Any(static member => member.Kind == IdiomaticMemberKind.HandWritten))
        {
            writer.Line();
            writer.Line("/// <summary>Lowers the members the configuration marks hand-written.</summary>");
            writer.Line("/// <param name=\"target\">The raw structure to write.</param>");
            writer.Line("/// <param name=\"arena\">The memory of the call.</param>");
            writer.Line($"private readonly partial void LowerHandWritten({mirror.RawType}* target, scoped ref Arena arena);");
        }

        var chain = mirror.Members.SingleOrDefault(static member => member.Kind == IdiomaticMemberKind.ChainHeader)?.Name;

        foreach (var root in mirror.InputRoots)
        {
            writer.Line();
            writer.Line("/// <inheritdoc/>");
            writer.Line($"static Raw.ChainedStruct* {InputInterface}<{mirror.Name}, {root}>.Lower(scoped in {mirror.Name} extension, scoped ref Arena arena)");
            writer.OpenBlock();
            writer.Line($"var target = arena.Allocate<{mirror.RawType}>();");
            writer.Line();
            writer.Line("extension.Lower(target, ref arena);");
            writer.Line();
            writer.Line($"return &target->{chain};");
            writer.CloseBlock();
        }

        writer.CloseBlock();
    }

    /// <summary>Writes the method that lowers a mirror to its raw structure.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="library">The projected idiomatic layer.</param>
    /// <param name="mirror">The mirror.</param>
    private static void EmitLower(CSharpWriter writer, IdiomaticLibrary library, IdiomaticStructure mirror)
    {
        var isRef = mirror.Role == IdiomaticRole.Mirror;
        var pinned = GetPinnedMembers(mirror).ToList();

        writer.Line("/// <summary>Writes the raw form of the mirror, copying into the arena what the caller did not pin.</summary>");
        writer.Line("/// <param name=\"target\">The raw structure to write.</param>");
        writer.Line("/// <param name=\"arena\">The memory of the call.</param>");

        foreach (var member in pinned)
        {
            writer.Line($"/// <param name=\"{GetPointerName(member)}\">The pinned <see cref=\"{member.Name}\"/>, or <see langword=\"null\"/> to copy it.</param>");
        }

        var parameters = new List<string> { $"{mirror.RawType}* target", "scoped ref Arena arena" };

        parameters.AddRange(pinned.Select(static member => $"{(member.Kind == IdiomaticMemberKind.Text ? "byte" : member.RawElementType)}* {GetPointerName(member)} = null"));
        writer.Line($"internal readonly void Lower({string.Join(", ", parameters)})");
        writer.OpenBlock();

        EmitMemberStatements(writer, mirror.Members, (memberWriter, member) => EmitMemberLowering(memberWriter, library, member, isRef));

        if (mirror.Members.Any(static member => member.Kind == IdiomaticMemberKind.HandWritten))
        {
            writer.Line();
            writer.Line("LowerHandWritten(target, ref arena);");
        }

        writer.CloseBlock();
    }

    /// <summary>Writes the lowering of one member of a mirror.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="library">The projected idiomatic layer.</param>
    /// <param name="member">The member.</param>
    /// <param name="isRef">Whether the mirror is a <c>ref struct</c>, whose members are spans and may be pinned.</param>
    private static void EmitMemberLowering(CSharpWriter writer, IdiomaticLibrary library, IdiomaticMember member, bool isRef)
    {
        var field = $"target->{member.Name}";
        var span = isRef ? member.Name : $"{member.Name}.Span";

        switch (member.Kind)
        {
            case IdiomaticMemberKind.Value:
            case IdiomaticMemberKind.Boolean:
            case IdiomaticMemberKind.FunctionPointer:
                writer.Line($"{field} = {member.Name};");
                break;

            case IdiomaticMemberKind.Pointer:
                writer.Line($"{field} = (void*){member.Name};");
                break;

            case IdiomaticMemberKind.Text:
                writer.Line(isRef
                    ? $"{field} = {member.Name}.Lower({GetPointerName(member)}, ref arena);"
                    : $"{field} = Utf8Text.Lower({member.Name}, ref arena);");
                break;

            case IdiomaticMemberKind.Span:
                EmitFixedLengthCheck(writer, member, span);
                EmitCount(writer, member, span);

                var copy = isRef ? $"{GetPointerName(member)} != null ? {GetPointerName(member)} : arena.Copy({span})" : $"arena.Copy({span})";

                if (member.Slots is { } slots)
                {
                    var elements = $"{Camel(member.Name)}Elements";

                    writer.Line($"if ({slots.Name}.Length != 0)");
                    writer.OpenBlock();
                    EmitSlotsLengthCheck(writer, slots, span);
                    writer.Line($"var {elements} = arena.Copy({span});");
                    writer.Line();
                    writer.Line($"for (var i = 0; i < {span}.Length; i++)");
                    writer.OpenBlock();
                    writer.Line($"{elements}[i].{slots.NextInChainField} = {slots.Name}[i].Lower(ref arena);");
                    writer.CloseBlock();
                    writer.Line();
                    writer.Line($"{field} = {elements};");
                    writer.CloseBlock();
                    writer.Line("else");
                    writer.OpenBlock();
                    writer.Line($"{field} = {copy};");
                    writer.CloseBlock();
                }
                else
                {
                    writer.Line($"{field} = {(member.FixedLength is not null && member.IsOptional ? $"{span}.IsEmpty ? null : {copy}" : copy)};");
                }

                break;

            case IdiomaticMemberKind.StructureSpan:
                var structureElements = $"{Camel(member.Name)}Elements";
                var source = $"{Camel(member.Name)}Source";

                writer.Line($"var {source} = {span};");
                writer.Line($"var {structureElements} = arena.Allocate<{member.RawElementType}>({source}.Length);");
                writer.Line();
                writer.Line($"for (var i = 0; i < {source}.Length; i++)");
                writer.OpenBlock();
                writer.Line($"{source}[i].Lower(&{structureElements}[i], ref arena);");
                writer.CloseBlock();
                writer.Line();

                if (member.Slots is { } elementSlots)
                {
                    writer.Line($"if ({elementSlots.Name}.Length != 0)");
                    writer.OpenBlock();
                    EmitSlotsLengthCheck(writer, elementSlots, source);
                    writer.Line($"for (var i = 0; i < {source}.Length; i++)");
                    writer.OpenBlock();
                    writer.Line($"{structureElements}[i].{elementSlots.NextInChainField} = {elementSlots.Name}[i].Lower(ref arena);");
                    writer.CloseBlock();
                    writer.CloseBlock();
                    writer.Line();
                }

                EmitCount(writer, member, source);
                writer.Line($"{field} = {structureElements};");
                break;

            case IdiomaticMemberKind.StringSpan:
                EmitFixedLengthCheck(writer, member, span);
                EmitCount(writer, member, span);
                writer.Line($"{field} = arena.CopyStrings({span});");
                break;

            case IdiomaticMemberKind.ValuePointer:
                var value = $"{Camel(member.Name)}Value";
                var source1 = member.IsOptional ? $"{member.Name}.GetValueOrDefault()" : member.Name;

                if (member.IsOptional)
                {
                    writer.Line($"if ({member.Name}.HasValue)");
                    writer.OpenBlock();
                }

                writer.Line($"var {value} = arena.Copy({source1});");

                if (member.Slots is { } pointerSlots)
                {
                    writer.Line();
                    writer.Line($"{value}->{pointerSlots.NextInChainField} = {pointerSlots.Name}.Lower(ref arena);");
                }

                writer.Line($"{field} = {value};");

                if (member.IsOptional)
                {
                    writer.CloseBlock();
                    writer.Line("else");
                    writer.OpenBlock();
                    writer.Line($"{field} = null;");
                    writer.CloseBlock();
                }

                break;

            case IdiomaticMemberKind.Nested:
                writer.Line($"{member.Name}.Lower(&{field}, ref arena);");
                break;

            case IdiomaticMemberKind.NestedPointer:
                var nested = $"{Camel(member.Name)}Raw";

                if (member.IsOptional)
                {
                    writer.Line($"if ({member.Name}.IsSet)");
                    writer.OpenBlock();
                }

                writer.Line($"var {nested} = arena.Allocate<{member.RawNestedType}>();");
                writer.Line();
                writer.Line($"{member.Name}.Lower({nested}, ref arena);");
                writer.Line($"{field} = {nested};");

                if (member.IsOptional)
                {
                    writer.CloseBlock();
                    writer.Line("else");
                    writer.OpenBlock();
                    writer.Line($"{field} = null;");
                    writer.CloseBlock();
                }

                break;

            case IdiomaticMemberKind.NextInChain:
                writer.Line($"{field} = null;");
                break;

            case IdiomaticMemberKind.ChainHeader:
                writer.Line($"{field}.{library.ChainNext} = null;");
                writer.Line($"{field}.{library.ChainType} = {member.StructureType};");
                break;

            case IdiomaticMemberKind.Count:
            case IdiomaticMemberKind.HandWritten:
            default:
                break;
        }
    }

    /// <summary>Writes the statements of each member, those of a member that takes several set off by blank lines.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="members">The members.</param>
    /// <param name="emit">Writes the statements of one member.</param>
    private static void EmitMemberStatements(CSharpWriter writer, IEnumerable<IdiomaticMember> members, Action<CSharpWriter, IdiomaticMember> emit)
    {
        var previousIsBlock = false;
        var first = true;

        foreach (var member in members)
        {
            // Each member is written apart and then copied line by line, at the writer's indentation.
            var memberWriter = new CSharpWriter();

            emit(memberWriter, member);

            var lines = memberWriter.ToString().Split('\n').SkipLast(1).ToList();

            if (lines.Count == 0)
            {
                continue;
            }

            var isBlock = lines.Count > 1;

            if (!first && (isBlock || previousIsBlock))
            {
                writer.Line();
            }

            foreach (var line in lines)
            {
                writer.Line(line);
            }

            first = false;
            previousIsBlock = isBlock;
        }
    }

    /// <summary>Writes the check of a span member whose length the API fixes.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="member">The member.</param>
    /// <param name="span">The expression of the span.</param>
    private static void EmitFixedLengthCheck(CSharpWriter writer, IdiomaticMember member, string span)
    {
        if (member.FixedLength is not { } length)
        {
            return;
        }

        var count = length.ToString(CultureInfo.InvariantCulture);
        var condition = member.IsOptional ? $"!{span}.IsEmpty && {span}.Length != {count}" : $"{span}.Length != {count}";

        writer.Line($"if ({condition})");
        writer.OpenBlock();
        writer.Line($"throw new global::System.ArgumentException(\"{member.Name} must hold {count} elements{(member.IsOptional ? ", or none" : string.Empty)}.\", nameof({member.Name}));");
        writer.CloseBlock();
        writer.Line();
    }

    /// <summary>Writes the check that the slots of a span member are absent or one per element.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="slots">The slots.</param>
    /// <param name="span">The expression of the span of elements.</param>
    private static void EmitSlotsLengthCheck(CSharpWriter writer, IdiomaticSlots slots, string span)
    {
        writer.Line($"if ({slots.Name}.Length != {span}.Length)");
        writer.OpenBlock();
        writer.Line($"throw new global::System.ArgumentException(\"{slots.Name} must be empty or hold one element per element it extends.\", nameof({slots.Name}));");
        writer.CloseBlock();
        writer.Line();
    }

    /// <summary>Writes the assignment of the count of a span member.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="member">The member.</param>
    /// <param name="span">The expression of the span.</param>
    private static void EmitCount(CSharpWriter writer, IdiomaticMember member, string span)
    {
        if (member.CountField is { } count)
        {
            writer.Line($"target->{count} = checked(({member.CountType}){span}.Length);");
        }
    }

    /// <summary>Writes a snapshot: an immutable copy of an output structure.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="library">The projected idiomatic layer.</param>
    /// <param name="snapshot">The snapshot.</param>
    public static void EmitSnapshot(CSharpWriter writer, IdiomaticLibrary library, IdiomaticStructure snapshot)
    {
        var interfaces = snapshot.OutputRoots.Select(root => $"{OutputInterface}<{snapshot.Name}, {root}>").ToList();
        var visible = snapshot.Members.Where(static member => member.Type.Length > 0).ToList();

        writer.Line($"/// <summary>A copy of <c>{snapshot.Declaration.CName}</c>, which the library fills in; it keeps nothing the library allocated.</summary>");
        writer.Line(RawLayerEmitter.GeneratedCodeAttribute);
        RawLayerEmitter.EmitPlatformAttributes(writer, snapshot.Availability);
        writer.Line($"public sealed unsafe partial class {snapshot.Name}{(interfaces.Count > 0 ? " : " + string.Join(", ", interfaces) : string.Empty)}");
        writer.OpenBlock();
        writer.Line($"/// <summary>Initializes a new instance of the <see cref=\"{snapshot.Name}\"/> class from the raw structure.</summary>");
        writer.Line("/// <param name=\"raw\">The raw structure, whose memory the copy does not keep.</param>");
        writer.Line($"internal {snapshot.Name}(in {snapshot.RawType} raw)");
        writer.OpenBlock();

        EmitMemberStatements(writer, visible, EmitMemberCopy);
        writer.CloseBlock();

        foreach (var member in visible)
        {
            writer.Line();
            writer.Line($"/// <summary>{GetSnapshotDocumentation(snapshot, member)}</summary>");
            writer.Line($"public {member.Type} {member.Name} {{ get; }}");
        }

        var chain = snapshot.Members.SingleOrDefault(static member => member.Kind == IdiomaticMemberKind.ChainHeader)?.Name;

        foreach (var root in snapshot.OutputRoots)
        {
            writer.Line();
            writer.Line("/// <inheritdoc/>");
            writer.Line($"static Raw.ChainedStruct* {OutputInterface}<{snapshot.Name}, {root}>.Allocate(scoped ref Arena arena)");
            writer.OpenBlock();
            writer.Line($"var target = arena.Allocate<{snapshot.RawType}>();");
            writer.Line();
            writer.Line($"*target = new {snapshot.RawType}();");
            writer.Line($"target->{chain}.{library.ChainNext} = null;");
            writer.Line($"target->{chain}.{library.ChainType} = {snapshot.StructureType};");
            writer.Line();
            writer.Line($"return &target->{chain};");
            writer.CloseBlock();
            writer.Line();
            writer.Line("/// <inheritdoc/>");
            writer.Line($"static {snapshot.Name} {OutputInterface}<{snapshot.Name}, {root}>.Raise(Raw.ChainedStruct* chain)");
            writer.OpenBlock();
            writer.Line($"var raw = ({snapshot.RawType}*)chain;");
            writer.Line();

            if (snapshot.FreeMembers is { } freeMembers)
            {
                writer.Line("try");
                writer.OpenBlock();
                writer.Line($"return new {snapshot.Name}(in *raw);");
                writer.CloseBlock();
                writer.Line("finally");
                writer.OpenBlock();
                writer.Line($"{NativeMethods}.{freeMembers}(*raw);");
                writer.CloseBlock();
            }
            else
            {
                writer.Line($"return new {snapshot.Name}(in *raw);");
            }

            writer.CloseBlock();
        }

        writer.CloseBlock();
    }

    /// <summary>Writes the copy of one member of a snapshot from its raw structure.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="member">The member.</param>
    private static void EmitMemberCopy(CSharpWriter writer, IdiomaticMember member)
    {
        var field = $"raw.{member.Name}";

        switch (member.Kind)
        {
            case IdiomaticMemberKind.Value:
            case IdiomaticMemberKind.Boolean:
                writer.Line($"{member.Name} = {field};");

                if (member.IsHandle)
                {
                    // The library releases what it allocated for the structure, the references it holds included.
                    writer.Line($"{member.Name}.AddRef();");
                }

                break;

            case IdiomaticMemberKind.Pointer:
                writer.Line($"{member.Name} = (nint){field};");
                break;

            case IdiomaticMemberKind.Text:
                writer.Line($"{member.Name} = Utf8Text.ToManagedString({field});");
                break;

            case IdiomaticMemberKind.Nested:
                writer.Line($"{member.Name} = new {member.Type}(in {field});");
                break;

            case IdiomaticMemberKind.Span:
                writer.Line($"{member.Name} = global::System.Collections.Immutable.ImmutableArray.Create(new global::System.ReadOnlySpan<{member.ElementType}>({field}, checked((int)raw.{member.CountField})));");

                if (member.IsHandle)
                {
                    writer.Line();
                    writer.Line($"foreach (var element in {member.Name})");
                    writer.OpenBlock();
                    writer.Line("element.AddRef();");
                    writer.CloseBlock();
                }

                break;

            case IdiomaticMemberKind.StructureSpan:
                var builder = $"{Camel(member.Name)}Builder";

                writer.Line($"var {builder} = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<{member.ElementType}>(checked((int)raw.{member.CountField}));");
                writer.Line();
                writer.Line($"for (var i = 0; i < {builder}.Capacity; i++)");
                writer.OpenBlock();
                writer.Line($"{builder}.Add(new {member.ElementType}(in {field}[i]));");
                writer.CloseBlock();
                writer.Line();
                writer.Line($"{member.Name} = {builder}.MoveToImmutable();");
                break;

            case IdiomaticMemberKind.FunctionPointer:
            case IdiomaticMemberKind.StringSpan:
            case IdiomaticMemberKind.ValuePointer:
            case IdiomaticMemberKind.NestedPointer:
            case IdiomaticMemberKind.Count:
            case IdiomaticMemberKind.NextInChain:
            case IdiomaticMemberKind.ChainHeader:
            case IdiomaticMemberKind.HandWritten:
            default:
                throw new InvalidDataException($"'{member.Member.CName}' cannot be copied into a snapshot as {member.Kind}.");
        }
    }

    /// <summary>Gets the summary of a mirror member.</summary>
    /// <param name="mirror">The mirror.</param>
    /// <param name="member">The member.</param>
    /// <returns>The summary text.</returns>
    private static string GetMemberDocumentation(IdiomaticStructure mirror, IdiomaticMember member)
    {
        var summary = $"Maps <c>{mirror.Declaration.CName}.{member.Member.CName}</c>.";
        var count = mirror.Members.FirstOrDefault(other => other.Name == member.CountField)?.Member.CName;

        return member.Kind switch
        {
            IdiomaticMemberKind.Text when mirror.Role == IdiomaticRole.Mirror => $"{summary} A <see langword=\"default\"/> value passes the null string.",
            IdiomaticMemberKind.Text => $"{summary} <see langword=\"null\"/> passes the null string.",
            IdiomaticMemberKind.Span or IdiomaticMemberKind.StructureSpan or IdiomaticMemberKind.StringSpan when member.FixedLength is { } length
                => $"{summary} It holds {length.ToString(CultureInfo.InvariantCulture)} elements{(member.IsOptional ? ", or none for a null pointer" : string.Empty)}.",
            IdiomaticMemberKind.Span or IdiomaticMemberKind.StructureSpan or IdiomaticMemberKind.StringSpan => $"{summary} Its length gives <c>{count}</c>.",
            IdiomaticMemberKind.ValuePointer when member.IsOptional => $"{summary} <see langword=\"null\"/> passes a null pointer.",
            IdiomaticMemberKind.NestedPointer when member.IsOptional => $"{summary} A <see langword=\"default\"/> value, which no constructor initialized, passes a null pointer.",
            IdiomaticMemberKind.Pointer => $"{summary} The native pointer, which the caller keeps valid.",
            IdiomaticMemberKind.HandWritten => $"The callback of <c>{mirror.Declaration.CName}.{member.Member.CName}</c>, or <see langword=\"null\"/>; see the delegate type for when the library calls it.",
            IdiomaticMemberKind.Value or IdiomaticMemberKind.Boolean or IdiomaticMemberKind.FunctionPointer or IdiomaticMemberKind.ValuePointer or IdiomaticMemberKind.Nested
                or IdiomaticMemberKind.NestedPointer or IdiomaticMemberKind.Count or IdiomaticMemberKind.NextInChain or IdiomaticMemberKind.ChainHeader => summary,
            _ => summary,
        };
    }

    /// <summary>Gets the summary of a snapshot property.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="member">The member.</param>
    /// <returns>The summary text.</returns>
    private static string GetSnapshotDocumentation(IdiomaticStructure snapshot, IdiomaticMember member)
    {
        var summary = $"Gets <c>{snapshot.Declaration.CName}.{member.Member.CName}</c>";

        return member switch
        {
            { Kind: IdiomaticMemberKind.Text } => $"{summary}; <see langword=\"null\"/> for the null string.",
            { IsHandle: true } => $"{summary}; the copy holds a reference to each object, which its owner releases.",
            _ => $"{summary}.",
        };
    }

    /// <summary>Gets the name of the parameter that receives a pinned member.</summary>
    /// <param name="member">The member.</param>
    /// <returns><c>labelPointer</c> for <c>Label</c>.</returns>
    private static string GetPointerName(IdiomaticMember member)
    {
        return $"{Camel(member.Name)}Pointer";
    }

    /// <summary>Lowers the first letter of a name.</summary>
    /// <param name="name">The PascalCase name.</param>
    /// <returns>The camelCase name.</returns>
    private static string Camel(string name)
    {
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>Writes a blank line before every member but the first.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="first">Whether no member was written yet; cleared.</param>
    private static void Separate(CSharpWriter writer, ref bool first)
    {
        if (!first)
        {
            writer.Line();
        }

        first = false;
    }
}
