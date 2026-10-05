using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Clang;

/// <summary>The declarations of a library's headers, parsed once per target.</summary>
internal sealed class ParsedHeaders
{
    /// <summary>The declarations of each target.</summary>
    private readonly IReadOnlyDictionary<Target, IReadOnlySet<HeaderDeclaration>> _declarations;

    /// <summary>Initializes a new instance of the <see cref="ParsedHeaders"/> class.</summary>
    /// <param name="targets">The targets, in the order of ADR 0012; at most 64, so that a set of them fits a <see cref="ulong"/>.</param>
    /// <param name="declarations">The declarations of each target.</param>
    /// <param name="byTarget">What each target declares, in the order of <paramref name="targets"/>.</param>
    public ParsedHeaders(IReadOnlyList<Target> targets, IReadOnlyDictionary<Target, IReadOnlySet<HeaderDeclaration>> declarations, IReadOnlyList<TargetDeclarations> byTarget)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(targets.Count, sizeof(ulong) * 8);

        Targets = targets;
        ByTarget = byTarget;
        _declarations = declarations;
        AllDeclarations = [.. declarations.Values.SelectMany(static set => set).Distinct().Order(HeaderDeclaration.ByName)];
    }

    /// <summary>Gets the targets the headers were parsed for.</summary>
    public IReadOnlyList<Target> Targets { get; }

    /// <summary>Gets what each target declares, the input of the merge into the intermediate representation.</summary>
    public IReadOnlyList<TargetDeclarations> ByTarget { get; }

    /// <summary>Gets the declarations of every target together, ordered by <see cref="HeaderDeclaration.ByName"/>.</summary>
    public IReadOnlyList<HeaderDeclaration> AllDeclarations { get; }

    /// <summary>Groups the declarations that some targets lack by the set of targets that declare them.</summary>
    /// <returns>The groups, the ones of the first targets of ADR 0012 first.</returns>
    /// <remarks>
    /// These declarations are where the intermediate representation needs platform availability,
    /// or an opaque type when a layout differs (ADR 0026).
    /// </remarks>
    public IReadOnlyList<DeclarationGroup> GroupPlatformSpecificDeclarations()
    {
        var everyTarget = GetTargetMask(_ => true);

        return [.. AllDeclarations
            .GroupBy(declaration => GetTargetMask(target => _declarations[target].Contains(declaration)))
            .Where(group => group.Key != everyTarget)
            .OrderBy(static group => group.Key)
            .Select(group => new DeclarationGroup(GetTargets(group.Key), [.. group]))];
    }

    /// <summary>Builds the bit mask of the targets that satisfy a condition, bit <c>i</c> standing for <c>Targets[i]</c>.</summary>
    /// <param name="predicate">The condition on a target.</param>
    /// <returns>The mask of the matching targets.</returns>
    private ulong GetTargetMask(Func<Target, bool> predicate)
    {
        var mask = 0UL;

        for (var i = 0; i < Targets.Count; i++)
        {
            if (predicate(Targets[i]))
            {
                mask |= 1UL << i;
            }
        }

        return mask;
    }

    /// <summary>Gets the targets of a bit mask built by <see cref="GetTargetMask"/>.</summary>
    /// <param name="mask">The mask.</param>
    /// <returns>The targets, in the order of ADR 0012.</returns>
    private List<Target> GetTargets(ulong mask)
    {
        return [.. Targets.Where((_, index) => (mask & (1UL << index)) != 0)];
    }
}
