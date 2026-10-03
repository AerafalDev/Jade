/// <summary>
/// Checks a library's bindings against what jade_native exports: every exported symbol of the library is bound or
/// excluded with a reason (by name, or through its excluded header), and every binding has an export.
/// </summary>
internal static class ExportCheck
{
    /// <summary>Runs the check.</summary>
    /// <param name="config">The library config.</param>
    /// <param name="targets">The merged per-target results; their function lists are identical.</param>
    /// <param name="exports">Every name jade_native exports.</param>
    /// <param name="report">Receives the report lines.</param>
    /// <param name="usedConfigKeys">Receives the exclusions that matched an export.</param>
    /// <returns>The errors, empty when every export is accounted for.</returns>
    public static IReadOnlyList<string> Run(LibraryConfig config, IReadOnlyList<TargetModel> targets, IReadOnlySet<string> exports, List<string> report, HashSet<string> usedConfigKeys)
    {
        var errors = new List<string>();
        var symbols = exports.Where(e => config.ExportPrefixes.Any(p => e.StartsWith(p, StringComparison.Ordinal))).Order(StringComparer.Ordinal).ToList();
        var bound = targets[0].Model.Functions.Select(f => f.NativeName).ToHashSet(StringComparer.Ordinal);
        var byHeader = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (function, header) in targets.SelectMany(t => t.ExcludedFunctions))
        {
            byHeader.TryAdd(function, header);
        }

        var headerCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var byReason = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        var boundCount = 0;
        foreach (var symbol in symbols)
        {
            if (bound.Contains(symbol))
            {
                boundCount++;
            }
            else if (config.Exclusions.TryGetValue(symbol, out var reason))
            {
                usedConfigKeys.Add(symbol);
                if (!byReason.TryGetValue(reason, out var names))
                {
                    names = [];
                    byReason[reason] = names;
                }

                names.Add(symbol);
            }
            else if (byHeader.TryGetValue(symbol, out var header))
            {
                headerCounts[header] = headerCounts.GetValueOrDefault(header) + 1;
            }
            else
            {
                errors.Add($"{symbol} is exported but neither bound nor excluded: bind its header, or add it to Exclusions with a reason.");
            }
        }

        foreach (var function in bound.Where(b => !exports.Contains(b)).Order(StringComparer.Ordinal))
        {
            errors.Add($"{function} is bound but jade_native does not export it.");
        }

        var excluded = symbols.Count - boundCount;
        report.Add($"{config.Name}: export cross-check: {symbols.Count} exported {string.Join("/", config.ExportPrefixes.Select(p => p + "*"))}, {boundCount} bound, {excluded} excluded:");
        foreach (var (header, count) in headerCounts)
        {
            report.Add($"  {count} declared in {header}: {config.ExcludedHeaders[header]}");
        }

        foreach (var (reason, names) in byReason)
        {
            report.Add($"  {names.Count} {reason}: {string.Join(", ", names)}");
        }

        return errors;
    }
}
