using System.Diagnostics;
using Jade.BindingGenerator.Projection;

namespace Jade.BindingGenerator.Emission;

/// <summary>
/// The overloads of an idiomatic method and the code each part of it needs: text in UTF-8 and as
/// strings (ADR 0016), with and without its optional pointer, and with zero to two extensions of
/// its chain roots (ADR 0029, ADR 0040).
/// </summary>
internal sealed class MethodShape
{
    /// <summary>The interface of input extensions.</summary>
    private const string InputInterface = "global::Jade.Wgpu.IChainedExtension";

    /// <summary>The interface of output extensions.</summary>
    private const string OutputInterface = "global::Jade.Wgpu.IChainedOutputExtension";

    /// <summary>The method.</summary>
    private readonly IdiomaticMethod _method;

    /// <summary>The parameters the method takes itself, in C order.</summary>
    private readonly List<IdiomaticParameter> _visible;

    /// <summary>The optional pointer parameter that an overload leaves out, if any.</summary>
    private readonly IdiomaticParameter? _optional;

    /// <summary>The parameter whose structure is a chain root with input extensions, if any.</summary>
    private readonly IdiomaticParameter? _inputRoot;

    /// <summary>The output parameter whose structure is a chain root with output extensions, if any.</summary>
    private readonly IdiomaticParameter? _outputRoot;

    /// <summary>Initializes a new instance of the <see cref="MethodShape"/> class.</summary>
    /// <param name="method">The method.</param>
    /// <param name="chainNext">The name of the raw chain header's field that links the next extension.</param>
    private MethodShape(IdiomaticMethod method, string chainNext)
    {
        _method = method;
        _visible = [.. method.Parameters.Where(static parameter => parameter.Kind is not (IdiomaticParameterKind.Count or IdiomaticParameterKind.Output or IdiomaticParameterKind.Callback))];
        _optional = _visible.SingleOrDefault(static parameter => parameter.IsOptional);
        _inputRoot = _visible.SingleOrDefault(static parameter => parameter.Kind is IdiomaticParameterKind.Descriptor or IdiomaticParameterKind.InValue && parameter.HasExtensions);
        _outputRoot = method.Parameters.SingleOrDefault(static parameter => parameter.Kind == IdiomaticParameterKind.Output && parameter.HasExtensions);
        ChainNext = chainNext;

        if (_inputRoot is not null && _outputRoot is not null)
        {
            throw new InvalidDataException($"'{method.Function.CName}' has both input and output extensions, which one overload set cannot take.");
        }
    }

    /// <summary>Gets the name of the raw chain header's field that links the next extension.</summary>
    public string ChainNext { get; }

    /// <summary>Gets whether the core lowers text or descriptors, and so takes the call's arena.</summary>
    public bool CoreNeedsArena => _visible.Any(static parameter => parameter.Kind is IdiomaticParameterKind.Text or IdiomaticParameterKind.Descriptor);

    /// <summary>Gets whether the method takes untyped data, as a span of a type parameter.</summary>
    public bool HasData => _visible.Any(static parameter => parameter.Kind is IdiomaticParameterKind.Data or IdiomaticParameterKind.MutableData);

    /// <summary>Creates the shape of a method.</summary>
    /// <param name="method">The method.</param>
    /// <param name="chainNext">The name of the raw chain header's field that links the next extension.</param>
    /// <returns>The shape.</returns>
    /// <exception cref="InvalidDataException">The method has extensions in both directions.</exception>
    public static MethodShape Create(IdiomaticMethod method, string chainNext)
    {
        return new MethodShape(method, chainNext);
    }

