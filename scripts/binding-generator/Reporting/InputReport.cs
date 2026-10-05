using Jade.BindingGenerator.Clang;
using Jade.BindingGenerator.Dawn;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Reporting;

/// <summary>Writes what the front-ends loaded for each library, so that a reviewer sees the inputs a run used.</summary>
internal static class InputReport
{
    /// <summary>The number of characters of a commit hash shown in the report.</summary>
    private const int ShortCommitLength = 12;

    /// <summary>Writes the report of one library.</summary>
    /// <param name="writer">The writer to report to.</param>
    /// <param name="library">The loaded library.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the report is written.</returns>
    public static async Task WriteAsync(TextWriter writer, LoadedLibrary library, CancellationToken cancellationToken)
    {
        var lines = new List<string>
        {
            FormattableString.Invariant($"{library.Library.Project}: {library.Configuration.Dependency} {library.Dependency.Tag} ({library.Dependency.Commit[..ShortCommitLength]})"),
        };

        if (library.Dawn is { } api)
        {
            lines.AddRange(DescribeDawnApi(api));
        }

        if (library.Headers is { } headers)
        {
            lines.AddRange(DescribeHeaders(headers));
        }

        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Describes the entries of <c>dawn.json</c> and the header variants that contain them.</summary>
    /// <param name="api">The API read from <c>dawn.json</c>.</param>
    /// <returns>The report lines.</returns>
    private static IEnumerable<string> DescribeDawnApi(DawnApi api)
    {
        var categories = api.Entries.Values
            .GroupBy(static entry => entry.Category)
            .OrderBy(static group => group.Key)
            .Select(static group => FormattableString.Invariant($"{group.Key} {group.Count()}"));
        var platforms = api.Entries.Values.Select(static entry => DawnVariants.GetPlatforms(entry.Tags)).ToList();

        yield return FormattableString.Invariant($"  dawn.json: {api.Entries.Count} entries ({string.Join(", ", categories)})");
        yield return FormattableString.Invariant($"  native variant: {platforms.Count(static availability => availability.HasFlag(Platforms.Native))} entries, browser variant: {platforms.Count(static availability => availability.HasFlag(Platforms.Browser))} entries");
    }

    /// <summary>Describes the parsed declarations, and those that some targets lack.</summary>
    /// <param name="headers">The parsed headers.</param>
    /// <returns>The report lines.</returns>
    private static IEnumerable<string> DescribeHeaders(ParsedHeaders headers)
    {
        var groups = headers.GroupPlatformSpecificDeclarations();

        yield return FormattableString.Invariant($"  {headers.Targets.Count} targets, {headers.AllDeclarations.Count} declarations, {groups.Sum(static group => group.Declarations.Count)} not on every target");

        foreach (var group in groups)
        {
            var targets = string.Join(", ", group.Targets.Select(static target => target.RuntimeIdentifier));
            var names = string.Join(", ", group.Declarations.Select(static declaration => declaration.Name).Distinct(StringComparer.Ordinal));

            yield return FormattableString.Invariant($"    only {targets}: {names}");
        }
    }
}
