using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jade.BindingGenerator.Projection;

/// <summary>Turns the words of C names into .NET names, with the exceptions of the library configuration (ADR 0027).</summary>
/// <remarks>
/// Each word becomes a PascalCase word. Inside a word, upper-case runs of more than two letters are
/// acronyms that become Pascal-cased (<c>RGBA8</c> gives <c>Rgba8</c>, <c>WebGPU</c> gives
/// <c>WebGpu</c>), two-letter ones stay in upper case (<c>IO</c>), and digits and the casing of the
/// other letters are kept (<c>float32x2</c> gives <c>Float32x2</c>).
/// </remarks>
/// <param name="names">The .NET names that replace the rule's, by C name.</param>
/// <param name="words">The casing of words that replaces the rule's, by word.</param>
internal sealed partial class DotNetNames(IReadOnlyDictionary<string, string> names, IReadOnlyDictionary<string, string> words)
{
    /// <summary>The longest acronym that keeps its upper case.</summary>
    private const int ShortAcronymLength = 2;

    /// <summary>The C names whose replacement was used.</summary>
    private readonly HashSet<string> _usedNames = [with(StringComparer.Ordinal)];

    /// <summary>The words whose replacement was used.</summary>
    private readonly HashSet<string> _usedWords = [with(StringComparer.Ordinal)];

    /// <summary>Gets the .NET name of a declaration or a member.</summary>
    /// <param name="cName">The C name that a replacement is keyed by; <c>Structure.member</c> for a member.</param>
    /// <param name="words">The words of the name.</param>
    /// <returns>The .NET name.</returns>
    /// <exception cref="InvalidDataException">The name would start with a digit.</exception>
    public string GetName(string cName, IReadOnlyList<string> words)
    {
        var name = GetReplacement(cName) ?? Join(words);

        return char.IsAsciiDigit(name[0])
            ? throw new InvalidDataException($"The .NET name of '{cName}' would be '{name}', which starts with a digit: add it to 'names'.")
            : name;
    }

    /// <summary>Gets the camel-case .NET name of a parameter.</summary>
    /// <param name="cName">The key of a replacement, <c>function.parameter</c>.</param>
    /// <param name="words">The words of the name.</param>
    /// <returns>The .NET name, whose first word is lowered whole when it is an acronym: <c>IOStream</c> gives <c>ioStream</c>.</returns>
    /// <exception cref="InvalidDataException">The name would start with a digit.</exception>
    public string GetParameterName(string cName, IReadOnlyList<string> words)
    {
        if (GetReplacement(cName) is { } replacement)
        {
            return replacement;
        }

        var first = ConvertWord(words[0]);
        var head = first.All(char.IsAsciiLetterUpper) ? AsciiText.ToLower(first) : char.ToLowerInvariant(first[0]) + first[1..];
        var name = head + string.Concat(words.Skip(1).Select(ConvertWord));

        return char.IsAsciiDigit(name[0])
            ? throw new InvalidDataException($"The .NET name of '{cName}' would be '{name}', which starts with a digit: add it to 'names'.")
            : name;
    }

    /// <summary>Gets the .NET name of an enum value, which does not repeat its enum's name.</summary>
    /// <param name="cName">The C name of the value.</param>
    /// <param name="words">The words of the value.</param>
    /// <param name="enumWords">The words of the enum, whose last one prefixes a name that would start with a digit.</param>
    /// <returns>The .NET name: <c>Dimension2D</c> for the <c>2D</c> value of <c>texture dimension</c>.</returns>
    public string GetEnumValueName(string cName, IReadOnlyList<string> words, IReadOnlyList<string> enumWords)
    {
        var name = GetReplacement(cName) ?? Join(words);

        return char.IsAsciiDigit(name[0]) ? ConvertWord(enumWords[^1]) + name : name;
    }

    /// <summary>Checks that every replacement of the configuration was used, so that the configuration cannot drift from the API.</summary>
    /// <exception cref="InvalidDataException">A replacement matches nothing.</exception>
    public void CheckAllUsed()
    {
        var unusedNames = names.Keys.Where(name => !_usedNames.Contains(name)).Order(StringComparer.Ordinal).ToList();
        var unusedWords = words.Keys.Where(word => !_usedWords.Contains(word)).Order(StringComparer.Ordinal).ToList();

        if (unusedNames.Count > 0)
        {
            throw new InvalidDataException($"'names' has entries that match no public declaration: {string.Join(", ", unusedNames)}.");
        }

        if (unusedWords.Count > 0)
        {
            throw new InvalidDataException($"'words' has entries that match no word of a public name: {string.Join(", ", unusedWords)}.");
        }
    }

    /// <summary>Gets the configured replacement of a name.</summary>
    /// <param name="cName">The C name.</param>
    /// <returns>The replacement, or <see langword="null"/> when the rule applies.</returns>
    private string? GetReplacement(string cName)
    {
        if (!names.TryGetValue(cName, out var name))
        {
            return null;
        }

        _ = _usedNames.Add(cName);

        return name;
    }

    /// <summary>Joins the words of a name into a .NET name.</summary>
    /// <param name="words">The words.</param>
    /// <returns>The name.</returns>
    private string Join(IReadOnlyList<string> words)
    {
        return string.Concat(words.Select(ConvertWord));
    }

    /// <summary>Converts one word into a PascalCase word.</summary>
    /// <param name="word">The word, such as <c>RGBA8</c>, <c>unorm</c> or <c>colorOffsetX</c>.</param>
    /// <returns>The converted word.</returns>
    private string ConvertWord(string word)
    {
        if (words.TryGetValue(word, out var replacement))
        {
            _ = _usedWords.Add(word);

            return replacement;
        }

        var builder = new StringBuilder(word.Length);
        var first = true;

        foreach (var token in Tokens().EnumerateMatches(word))
        {
            var text = word.AsSpan(token.Index, token.Length);
            var converted = IsUpperCase(text) && text.Length > ShortAcronymLength
                ? text[0] + AsciiText.ToLower(text[1..])
                : first && char.IsAsciiLetterLower(text[0])
                    ? char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..].ToString()
                    : text.ToString();

            _ = builder.Append(converted);
            first = false;
        }

        return builder.ToString();
    }

    /// <summary>Tells whether a token is an upper-case run.</summary>
    /// <param name="token">The token.</param>
    /// <returns><see langword="true"/> when every character is an upper-case ASCII letter.</returns>
    private static bool IsUpperCase(ReadOnlySpan<char> token)
    {
        foreach (var character in token)
        {
            if (!char.IsAsciiLetterUpper(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits a word into an acronym (an upper-case run, without the capital that starts a
    /// following lower-case run), a capitalized run, a lower-case run, a digit run or a single
    /// other character.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z][a-z]*|[a-z]+|[0-9]+|.", RegexOptions.CultureInvariant)]
    private static partial Regex Tokens();
}