    /// <summary>Gets the name of the core's flag that tells whether an optional parameter is present.</summary>
    /// <param name="parameter">The optional parameter.</param>
    /// <returns><c>hasDescriptor</c> for <c>descriptor</c>.</returns>
    public static string GetPresenceName(IdiomaticParameter parameter)
    {
        var name = parameter.Name.TrimStart('@');

        return "has" + char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>Gets the members of a descriptor parameter's mirror that the call pins rather than copies (ADR 0029).</summary>
    /// <param name="parameter">The descriptor parameter.</param>
    /// <returns>The text and blittable span members of a <c>ref struct</c> mirror, in order; none for an element mirror.</returns>
    public static IEnumerable<IdiomaticMember> GetPinnedMembers(IdiomaticParameter parameter)
    {
        return parameter.Mirror is { Role: IdiomaticRole.Mirror } mirror ? MirrorEmitter.GetPinnedMembers(mirror) : [];
    }

    /// <summary>Gets the public overloads of the method.</summary>
    /// <returns>The overloads, in declaration order.</returns>
    public IReadOnlyList<MethodOverload> GetOverloads()
    {
        TextForm[] texts = _visible.Any(static parameter => parameter.Kind == IdiomaticParameterKind.Text) ? [TextForm.Utf8, TextForm.String] : [TextForm.None];
        bool[] presences = _optional is null ? [true] : [false, true];
        var overloads = new List<MethodOverload>();

        foreach (var text in texts)
        {
            foreach (var present in presences)
            {
                int[] inputs = _inputRoot is not null && (_inputRoot != _optional || present) ? [0, 1, 2] : [0];
                int[] outputs = _outputRoot is not null ? [0, 1, 2] : [0];

                foreach (var input in inputs)
                {
                    foreach (var output in outputs)
                    {
                        overloads.Add(new MethodOverload(text, present, input, output));
                    }
                }
            }
        }

        return overloads;
    }

    /// <summary>Gets the parameters of a public overload.</summary>
    /// <param name="overload">The overload.</param>
    /// <returns>The parameters, the extensions last.</returns>
    public IReadOnlyList<OverloadParameter> GetOverloadParameters(MethodOverload overload)
    {
        var hasExtensions = overload.InputExtensions > 0 || overload.OutputExtensions > 0;
        var parameters = new List<OverloadParameter>();

        foreach (var parameter in _visible)
        {
            if (parameter == _optional && !overload.IsOptionalPresent)
            {
                continue;
            }

            var argument = $"The <c>{parameter.Parameter.CName}</c> argument";

            parameters.Add(parameter.Kind switch
            {
                IdiomaticParameterKind.Text when overload.Text == TextForm.String => new OverloadParameter(parameter.Name, $"string? {parameter.Name}", $"{argument}, encoded to UTF-8 for the call; <see langword=\"null\"/> passes the null string."),
                IdiomaticParameterKind.Text => new OverloadParameter(parameter.Name, $"{parameter.Type} {parameter.Name}", $"{argument}, in UTF-8."),
                IdiomaticParameterKind.Descriptor => new OverloadParameter(parameter.Name, $"scoped in {parameter.Type} {parameter.Name}", $"{argument}, lowered to its raw form for the call."),
                IdiomaticParameterKind.InValue => new OverloadParameter(parameter.Name, $"scoped in {parameter.Type} {parameter.Name}", $"{argument}."),
                IdiomaticParameterKind.Span or IdiomaticParameterKind.MutableSpan => new OverloadParameter(
                    parameter.Name,
                    $"{(parameter.IsParams && !hasExtensions ? "params " : string.Empty)}{parameter.Type} {parameter.Name}",
                    $"{argument}; its length gives <c>{GetCountName(parameter)}</c>."),
                IdiomaticParameterKind.Data or IdiomaticParameterKind.MutableData => new OverloadParameter(parameter.Name, $"{parameter.Type} {parameter.Name}", $"{argument}; its size in bytes gives <c>{GetCountName(parameter)}</c>."),
                IdiomaticParameterKind.Value or IdiomaticParameterKind.Boolean or IdiomaticParameterKind.Pointer or IdiomaticParameterKind.Count or IdiomaticParameterKind.Output or IdiomaticParameterKind.Callback
                    => new OverloadParameter(parameter.Name, parameter.Default is { } value && !hasExtensions ? $"{parameter.Type} {parameter.Name} = {value}" : $"{parameter.Type} {parameter.Name}", $"{argument}."),
                _ => throw new UnreachableException($"Unknown parameter kind {parameter.Kind}."),
            });
        }

        var root = _inputRoot?.Name;

        switch (overload.InputExtensions)
        {
            case 1:
                parameters.Add(new OverloadParameter("extension", "scoped in TExtension extension", $"An extension chained to <paramref name=\"{root}\"/>."));
                break;

            case 2:
                parameters.Add(new OverloadParameter("extension1", "scoped in TExtension1 extension1", $"An extension chained to <paramref name=\"{root}\"/>."));
                parameters.Add(new OverloadParameter("extension2", "scoped in TExtension2 extension2", $"Another extension chained to <paramref name=\"{root}\"/>."));
                break;

            default:
                break;
        }

        switch (overload.OutputExtensions)
        {
            case 1:
                parameters.Add(new OverloadParameter("extension", "out TExtension extension", "Receives an extension of the result, which the function fills in."));
                break;

            case 2:
                parameters.Add(new OverloadParameter("extension1", "out TExtension1 extension1", "Receives an extension of the result, which the function fills in."));
                parameters.Add(new OverloadParameter("extension2", "out TExtension2 extension2", "Receives another extension of the result, which the function fills in."));
                break;

            default:
                break;
        }

        return parameters;
    }

    /// <summary>Gets the type parameters of a public overload and their constraints.</summary>
    /// <param name="overload">The overload.</param>
    /// <returns>The type parameters, in order.</returns>
    public IReadOnlyList<(string Name, string Constraint)> GetTypeParameters(MethodOverload overload)
    {
        var typeParameters = new List<(string, string)>();

        if (HasData)
        {
            typeParameters.Add(("TData", "unmanaged"));
        }

        if (overload.InputExtensions > 0)
        {
            var names = overload.InputExtensions == 1 ? ["TExtension"] : new[] { "TExtension1", "TExtension2" };

            typeParameters.AddRange(names.Select(name => (name, $"{InputInterface}<{name}, {_inputRoot!.Type}>, allows ref struct")));
        }

        if (overload.OutputExtensions > 0)
        {
            var names = overload.OutputExtensions == 1 ? ["TExtension"] : new[] { "TExtension1", "TExtension2" };

            typeParameters.AddRange(names.Select(name => (name, $"{OutputInterface}<{name}, {_outputRoot!.Type}>")));
        }

        return typeParameters;
    }

    /// <summary>Gets the parameters of the core.</summary>
    /// <returns>The declarations, the chains and the arena last.</returns>
    public IEnumerable<string> GetCoreParameters()
    {
        foreach (var parameter in _visible)
        {
            switch (parameter.Kind)
            {
                case IdiomaticParameterKind.Text:
                    // Not scoped: the core passes the text and the arena to the same call, which the
                    // compiler only allows when both can escape as far.
                    yield return $"Utf8Text {parameter.Name}";
                    break;

                case IdiomaticParameterKind.Descriptor or IdiomaticParameterKind.InValue:
                    yield return $"scoped in {parameter.Type} {parameter.Name}";

                    if (parameter.IsOptional)
                    {
                        yield return $"bool {GetPresenceName(parameter)}";
                    }

                    break;

                case IdiomaticParameterKind.Value:
                case IdiomaticParameterKind.Boolean:
                case IdiomaticParameterKind.Pointer:
                case IdiomaticParameterKind.Span:
                case IdiomaticParameterKind.MutableSpan:
                case IdiomaticParameterKind.Data:
                case IdiomaticParameterKind.MutableData:
                case IdiomaticParameterKind.Count:
                case IdiomaticParameterKind.Output:
                case IdiomaticParameterKind.Callback:
                default:
                    yield return $"{parameter.Type} {parameter.Name}";
                    break;
            }
        }

        if (_inputRoot is not null)
        {
            yield return "Raw.ChainedStruct* chain";
        }

        if (_outputRoot is not null)
        {
            yield return "Raw.ChainedStruct* outputChain";
        }

        if (CoreNeedsArena)
        {
            yield return "scoped ref Arena arena";
        }
    }

    /// <summary>Gets the arguments a public overload passes to the core.</summary>
    /// <param name="overload">The overload.</param>
    /// <returns>The arguments, in the order of <see cref="GetCoreParameters"/>.</returns>
    public IEnumerable<string> GetCoreArguments(MethodOverload overload)
    {
        foreach (var parameter in _visible)
        {
            if (parameter == _optional)
            {
                yield return overload.IsOptionalPresent ? $"in {parameter.Name}" : "default";
                yield return overload.IsOptionalPresent ? "true" : "false";
            }
            else
            {
                yield return parameter.Kind is IdiomaticParameterKind.Descriptor or IdiomaticParameterKind.InValue ? $"in {parameter.Name}" : parameter.Name;
            }
        }

        if (_inputRoot is not null)
        {
            yield return overload.InputExtensions > 0 ? "chain" : "null";
        }

        if (_outputRoot is not null)
        {
            yield return overload.OutputExtensions > 0 ? "outputChain" : "null";
        }

        if (CoreNeedsArena)
        {
            yield return "ref arena";
        }
    }

    /// <summary>Gets the <see langword="fixed"/> statements that pin what the call passes without a copy (ADR 0029).</summary>
    /// <returns>The statements, which share one block.</returns>
    public IEnumerable<string> GetFixedStatements()
    {
        foreach (var parameter in _visible)
        {
            switch (parameter.Kind)
            {
                case IdiomaticParameterKind.Text:
                    yield return $"fixed (byte* {parameter.Name}Pointer = {parameter.Name}.Bytes)";
                    break;

                case IdiomaticParameterKind.Span or IdiomaticParameterKind.MutableSpan or IdiomaticParameterKind.Data or IdiomaticParameterKind.MutableData:
                    yield return $"fixed ({parameter.ElementType}* {parameter.Name}Pointer = {parameter.Name})";
                    break;

                case IdiomaticParameterKind.InValue when !parameter.HasExtensions:
                    yield return $"fixed ({parameter.Type}* {parameter.Name}Pointer = &{parameter.Name})";
                    break;

                case IdiomaticParameterKind.Descriptor:
                    foreach (var member in GetPinnedMembers(parameter))
                    {
                        yield return member.Kind == IdiomaticMemberKind.Text
                            ? $"fixed (byte* {parameter.Name}{member.Name}Pointer = {parameter.Name}.{member.Name}.Bytes)"
                            : $"fixed ({member.RawElementType}* {parameter.Name}{member.Name}Pointer = {parameter.Name}.{member.Name})";
                    }

                    break;

                case IdiomaticParameterKind.Value:
                case IdiomaticParameterKind.Boolean:
                case IdiomaticParameterKind.Pointer:
                case IdiomaticParameterKind.InValue:
                case IdiomaticParameterKind.Count:
                case IdiomaticParameterKind.Output:
                case IdiomaticParameterKind.Callback:
                default:
                    break;
            }
        }
    }

    /// <summary>Gets the arguments of the native call.</summary>
    /// <param name="isCore">Whether the call is in the core, whose optional parameters come with a presence flag.</param>
    /// <returns>The arguments, the handle first for an instance method.</returns>
    public IEnumerable<string> GetNativeArguments(bool isCore)
    {
        if (_method.Kind != IdiomaticMethodKind.Static)
        {
            yield return "this";
        }

        foreach (var parameter in _method.Parameters)
        {
            var presence = isCore && parameter.IsOptional ? GetPresenceName(parameter) : null;

            yield return parameter.Kind switch
            {
                IdiomaticParameterKind.Value or IdiomaticParameterKind.Boolean => parameter.Name,
                IdiomaticParameterKind.Pointer => $"(void*){parameter.Name}",
                IdiomaticParameterKind.Text => $"{parameter.Name}.Lower({parameter.Name}Pointer, ref arena)",
                IdiomaticParameterKind.InValue => (isCore && parameter.HasExtensions ? $"&{parameter.Name}Copy" : $"{parameter.Name}Pointer") is var pointer && presence is not null ? $"{presence} ? {pointer} : null" : pointer,
                IdiomaticParameterKind.Descriptor => presence is not null ? $"{presence} ? &{parameter.Name}Raw : null" : $"&{parameter.Name}Raw",
                IdiomaticParameterKind.Span or IdiomaticParameterKind.MutableSpan or IdiomaticParameterKind.Data or IdiomaticParameterKind.MutableData => $"{parameter.Name}Pointer",
                IdiomaticParameterKind.Count when parameter.CountsBytes => $"checked(({parameter.RawType})((nuint){parameter.CountOf}.Length * (nuint)sizeof(TData)))",
                IdiomaticParameterKind.Count => $"checked(({parameter.RawType}){parameter.CountOf}.Length)",
                IdiomaticParameterKind.Output => $"&{parameter.Name}",
                IdiomaticParameterKind.Callback => "callbackInfo",
                _ => throw new InvalidDataException($"Unknown parameter kind {parameter.Kind}."),
            };
        }
    }

    /// <summary>Gets the C name of the parameter that receives the length of a span parameter.</summary>
    /// <param name="span">The span parameter.</param>
    /// <returns>The C name of the count parameter.</returns>
    private string GetCountName(IdiomaticParameter span)
    {
        return _method.Parameters.Single(parameter => parameter.Kind == IdiomaticParameterKind.Count && parameter.CountOf == span.Name).Parameter.CName;
    }
}
