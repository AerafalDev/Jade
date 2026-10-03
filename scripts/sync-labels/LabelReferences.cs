using YamlDotNet.RepresentationModel;

/// <summary>Finds the labels that the repository's automation applies, so that each one is declared before a sync.</summary>
internal static class LabelReferences
{
    private const string Labeler = ".github/labeler.yml";
    private const string Dependabot = ".github/dependabot.yml";
    private const string IssueTemplates = ".github/ISSUE_TEMPLATE";

    // Top-level keys of labeler.yml that are options rather than labels.
    private static readonly string[] s_labelerOptions = ["changed-files-labels-limit", "max-files-changed"];

    /// <summary>Lists every label that the labeler, Dependabot or an issue form applies but .github/labels.yml does not declare.</summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="declared">The declared labels.</param>
    /// <returns>One message per undeclared label; empty when all are declared.</returns>
    /// <exception cref="InvalidOperationException">One of the files is not valid YAML.</exception>
    public static IReadOnlyList<string> Check(string repositoryRoot, IReadOnlyList<Label> declared)
    {
        // The labeler and Dependabot look labels up by exact name, so the check does too.
        var names = declared.Select(label => label.Name).ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var (where, label) in Find(repositoryRoot))
        {
            if (!names.Contains(label))
            {
                problems.Add($"{where} applies \"{label}\", which {LabelFile.Path} does not declare.");
            }
        }

        return problems;
    }

    private static IEnumerable<(string Where, string Label)> Find(string repositoryRoot)
    {
        if (File.Exists(Path.Combine(repositoryRoot, Labeler)) && Yaml.Load(repositoryRoot, Labeler) is YamlMappingNode labeler)
        {
            foreach (var key in labeler.Children.Keys.OfType<YamlScalarNode>())
            {
                if (!s_labelerOptions.Contains(key.Value))
                {
                    yield return (Yaml.Where(Labeler, key), key.Value!);
                }
            }
        }

        if (File.Exists(Path.Combine(repositoryRoot, Dependabot))
            && Yaml.Load(repositoryRoot, Dependabot) is YamlMappingNode dependabot
            && Yaml.Get(dependabot, "updates") is YamlSequenceNode updates)
        {
            foreach (var update in updates.OfType<YamlMappingNode>())
            {
                if (Yaml.Get(update, "labels") is YamlSequenceNode labels)
                {
                    foreach (var label in labels.OfType<YamlScalarNode>())
                    {
                        yield return (Yaml.Where(Dependabot, label), label.Value!);
                    }
                }
            }
        }

        var templates = Path.Combine(repositoryRoot, IssueTemplates);
        if (Directory.Exists(templates))
        {
            // config.yml configures the template chooser; every other YAML file there is an issue form.
            var forms = Directory.EnumerateFiles(templates, "*.yml")
                .Concat(Directory.EnumerateFiles(templates, "*.yaml"))
                .Select(Path.GetFileName)
                .Where(name => name is not ("config.yml" or "config.yaml"))
                .Order(StringComparer.Ordinal);
            foreach (var name in forms)
            {
                var path = $"{IssueTemplates}/{name}";
                if (Yaml.Load(repositoryRoot, path) is not YamlMappingNode form)
                {
                    continue;
                }

                // An issue form takes its labels as a sequence or as one comma-separated string.
                switch (Yaml.Get(form, "labels"))
                {
                    case YamlSequenceNode labels:
                        foreach (var label in labels.OfType<YamlScalarNode>())
                        {
                            yield return (Yaml.Where(path, label), label.Value!);
                        }

                        break;

                    case YamlScalarNode { Value: { } list } scalar:
                        foreach (var label in list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                        {
                            yield return (Yaml.Where(path, scalar), label);
                        }

                        break;
                }
            }
        }
    }
}
