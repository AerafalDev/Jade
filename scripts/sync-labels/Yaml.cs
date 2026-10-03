using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>Reads YAML files as node trees. The representation model maps nothing to types, so it needs no reflection.</summary>
internal static class Yaml
{
    /// <summary>Loads the root node of a file's first document.</summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="path">The file, relative to the repository root with forward slashes, as messages show it.</param>
    /// <returns>The root node, or <see langword="null"/> when the file is empty.</returns>
    /// <exception cref="InvalidOperationException">The file is missing or is not valid YAML.</exception>
    public static YamlNode? Load(string repositoryRoot, string path)
    {
        try
        {
            using var reader = File.OpenText(Path.Combine(repositoryRoot, path));
            var stream = new YamlStream();
            stream.Load(reader);
            return stream.Documents.Count == 0 ? null : stream.Documents[0].RootNode;
        }
        catch (YamlException e)
        {
            throw new InvalidOperationException($"cannot read {path}:{e.Start.Line}: {e.Message}", e);
        }
        catch (IOException e)
        {
            throw new InvalidOperationException($"cannot read {path}: {e.Message}", e);
        }
        catch (InvalidOperationException e)
        {
            // YamlDotNet 18.1.0's scanner throws this, without a position, on some malformed input such as an unclosed
            // flow sequence.
            throw new InvalidOperationException($"cannot read {path}: not valid YAML.", e);
        }
    }

    /// <summary>Gets the value of a mapping entry.</summary>
    /// <param name="mapping">The mapping.</param>
    /// <param name="key">The entry's key.</param>
    /// <returns>The value, or <see langword="null"/> when the mapping has no such key.</returns>
    public static YamlNode? Get(YamlMappingNode mapping, string key)
    {
        foreach (var (name, value) in mapping.Children)
        {
            if (name is YamlScalarNode { Value: var text } && text == key)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Formats where a node starts, for messages.</summary>
    /// <param name="path">The file, as messages show it.</param>
    /// <param name="node">The node.</param>
    /// <returns><c>path:line</c>.</returns>
    public static string Where(string path, YamlNode node) => $"{path}:{node.Start.Line}";
}
