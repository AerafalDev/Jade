namespace Jade.BindingGenerator.Configuration;

/// <summary>The locations the generator reads from and writes to in the repository.</summary>
internal sealed class RepositoryLayout
{
    /// <summary>The name of a generated library's configuration file in its interop project.</summary>
    private const string ConfigurationFileName = "bindings.json";

    /// <summary>Initializes a new instance of the <see cref="RepositoryLayout"/> class.</summary>
    /// <param name="root">The repository root.</param>
    private RepositoryLayout(string root)
    {
        Root = root;
    }

    /// <summary>Gets the absolute path of the repository root.</summary>
    public string Root { get; }

    /// <summary>Gets the path of <c>build/versions.json</c>, which pins every native dependency.</summary>
    public string VersionsFile => Path.Combine(Root, "build", "versions.json");

    /// <summary>Gets the directory where the pinned sources are cached, under the ignored <c>artifacts/</c>.</summary>
    public string SourceCacheDirectory => Path.Combine(Root, "artifacts", "binding-generator", "sources");

    /// <summary>Gets the directory of the generator's own C runtime headers, the only system include directory of a parse.</summary>
    public string RuntimeHeadersDirectory => Path.Combine(Root, "scripts", "binding-generator", "include");

    /// <summary>Gets the directory of the interop projects.</summary>
    public string InteropDirectory => Path.Combine(Root, "interop");

    /// <summary>Gets the directory of the C sources of the layout libraries, relative to the repository root with <c>/</c> separators (ADR 0036).</summary>
    public static string LayoutSourcePath => "build/layout";

    /// <summary>Gets the directory of the C sources of the layout libraries, which the native build compiles (ADR 0036).</summary>
    public string LayoutSourceDirectory => Path.Combine(Root, LayoutSourcePath);

    /// <summary>Gets the directory that receives the generated tests of an interop project, <c>Generated/</c> in its test project.</summary>
    /// <param name="project">The interop project, such as <c>Jade.Sdl</c>.</param>
    /// <returns>The directory, in <c>tests/&lt;project&gt;.Tests/</c>.</returns>
    /// <exception cref="InvalidDataException">The interop project has no test project.</exception>
    public string GetTestGeneratedDirectory(string project)
    {
        var testProject = Path.Combine(Root, "tests", $"{project}.Tests");

        return Directory.Exists(testProject)
            ? Path.Combine(testProject, "Generated")
            : throw new InvalidDataException($"{project} has no test project in '{testProject}', which receives its layout tests.");
    }

    /// <summary>Gets the path of a file or directory relative to the repository root, as the reports show it.</summary>
    /// <param name="path">The absolute path.</param>
    /// <returns>The relative path, with <c>/</c> separators on every host.</returns>
    public string GetRelativePath(string path)
    {
        return Path.GetRelativePath(Root, path).Replace('\\', '/');
    }

    /// <summary>Locates the repository from the directory of the entry-point script.</summary>
    /// <param name="scriptDirectory">The directory of <c>binding-generator.cs</c>, as the SDK reports it.</param>
    /// <returns>The layout of the repository that contains the script.</returns>
    /// <exception cref="InvalidDataException">The directory is unknown or not in the Jade repository.</exception>
    public static RepositoryLayout FromScriptDirectory(string? scriptDirectory)
    {
        if (string.IsNullOrEmpty(scriptDirectory))
        {
            throw new InvalidDataException("The script directory is unknown: run the generator with 'dotnet run scripts/binding-generator.cs'.");
        }

        var root = Path.GetFullPath(Path.Combine(scriptDirectory, ".."));

        return File.Exists(Path.Combine(root, "global.json")) && File.Exists(Path.Combine(root, "Jade.slnx"))
            ? new RepositoryLayout(root)
            : throw new InvalidDataException($"'{root}' is not the root of the Jade repository.");
    }

    /// <summary>Lists the interop projects that the generator produces, in ordinal order of their names.</summary>
    /// <returns>The projects that have a <c>bindings.json</c>.</returns>
    public IReadOnlyList<GeneratedLibrary> GetGeneratedLibraries()
    {
        return [.. Directory.EnumerateDirectories(InteropDirectory)
            .Select(static directory => new GeneratedLibrary(Path.GetFileName(directory), Path.Combine(directory, ConfigurationFileName)))
            .Where(static library => File.Exists(library.ConfigurationFile))
            .OrderBy(static library => library.Project, StringComparer.Ordinal)];
    }
}
