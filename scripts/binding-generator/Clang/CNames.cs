using System.Text.RegularExpressions;

namespace Jade.BindingGenerator.Clang;

/// <summary>Splits the C names of a header library into the words the projection turns into .NET names (ADR 0027, ADR 0033).</summary>
/// <remarks>
/// Parts are separated by underscores and split again at case changes, so that <c>SDL_HapticID</c>
/// gives <c>Haptic</c> and <c>ID</c>, and the <c>words</c> of the configuration can change one of
/// them; a lower-case part, such as <c>vec3f</c> in <c>ma_vec3f</c>, is one word. A name without
/// lower-case letters, such as <c>SDL_EVENT_KEY_UP</c>, is SCREAMING_CASE: its parts are words,
/// not acronyms, and are lowered so that they become <c>Key</c> and <c>Up</c>.
/// </remarks>
/// <param name="prefixes">The library prefixes that names drop, matched without regard to case.</param>
/// <param name="memberPrefixes">The words that member and parameter names drop when they come first.</param>
internal sealed partial class CNames(IReadOnlyList<string> prefixes, IReadOnlyList<string> memberPrefixes)
{
    /// <summary>The prefixes, longest first, so that <c>jade_ma_</c> wins over a shorter one.</summary>
    private readonly string[] _prefixes = [.. prefixes.OrderByDescending(static prefix => prefix.Length).ThenBy(static prefix => prefix, StringComparer.Ordinal)];

    /// <summary>Gets the words of a declaration's name, without the library prefix.</summary>
    /// <param name="name">The C name.</param>
    /// <returns>The words.</returns>
    public IReadOnlyList<string> GetWords(string name)
    {
        var prefix = _prefixes.FirstOrDefault(prefix => name.Length > prefix.Length && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return Split(prefix is null ? name : name[prefix.Length..]);
    }

    /// <summary>Gets the words of a structure member or a parameter, without the prefix word of the library's notation.</summary>
    /// <param name="name">The C name, such as <c>pUserData</c>.</param>
    /// <returns>The words, such as <c>User</c> and <c>Data</c>.</returns>
    public IReadOnlyList<string> GetMemberWords(string name)
    {
        var words = Split(name);

        return words.Count > 1 && memberPrefixes.Contains(words[0], StringComparer.Ordinal) ? [.. words.Skip(1)] : words;
    }

    /// <summary>Gets the words of a name that has no library prefix, such as a structure member.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The words.</returns>
    public static IReadOnlyList<string> Split(string name)
    {
        var isScreamingCase = !name.Any(char.IsAsciiLetterLower);
        var words = new List<string>();

        foreach (var part in name.Split('_', StringSplitOptions.RemoveEmptyEntries))
        {
            if (isScreamingCase)
            {
                words.Add(AsciiText.ToLower(part));
            }
            else
            {
                words.AddRange(Tokens().Matches(part).Select(static match => match.Value));
            }
        }

        return words.Count > 0 ? words : throw new InvalidDataException($"'{name}' has no word to make a .NET name of.");
    }

    /// <summary>Gets the part of the names of an enum's values that they all share, which their .NET names drop (ADR 0033).</summary>
    /// <param name="names">The C names of the values.</param>
    /// <returns>The shared prefix, made of whole underscore-separated parts and never a whole name.</returns>
    public static string GetCommonPrefix(IReadOnlyList<string> names)
    {
        var parts = names.Select(static name => name.Split('_')).ToList();
        var length = 0;

        // The last part of every name stays: a value keeps at least one word.
        while (parts.All(nameParts => length < nameParts.Length - 1 && string.Equals(nameParts[length], parts[0][length], StringComparison.Ordinal)))
        {
            length++;
        }

        return length == 0 ? string.Empty : string.Join('_', parts[0][..length]) + "_";
    }

    /// <summary>
    /// Splits a part at case changes: a capital followed by lower-case letters and digits, an
    /// upper-case run, or a run of lower-case letters and digits. Digits stay in their word, so that
    /// <c>vec3f</c> stays one word, as the words of <c>dawn.json</c> do.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("[A-Z][a-z0-9]+|[A-Z]+(?![a-z])|[a-z0-9]+|[^_]", RegexOptions.CultureInvariant)]
    private static partial Regex Tokens();
}
