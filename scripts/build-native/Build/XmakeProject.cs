using Jade.NativeBuild.Configuration;
using Jade.NativeBuild.Tools;

namespace Jade.NativeBuild.Build;

/// <summary>Runs the xmake project of <c>build/</c> for one target.</summary>
/// <param name="layout">The repository layout.</param>
/// <param name="target">The target to build.</param>
internal sealed class XmakeProject(BuildLayout layout, NativeTarget target)
{
    /// <summary>
    /// The environment variables removed from every tool's environment: compilers and CMake read
    /// them, so they would change the libraries without appearing in any input of the build.
    /// </summary>
    private static readonly string[] _ignoredVariables = ["CFLAGS", "CXXFLAGS", "CPPFLAGS", "LDFLAGS", "CMAKE_GENERATOR", "CMAKE_TOOLCHAIN_FILE"];

    /// <summary>Configures, builds and installs the libraries of the target.</summary>
    /// <param name="options">The project options (<c>--name=value</c>) of <c>build/xmake.lua</c>.</param>
    /// <param name="cancellationToken">Stops xmake.</param>
    /// <returns>The directory the libraries are installed to.</returns>
    /// <exception cref="CommandFailedException">xmake fails.</exception>
    public async Task<string> BuildAsync(IReadOnlyList<string> options, CancellationToken cancellationToken)
    {
        var objectDirectory = layout.GetObjectDirectory(target.RuntimeIdentifier);
        var outputDirectory = layout.GetOutputDirectory(target.RuntimeIdentifier);
        var environment = CreateEnvironment(objectDirectory);
        string[] project = ["--project=" + layout.XmakeProjectDirectory];

        // Packages are only built from the local definitions: xmake must not fetch its package
        // repository or prebuilt binaries.
        await RunAsync(["global", "--network=private"], environment, cancellationToken).ConfigureAwait(false);

        await RunAsync(
            [
                "config",
                .. project,
                "--builddir=" + Path.Combine(objectDirectory, "build"),
                "--plat=" + target.XmakePlatform,
                "--arch=" + target.XmakeArchitecture,
                "--mode=release",
                "--yes",
                .. options,
            ],
            environment,
            cancellationToken).ConfigureAwait(false);

        await RunAsync(["build", .. project, "--yes"], environment, cancellationToken).ConfigureAwait(false);

        // Files left by a previous build would end up in the packages.
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }

        await RunAsync(["install", .. project, "--installdir=" + outputDirectory, "--yes"], environment, cancellationToken).ConfigureAwait(false);

        return outputDirectory;
    }

    /// <summary>Runs xmake.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <param name="environment">The environment changes.</param>
    /// <param name="cancellationToken">Stops xmake.</param>
    /// <returns>A task that completes when xmake exits successfully.</returns>
    private static Task RunAsync(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string?> environment, CancellationToken cancellationToken)
    {
        return Command.RunAsync("xmake", arguments, environment, cancellationToken);
    }

    /// <summary>Creates the environment of the xmake runs.</summary>
    /// <param name="objectDirectory">The intermediate directory of the target.</param>
    /// <returns>The environment changes.</returns>
    /// <remarks>
    /// xmake's global directory (<c>~/.xmake</c>) and project configuration (<c>.xmake/</c> next to
    /// <c>xmake.lua</c>) move under <c>artifacts/native/</c>: the build neither reads a user's xmake
    /// settings nor writes into <c>build/</c>.
    /// </remarks>
    private Dictionary<string, string?> CreateEnvironment(string objectDirectory)
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["XMAKE_GLOBALDIR"] = layout.XmakeGlobalDirectory,
            ["XMAKE_CONFIGDIR"] = Path.Combine(objectDirectory, "config"),
        };

        foreach (var variable in _ignoredVariables)
        {
            environment[variable] = null;
        }

        return environment;
    }
}
