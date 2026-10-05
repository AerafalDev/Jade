using System.Text.RegularExpressions;
using Jade.BindingGenerator.Configuration;

namespace Jade.BindingGenerator.Clang;

/// <summary>The macros a library configuration binds: enum values of integer typedefs, and constants (ADR 0027).</summary>
/// <param name="configuration">The front-end settings, whose patterns are matched against whole macro names.</param>
/// <exception cref="InvalidDataException">A pattern is not a valid regular expression.</exception>
internal sealed class MacroPatterns(ClangConfiguration configuration)
{
    /// <summary>Gets the patterns of the integer typedefs whose values are macros, by C name of the typedef.</summary>
    public IReadOnlyList<(string Typedef, Regex Pattern)> Enums { get; } = [.. configuration.Enums
        .Where(static entry => entry.Value.Macros is not null)
        .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
        .Select(static entry => (entry.Key, Create(entry.Value.Macros!)))];

    /// <summary>Gets the patterns of the macros bound as constants.</summary>
    public IReadOnlyList<Regex> Constants { get; } = [.. configuration.Constants.Select(Create)];

    /// <summary>Tells whether a macro is bound, so that clang has to evaluate it.</summary>
    /// <param name="name">The name of the macro.</param>
    /// <returns><see langword="true"/> when a pattern matches it.</returns>
    public bool IsSelected(string name)
    {
        return Enums.Any(entry => entry.Pattern.IsMatch(name)) || Constants.Any(pattern => pattern.IsMatch(name));
    }

    /// <summary>Compiles a pattern that must match a whole macro name.</summary>
    /// <param name="pattern">The regular expression of the configuration.</param>
    /// <returns>The anchored expression.</returns>
    private static Regex Create(string pattern)
    {
        try
        {
            return new Regex($"^(?:{pattern})$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"'{pattern}' is not a valid regular expression: {exception.Message}", exception);
        }
    }
}
