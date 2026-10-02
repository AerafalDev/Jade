/// <summary>
/// Compares what each target triple yielded. Declarations must be identical everywhere, and the CLR layout of every
/// generated struct must equal clang's on every target. Raw size differences that the generated definition absorbs
/// (pointer-sized or <c>long</c>-sized fields) are reported but accepted.
/// </summary>
internal static class VarianceCheck
{
    /// <summary>Runs the check.</summary>
    /// <param name="targets">The per-target results, the first one being the reference.</param>
    /// <param name="emit">Renders a model to generated files, used to compare declarations exactly.</param>
    /// <param name="report">Receives the report lines.</param>
    /// <returns>The unhandled variances, empty when the library is clean.</returns>
    public static IReadOnlyList<string> Run(IReadOnlyList<TargetModel> targets, Func<LibraryModel, IReadOnlyDictionary<string, string>> emit, List<string> report)
    {
        var errors = new List<string>();
        var reference = targets[0];
        var referenceFiles = emit(reference.Model);
        foreach (var target in targets.Skip(1))
        {
            var files = emit(target.Model);
            foreach (var name in referenceFiles.Keys.Union(files.Keys).Order(StringComparer.Ordinal))
            {
                referenceFiles.TryGetValue(name, out var expected);
                files.TryGetValue(name, out var actual);
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    errors.Add($"{name}: declarations differ between {reference.Target.Rid} and {target.Target.Rid}: {FirstDifference(expected, actual)}");
                }
            }
        }

        var calculators = targets.ToDictionary(t => t, t => new LayoutCalculator(t.Model, t.PointerSize, t.LongSize));
        foreach (var structModel in reference.Model.Structs.Where(s => !s.IsOpaque).OrderBy(s => s.NativeName, StringComparer.Ordinal))
        {
            var native = new List<(Target Target, string Layout)>();
            foreach (var target in targets)
            {
                var model = target.Model.Structs.FirstOrDefault(s => s.NativeName == structModel.NativeName);
                if (model is null || !target.Layouts.TryGetValue(structModel.NativeName, out var nativeLayout))
                {
                    continue;
                }

                var managedLayout = calculators[target].Layout(model);
                native.Add((target.Target, nativeLayout.Describe()));
                if (managedLayout.Describe() != nativeLayout.Describe())
                {
                    errors.Add($"{structModel.NativeName} on {target.Target.Rid}: native {nativeLayout.Describe()}, generated {managedLayout.Describe()}. " +
                        "Record a LayoutDecision for it in the library config.");
                }
            }

            var groups = native.GroupBy(n => n.Layout, StringComparer.Ordinal).ToList();
            if (groups.Count > 1)
            {
                report.Add($"  {structModel.NativeName}: {string.Join("; ", groups.Select(g => $"{g.Key} on {Targets(g.Select(n => n.Target))}"))}");
            }
        }

        return errors;
    }

    private static string Targets(IEnumerable<Target> targets)
    {
        var list = targets.ToList();
        return list.Count > 3 ? $"{list.Count} targets" : string.Join(", ", list.Select(t => t.Rid));
    }

    private static string FirstDifference(string? expected, string? actual)
    {
        if (expected is null || actual is null)
        {
            return expected is null ? "missing on the first target" : "missing on the second target";
        }

        var left = expected.Split('\n');
        var right = actual.Split('\n');
        for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            var a = i < left.Length ? left[i].Trim() : "<end>";
            var b = i < right.Length ? right[i].Trim() : "<end>";
            if (a != b)
            {
                return $"line {i + 1}: `{a}` vs `{b}`";
            }
        }

        return "line endings";
    }
}
