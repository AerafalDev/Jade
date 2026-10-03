using System.Text.RegularExpressions;

/// <summary>
/// An integer typedef whose values are <c>#define</c>s, such as <c>typedef Uint32 SDL_InitFlags;</c> followed by
/// <c>#define SDL_INIT_VIDEO 0x20u</c>. It becomes a C# enum of the macros in the typedef's header that start with
/// <see cref="Prefix"/> and match <see cref="Pattern"/>.
/// </summary>
internal sealed class MacroEnum
{
    private MacroEnum(string prefix, string? pattern, bool isFlags)
    {
        Prefix = prefix;
        Pattern = pattern is null ? null : new Regex(pattern, RegexOptions.CultureInvariant);
        IsFlags = isFlags;
    }

    /// <summary>Gets the prefix every member macro starts with; it is stripped from member names.</summary>
    public string Prefix { get; }

    /// <summary>Gets a pattern the macro name must also match, for prefixes shared with unrelated macros, or <see langword="null"/>.</summary>
    public Regex? Pattern { get; }

    /// <summary>Gets whether the values combine as bits, which adds <c>[Flags]</c>.</summary>
    public bool IsFlags { get; }

    /// <summary>Creates a <c>[Flags]</c> enum.</summary>
    /// <param name="prefix">The prefix of the member macros.</param>
    /// <param name="pattern">A pattern the macro names must also match, or <see langword="null"/>.</param>
    /// <returns>The entry.</returns>
    public static MacroEnum Flags(string prefix, string? pattern = null) => new(prefix, pattern, isFlags: true);

    /// <summary>Creates an enum of distinct values.</summary>
    /// <param name="prefix">The prefix of the member macros.</param>
    /// <param name="pattern">A pattern the macro names must also match, or <see langword="null"/>.</param>
    /// <returns>The entry.</returns>
    public static MacroEnum Values(string prefix, string? pattern = null) => new(prefix, pattern, isFlags: false);

    /// <summary>Returns whether a macro is a member.</summary>
    /// <param name="name">The macro name.</param>
    /// <returns><see langword="true"/> if the name has the prefix and matches the pattern.</returns>
    public bool Matches(string name) => name.StartsWith(Prefix, StringComparison.Ordinal) && Pattern?.IsMatch(name) != false;
}
