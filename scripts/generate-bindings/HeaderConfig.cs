using System.Text.RegularExpressions;

/// <summary>A header whose functions are bound, and which of them.</summary>
internal sealed class HeaderConfig
{
    /// <summary>Creates a header entry.</summary>
    /// <param name="path">The header, relative to the staged <c>include/</c> folder (for example <c>SDL3/SDL_video.h</c>).</param>
    /// <param name="group">The function group, which names the partial class file (for example <c>Video</c>).</param>
    /// <param name="functions">A pattern the C function name must match, or <see langword="null"/> for every function.</param>
    public HeaderConfig(string path, string group, string? functions = null)
    {
        Path = path;
        Group = group;
        Functions = functions is null ? null : new Regex(functions, RegexOptions.CultureInvariant);
    }

    /// <summary>Gets the header, relative to the staged <c>include/</c> folder.</summary>
    public string Path { get; }

    /// <summary>Gets the function group.</summary>
    public string Group { get; }

    /// <summary>Gets the pattern the C function name must match, or <see langword="null"/> for every function.</summary>
    public Regex? Functions { get; }
}
