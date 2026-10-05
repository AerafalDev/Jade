using ClangSharp;
using ClangSharp.Interop;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Clang;

/// <summary>Evaluates object-like macros with clang, never by reading their tokens (ADR 0027).</summary>
/// <remarks>
/// Each macro initializes a variable of its own type, <c>const __typeof__(M) v = M;</c>, appended to
/// the parsed headers; clang then evaluates the initializer as the compiler would, with the
/// target's integer sizes and the library's other macros.
/// </remarks>
internal static class MacroEvaluator
{
    /// <summary>The prefix of the variables that hold the macros, a reserved identifier that no library declares.</summary>
    private const string VariablePrefix = "__jade_macro_";

    /// <summary>Gets the declarations that make clang evaluate macros.</summary>
    /// <param name="macros">The macros to evaluate.</param>
    /// <returns>The C source to append to the parsed headers.</returns>
    public static string GetSource(IEnumerable<MacroDefinition> macros)
    {
        return string.Concat(macros.Select(static macro => $"const __typeof__({macro.Name}) {VariablePrefix}{macro.Name} = {macro.Name};\n"));
    }

    /// <summary>Reads the values of the macros from a translation unit parsed with <see cref="GetSource"/>.</summary>
    /// <param name="translationUnit">The translation unit.</param>
    /// <param name="macros">The evaluated macros, by name.</param>
    /// <param name="library">The key of the library, for error messages.</param>
    /// <param name="target">The target of the parse, for error messages.</param>
    /// <returns>The values, by macro name.</returns>
    /// <exception cref="InvalidDataException">A macro is not a number or a string.</exception>
    public static Dictionary<string, MacroValue> Evaluate(TranslationUnit translationUnit, IReadOnlyDictionary<string, MacroDefinition> macros, string library, Target target)
    {
        var values = new Dictionary<string, MacroValue>(StringComparer.Ordinal);

        foreach (var variable in translationUnit.TranslationUnitDecl.Decls.OfType<VarDecl>())
        {
            if (!variable.Name.StartsWith(VariablePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var macro = macros[variable.Name[VariablePrefix.Length..]];
            var type = variable.Type.CanonicalType;
            using var result = variable.Handle.Evaluate;

            values.Add(macro.Name, result.Kind switch
            {
                CXEvalResultKind.CXEval_Int => new MacroValue(
                    TargetModelBuilder.IsSigned(type.Kind) ? MacroValueKind.SignedInteger : MacroValueKind.UnsignedInteger,
                    type.Handle.SizeOf,
                    result.IsUnsignedInt ? result.AsUnsigned : unchecked((ulong)result.AsLongLong),
                    0,
                    null,
                    macro.Header),
                CXEvalResultKind.CXEval_Float => new MacroValue(MacroValueKind.Float, type.Handle.SizeOf, 0, result.AsDouble, null, macro.Header),
                CXEvalResultKind.CXEval_StrLiteral when type is ArrayType array && array.ElementType.CanonicalType.Handle.SizeOf == 1 =>
                    new MacroValue(MacroValueKind.String, 1, 0, 0, result.AsStr, macro.Header),
                CXEvalResultKind.CXEval_StrLiteral or CXEvalResultKind.CXEval_ObjCStrLiteral or CXEvalResultKind.CXEval_CFStr or CXEvalResultKind.CXEval_Other or CXEvalResultKind.CXEval_UnExposed =>
                    throw CreateUnsupportedValueException(library, target, macro, result.Kind, type),
                _ => throw CreateUnsupportedValueException(library, target, macro, result.Kind, type),
            });
        }

        return macros.Keys.FirstOrDefault(name => !values.ContainsKey(name)) is { } missing
            ? throw new InvalidDataException($"{library} ({target.Triple}): clang did not evaluate the macro {missing}.")
            : values;
    }

    /// <summary>Creates the error of a macro that is not a number or a string.</summary>
    /// <param name="library">The key of the library.</param>
    /// <param name="target">The target of the parse.</param>
    /// <param name="macro">The macro.</param>
    /// <param name="kind">What clang evaluated it to.</param>
    /// <param name="type">Its C type.</param>
    /// <returns>The exception.</returns>
    private static InvalidDataException CreateUnsupportedValueException(string library, Target target, MacroDefinition macro, CXEvalResultKind kind, ClangSharp.Type type)
    {
        return new InvalidDataException($"{library} ({target.Triple}): the macro {macro.Name} is not a number or a string of 8-bit characters ({kind}, {type.AsString}).");
    }
}
