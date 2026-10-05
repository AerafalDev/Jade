using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>A generated library's configuration, read from <c>interop/&lt;project&gt;/bindings.json</c> (ADR 0026).</summary>
/// <remarks>
/// Unknown members are rejected so that a misspelt key fails instead of being ignored. Exactly one
/// of <see cref="DawnJson"/> and <see cref="Clang"/> is set; it selects the front-end.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record LibraryConfiguration
{
    /// <summary>Gets the key of the library in the dependencies of <c>build/versions.json</c>.</summary>
    public required string Dependency { get; init; }

    /// <summary>
    /// Gets the paths fetched from the dependency's repository at the pinned commit. A trailing
    /// <c>/</c> marks a directory.
    /// </summary>
    public required IReadOnlyList<string> Sources { get; init; }

    /// <summary>Gets the path of <c>dawn.json</c> in the sources, for the WebGPU front-end.</summary>
    public string? DawnJson { get; init; }

    /// <summary>Gets the headers to parse, for the C header front-end.</summary>
    public ClangConfiguration? Clang { get; init; }

    /// <summary>
    /// Gets the name the functions are imported from, as <c>LibraryImport</c> resolves it on every
    /// platform: <c>webgpu_dawn</c> loads <c>libwebgpu_dawn.so</c> on Linux and
    /// <c>webgpu_dawn.dll</c> on Windows.
    /// </summary>
    public string? Library { get; init; }

    /// <summary>
    /// Gets the declarations left out of the bindings, by C name, each with the reason, which the
    /// generator reports.
    /// </summary>
    public IReadOnlyDictionary<string, string> Exclude { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the .NET names that replace the ones the naming rules give, by C name: a declaration's
    /// name, an enum value's name, or <c>Structure.member</c> for a member (ADR 0027).
    /// </summary>
    public IReadOnlyDictionary<string, string> Names { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the casing of the words that the naming rules get wrong, by word, such as <c>Id</c> for
    /// <c>ID</c>, which is an abbreviation rather than an acronym (ADR 0027).
    /// </summary>
    public IReadOnlyDictionary<string, string> Words { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>Reads and validates a library configuration.</summary>
    /// <param name="path">The path of the <c>bindings.json</c> file.</param>
    /// <returns>The configuration.</returns>
    /// <exception cref="InvalidDataException">The file is invalid.</exception>
    public static LibraryConfiguration Load(string path)
    {
        var configuration = JsonFile.Read(path, ConfigurationJsonContext.Default.LibraryConfiguration);

        configuration.Validate(path);

        return configuration;
    }

    /// <summary>Checks the rules that the JSON schema alone does not enforce.</summary>
    /// <param name="path">The path of the file, for error messages.</param>
    /// <exception cref="InvalidDataException">A rule is broken.</exception>
    private void Validate(string path)
    {
        if ((DawnJson is null) == (Clang is null))
        {
            throw new InvalidDataException($"'{path}' must set exactly one of 'dawnJson' and 'clang'.");
        }

        var sourcePaths = Sources
            .Concat(DawnJson is null ? [] : [DawnJson])
            .Concat(Clang?.IncludeDirectories ?? []);

        if (sourcePaths.FirstOrDefault(static sourcePath => !IsRelativeSourcePath(sourcePath)) is { } invalid)
        {
            throw new InvalidDataException($"'{path}': '{invalid}' must be a relative path with '/' separators and no '..' segment.");
        }
    }

    /// <summary>Tells whether a path stays inside the dependency's sources on every operating system.</summary>
    /// <param name="path">A path from the configuration.</param>
    /// <returns><see langword="true"/> for a relative, forward-slash path without <c>..</c> segments.</returns>
    private static bool IsRelativeSourcePath(string path)
    {
        return path.Length > 0
            && !path.StartsWith('/', StringComparison.Ordinal)
            && !path.Contains('\\', StringComparison.Ordinal)
            && !path.Split('/').Contains("..", StringComparer.Ordinal);
    }
}
