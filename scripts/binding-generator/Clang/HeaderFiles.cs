namespace Jade.BindingGenerator.Clang;

/// <summary>Tells which parsed files belong to a library, and names them as an <c>#include</c> directive does.</summary>
/// <remarks>
/// The library's files are its sources and the repository's shim headers; the generator's C runtime
/// headers and clang's builtins are not, so their declarations are never bound.
/// </remarks>
/// <param name="sourceDirectory">The directory of the library's sources.</param>
/// <param name="includeDirectories">The include directories, relative to the sources.</param>
/// <param name="repositoryRoot">The repository root, which the shim paths are relative to.</param>
/// <param name="shims">The shim headers, relative to the repository root.</param>
internal sealed class HeaderFiles(string sourceDirectory, IEnumerable<string> includeDirectories, string repositoryRoot, IReadOnlyList<string> shims)
{
    /// <summary>The include directories inside the sources, as absolute paths ending with a separator, longest first.</summary>
    private readonly List<string> _includeDirectories = [.. includeDirectories
        .Select(directory => WithSeparator(Path.GetFullPath(Path.Combine(sourceDirectory, directory))))
        .OrderByDescending(static directory => directory.Length)];

    /// <summary>The root of the sources, ending with a separator.</summary>
    private readonly string _sourceRoot = WithSeparator(Path.GetFullPath(sourceDirectory));

    /// <summary>The shim headers, by absolute path, with their repository path.</summary>
    private readonly Dictionary<string, string> _shims = shims.ToDictionary(shim => Path.GetFullPath(Path.Combine(repositoryRoot, shim)), static shim => shim, StringComparer.Ordinal);

    /// <summary>Gets the absolute paths of the shim headers, in the order of the configuration.</summary>
    public IReadOnlyList<string> ShimPaths { get; } = [.. shims.Select(shim => Path.GetFullPath(Path.Combine(repositoryRoot, shim)))];

    /// <summary>Gets the name of a library header, as an <c>#include</c> directive writes it.</summary>
    /// <param name="path">The path of a parsed file, as libclang reports it.</param>
    /// <returns>
    /// The path relative to the include directory that holds it, such as <c>SDL3/SDL_video.h</c>,
    /// the repository path of a shim, or <see langword="null"/> for a file outside the library.
    /// </returns>
    public string? GetHeader(string path)
    {
        if (path.Length == 0)
        {
            return null;
        }

        var fullPath = Path.GetFullPath(path);

        if (_shims.TryGetValue(fullPath, out var shim))
        {
            return shim;
        }

        if (!fullPath.StartsWith(_sourceRoot, StringComparison.Ordinal))
        {
            return null;
        }

        var directory = _includeDirectories.FirstOrDefault(directory => fullPath.StartsWith(directory, StringComparison.Ordinal)) ?? _sourceRoot;

        return fullPath[directory.Length..].Replace('\\', '/');
    }

    /// <summary>Ends a directory path with a separator, so that a prefix test cannot match a sibling directory.</summary>
    /// <param name="directory">The absolute directory path.</param>
    /// <returns>The path with a trailing separator.</returns>
    private static string WithSeparator(string directory)
    {
        return Path.EndsInDirectorySeparator(directory) ? directory : directory + Path.DirectorySeparatorChar;
    }
}
