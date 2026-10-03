// Labeler check.
using System.Text;

/// <summary>Turns C names into C# names (ADR-0006: prefixes stripped, PascalCase).</summary>
internal static class Naming
{
    private static readonly HashSet<string> s_keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    ];

    /// <summary>Removes the first matching prefix.</summary>
    /// <param name="name">A C name.</param>
    /// <param name="prefixes">Candidate prefixes, longest first.</param>
    /// <returns>The name without its prefix, or unchanged if none matches or nothing would remain.</returns>
    public static string StripPrefix(string name, IReadOnlyList<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (name.Length > prefix.Length && name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return name[prefix.Length..];
            }
        }

        return name;
    }

    /// <summary>
    /// Converts a C name to PascalCase, one <c>_</c>-separated word at a time: all-caps words become capitalized
    /// (<c>WINDOW_SHOWN</c> to <c>WindowShown</c>), with a new word after each run of digits (<c>INDEX1LSB</c> to
    /// <c>Index1Lsb</c>), unless <paramref name="words"/> says otherwise; other words only get their first letter raised,
    /// which keeps C names already in PascalCase (<c>GetWindowID</c>) intact.
    /// </summary>
    /// <param name="name">A C name without its library prefix.</param>
    /// <param name="words">Casing overrides for all-caps words.</param>
    /// <param name="leadingDigitPrefix">What goes before a name that would start with a digit (C# names cannot).</param>
    /// <returns>A valid C# identifier.</returns>
    public static string Pascal(string name, IReadOnlyDictionary<string, string> words, string leadingDigitPrefix)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var word in name.Split('_', StringSplitOptions.RemoveEmptyEntries))
        {
            if (words.TryGetValue(word, out var cased))
            {
                builder.Append(cased);
            }
            else if (word.Any(char.IsLetter) && !word.Any(char.IsLower))
            {
                for (var i = 0; i < word.Length; i++)
                {
                    var startsWord = i == 0 || (char.IsLetter(word[i]) && char.IsDigit(word[i - 1]));
                    builder.Append(startsWord ? word[i] : char.ToLowerInvariant(word[i]));
                }
            }
            else
            {
                builder.Append(char.ToUpperInvariant(word[0])).Append(word.AsSpan(1));
            }
        }

        var pascal = builder.ToString();
        return pascal.Length == 0 || char.IsDigit(pascal[0]) ? leadingDigitPrefix + pascal : pascal;
    }

    /// <summary>Converts a C parameter name to camelCase, escaping C# keywords.</summary>
    /// <param name="name">A C parameter name.</param>
    /// <param name="words">Casing overrides for all-caps words.</param>
    /// <returns>A valid C# identifier.</returns>
    public static string Camel(string name, IReadOnlyDictionary<string, string> words)
    {
        var pascal = Pascal(name, words, "_");
        var camel = pascal[0] == '_' ? pascal : char.ToLowerInvariant(pascal[0]) + pascal[1..];
        return s_keywords.Contains(camel) ? "@" + camel : camel;
    }

    /// <summary>Finds the longest run of leading <c>_</c>-separated words shared by every name, never a whole name.</summary>
    /// <param name="names">C names, for example the constants of one enum.</param>
    /// <returns>The prefix including its trailing <c>_</c>, or an empty string.</returns>
    public static string CommonPrefix(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
        {
            return string.Empty;
        }

        var split = names.Select(n => n.Split('_')).ToList();
        var shortest = split.Min(s => s.Length);
        var count = 0;
        while (count < shortest - 1 && split.All(s => string.Equals(s[count], split[0][count], StringComparison.Ordinal)))
        {
            count++;
        }

        return count == 0 ? string.Empty : string.Join('_', split[0][..count]) + "_";
    }
}
