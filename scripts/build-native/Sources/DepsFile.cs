using System.Text.RegularExpressions;

namespace Jade.NativeBuild.Sources;

/// <summary>Reads the dependencies that a gclient <c>DEPS</c> file pins, as Dawn's <c>tools/fetch_dawn_dependencies.py</c> does.</summary>
/// <remarks>
/// A <c>DEPS</c> file is Python, which the fetch script executes. Only the form Dawn uses for the
/// entries the build needs is accepted, <c>'path': { 'url': '{variable}/repository@commit', ... }</c>
/// with string variables; any other form is an error rather than a guess.
/// </remarks>
internal sealed partial class DepsFile
{
    /// <summary>The text of the file.</summary>
    private readonly string _text;

    /// <summary>The string variables of the file's <c>vars</c>, by name.</summary>
    private readonly Dictionary<string, string> _variables;

    /// <summary>Initializes a new instance of the <see cref="DepsFile"/> class.</summary>
    /// <param name="text">The text of the file.</param>
    /// <param name="variables">The string variables of the file.</param>
    private DepsFile(string text, Dictionary<string, string> variables)
    {
        _text = text;
        _variables = variables;
    }

    /// <summary>Reads a <c>DEPS</c> file.</summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The file, ready to resolve its entries.</returns>
    /// <exception cref="InvalidDataException">The file has no <c>vars</c>.</exception>
    public static DepsFile Load(string path)
    {
        var text = File.ReadAllText(path);
        var block = VariablesBlock().Match(text);

        if (!block.Success)
        {
            throw new InvalidDataException($"'{path}' has no 'vars' block.");
        }

        var variables = StringVariable().Matches(block.Groups["body"].Value)
            .ToDictionary(static match => match.Groups["name"].Value, static match => match.Groups["value"].Value, StringComparer.Ordinal);

        return new DepsFile(text, variables);
    }

    /// <summary>Resolves the entry checked out at a path.</summary>
    /// <param name="path">The path that keys the entry in <c>deps</c>, such as <c>third_party/abseil-cpp</c>.</param>
    /// <returns>The repository and commit of the entry.</returns>
    /// <exception cref="InvalidDataException">The entry is missing or not in the accepted form.</exception>
    public DepsEntry GetEntry(string path)
    {
        var entry = Regex.Match(_text, $@"'{Regex.Escape(path)}':\s*\{{\s*'url':\s*'(?<url>[^']+)'", RegexOptions.None, TimeSpan.FromSeconds(1));

        if (!entry.Success)
        {
            throw new InvalidDataException($"DEPS has no entry '{path}' with a literal 'url'.");
        }

        var url = Placeholder().Replace(entry.Groups["url"].Value, match => _variables.TryGetValue(match.Groups["name"].Value, out var value)
            ? value
            : throw new InvalidDataException($"DEPS entry '{path}' uses the unknown variable '{match.Groups["name"].Value}'."));

        var separator = url.LastIndexOf('@', StringComparison.Ordinal);
        var commit = separator < 0 ? string.Empty : url[(separator + 1)..];

        return CommitHash().IsMatch(commit)
            ? new DepsEntry(path, url[..separator], commit)
            : throw new InvalidDataException($"DEPS entry '{path}' is not pinned to a full commit hash: '{url}'.");
    }

    /// <summary>Matches the <c>vars</c> block, which ends with a closing brace at the start of a line.</summary>
    /// <returns>The pattern, with the block's content in the <c>body</c> group.</returns>
    [GeneratedRegex(@"^vars = \{(?<body>.*?)^\}", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex VariablesBlock();

    /// <summary>Matches a variable whose value is a string literal; booleans and expressions are skipped.</summary>
    /// <returns>The pattern, with the <c>name</c> and <c>value</c> groups.</returns>
    [GeneratedRegex(@"'(?<name>[\w-]+)':\s*'(?<value>[^']*)'")]
    private static partial Regex StringVariable();

    /// <summary>Matches a <c>{variable}</c> placeholder in a URL, which Python's <c>str.format</c> replaces.</summary>
    /// <returns>The pattern, with the variable in the <c>name</c> group.</returns>
    [GeneratedRegex(@"\{(?<name>[\w-]+)\}")]
    private static partial Regex Placeholder();

    /// <summary>Matches a full lowercase SHA-1 commit hash.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex CommitHash();
}
