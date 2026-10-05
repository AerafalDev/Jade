namespace Jade.NativeBuild.Configuration;

/// <summary>The locations the native build reads from and writes to in the repository.</summary>
internal sealed class BuildLayout
{
    /// <summary>Initializes a new instance of the <see cref="BuildLayout"/> class.</summary>
    /// <param name="root">The repository root.</param>
    private BuildLayout(string root)
    {
        Root = root;
    }

    /// <summary>Gets the absolute path of the repository root.</summary>
    public string Root { get; }

    /// <summary>Gets the path of <c>build/versions.json</c>, which pins every native dependency and toolchain.</summary>
    public string VersionsFile => Path.Combine(Root, "build", "versions.json");

    /// <summary>Gets the directory of the xmake project, <c>build/</c>.</summary>
    public string XmakeProjectDirectory => Path.Combine(Root, "build");

    /// <summary>Gets the path of the root <c>xmake.lua</c>, which every package build depends on.</summary>
    public string XmakeProjectFile => Path.Combine(XmakeProjectDirectory, "xmake.lua");

    /// <summary>Gets the path of the list of Dawn's <c>DEPS</c> entries that its build needs.</summary>
    public string DawnThirdPartyFile => Path.Combine(XmakeProjectDirectory, "dawn", "deps.json");

    /// <summary>Gets the root of everything the native build writes, under the ignored <c>artifacts/</c>.</summary>
    public string NativeArtifactsDirectory => Path.Combine(Root, "artifacts", "native");

    /// <summary>Gets the directory where the pinned sources are cached.</summary>
    public string SourceCacheDirectory => Path.Combine(NativeArtifactsDirectory, "sources");

    /// <summary>Gets the directory that replaces xmake's global directory (<c>~/.xmake</c>).</summary>
    public string XmakeGlobalDirectory => Path.Combine(NativeArtifactsDirectory, "xmake");

    /// <summary>Gets the definition directory of a dependency, such as <c>build/dawn/</c>.</summary>
    /// <param name="dependency">The key of the dependency in <c>build/versions.json</c>.</param>
    /// <returns>The directory of its xmake definition.</returns>
    public string GetDefinitionDirectory(string dependency)
    {
        return Path.Combine(XmakeProjectDirectory, dependency);
    }

    /// <summary>Gets the directory of the intermediate files of a runtime identifier.</summary>
    /// <param name="runtimeIdentifier">The runtime identifier, such as <c>linux-x64</c>.</param>
    /// <returns>The directory that holds xmake's configuration and build directory for that target.</returns>
    public string GetObjectDirectory(string runtimeIdentifier)
    {
        return Path.Combine(NativeArtifactsDirectory, "obj", runtimeIdentifier);
    }

    /// <summary>Gets the directory of the libraries built for a runtime identifier.</summary>
    /// <param name="runtimeIdentifier">The runtime identifier, such as <c>linux-x64</c>.</param>
    /// <returns>The directory that holds the libraries, laid out as <c>runtimes/{rid}/native/</c>.</returns>
    public string GetOutputDirectory(string runtimeIdentifier)
    {
        return Path.Combine(NativeArtifactsDirectory, "bin", runtimeIdentifier);
    }

    /// <summary>Locates the repository from the directory of the entry-point script.</summary>
    /// <param name="scriptDirectory">The directory of <c>build-native.cs</c>, as the SDK reports it.</param>
    /// <returns>The layout of the repository that contains the script.</returns>
    /// <exception cref="InvalidDataException">The directory is unknown or not in the Jade repository.</exception>
    public static BuildLayout FromScriptDirectory(string? scriptDirectory)
    {
        if (string.IsNullOrEmpty(scriptDirectory))
        {
            throw new InvalidDataException("The script directory is unknown: run the build with 'dotnet run scripts/build-native.cs'.");
        }

        var root = Path.GetFullPath(Path.Combine(scriptDirectory, ".."));

        return File.Exists(Path.Combine(root, "global.json")) && File.Exists(Path.Combine(root, "Jade.slnx"))
            ? new BuildLayout(root)
            : throw new InvalidDataException($"'{root}' is not the root of the Jade repository.");
    }
}
