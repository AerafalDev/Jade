using YamlDotNet.RepresentationModel;

/// <summary>Reads .github/labels.yml: a sequence of labels with a name, a color, a description and optional aliases.</summary>
internal static class LabelFile
{
    /// <summary>The file, relative to the repository root.</summary>
    public const string Path = ".github/labels.yml";

    private static readonly string[] s_keys = ["name", "color", "description", "aliases"];

    /// <summary>Loads and checks the declared labels.</summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <returns>The labels, in file order.</returns>
    /// <exception cref="InvalidOperationException">The file is missing, malformed, or declares a name or alias twice.</exception>
    public static IReadOnlyList<Label> Load(string repositoryRoot)
    {
        if (Yaml.Load(repositoryRoot, Path) is not YamlSequenceNode sequence)
        {
            throw new InvalidOperationException($"{Path} must be a sequence of labels.");
        }

        var labels = new List<Label>();

        // A rename picks the label by name ignoring case, as GitHub does, so a name or alias may only appear once.
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in sequence)
        {
            var label = Read(node);
            foreach (var name in label.Aliases.Prepend(label.Name))
            {
                if (!seen.TryAdd(name, label.Name))
                {
                    throw new InvalidOperationException($"{Yaml.Where(Path, node)}: \"{name}\" is already a name or alias of \"{seen[name]}\".");
                }
            }

            labels.Add(label);
        }

        return labels;
    }

    private static Label Read(YamlNode node)
    {
        if (node is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException($"{Yaml.Where(Path, node)}: a label must be a mapping.");
        }

        foreach (var key in mapping.Children.Keys)
        {
            if (key is not YamlScalarNode { Value: var name } || !s_keys.Contains(name))
            {
                throw new InvalidOperationException($"{Yaml.Where(Path, key)}: unknown key, expected one of {string.Join(", ", s_keys)}.");
            }
        }

        var labelName = Text(mapping, "name");
        var color = Text(mapping, "color");
        if (color.Length != 6 || !color.All(char.IsAsciiHexDigit))
        {
            throw new InvalidOperationException($"{Yaml.Where(Path, mapping)}: the color of \"{labelName}\" must be six hexadecimal digits without #.");
        }

        var aliases = new List<string>();
        switch (Yaml.Get(mapping, "aliases"))
        {
            case null:
                break;

            case YamlSequenceNode items when items.All(item => item is YamlScalarNode { Value.Length: > 0 }):
                aliases.AddRange(items.Select(item => ((YamlScalarNode)item).Value!));
                break;

            case var other:
                throw new InvalidOperationException($"{Yaml.Where(Path, other)}: aliases must be a sequence of label names.");
        }

        return new Label(labelName, color, Text(mapping, "description"), aliases);
    }

    private static string Text(YamlMappingNode mapping, string key) =>
        Yaml.Get(mapping, key) is YamlScalarNode { Value: { Length: > 0 } value }
            ? value
            : throw new InvalidOperationException($"{Yaml.Where(Path, mapping)}: every label needs a non-empty {key}.");
}
