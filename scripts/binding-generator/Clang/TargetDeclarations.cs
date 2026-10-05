using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Clang;

/// <summary>What a library's headers declare for one target: the input of the merge into the intermediate representation (ADR 0033).</summary>
/// <param name="Target">The target.</param>
/// <param name="Declarations">The declarations, by C name.</param>
/// <param name="Skipped">The declarations left out on this target, by C name.</param>
/// <param name="Macros">The object-like macros, in definition order.</param>
/// <param name="FunctionLikeMacroCount">The number of function-like macros, which are never bound (ADR 0027).</param>
/// <param name="MacroValues">The values of the macros the configuration selects, by name.</param>
internal sealed record TargetDeclarations(
    Target Target,
    IReadOnlyDictionary<string, TargetDeclaration> Declarations,
    IReadOnlyDictionary<string, SkippedDeclaration> Skipped,
    IReadOnlyList<MacroDefinition> Macros,
    int FunctionLikeMacroCount,
    IReadOnlyDictionary<string, MacroValue> MacroValues);
