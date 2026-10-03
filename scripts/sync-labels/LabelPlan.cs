/// <summary>Works out what a sync changes, without calling GitHub.</summary>
internal static class LabelPlan
{
    /// <summary>Matches every declared label with an existing one, by name and then by alias.</summary>
    /// <param name="declared">The labels of .github/labels.yml, whose names and aliases are unique ignoring case.</param>
    /// <param name="existing">The labels on GitHub.</param>
    /// <returns>One change per declared label, in file order, then one per undeclared label, in GitHub's order.</returns>
    public static IReadOnlyList<LabelChange> Compute(IReadOnlyList<Label> declared, IReadOnlyList<Label> existing)
    {
        // GitHub label names are unique ignoring case, and so are the declared names and aliases.
        var remaining = existing.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);
        var changes = new List<LabelChange>();
        foreach (var label in declared)
        {
            if (remaining.Remove(label.Name, out var current))
            {
                var differences = Differences(current, label);
                changes.Add(new LabelChange(differences.Count == 0 ? LabelChangeKind.Unchanged : LabelChangeKind.Update, current.Name, label, differences));
                continue;
            }

            var alias = label.Aliases.FirstOrDefault(remaining.ContainsKey);
            if (alias is not null && remaining.Remove(alias, out current))
            {
                changes.Add(new LabelChange(LabelChangeKind.Rename, current.Name, label, Differences(current, label)));
                continue;
            }

            changes.Add(new LabelChange(LabelChangeKind.Create, label.Name, label, []));
        }

        foreach (var label in existing)
        {
            if (remaining.ContainsKey(label.Name))
            {
                changes.Add(new LabelChange(LabelChangeKind.Undeclared, label.Name, null, []));
            }
        }

        return changes;
    }

    private static List<string> Differences(Label current, Label declared)
    {
        var differences = new List<string>();
        if (!string.Equals(current.Name, declared.Name, StringComparison.Ordinal))
        {
            differences.Add("name");
        }

        if (!string.Equals(current.Color, declared.Color, StringComparison.OrdinalIgnoreCase))
        {
            differences.Add("color");
        }

        if (!string.Equals(current.Description, declared.Description, StringComparison.Ordinal))
        {
            differences.Add("description");
        }

        return differences;
    }
}
