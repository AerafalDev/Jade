using System.Diagnostics;
using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Clang;

/// <summary>Describes what a declaration means for the bindings, so that its parses for different targets can be compared (ADR 0026).</summary>
/// <remarks>
/// The description leaves out what does not change the C# declaration: availability, the words of
/// names and parameter names. Two targets agree on a declaration when their descriptions are equal.
/// </remarks>
internal static class DeclarationSignature
{
    /// <summary>Describes a declaration.</summary>
    /// <param name="declaration">The declaration of one target.</param>
    /// <returns>A C-like description, such as <c>struct { int x; int y; }</c>.</returns>
    public static string Describe(Declaration declaration)
    {
        return declaration switch
        {
            StructureDeclaration structure =>
                $"{(structure.IsUnion ? "union" : "struct")} {{ {string.Concat(structure.Members.Select(static member => $"{Describe(member.Type)} {member.CName}; "))}}}",
            FunctionDeclaration function => $"{Describe(function.ReturnType)} ({DescribeParameters(function.Parameters)})",
            FunctionPointerDeclaration function => $"{Describe(function.ReturnType)} (*)({DescribeParameters(function.Parameters)})",
            TypedefDeclaration typedef => $"typedef {Describe(typedef.Target)}",
            EnumDeclaration enumDeclaration => $"enum : {Describe(enumDeclaration.UnderlyingType)}",
            HandleDeclaration => "handle",
            _ => throw new UnreachableException($"Unknown declaration {declaration}."),
        };
    }

    /// <summary>Describes a type reference.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The C spelling of the type.</returns>
    public static string Describe(TypeReference type)
    {
        return type switch
        {
            BuiltinTypeReference builtin => builtin.Spelling,
            DotNetTypeReference dotNet => dotNet.FullName,
            NamedTypeReference named => named.CName,
            PointerTypeReference pointer => $"{(pointer.IsConst ? "const " : string.Empty)}{Describe(pointer.Pointee)}*",
            ArrayTypeReference array => FormattableString.Invariant($"{Describe(array.Element)}[{array.Length}]"),
            FunctionPointerTypeReference function => $"{Describe(function.ReturnType)} (*)({string.Join(", ", function.ParameterTypes.Select(Describe))})",
            _ => throw new UnreachableException($"Unknown type reference {type}."),
        };
    }

    /// <summary>Describes the types of parameters.</summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The types, separated by commas.</returns>
    private static string DescribeParameters(IEnumerable<Parameter> parameters)
    {
        return string.Join(", ", parameters.Select(static parameter => Describe(parameter.Type)));
    }
}
